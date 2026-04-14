/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Jitter2.Collision;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.Parallelization;
using Jitter2.Unmanaged;

namespace Jitter2;

/// <summary>
/// Controls which solver strategy is used during <see cref="World.Step"/>.
/// </summary>
public enum SolveMode
{
    /// <summary>
    /// Standard parallel solver. Contacts and constraints are distributed across threads
    /// in fixed-size batches. An incremental cache-friendly reorder pass keeps related
    /// contacts adjacent in memory. Fast, but non-deterministic.
    /// </summary>
    Regular,

    /// <summary>
    /// Island-based deterministic solver. Each simulation island is solved sequentially
    /// as an independent task. Islands are distributed across threads, so multithreading
    /// is still used. This mode can be significantly slower than <see cref="Regular"/>.
    /// </summary>
    Deterministic
}

public sealed partial class World
{
    private SolveMode solveMode = Jitter2.SolveMode.Regular;

    /// <summary>
    /// The solver strategy used during <see cref="Step"/>. Defaults to <see cref="Jitter2.SolveMode.Regular"/>.
    /// <remarks> <see cref="Jitter2.SolveMode.Deterministic"/> can be significantly slower than
    /// <see cref="Jitter2.SolveMode.Regular"/>.</remarks>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an undefined <see cref="Jitter2.SolveMode"/>
    /// value is assigned.</exception>
    public SolveMode SolveMode
    {
        get => solveMode;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "The solve mode must be a defined enum value.");
            }

            solveMode = value;
        }
    }

    // -------------------------------------------------------------------------
    // Internal state for island solving
    // -------------------------------------------------------------------------

    private struct IslandRange
    {
        public int ContactStart, ContactEnd;
        public int SmallStart, SmallEnd;
        public int ConstraintStart, ConstraintEnd;
    }

    private struct ContactEntry
    {
        public int Index;
        public int IslandIndex;
        public ArbiterKey Key;
    }

    private struct SmallConstraintEntry
    {
        public JHandle<SmallConstraintData> Handle;
        public int IslandIndex;
        public ulong ConstraintId;
    }

    private struct ConstraintEntry
    {
        public JHandle<ConstraintData> Handle;
        public int IslandIndex;
        public ulong ConstraintId;
    }

    // Maps RigidBodyData._index → island SetIndex. Cleared each frame; capacity grows on demand but is not trimmed.
    private readonly Dictionary<int, int> handleToIsland = new();

    // Per-frame island ranges built from the sorted buffers.
    private readonly List<IslandRange> islandRanges = new();
    private readonly List<ContactEntry> sortedContacts = new();
    private readonly List<SmallConstraintEntry> sortedSmallConstraints = new();
    private readonly List<ConstraintEntry> sortedConstraints = new();

    // -------------------------------------------------------------------------
    // Build island lookup.
    // -------------------------------------------------------------------------

    private void BuildIslandLookup()
    {
        handleToIsland.Clear();

        // Map each active body's handle-index to its active island partition index.
        for (int i = 0; i < bodies.ActiveCount; i++)
        {
            RigidBody body = bodies[i];
            handleToIsland.Add(body.Data._index, ((IPartitionedSetIndex)body.Island).SetIndex);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private int IslandOf(JHandle<RigidBodyData> b1, JHandle<RigidBodyData> b2)
    {
        return b1.Data.MotionType != MotionType.Static
            ? handleToIsland[b1.Data._index]
            : handleToIsland[b2.Data._index];
    }

    // -------------------------------------------------------------------------
    // Build sorted sidecar lists for contacts and constraints.
    // -------------------------------------------------------------------------

    private void SortForIslands()
    {
        sortedContacts.Clear();
        var contacts = memContacts.Active;
        for (int i = 0; i < contacts.Length; i++)
        {
            ref ContactData contact = ref contacts[i];
            sortedContacts.Add(new ContactEntry
            {
                Index = i,
                IslandIndex = IslandOf(contact.Body1, contact.Body2),
                Key = contact.Key
            });
        }

        sortedContacts.Sort(static (a, b) =>
        {
            if (a.IslandIndex != b.IslandIndex) return a.IslandIndex.CompareTo(b.IslandIndex);
            if (a.Key.Key1 != b.Key.Key1) return a.Key.Key1 < b.Key.Key1 ? -1 : 1;
            if (a.Key.Key2 != b.Key.Key2) return a.Key.Key2 < b.Key.Key2 ? -1 : 1;
            return 0; // Contacts must be the same. Sort *does* compare objects to themselves.
        });

        sortedSmallConstraints.Clear();
        var small = memSmallConstraints.Active;
        for (int i = 0; i < small.Length; i++)
        {
            var handle = memSmallConstraints.GetHandle(ref small[i]);
            sortedSmallConstraints.Add(new SmallConstraintEntry
            {
                Handle = handle,
                IslandIndex = IslandOf(handle.Data.Body1, handle.Data.Body2),
                ConstraintId = handle.Data.ConstraintId
            });
        }

        sortedConstraints.Clear();
        var constraints = memConstraints.Active;
        for (int i = 0; i < constraints.Length; i++)
        {
            var handle = memConstraints.GetHandle(ref constraints[i]);
            sortedConstraints.Add(new ConstraintEntry
            {
                Handle = handle,
                IslandIndex = IslandOf(handle.Data.Body1, handle.Data.Body2),
                ConstraintId = handle.Data.ConstraintId
            });
        }

        // Group constraints by island and use the stored constraint identifier
        // as a deterministic tie-breaker within each island.
        sortedSmallConstraints.Sort(static (a, b) =>
        {
            if (a.IslandIndex != b.IslandIndex) return a.IslandIndex.CompareTo(b.IslandIndex);
            return a.ConstraintId.CompareTo(b.ConstraintId);
        });
        sortedConstraints.Sort(static (a, b) =>
        {
            if (a.IslandIndex != b.IslandIndex) return a.IslandIndex.CompareTo(b.IslandIndex);
            return a.ConstraintId.CompareTo(b.ConstraintId);
        });
    }

    // -------------------------------------------------------------------------
    // Build per-island ranges from the sorted buffers (single linear pass)
    // -------------------------------------------------------------------------

    private void BuildIslandRanges()
    {
        islandRanges.Clear();

        var contacts = CollectionsMarshal.AsSpan(sortedContacts);
        var small = CollectionsMarshal.AsSpan(sortedSmallConstraints);
        var constraints = CollectionsMarshal.AsSpan(sortedConstraints);

        int ci = 0, si = 0, ri = 0;

        while (ci < contacts.Length || si < small.Length || ri < constraints.Length)
        {
            // Find the lowest island index among the three current scan positions.
            int minIsland = int.MaxValue;
            if (ci < contacts.Length)    minIsland = Math.Min(minIsland, contacts[ci].IslandIndex);
            if (si < small.Length)       minIsland = Math.Min(minIsland, small[si].IslandIndex);
            if (ri < constraints.Length) minIsland = Math.Min(minIsland, constraints[ri].IslandIndex);

            IslandRange range = new();

            range.ContactStart = ci;
            while (ci < contacts.Length    && contacts[ci].IslandIndex    == minIsland) ci++;
            range.ContactEnd = ci;

            range.SmallStart = si;
            while (si < small.Length       && small[si].IslandIndex       == minIsland) si++;
            range.SmallEnd = si;

            range.ConstraintStart = ri;
            while (ri < constraints.Length && constraints[ri].IslandIndex == minIsland) ri++;
            range.ConstraintEnd = ri;

            islandRanges.Add(range);
        }
    }

    // -------------------------------------------------------------------------
    // Island solve task: prepare + iterate all contacts/constraints for a range
    // of islands sequentially. No locking needed — islands are independent.
    // -------------------------------------------------------------------------

    private void SolveIslandBatch(Parallel.Batch batch)
    {
        var allContacts = memContacts.Active;
        var orderedContacts = CollectionsMarshal.AsSpan(sortedContacts);
        var allSmall = CollectionsMarshal.AsSpan(sortedSmallConstraints);
        var allConstraints = CollectionsMarshal.AsSpan(sortedConstraints);

        for (int idx = batch.Start; idx < batch.End; idx++)
        {
            IslandRange range = islandRanges[idx];

            var contacts = orderedContacts[range.ContactStart..range.ContactEnd];
            var small = allSmall[range.SmallStart..range.SmallEnd];
            var constraints = allConstraints[range.ConstraintStart..range.ConstraintEnd];

            // --- Prepare ---

            for (int i = 0; i < contacts.Length; i++)
            {
                ref ContactData c = ref allContacts[contacts[i].Index];
                c.PrepareForIteration(invStepDt);
            }

            for (int i = 0; i < constraints.Length; i++)
            {
                ref ConstraintData c = ref constraints[i].Handle.Data;
                if (c.IsEnabled)
                    c.PrepareForIteration(ref c, invStepDt);
            }

            for (int i = 0; i < small.Length; i++)
            {
                ref SmallConstraintData c = ref small[i].Handle.Data;
                if (c.IsEnabled)
                    c.PrepareForIteration(ref c, invStepDt);
            }

            // --- Iterate ---

            for (int iter = 0; iter < solverIterations; iter++)
            {
                for (int i = 0; i < contacts.Length; i++)
                {
                    ref ContactData c = ref allContacts[contacts[i].Index];
                    c.Iterate(true);
                }

                for (int i = 0; i < constraints.Length; i++)
                {
                    ref ConstraintData c = ref constraints[i].Handle.Data;
                    if (c.IsEnabled)
                        c.Iterate(ref c, invStepDt);
                }

                for (int i = 0; i < small.Length; i++)
                {
                    ref SmallConstraintData c = ref small[i].Handle.Data;
                    if (c.IsEnabled)
                        c.Iterate(ref c, invStepDt);
                }
            }

        }
    }

    private void RelaxIslandBatch(Parallel.Batch batch)
    {
        var allContacts = memContacts.Active;
        var orderedContacts = CollectionsMarshal.AsSpan(sortedContacts);

        for (int idx = batch.Start; idx < batch.End; idx++)
        {
            IslandRange range = islandRanges[idx];
            var contacts = orderedContacts[range.ContactStart..range.ContactEnd];

            for (int iter = 0; iter < velocityRelaxations; iter++)
            {
                for (int i = 0; i < contacts.Length; i++)
                {
                    ref ContactData c = ref allContacts[contacts[i].Index];
                    c.Iterate(false);
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // Entry point: build island lookup → sort sidecars → build ranges.
    // -------------------------------------------------------------------------

    private void PrepareIslandSolveOrder()
    {
        BuildIslandLookup();
        SortForIslands();
        BuildIslandRanges();
    }

    private void RelaxIslands()
    {
        int numIslands = islandRanges.Count;
        if (numIslands == 0) return;

        int numThreads = ThreadPool.Instance.ThreadCount;
        int numTasks = Math.Min(numIslands, numThreads);

        if (numTasks <= 1)
        {
            RelaxIslandBatch(new Parallel.Batch(0, numIslands));
            return;
        }

        Parallel.ForBatch(0, numIslands, numTasks, relaxIsland);
    }

    private void SolveIslands(int iterations)
    {
        int numIslands = islandRanges.Count;
        if (numIslands == 0) return;

        int numThreads = ThreadPool.Instance.ThreadCount;
        int numTasks = Math.Min(numIslands, numThreads);

        if (numTasks <= 1)
        {
            // Single-threaded fallback: process all islands on the calling thread.
            SolveIslandBatch(new Parallel.Batch(0, numIslands));
            return;
        }

        // Distribute island ranges evenly across threads. Each task gets a
        // contiguous slice of islandRanges and solves them sequentially.
        // Islands across tasks are fully independent — no locking required.
        Parallel.ForBatch(0, numIslands, numTasks, solveIsland);
    }
}
