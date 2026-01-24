/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Threading;

namespace Jitter2.Parallelization;

/// <summary>
/// Provides a lightweight reader-writer lock optimized for rare write operations.
/// </summary>
/// <remarks>
/// <para>
/// Multiple readers can hold the lock concurrently, but writers have exclusive access.
/// This implementation uses spin-waiting and is best suited for short critical sections.
/// </para>
/// <para>
/// Thread-safe. All methods use atomic operations and memory barriers.
/// </para>
/// </remarks>
public struct ReaderWriterLock
{
    private volatile int writer;
    private volatile int reader;

    /// <summary>
    /// Acquires the read lock. Blocks while a writer holds the lock.
    /// </summary>
    /// <remarks>
    /// Multiple threads can hold the read lock simultaneously.
    /// Call <see cref="ExitReadLock"/> to release.
    /// </remarks>
    public void EnterReadLock()
    {
        while (true)
        {
            while (writer == 1) Thread.SpinWait(1);

            Interlocked.Increment(ref reader);
            if (writer == 0) break;
            Interlocked.Decrement(ref reader);
        }
    }

    /// <summary>
    /// Acquires the write lock with exclusive access. Blocks until all readers and writers release.
    /// </summary>
    /// <remarks>
    /// Only one thread can hold the write lock at a time.
    /// Call <see cref="ExitWriteLock"/> to release.
    /// </remarks>
    public void EnterWriteLock()
    {
        SpinWait sw = new();

        while (true)
        {
            if (Interlocked.CompareExchange(ref writer, 1, 0) == 0)
            {
                while (reader != 0) Thread.SpinWait(1);
                break;
            }

            sw.SpinOnce();
        }
    }

    /// <summary>
    /// Exits the read section.
    /// </summary>
    public void ExitReadLock()
    {
        Interlocked.Decrement(ref reader);
    }

    /// <summary>
    /// Exits the write section.
    /// </summary>
    public void ExitWriteLock()
    {
        writer = 0;
    }
}