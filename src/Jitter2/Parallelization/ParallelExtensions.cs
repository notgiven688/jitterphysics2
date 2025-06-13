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
/// Provides a ParallelForBatch extension for <see cref="PartitionedBuffer{T}"/> and <see
/// cref="PartitionedBuffer{T}"/>.
/// </summary>
public static class ParallelExtensions
{
    /// <summary>
    /// Loop in batches over the elements of an array.
    /// </summary>
    /// <param name="taskThreshold">If the number of elements is less than this value, only
    /// one batch is generated.</param>
    /// <param name="execute">True if <see cref="ThreadPool.Execute"/> should be called.</param>
    /// <returns>The number of batches(/tasks) generated.</returns>
    public static int ParallelForBatch(this Array array, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true)
    {
        int numTasks = array.Length / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, array.Length, numTasks, action, execute);

        return numTasks;
    }

    /// <summary>
    /// Loop in batches over the active elements of the <see cref="PartitionedBuffer{T}"/>.
    /// </summary>
    /// <param name="taskThreshold">If the number of elements is less than this value, only
    /// one batch is generated.</param>
    /// <param name="execute">True if <see cref="ThreadPool.Execute"/> should be called.</param>
    /// <returns>The number of batches(/tasks) generated.</returns>
    public static int ParallelForBatch<T>(this PartitionedBuffer<T> list, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : unmanaged
    {
        int numTasks = list.Active.Length / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, list.Active.Length, numTasks, action, execute);

        return numTasks;
    }

    /// <summary>
    /// Loop in batches over the active elements of the <see cref="ReadOnlyPartitionedSet{T}"/>.
    /// </summary>
    /// <param name="taskThreshold">If the number of elements is less than this value, only
    /// one batch is generated.</param>
    /// <param name="execute">True if <see cref="ThreadPool.Execute"/> should be called.</param>
    /// <returns>The number of batches(/tasks) generated.</returns>
    public static int ParallelForBatch<T>(this ReadOnlyPartitionedSet<T> list, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : class, IPartitionedSetIndex
    {
        int numTasks = list.ActiveCount / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, list.ActiveCount, numTasks, action, execute);

        return numTasks;
    }

    /// <summary>
    /// Loop in batches over the active elements of the <see cref="PartitionedSet{T}"/>.
    /// </summary>
    /// <param name="taskThreshold">If the number of elements is less than this value, only
    /// one batch is generated.</param>
    /// <param name="execute">True if <see cref="ThreadPool.Execute"/> should be called.</param>
    /// <returns>The number of batches(/tasks) generated.</returns>
    internal static int ParallelForBatch<T>(this PartitionedSet<T> partitionedSet, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : class, IPartitionedSetIndex
    {
        int numTasks = partitionedSet.ActiveCount / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, partitionedSet.ActiveCount, numTasks, action, execute);

        return numTasks;
    }

    internal static int ParallelForBatch<T>(this SlimBag<T> list, int taskThreshold,
        Action<Parallel.Batch> action, bool execute = true) where T : class, IPartitionedSetIndex
    {
        int numTasks = list.Count / taskThreshold + 1;
        numTasks = Math.Min(numTasks, ThreadPool.Instance.ThreadCount);

        Parallel.ForBatch(0, list.Count, numTasks, action, execute);

        return numTasks;
    }
}