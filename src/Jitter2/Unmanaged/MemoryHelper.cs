/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitter2.Unmanaged;

/// <summary>
/// Provides helper methods for allocating and managing unmanaged memory.
/// </summary>
/// <remarks>
/// <para>
/// Free each allocation with the matching <see cref="Free(void*)"/> or <see cref="AlignedFree"/> method.
/// </para>
/// <para>
/// Allocation and deallocation are thread-safe. Synchronizing access to the allocated memory
/// is the caller's responsibility.
/// </para>
/// </remarks>
public static unsafe class MemoryHelper
{
    /// <summary>
    /// An integer padded on both sides so updates do not contend with adjacent data on a 64-byte cache line.
    /// </summary>
    /// <remarks>
    /// The struct occupies 132 bytes, with 64 bytes of padding on each side of <see cref="Value"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 132)]
    public struct IsolatedInt
    {
        [FieldOffset(0)]
        private fixed byte pad0[64];

        [FieldOffset(64)]
        public int Value;

        [FieldOffset(68)]
        private fixed byte pad1[64];
    }

    /// <summary>
    /// An integer padded so adjacent array elements do not share a 64-byte cache line.
    /// </summary>
    /// <remarks>
    /// Use for arrays of counters. For standalone fields, use <see cref="IsolatedInt"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public struct PaddedInt
    {
        [FieldOffset(0)]
        public int Value;
    }

    /// <summary>
    /// A memory block with a size equivalent to six instances of the <see cref="Real"/> type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 6 * sizeof(Real))]
    public struct MemBlock6Real { }

    /// <summary>
    /// A memory block with a size equivalent to nine instances of the <see cref="Real"/> type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 9 * sizeof(Real))]
    public struct MemBlock9Real { }

    /// <summary>
    /// A memory block with a size equivalent to twelve instances of the <see cref="Real"/> type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 12 * sizeof(Real))]
    public struct MemBlock12Real { }

    /// <summary>
    /// A memory block with a size equivalent to sixteen instances of the <see cref="Real"/> type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 16 * sizeof(Real))]
    public struct MemBlock16Real { }

    /// <summary>
    /// Allocates a block of unmanaged memory for an array of the specified type.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the elements to allocate memory for.</typeparam>
    /// <param name="num">The number of elements to allocate memory for.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <remarks>
    /// <b>Safety:</b> The caller must free the returned pointer using <see cref="Free{T}"/>.
    /// The memory is not zero-initialized.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="num"/> is negative.</exception>
    /// <exception cref="OverflowException">Thrown when the requested byte count exceeds the native address space.</exception>
    public static T* AllocateHeap<T>(int num) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(num);
        nuint byteCount = checked((nuint)num * (nuint)sizeof(T));
        return (T*)NativeMemory.Alloc(byteCount);
    }

    /// <summary>
    /// Allocates a block of aligned unmanaged memory for an array of the specified type.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the elements to allocate memory for.</typeparam>
    /// <param name="num">The number of elements to allocate memory for.</param>
    /// <param name="alignment">The memory alignment in bytes (must be a power of 2).</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <remarks>
    /// <b>Safety:</b> The caller must free the returned pointer using <see cref="AlignedFree"/>.
    /// The memory is not zero-initialized.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="num"/> is negative or <paramref name="alignment"/> is not a positive power of two.
    /// </exception>
    /// <exception cref="OverflowException">Thrown when the requested byte count exceeds the native address space.</exception>
    public static T* AlignedAllocateHeap<T>(int num, int alignment) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(num);
        ValidateAlignment(alignment);
        nuint byteCount = checked((nuint)num * (nuint)sizeof(T));
        return (T*)NativeMemory.AlignedAlloc(byteCount, (nuint)alignment);
    }

    /// <summary>
    /// Frees a block of unmanaged memory previously allocated for an array of the specified type.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the elements in the memory block.</typeparam>
    /// <param name="ptr">A pointer to the memory block to free.</param>
    public static void Free<T>(T* ptr) where T : unmanaged
    {
        Free((void*)ptr);
    }

    /// <summary>
    /// Allocates a block of unmanaged memory of the specified length in bytes.
    /// </summary>
    /// <param name="len">The length of the memory block to allocate, in bytes.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <remarks>
    /// <b>Safety:</b> The caller must free the returned pointer using <see cref="Free(void*)"/>.
    /// The memory is not zero-initialized.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="len"/> is negative.</exception>
    public static void* AllocateHeap(int len)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(len);
        return NativeMemory.Alloc((nuint)len);
    }

    /// <summary>
    /// Allocates a block of aligned unmanaged memory of the specified length in bytes.
    /// </summary>
    /// <param name="len">The length of the memory block to allocate, in bytes.</param>
    /// <param name="alignment">The memory alignment in bytes (must be a power of 2).</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <remarks>
    /// <b>Safety:</b> The caller must free the returned pointer using <see cref="AlignedFree"/>.
    /// The memory is not zero-initialized.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="len"/> is negative or <paramref name="alignment"/> is not a positive power of two.
    /// </exception>
    public static void* AlignedAllocateHeap(int len, int alignment)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(len);
        ValidateAlignment(alignment);
        return NativeMemory.AlignedAlloc((nuint)len, (nuint)alignment);
    }

    /// <summary>
    /// Frees a block of unmanaged memory previously allocated.
    /// </summary>
    /// <param name="ptr">A pointer to the memory block to free.</param>
    public static void Free(void* ptr) => NativeMemory.Free(ptr);

    /// <summary>
    /// Frees a block of aligned unmanaged memory previously allocated.
    /// </summary>
    /// <param name="ptr">A pointer to the aligned memory block to free.</param>
    public static void AlignedFree(void* ptr) => NativeMemory.AlignedFree(ptr);

    /// <summary>
    /// Zeros out unmanaged memory.
    /// </summary>
    /// <param name="buffer">A pointer to the memory block to zero out.</param>
    /// <param name="len">The length of the memory block to zero out, in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="len"/> is negative.</exception>
    public static void MemSet(void* buffer, int len)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(len);
        Unsafe.InitBlockUnaligned(buffer, 0, (uint)len);
    }

    private static void ValidateAlignment(int alignment)
    {
        if (alignment <= 0 || (alignment & (alignment - 1)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(alignment), alignment,
                "Alignment must be a positive power of two.");
        }
    }
}
