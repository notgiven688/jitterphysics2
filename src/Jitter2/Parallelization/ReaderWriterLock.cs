/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Threading;

namespace Jitter2.Parallelization;

/// <summary>
/// An efficient reader-writer lock implementation optimized
/// for rare write events.
/// </summary>
public struct ReaderWriterLock
{
    private volatile int writer;
    private volatile int reader;

    /// <summary>
    /// Enters the critical read section.
    /// </summary>
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
    /// Enters the critical write section.
    /// </summary>
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