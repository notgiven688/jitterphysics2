/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;

namespace Jitter2.Parallelization;

/// <summary>
/// Contains methods and structures used for parallelization within the Jitter Physics engine.
/// </summary>
public static class Parallel
{
    /// <summary>
    /// Represents a batch defined by a start index, an end index, and a batch index.
    /// This struct is utilized in <see cref="ForBatch"/> to facilitate multithreaded batch processing within a for-loop.
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
    /// Given the total number of elements, the number of divisions, and a specific part index,
    /// this method calculates the start and end indices for that part.
    /// </summary>
    /// <param name="numElements">The total number of elements to be divided.</param>
    /// <param name="numDivisions">The number of divisions to split the elements into.</param>
    /// <param name="part">The index of the specific part (0-based).</param>
    /// <param name="start">The calculated start index for the specified part (output parameter).</param>
    /// <param name="end">The calculated end index for the specified part (output parameter).</param>
    /// <example>
    /// For numElements = 14, numDivisions = 4, the parts are divided as follows:
    /// - Part 0: start = 0, end = 4
    /// - Part 1: start = 4, end = 8
    /// - Part 2: start = 8, end = 11
    /// - Part 3: start = 11, end = 14
    /// </example>
    public static void GetBounds(int numElements, int numDivisions, int part, out int start, out int end)
    {
        Debug.Assert(part < numDivisions);

        int div = Math.DivRem(numElements, numDivisions, out int mod);

        start = div * part + Math.Min(part, mod);
        end = start + div + (part < mod ? 1 : 0);
    }

    private static readonly ThreadPool threadPool = ThreadPool.Instance;

    /// <summary>
    /// Executes tasks in parallel by dividing the work into batches using the <see cref="ThreadPool"/>.
    /// </summary>
    /// <param name="lower">The inclusive lower bound of the range to be processed.</param>
    /// <param name="upper">The exclusive upper bound of the range to be processed.</param>
    /// <param name="numTasks">The number of batches to divide the work into.</param>
    /// <param name="action">The callback function to execute for each batch.</param>
    /// <param name="execute">Indicates whether to execute the tasks immediately after adding them to the thread pool.</param>
    /// <remarks>
    /// This method splits the range [lower, upper) into <paramref name="numTasks"/> batches and processes each batch in parallel.
    /// The <paramref name="action"/> callback is invoked for each batch, which is represented by a <see cref="Batch"/> struct.
    /// If <paramref name="execute"/> is true, the method will call <see cref="ThreadPool.Execute"/> to start executing the tasks.
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