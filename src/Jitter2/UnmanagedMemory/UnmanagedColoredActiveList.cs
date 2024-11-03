/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitter2.Parallelization;

namespace Jitter2.UnmanagedMemory;

// this is a mixture of a data structure and an allocator.

/// <summary>
/// Manages memory for unmanaged structs, storing them sequentially in contiguous memory blocks. Each struct can either be active or inactive. Despite its name, this class does not fully mimic the behavior of a conventional list; the order of elements is not guaranteed to remain consistent. Indices of elements might change following calls to methods such as <see cref="UnmanagedActiveList{T}.Allocate(bool, bool)"/>, <see cref="UnmanagedActiveList{T}.Free(JHandle{T})"/>, <see cref="UnmanagedActiveList{T}.MoveToActive(JHandle{T})"/>, or <see cref="UnmanagedActiveList{T}.MoveToInactive(JHandle{T})"/>.
/// </summary>
public sealed unsafe class UnmanagedColoredActiveList<T> : IDisposable where T : unmanaged
{
    internal struct ColorList
    {
        public int ActiveCount;
        public int Count;
        public int Size;

        public ColorList(int size)
        {
            this.Size = size;
            this.ActiveCount = 0;
            this.Count = 0;
        }

        // layout:
        // 0 [ .... ] active [ .... ] count [ .... ] size
        public T* Memory = (T*)IntPtr.Zero;
    }

    private T** handles;

    private ushort[] colors;

    private readonly ColorList[] colorList;

    private bool disposed;

    private readonly int maximumSize;

    public const int ColorCount = 16;

    private int growHandle = 0;

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="maximumSize">The maximum number of elements that can be accommodated within this structure, as determined by the <see cref="Allocate"/> method. The preallocated memory is calculated as the product of maximumSize and IntPtr.Size (in bytes).</param>
    /// <param name="initialSize">The initial size of the contiguous memory block, denoted in the number of elements. The default value is 1024.</param>
    public UnmanagedColoredActiveList(int maximumSize, int initialSize = 1024)
    {
        if (maximumSize < initialSize * ColorCount)
        {
            throw new InvalidOperationException($"MaximumSize too small!");
        }

        this.maximumSize = maximumSize;
        handles = (T**)MemoryHelper.AllocateHeap(maximumSize * sizeof(IntPtr));
        colors = new ushort[maximumSize];

        colorList = new ColorList[ColorCount];

        for (int i = 0; i < ColorCount; i++)
        {
            var list = new ColorList(initialSize);
            list.Memory = (T*)MemoryHelper.AllocateHeap(initialSize * sizeof(T));

            for (int e = 0; e < list.Size; e++)
            {
                //Unsafe.AsRef<int>(&list.Memory[e]) = i * list.Size + e;
                Unsafe.AsRef<int>(&list.Memory[e]) = growHandle++;
                //Unsafe.AsRef<int>(&list.Memory[e]) = e;
            }

            colorList[i] = list;
        }
    }

    /// <summary>
    /// Removes the associated native structure from the data structure.
    /// </summary>
    public void Free(JHandle<T> handle)
    {
        Debug.Assert(!disposed);

        MoveToInactive(handle);

        int index = Unsafe.Read<int>(*handle.Pointer);

        ref ColorList list = ref colorList[colors[index]];
        list.Count--;

        (**handle.Pointer, list.Memory[list.Count]) = (list.Memory[list.Count], **handle.Pointer);

        handles[Unsafe.Read<int>(*handle.Pointer)] = *handle.Pointer;
        handles[Unsafe.Read<int>(&list.Memory[list.Count])] = &list.Memory[list.Count];

        handle.Pointer = (T**)0;
    }

    public ushort GetColor(JHandle<T> handle)
    {
        int index = Unsafe.Read<int>(*handle.Pointer);
        return colors[index];
    }

    /// <summary>
    /// A span for all elements marked as active.
    /// </summary>
    public Span<T> Active(int color) => new(colorList[color].Memory, colorList[color].ActiveCount);

    /// <summary>
    /// A span for all elements marked as inactive.
    /// </summary>
    public Span<T> Inactive(int color)
    {
        ref var list = ref colorList[color];
        return new Span<T>(&list.Memory[list.ActiveCount], list.Count - list.ActiveCount);
    }

    /// <summary>
    /// A span for all elements.
    /// </summary>
    public Span<T> Elements(int color) => new(colorList[color].Memory, colorList[color].Count);


    /// <summary>
    /// Checks if the element is stored as an active element. The object has to be in this instance
    /// of <see cref="Jitter2.UnmanagedMemory.UnmanagedActiveList{T}"/>. This operation is O(1).
    /// </summary>
    public bool IsActive(JHandle<T> handle)
    {
        int index = Unsafe.Read<int>(*handle.Pointer);
        ref var list = ref colorList[colors[index]];
        return (nint)(*handle.Pointer) - (nint)list.Memory < list.ActiveCount * sizeof(T);
    }

    /// <summary>
    /// Moves an object from inactive to active.
    /// </summary>
    public void MoveToActive(JHandle<T> handle)
    {
        int index = Unsafe.Read<int>(*handle.Pointer);
        ref var list = ref colorList[colors[index]];

        var memory = list.Memory;
        int activeCount = list.ActiveCount;

        if ((nint)(*handle.Pointer) - (nint)memory < activeCount * sizeof(T)) return;
        (**handle.Pointer, memory[activeCount]) = (memory[activeCount], **handle.Pointer);
        handles[Unsafe.Read<int>(*handle.Pointer)] = *handle.Pointer;
        handles[Unsafe.Read<int>(&memory[activeCount])] = &memory[activeCount];
        list.ActiveCount += 1;
    }

    /// <summary>
    /// Moves an object from active to inactive.
    /// </summary>
    public void MoveToInactive(JHandle<T> handle)
    {
        int index = Unsafe.Read<int>(*handle.Pointer);

        ref var list = ref colorList[colors[index]];

        var memory = list.Memory;

        if ((nint)(*handle.Pointer) - (nint)memory >= list.ActiveCount * sizeof(T)) return;

        list.ActiveCount -= 1;
        int activeCount = list.ActiveCount;

        (**handle.Pointer, memory[activeCount]) = (memory[activeCount], **handle.Pointer);
        handles[Unsafe.Read<int>(*handle.Pointer)] = *handle.Pointer;
        handles[Unsafe.Read<int>(&memory[activeCount])] = &memory[activeCount];
    }

    /// <summary>
    /// Reader-writer lock. Locked by a writer when a resize (triggered by <see
    /// cref="Jitter2.UnmanagedMemory.UnmanagedActiveList{T}.Allocate(bool, bool)"/>) occurs. Resizing does move all structs and
    /// their memory addresses. It is not safe to use handles (<see cref="JHandle{T}"/>) during this
    /// operation. Use a reader lock to access native data if concurrent calls to <see cref="Allocate"/>
    /// are made.
    /// </summary>
    public ReaderWriterLock ResizeLock;

    public JHandle<T> GetHandle(ref T t)
    {
        int index = Unsafe.Read<int>(Unsafe.AsPointer(ref t));
        return new JHandle<T>(&handles[index]);
    }

    /// <summary>
    /// Allocates an unmanaged object.
    /// </summary>
    /// <param name="active">The state of the object.</param>
    /// <param name="clear">Write zeros into the object's memory.</param>
    /// <returns>A native handle.</returns>
    /// <exception cref="MaximumSizeException">Raised when the maximum size limit
    /// of the datastructure is exceeded.</exception>
    public JHandle<T> Allocate(int color, bool active = false, bool clear = false)
    {
        Debug.Assert(!disposed);

        ref var list = ref colorList[color];

        JHandle<T> handle;

        if (list.Count == list.Size)
        {
            ResizeLock.EnterWriteLock();

            int osize = list.Size;

            if (osize == maximumSize)
            {
                throw new MaximumSizeException($"{nameof(UnmanagedActiveList<T>)} reached " +
                                               $"its maximum size limit ({nameof(maximumSize)}={maximumSize}).");
            }

            list.Size = Math.Min(2 * osize, maximumSize);

            Trace.WriteLine($"{nameof(UnmanagedActiveList<T>)}: " +
                            $"Resizing to {list.Size}x{typeof(T)} ({list.Size}x{sizeof(T)} Bytes).");

            var oldmemory = list.Memory;
            list.Memory = (T*)MemoryHelper.AllocateHeap(list.Size * sizeof(T));

            for (int i = 0; i < osize; i++)
            {
                list.Memory[i] = oldmemory[i];
                handles[Unsafe.Read<int>(&list.Memory[i])] = &list.Memory[i];
            }

            for (int i = osize; i < list.Size; i++)
            {
                //Unsafe.AsRef<int>(&list.Memory[i]) = i;
                Unsafe.AsRef<int>(&list.Memory[i]) = growHandle++;
            }

            MemoryHelper.Free(oldmemory);
            ResizeLock.ExitWriteLock();
        }

        int hdl = Unsafe.Read<int>(&list.Memory[list.Count]);
        handles[hdl] = &list.Memory[list.Count];

        handle = new JHandle<T>(&handles[hdl]);

        int index = Unsafe.Read<int>(*handle.Pointer);
        colors[index] = (ushort)color;

        if (clear)
        {
            MemoryHelper.Memset((byte*)handles[hdl] + sizeof(IntPtr),
                sizeof(T) - sizeof(IntPtr));
        }

        list.Count += 1;

        if (active) MoveToActive(handle);

        return handle;
    }

    private void FreeResources()
    {
        if (!disposed)
        {
            MemoryHelper.Free(handles);
            handles = (T**)0;

            for (int i = 0; i < 8; i++)
            {
                ref var list = ref colorList[i];
                MemoryHelper.Free(list.Memory);
                list.Memory = (T*)0;
            }

            disposed = true;
        }
    }

    ~UnmanagedColoredActiveList()
    {
        FreeResources();
    }

    /// <summary>
    /// Call to explicitly free all unmanaged memory. Invalidates any further use of this instance
    /// of <see cref="UnmanagedActiveList{T}"/>.
    /// </summary>
    public void Dispose()
    {
        FreeResources();
        GC.SuppressFinalize(this);
    }
}