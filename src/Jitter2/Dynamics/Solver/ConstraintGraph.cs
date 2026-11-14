/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Numerics;
using Jitter2.Parallelization;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2.Dynamics.Solver;

/// <summary>
/// What contacts and joints have in common for the solver: they are the edges of a graph whose
/// nodes are the dynamic bodies. Edges of one color share no dynamic body, so they can be solved
/// at the same time without locks.
/// </summary>
/// <remarks>
/// Colors are assigned in the order of keys that depend only on which edges exist, never on
/// where their records are stored or on which thread created them. The same world therefore
/// gets the same colors on every run and for any number of threads.
/// </remarks>
internal abstract class ConstraintGraph
{
    /// <summary>An edge that finds none of these colors free is solved after the colored ones, alone.</summary>
    internal const int MaximumColors = 64;

    private const int ParallelReadThreshold = 4096;
    private const int InsertionSortLimit = 24;
    private const int RadixBits = 11;

    /// <summary>
    /// The dynamic bodies of an edge by <see cref="RigidBodyData._index"/>, -1 for a body the
    /// solver only reads: static and kinematic bodies are read by any number of edges at once.
    /// </summary>
    protected struct Endpoints
    {
        internal int Body1, Body2;

        // An empty manifold or a disabled joint is left out of the solve.
        private const int Unsolved = int.MinValue;

        internal readonly bool IsSolved => Body1 != Unsolved;

        internal static Endpoints Of(ref RigidBodyData body1, ref RigidBodyData body2) => new()
        {
            Body1 = body1.MotionType == MotionType.Dynamic ? body1._index : -1,
            Body2 = body2.MotionType == MotionType.Dynamic ? body2._index : -1
        };

        internal static readonly Endpoints None = new() { Body1 = Unsolved, Body2 = Unsolved };
    }

    private int readCount, readWorkers;
    private Action<int>? readShare;

    /// <summary>Records the endpoints and keys of the edges in the given range of positions.</summary>
    protected abstract void ReadEdges(int start, int end);

    /// <summary>
    /// Reads all edges. This touches every record and its bodies, which is most of the cost of
    /// coloring, so the given number of pool workers share it.
    /// </summary>
    protected void ReadAllEdges(int count, int workers)
    {
        if (workers <= 1 || count < ParallelReadThreshold)
        {
            ReadEdges(0, count);
            return;
        }

        (readCount, readWorkers) = (count, workers);
        readShare ??= worker =>
        {
            Parallel.GetBounds(readCount, readWorkers, worker, out int start, out int end);
            ReadEdges(start, end);
        };
        ThreadPool.Instance.ExecuteWorkerLoop(readShare, workers);
    }

    /// <summary>Returns the first color free on both bodies and takes it, or the color count if none is.</summary>
    protected static int TakeColor(ulong[] colors, Endpoints edge)
    {
        ulong occupied = (edge.Body1 >= 0 ? colors[edge.Body1] : 0) | (edge.Body2 >= 0 ? colors[edge.Body2] : 0);
        int color = BitOperations.TrailingZeroCount(~occupied);
        if (color < MaximumColors)
        {
            if (edge.Body1 >= 0) colors[edge.Body1] |= 1UL << color;
            if (edge.Body2 >= 0) colors[edge.Body2] |= 1UL << color;
        }

        return color;
    }

    /// <summary>
    /// Sorts indices by their key. The sort is stable and its cost does not depend on the order
    /// the indices come in, only on the spread of the keys.
    /// </summary>
    protected static void SortByKey(ref int[] order, ref int[] scratch, int count, ulong[] keys)
    {
        if (count < 2) return;

        ulong min = ulong.MaxValue, max = 0;
        for (int k = 0; k < count; k++)
        {
            ulong key = keys[order[k]];
            min = Math.Min(min, key);
            max = Math.Max(max, key);
        }

        ulong range = max - min;
        Span<int> buckets = stackalloc int[1 << RadixBits];
        for (int shift = 0; shift < 64 && range >> shift != 0; shift += RadixBits)
        {
            buckets.Clear();
            for (int k = 0; k < count; k++) buckets[Digit(keys[order[k]] - min, shift)]++;

            int offset = 0;
            for (int b = 0; b < buckets.Length; b++)
            {
                int size = buckets[b];
                buckets[b] = offset;
                offset += size;
            }

            for (int k = 0; k < count; k++)
            {
                int index = order[k];
                scratch[buckets[Digit(keys[index] - min, shift)]++] = index;
            }

            (order, scratch) = (scratch, order);
        }
    }

    private static int Digit(ulong key, int shift) => (int)(key >> shift) & ((1 << RadixBits) - 1);

    /// <summary>Orders a run of indices by key. Meant for the short runs of equal first keys.</summary>
    protected static void SortRun(Span<int> run, ulong[] keys)
    {
        if (run.Length > InsertionSortLimit)
        {
            run.Sort(new KeyOrder(keys));
            return;
        }

        for (int k = 1; k < run.Length; k++)
        {
            int moving = run[k], slot = k;
            ulong key = keys[moving];
            for (; slot > 0 && keys[run[slot - 1]] > key; slot--) run[slot] = run[slot - 1];
            run[slot] = moving;
        }
    }

    private readonly struct KeyOrder(ulong[] keys) : IComparer<int>
    {
        public int Compare(int x, int y) => keys[x].CompareTo(keys[y]);
    }
}
