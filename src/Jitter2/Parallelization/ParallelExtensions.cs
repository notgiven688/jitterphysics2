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
/// Provides parallel batch processing extension methods for collections used by the physics engine.
/// </summary>
/// <remarks>
/// These methods divide collections into batches and distribute them across worker threads.
/// The batch count is determined by the task threshold and available threads.
/// </remarks>
public static class ParallelExtensions
{
    /// <summary>
    /// Processes array elements in parallel batches.
    /// </summary>
    /// <param name="array">The array to process.</param>
    /// <param name="taskThreshold">Minimum elements per batch. Fewer elements result in a single batch.</param>
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
    /// <param name="taskThreshold">Minimum elements per batch. Fewer elements result in a single batch.</param>
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
    /// <param name="taskThreshold">Minimum elements per batch. Fewer elements result in a single batch.</param>
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
    /// <param name="taskThreshold">Minimum elements per batch. Fewer elements result in a single batch.</param>
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
    /// <param name="taskThreshold">Minimum elements per batch. Fewer elements result in a single batch.</param>
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