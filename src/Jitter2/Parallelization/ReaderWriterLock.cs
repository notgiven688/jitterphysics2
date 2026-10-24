/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Jitter2.Unmanaged;

namespace Jitter2.Parallelization;

/// <summary>
/// Allows concurrent readers or one exclusive writer.
/// </summary>
/// <remarks>
/// <para>
/// Waiting threads spin, so use this lock only for short operations with infrequent writes.
/// </para>
/// <para>
/// Release each acquired lock exactly once, on the thread that acquired it. Do not acquire
/// the lock again while holding it or switch directly between a read lock and a write lock.
/// </para>
/// </remarks>
public sealed class ReaderWriterLock
{
    private const int YieldThreshold = 32;
    private const int SleepThreshold = 128;
    private const int ReaderSlotCount = 128;

    // Give each thread a stable reader slot. This allows readers to usually update
    // separate counters instead of contending on one shared counter and causing
    // cache-line bouncing.
    [ThreadStatic]
    private static int readerSlotPlusOne;

    // Slot assignment is shared across lock instances so that a thread normally
    // reuses the same slot for every ReaderWriterLock instance.
    private static int nextReaderSlot;

    private int writer;

    // Reader counters are padded so counters in adjacent slots do not normally
    // occupy the same cache line. Together with per-thread slot assignment, this
    // reduces cache-line bouncing between concurrent readers.
    private readonly MemoryHelper.PaddedInt[] readers =
        new MemoryHelper.PaddedInt[ReaderSlotCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Backoff(ref int count)
    {
        int current = count;
        if (count < int.MaxValue) count++;

        if (current < YieldThreshold)
        {
            Thread.SpinWait(1);
        }
        else if (current < SleepThreshold)
        {
            Thread.Yield();
        }
        else
        {
            Thread.Sleep(0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetReaderSlot()
    {
        int slot = readerSlotPlusOne - 1;
        if (slot >= 0) return slot;

        // If the number of participating threads exceeds ReaderSlotCount, slots
        // are shared. This increases contention but does not affect correctness.
        slot = (int)((uint)(Interlocked.Increment(ref nextReaderSlot) - 1) %
                     ReaderSlotCount);

        readerSlotPlusOne = slot + 1;
        return slot;
    }

    /// <summary>
    /// Acquires the read lock.
    /// </summary>
    /// <remarks>
    /// Multiple threads can hold the read lock simultaneously. This method blocks
    /// while a writer holds the lock.
    /// Release it with <see cref="ExitReadLock"/> on the same thread.
    /// </remarks>
    public void EnterReadLock()
    {
        int slot = GetReaderSlot();
        int backoff = 0;

        while (true)
        {
            while (Volatile.Read(ref writer) == 1)
            {
                Backoff(ref backoff);
            }

            Interlocked.Increment(ref readers[slot].Value);

            if (Volatile.Read(ref writer) == 0)
            {
                break;
            }

            Interlocked.Decrement(ref readers[slot].Value);
            Backoff(ref backoff);
        }
    }

    /// <summary>
    /// Acquires the write lock with exclusive access.
    /// </summary>
    /// <remarks>
    /// This method blocks until all readers and other writers have released the lock.
    /// Release it with <see cref="ExitWriteLock"/> on the same thread.
    /// </remarks>
    public void EnterWriteLock()
    {
        int backoff = 0;

        while (true)
        {
            if (Interlocked.CompareExchange(ref writer, 1, 0) == 0)
            {
                while (HasReaders())
                {
                    Backoff(ref backoff);
                }

                break;
            }

            Backoff(ref backoff);
        }
    }

    private bool HasReaders()
    {
        for (int i = 0; i < readers.Length; i++)
        {
            if (Volatile.Read(ref readers[i].Value) != 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Releases a read lock previously acquired by the current thread.
    /// </summary>
    /// <remarks>
    /// Must be called exactly once for each successful call to
    /// <see cref="EnterReadLock"/> and on the same thread that acquired the lock.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ExitReadLock()
    {
        Interlocked.Decrement(ref readers[GetReaderSlot()].Value);
    }

    /// <summary>
    /// Releases the write lock held by the current thread.
    /// </summary>
    /// <remarks>
    /// Must be called exactly once after a successful call to
    /// <see cref="EnterWriteLock"/> and on the same thread that acquired the lock.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ExitWriteLock()
    {
        Volatile.Write(ref writer, 0);
    }
}
