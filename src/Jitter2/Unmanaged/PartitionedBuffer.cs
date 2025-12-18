/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitter2.Parallelization;

namespace Jitter2.Unmanaged;

/// <summary>
/// Handle for an unmanaged object.
/// </summary>
public readonly unsafe struct JHandle<T> : IEquatable<JHandle<T>> where T : unmanaged
{
    public static readonly JHandle<T> Zero = new(null);

    internal readonly T** Pointer;

    public ref T Data => ref Unsafe.AsRef<T>(*Pointer);

    internal JHandle(T** ptr)
    {
        Pointer = ptr;
    }

    public readonly bool IsZero => Pointer == null;

    public static JHandle<TConvert> AsHandle<TConvert>(JHandle<T> handle) where TConvert : unmanaged
    {
        return new JHandle<TConvert>((TConvert**)handle.Pointer);
    }

    public readonly bool Equals(JHandle<T> other)
    {
        return Pointer == other.Pointer;
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is JHandle<T> other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return ((nint)Pointer).GetHashCode();
    }

    public static bool operator ==(JHandle<T> left, JHandle<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JHandle<T> left, JHandle<T> right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Manages memory for unmanaged structs, storing them sequentially in contiguous memory blocks.
/// Each struct can either be active or inactive.
/// </summary>
/// <remarks>
/// <para>
/// <b>Memory Layout Requirement:</b> The type <typeparamref name="T"/> must reserve its first
/// 4 bytes (sizeof(int)) for internal bookkeeping. This memory region is used to store
/// the stable ID that maps the data back to its <see cref="JHandle{T}"/>.
/// </para>
/// <para>
/// Do not modify these bytes manually. A compatible struct should look like this:
/// <code>
/// [StructLayout(LayoutKind.Sequential)]
/// public struct RigidBodyData
/// {
///     private readonly int _internalIndex; // Reserved by PartitionedBuffer
///
///     public JVector Position;
///     public JQuaternion Orientation;
///     // ... other fields
/// }
/// </code>
/// </para>
/// </remarks>
public sealed unsafe class PartitionedBuffer<T> : IDisposable where T : unmanaged
{
    public class MaximumSizeException : Exception
    {
        public MaximumSizeException() { }
        public MaximumSizeException(string message) : base(message) { }
        public MaximumSizeException(string message, Exception inner) : base(message, inner) { }
    }

    // Paging constants for the indirection table
    private const int PageSize = 4096;
    private const int PageMask = PageSize - 1;
    private const int PageShift = 12; // 2^12 = 4096
    private const int MaxPages = 1024 * 16; // Capacity for ~67 million handles

    private T* memory;
    private T*** pages; // Array of pointers to pages (indirection table)

    private int activeCount;
    private int size;
    private int pageCount = 0;
    private bool disposed;

    /// <summary>
    /// Reader-writer lock. Locked by a writer when a resize occurs.
    /// Resizing moves the contiguous data memory addresses. Use a reader lock
    /// to access data if concurrent calls to Allocate are made.
    /// </summary>
    public ReaderWriterLock ResizeLock = new();

    public int Count { get; private set; }

    /// <summary>
    /// Indicates whether the allocated memory is aligned to a 64-byte boundary.
    /// </summary>
    public bool Aligned64 { get; }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="initialSize">The initial size of the contiguous memory block.</param>
    /// <param name="aligned64">Indicates whether the memory should be aligned to 64 bytes.</param>
    public PartitionedBuffer(int initialSize = 1024, bool aligned64 = false)
    {
        if (sizeof(T) < sizeof(int))
        {
            throw new ArgumentException($"Type {typeof(T).Name} is too small. It must be at least {sizeof(int)} bytes to store the internal ID.");
        }

        size = initialSize;

        // Allocate the master list of page pointers
        pages = (T***)MemoryHelper.AllocateHeap(MaxPages * sizeof(IntPtr));

        if (aligned64)
        {
            try { memory = (T*)MemoryHelper.AlignedAllocateHeap(size * sizeof(T), 64); }
            catch
            {
                Logger.Warning("Could not allocate aligned memory. Falling back to unaligned memory.");
                aligned64 = false;
            }
        }

        if (!aligned64)
        {
            memory = (T*)MemoryHelper.AllocateHeap(size * sizeof(T));
        }

        this.Aligned64 = aligned64;

        EnsureHandleCapacity(size);

        // Initialize ID slots in the memory
        for (int i = 0; i < size; i++)
        {
            Unsafe.AsRef<int>(&memory[i]) = i;
        }
    }

    /// <summary>
    /// Returns the total amount of unmanaged memory allocated in bytes (data + indirection pages).
    /// </summary>
    public long TotalBytesAllocated => (long)size * sizeof(T) + (long)pageCount * PageSize * sizeof(IntPtr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T** GetHandleSlot(int id)
    {
        return &pages[id >> PageShift][id & PageMask];
    }

    private void EnsureHandleCapacity(int requiredCount)
    {
        while (pageCount * PageSize < requiredCount)
        {
            if (pageCount >= MaxPages)
                throw new MaximumSizeException("Internal indirection table limit reached.");

            pages[pageCount] = (T**)MemoryHelper.AllocateHeap(PageSize * sizeof(IntPtr));
            pageCount++;
        }
    }

    /// <summary>
    /// Removes the associated native structure from the data structure.
    /// </summary>
    public void Free(JHandle<T> handle)
    {
        Debug.Assert(!disposed);

        MoveToInactive(handle);

        Count -= 1;
        // Swap with the last element
        (**handle.Pointer, memory[Count]) = (memory[Count], **handle.Pointer);

        // Update the handle slots for the two swapped elements
        *GetHandleSlot(Unsafe.Read<int>(*handle.Pointer)) = *handle.Pointer;
        *GetHandleSlot(Unsafe.Read<int>(&memory[Count])) = &memory[Count];
    }

    /// <summary>
    /// A span for all elements marked as active.
    /// </summary>
    public Span<T> Active => new(memory, activeCount);

    /// <summary>
    /// A span for all elements marked as inactive.
    /// </summary>
    public Span<T> Inactive => new(&memory[activeCount], Count - activeCount);

    /// <summary>
    /// A span for all elements.
    /// </summary>
    public Span<T> Elements => new(memory, Count);

    /// <summary>
    /// Returns the handle of the object. O(1) operation.
    /// </summary>
    public JHandle<T> GetHandle(ref T t)
    {
        return new JHandle<T>(GetHandleSlot(Unsafe.Read<int>(Unsafe.AsPointer(ref t))));
    }

    /// <summary>
    /// Checks if the element is stored as an active element. O(1).
    /// </summary>
    public bool IsActive(JHandle<T> handle)
    {
        Debug.Assert(*handle.Pointer - memory < Count);
        return (nint)(*handle.Pointer) - (nint)memory < activeCount * sizeof(T);
    }

    /// <summary>
    /// Moves an object from inactive to active.
    /// </summary>
    public void MoveToActive(JHandle<T> handle)
    {
        Debug.Assert(*handle.Pointer - memory < Count);

        if ((nint)(*handle.Pointer) - (nint)memory < activeCount * sizeof(T)) return;

        (**handle.Pointer, memory[activeCount]) = (memory[activeCount], **handle.Pointer);

        *GetHandleSlot(Unsafe.Read<int>(*handle.Pointer)) = *handle.Pointer;
        *GetHandleSlot(Unsafe.Read<int>(&memory[activeCount])) = &memory[activeCount];

        activeCount += 1;
    }

    /// <summary>
    /// Moves an object from active to inactive.
    /// </summary>
    public void MoveToInactive(JHandle<T> handle)
    {
        if ((nint)(*handle.Pointer) - (nint)memory >= activeCount * sizeof(T)) return;

        activeCount -= 1;
        (**handle.Pointer, memory[activeCount]) = (memory[activeCount], **handle.Pointer);

        *GetHandleSlot(Unsafe.Read<int>(*handle.Pointer)) = *handle.Pointer;
        *GetHandleSlot(Unsafe.Read<int>(&memory[activeCount])) = &memory[activeCount];
    }

    /// <summary>
    /// Swap two entries based on their index. Adjusts handles accordingly.
    /// </summary>
    public void Swap(int i, int j)
    {
        (memory[i], memory[j]) = (memory[j], memory[i]);

        *GetHandleSlot(Unsafe.Read<int>(&memory[i])) = &memory[i];
        *GetHandleSlot(Unsafe.Read<int>(&memory[j])) = &memory[j];
    }

    /// <summary>
    /// Retrieves the target index of the handle.
    /// </summary>
    public int GetIndex(JHandle<T> handle)
    {
        return (int)(((nint)(*handle.Pointer) - (nint)memory) / sizeof(T));
    }

    /// <summary>
    /// Allocates an unmanaged object. Growth is dynamic.
    /// </summary>
    public JHandle<T> Allocate(bool active = false, bool clear = false)
    {
        Debug.Assert(!disposed);

        if (Count == size)
        {
            ResizeLock.EnterWriteLock();

            int oldSize = size;
            size *= 2; // Dynamic doubling

            T* oldMemory = memory;

            if (Aligned64) memory = (T*)MemoryHelper.AlignedAllocateHeap(size * sizeof(T), 64);
            else memory = (T*)MemoryHelper.AllocateHeap(size * sizeof(T));

            // Ensure handles are ready for the new memory range
            EnsureHandleCapacity(size);

            for (int i = 0; i < oldSize; i++)
            {
                memory[i] = oldMemory[i];
                // Update stable pointers to the new memory location
                *GetHandleSlot(Unsafe.Read<int>(&memory[i])) = &memory[i];
            }

            for (int i = oldSize; i < size; i++)
            {
                Unsafe.AsRef<int>(&memory[i]) = i;
            }

            if (Aligned64) MemoryHelper.AlignedFree(oldMemory);
            else MemoryHelper.Free(oldMemory);

            ResizeLock.ExitWriteLock();
        }

        int hdlId = Unsafe.Read<int>(&memory[Count]);
        T** slot = GetHandleSlot(hdlId);
        *slot = &memory[Count];

        var handle = new JHandle<T>(slot);

        if (clear)
        {
            // Skip the first 4 bytes (the ID) when clearing
            MemoryHelper.MemSet((byte*)*slot + sizeof(int), sizeof(T) - sizeof(int));
        }

        Count += 1;
        if (active) MoveToActive(handle);

        return handle;
    }

    private void FreeResources()
    {
        if (!disposed)
        {
            for (int i = 0; i < pageCount; i++)
            {
                MemoryHelper.Free(pages[i]);
            }
            MemoryHelper.Free(pages);
            pages = (T***)0;

            if (Aligned64) MemoryHelper.AlignedFree(memory);
            else MemoryHelper.Free(memory);
            memory = (T*)0;

            disposed = true;
        }
    }

    ~PartitionedBuffer() => FreeResources();

    public void Dispose()
    {
        FreeResources();
        GC.SuppressFinalize(this);
    }
}