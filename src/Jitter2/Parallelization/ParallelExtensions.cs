/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.DataStructures;
using Jitter2.Unmanaged;

namespace Jitter2.Parallelization;

/// <summary>
/// Divides collections into index ranges for parallel processing.
/// </summary>
/// <remarks>
/// Callbacks may run concurrently. With <c>execute: true</c>, each method waits for completion;
/// otherwise, call <see cref="ThreadPool.Execute"/> on the shared pool to run the queued batches.
/// Do not queue batches concurrently or change a collection while its batches are being processed.
/// </remarks>
public static class ParallelExtensions
{
    /// <summary>
    /// Processes array elements in parallel batches.
    /// </summary>
    /// <param name="array">The array to process.</param>
    /// <param name="taskThreshold">Positive target batch size used to choose the number of batches;
    /// actual batches can be smaller or larger.</param>
    /// <param name="action">The callback to invoke for each batch.</param>
    /// <param name="execute">If <see langword="true"/>, calls <see cref="ThreadPool.Execute"/> to start execution.</param>
    /// <returns>The number of batches generated.</returns>
    public static int ParallelForBatch(this Array array, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true)
    {
        int numTasks = array.Length / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, array.Length, numTasks, action, execute);

        return numTasks;
    }

    /// <summary>
    /// Processes active elements of a <see cref="PartitionedBuffer{T}"/> in parallel batches.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <param name="list">The buffer to process.</param>
    /// <param name="taskThreshold">Positive target batch size used to choose the number of batches;
    /// actual batches can be smaller or larger.</param>
    /// <param name="action">The callback to invoke for each batch.</param>
    /// <param name="execute">If <see langword="true"/>, calls <see cref="ThreadPool.Execute"/> to start execution.</param>
    /// <returns>The number of batches generated.</returns>
    public static int ParallelForBatch<T>(this PartitionedBuffer<T> list, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : unmanaged
    {
        int numTasks = list.Active.Length / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, list.Active.Length, numTasks, action, execute);

        return numTasks;
    }

    /// <summary>
    /// Processes active elements of a <see cref="ReadOnlyPartitionedSet{T}"/> in parallel batches.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The set to process.</param>
    /// <param name="taskThreshold">Positive target batch size used to choose the number of batches;
    /// actual batches can be smaller or larger.</param>
    /// <param name="action">The callback to invoke for each batch.</param>
    /// <param name="execute">If <see langword="true"/>, calls <see cref="ThreadPool.Execute"/> to start execution.</param>
    /// <returns>The number of batches generated.</returns>
    public static int ParallelForBatch<T>(this ReadOnlyPartitionedSet<T> list, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : class, IPartitionedSetIndex
    {
        int numTasks = list.ActiveCount / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, list.ActiveCount, numTasks, action, execute);

        return numTasks;
    }

    /// <summary>
    /// Processes active elements of a <see cref="PartitionedSet{T}"/> in parallel batches.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="partitionedSet">The set to process.</param>
    /// <param name="taskThreshold">Positive target batch size used to choose the number of batches;
    /// actual batches can be smaller or larger.</param>
    /// <param name="action">The callback to invoke for each batch.</param>
    /// <param name="execute">If <see langword="true"/>, calls <see cref="ThreadPool.Execute"/> to start execution.</param>
    /// <returns>The number of batches generated.</returns>
    internal static int ParallelForBatch<T>(this PartitionedSet<T> partitionedSet, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : class, IPartitionedSetIndex
    {
        int numTasks = partitionedSet.ActiveCount / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, partitionedSet.ActiveCount, numTasks, action, execute);

        return numTasks;
    }

    /// <summary>
    /// Processes elements of a <see cref="SlimBag{T}"/> in parallel batches.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The bag to process.</param>
    /// <param name="taskThreshold">Positive target batch size used to choose the number of batches;
    /// actual batches can be smaller or larger.</param>
    /// <param name="action">The callback to invoke for each batch.</param>
    /// <param name="execute">If <see langword="true"/>, calls <see cref="ThreadPool.Execute"/> to start execution.</param>
    /// <returns>The number of batches generated.</returns>
    internal static int ParallelForBatch<T>(this SlimBag<T> list, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : class, IPartitionedSetIndex
    {
        int numTasks = list.Count / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, list.Count, numTasks, action, execute);

        return numTasks;
    }
}
