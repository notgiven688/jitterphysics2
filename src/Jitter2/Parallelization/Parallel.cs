/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;

namespace Jitter2.Parallelization;

/// <summary>
/// Divides index ranges into batches for processing with <see cref="ThreadPool"/>.
/// </summary>
public static class Parallel
{
    /// <summary>
    /// An index range that includes <see cref="Start"/> and excludes <see cref="End"/>.
    /// </summary>
    public readonly struct Batch(int start, int end)
    {
        /// <summary>Returns a string representation of the batch.</summary>
        public override string ToString()
        {
            return $"Batch(Start: {Start}, End: {End})";
        }

        /// <summary>The inclusive start index of the batch.</summary>
        public readonly int Start = start;

        /// <summary>The exclusive end index of the batch.</summary>
        public readonly int End = end;
    }

    /// <summary>
    /// Computes the start and end indices for a specific part of an evenly divided range.
    /// </summary>
    /// <param name="numElements">The total number of elements to divide.</param>
    /// <param name="numDivisions">The number of divisions (parts).</param>
    /// <param name="part">The zero-based index of the part.</param>
    /// <param name="start">The inclusive start index for the specified part.</param>
    /// <param name="end">The exclusive end index for the specified part.</param>
    /// <remarks>
    /// Distributes remainder elements across the first parts. For example, with 14 elements
    /// and 4 divisions: part 0 gets [0,4), part 1 gets [4,8), part 2 gets [8,11), part 3 gets [11,14).
    /// </remarks>
    public static void GetBounds(int numElements, int numDivisions, int part, out int start, out int end)
    {
        Debug.Assert(part < numDivisions);

        int div = Math.DivRem(numElements, numDivisions, out int mod);

        start = div * part + Math.Min(part, mod);
        end = start + div + (part < mod ? 1 : 0);
    }

    private static readonly ThreadPool threadPool = ThreadPool.Instance;

    /// <summary>
    /// Divides an index range into batches and queues a callback for each batch.
    /// </summary>
    /// <param name="lower">The inclusive lower bound of the range to be processed.</param>
    /// <param name="upper">The exclusive upper bound of the range to be processed.</param>
    /// <param name="numTasks">The number of batches to create.</param>
    /// <param name="action">The callback to execute for each batch.</param>
    /// <param name="execute">Whether to execute the queued batches immediately.</param>
    /// <remarks>
    /// Callbacks may run concurrently. When <paramref name="execute"/> is <see langword="true"/>,
    /// this method waits for all queued tasks to finish. Otherwise, call <see cref="ThreadPool.Execute"/>
    /// on the shared pool to run them later. Queueing batches is not thread-safe.
    /// </remarks>
    public static void ForBatch(int lower, int upper, int numTasks, Action<Batch> action, bool execute = true)
    {
        Debug.Assert(numTasks <= ushort.MaxValue);
        for (int i = 0; i < numTasks; i++)
        {
            GetBounds(upper - lower, numTasks, i, out int start, out int end);
            threadPool.AddTask(action, new Batch(start + lower, end + lower));
        }

        if (execute) threadPool.Execute();
    }
}
