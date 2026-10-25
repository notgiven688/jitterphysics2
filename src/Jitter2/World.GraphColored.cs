/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Parallelization;
using Jitter2.Unmanaged;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2;

public sealed partial class World
{
    // Limit synchronization overhead for high-degree graphs. Contacts that cannot be
    // assigned one of these colors are solved with body locks in the shared locked pass.
    private const int MaximumContactColors = 64;

    // Active contacts are stored grouped by color. Segment s occupies
    // [segmentStart[s], segmentStart[s + 1]); the final segment ends at the active count.
    // Colors are stored in reverse order, then the overflow, then contacts that are not part
    // of the graph. The buffer allocates, activates, deactivates and frees at the active
    // boundary, which lies inside that last segment, so those operations never disturb a
    // color. Moving a record between segments costs one swap per non-empty segment crossed;
    // the reverse order keeps the large, frequently changing low colors next to the tail.
    private const int OverflowSegment = MaximumContactColors;
    private const int UncoloredSegment = MaximumContactColors + 1;
    private const int SegmentCount = MaximumContactColors + 2;

    private static int ColorSegment(int color) =>
        color == OverflowSegment ? OverflowSegment : MaximumContactColors - 1 - color;

    // Every lock-free color ends with a barrier whose cost grows with the worker count while
    // the work per worker shrinks, so the contact count at which a color pays for its barrier
    // grows with the square of the worker count. Smaller colors and overflow contacts take body
    // locks and share the pass that also solves the joints, adding no barrier of their own.
    private const int WideColorBarrierFactor = 2;

    // Bundles per task when a wide color is spread over the workers.
    private const int MinimumBundlesPerTask = 2;

    private sealed class ContactColor
    {
        internal readonly List<ContactSolverWide> Bundles = new();
        internal int BundleCount;
    }

    private readonly ContactColor?[] contactColors = new ContactColor?[MaximumContactColors];
    private readonly int[] segmentStart = new int[SegmentCount];
    // Planned once per solve: colors solved lock-free as bundles, and contacts solved with locks.
    private readonly List<int> wideColors = new();
    private readonly List<int> lockedContactIndices = new();
    private ulong[] bodyContactColors = Array.Empty<ulong>();
    private bool[] contactBodyWritable = Array.Empty<bool>();
    private int contactColorCount;

    // These entries are indexed by the stable PartitionedBuffer handle ID, never
    // the physical slot: activation and color grouping move that slot.
    private struct ContactGraphEntry
    {
        internal JHandle<ContactData> Handle;
        internal JHandle<RigidBodyData> Body1, Body2;
        internal ContactData.SolveMode Mode;
        internal int BodyId1, BodyId2;
        // -1 while uncolored, otherwise a color or the overflow segment.
        internal int Color;
    }

    private ContactGraphEntry[] contactGraph = Array.Empty<ContactGraphEntry>();
    private int[] contactBodyReferences = Array.Empty<int>();
    private readonly List<JHandle<ContactData>> pendingColoredContacts = new();
    // Manifolds whose participation or solve mode may have changed since the last solve. Marks
    // come from RegisterContact (possibly on narrow-phase workers), the contact update, wake-ups
    // and ClearContactCache. RawData is read-only, so no other path changes what the graph caches.
    private readonly SlimBag<JHandle<ContactData>> pendingContactChanges = [];
    private bool contactGraphValid;
    private bool contactGraphChanged;
    // Diagnostic counts used by the focused benchmark and lifecycle checks.
    private int contactGraphRebuildCount, contactGraphReuseCount, contactGraphIncrementalCount;
    private long contactRecordSwapCount;

    // Within a color segment the records are ordered by a locality key, so that a worker's
    // contiguous slice of every color covers the same bodies and those bodies stay in its caches
    // across the color passes. Incremental changes append and move records out of order; the
    // keyed order is restored after this many incremental updates.
    private const int ResortInterval = 16;
    private int incrementalChangesSinceSort;
    private long[] sortKeys = Array.Empty<long>();
    private int[] sortOrder = Array.Empty<int>(), sortTarget = Array.Empty<int>();

    private int SegmentEnd(int segment) =>
        segment + 1 < SegmentCount ? segmentStart[segment + 1] : memContacts.Active.Length;

    private int SegmentLength(int segment) => SegmentEnd(segment) - segmentStart[segment];

    // Serial entry point used by graph diagnostics and tests.
    private void BuildContactColors() => UpdateContactColors();

    /// <summary>
    /// Records that a manifold's participation or solve mode may have changed. Safe to call from
    /// narrow-phase workers; the mark is applied before the next solve.
    /// </summary>
    internal void MarkContactGraphChange(JHandle<ContactData> handle)
    {
        if (!contactGraphValid || solveMode != Jitter2.SolveMode.Regular) return;
        pendingContactChanges.ConcurrentAdd(handle);
    }

    // Called by RegisterContact after the points were added. Registration happens every step
    // for every manifold, so only a new manifold or a changed solve mode leaves a mark. The graph
    // only changes while solving, so reading the cached entry here is race-free.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NoteContactRegistered(JHandle<ContactData> handle, ref ContactData contact)
    {
        if (!contactGraphValid || solveMode != Jitter2.SolveMode.Regular) return;
        int id = contact._internal;
        if (id < contactGraph.Length)
        {
            ref ContactGraphEntry entry = ref contactGraph[id];
            if (!entry.Handle.IsZero && entry.Mode == contact.Mode) return;
        }
        pendingContactChanges.ConcurrentAdd(handle);
    }

    private void UpdateContactColors()
    {
        if (!contactGraphValid)
        {
            RebuildContactColors();
            return;
        }

        pendingColoredContacts.Clear();
        bool rebuild = false;
        // Applying a mark re-reads the record, so repeated marks are harmless. A record freed
        // since it was marked is skipped; a recycled slot is simply re-evaluated.
        int activeCount = memContacts.Active.Length;
        foreach (var handle in pendingContactChanges)
        {
            if (memContacts.GetIndex(handle) >= activeCount) continue;
            if (!IsContactGraphEntryCurrent(ref handle.Data)) UpdateContactGraphEntry(handle, ref rebuild);
        }
        pendingContactChanges.Clear();

        // Only a reader-to-writer promotion invalidates colors already assigned to
        // other contacts. Ordinary contact churn can stay incremental.
        if (rebuild)
        {
            RebuildContactColors();
            return;
        }

        foreach (var handle in pendingColoredContacts) AddColoredContact(handle);
        if (contactGraphChanged)
        {
            // Retry overflow only when the graph has changed.
            RetryOverflowContacts();
            if (++incrementalChangesSinceSort >= ResortInterval) SortContactsBySegment();
            RefreshContactColorBundles();
            contactGraphIncrementalCount++;
        }
        else contactGraphReuseCount++;
        contactGraphChanged = false;
        AssertContactGraphCurrent();
    }

    // Debug safety net for the marking contract: every used manifold has a current entry and
    // every unused record sits outside the graph.
    [Conditional("DEBUG")]
    private void AssertContactGraphCurrent()
    {
        foreach (ref ContactData contact in memContacts.Active)
        {
            bool used = (contact.UsageMask & ContactData.MaskContactAll) != 0;
            Debug.Assert(used ? IsContactGraphEntryCurrent(ref contact) : SegmentOf(ref contact) == UncoloredSegment,
                "A contact changed without a graph mark.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsContactGraphEntryCurrent(ref ContactData contact)
    {
        int id = contact._internal;
        if (id >= contactGraph.Length) return false;
        ref ContactGraphEntry entry = ref contactGraph[id];
        return !entry.Handle.IsZero && (contact.UsageMask & ContactData.MaskContactAll) != 0 &&
               entry.Body1 == contact.Body1 && entry.Body2 == contact.Body2 && entry.Mode == contact.Mode;
    }

    private void UpdateContactGraphEntry(JHandle<ContactData> handle, ref bool rebuild)
    {
        int id = handle.Data._internal;
        EnsureContactGraphCapacity(id);
        // Removal moves records between segments: read the contact through its handle
        // afterwards, never through a reference into its previous storage slot.
        if (!contactGraph[id].Handle.IsZero) RemoveColoredContact(handle);
        if ((handle.Data.UsageMask & ContactData.MaskContactAll) == 0) return;
        InitializeContactGraphEntry(handle, ref rebuild);
        pendingColoredContacts.Add(handle);
        contactGraphChanged = true;
    }

    private void EnsureContactGraphCapacity(int id)
    {
        if (id >= contactGraph.Length)
            Array.Resize(ref contactGraph, Math.Max(id + 1, Math.Max(128, contactGraph.Length * 2)));
    }

    private void EnsureContactBodyCapacity(int id)
    {
        if (id < bodyContactColors.Length) return;
        int capacity = Math.Max(id + 1, Math.Max(128, bodyContactColors.Length * 2));
        Array.Resize(ref bodyContactColors, capacity);
        Array.Resize(ref contactBodyWritable, capacity);
        Array.Resize(ref contactBodyReferences, capacity);
    }

    private void AddContactBodyReference(int id, bool writable, ref bool rebuild)
    {
        EnsureContactBodyCapacity(id);
        if (writable && !contactBodyWritable[id])
        {
            // Previously shared readers must now exclude each other too. Updating
            // only this edge would permit a reader/writer race in an existing color.
            rebuild |= contactBodyReferences[id] != 0;
            contactBodyWritable[id] = true;
        }
        contactBodyReferences[id]++;
    }

    private void InitializeContactGraphEntry(JHandle<ContactData> handle, ref bool rebuild)
    {
        ref ContactData contact = ref handle.Data;
        EnsureContactGraphCapacity(contact._internal);
        int first = contact.Body1.Data._index, second = contact.Body2.Data._index;
        contactGraph[contact._internal] = new ContactGraphEntry
        {
            Handle = handle,
            Body1 = contact.Body1,
            Body2 = contact.Body2,
            Mode = contact.Mode,
            BodyId1 = first,
            BodyId2 = second,
            Color = -1
        };
        AddContactBodyReference(first, (contact.Mode & ContactData.SolveMode.FullBody1) != 0, ref rebuild);
        AddContactBodyReference(second, (contact.Mode & ContactData.SolveMode.FullBody2) != 0, ref rebuild);
    }

    private void RebuildContactColors()
    {
        Array.Clear(contactGraph);
        Array.Clear(bodyContactColors);
        Array.Clear(contactBodyWritable);
        Array.Clear(contactBodyReferences);
        pendingColoredContacts.Clear();
        pendingContactChanges.Clear();
        contactColorCount = 0;
        bool ignored = false;
        var active = memContacts.Active;
        // Classify global writers before assigning any edges, including custom modes.
        foreach (ref ContactData contact in active)
            if ((contact.UsageMask & ContactData.MaskContactAll) != 0)
                InitializeContactGraphEntry(memContacts.GetHandle(ref contact), ref ignored);
        foreach (ref ContactData contact in active)
            if ((contact.UsageMask & ContactData.MaskContactAll) != 0)
                AssignContactColor(ref contactGraph[contact._internal]);
        SortContactsBySegment();
        RefreshContactColorBundles();
        contactGraphValid = true;
        contactGraphChanged = false;
        contactGraphRebuildCount++;
    }

    private int SegmentOf(ref ContactData contact)
    {
        int id = contact._internal;
        if (id >= contactGraph.Length) return UncoloredSegment;
        ref ContactGraphEntry entry = ref contactGraph[id];
        return entry.Handle.IsZero || entry.Color < 0 ? UncoloredSegment : ColorSegment(entry.Color);
    }

    // The locality key of a record: the stable index of the body it writes, the lower one when it
    // writes both. In scenes built in spatial order this approximates a spatial sort for free.
    private int SortKey(ref ContactData contact)
    {
        int id = contact._internal;
        if (id >= contactGraph.Length) return 0;
        ref ContactGraphEntry entry = ref contactGraph[id];
        if (entry.Handle.IsZero) return 0;
        bool writes1 = (entry.Mode & ContactData.SolveMode.FullBody1) != 0;
        bool writes2 = (entry.Mode & ContactData.SolveMode.FullBody2) != 0;
        if (writes1 && writes2) return Math.Min(entry.BodyId1, entry.BodyId2);
        return writes1 ? entry.BodyId1 : entry.BodyId2;
    }

    // Groups the active records by segment and orders each segment by the locality key. Each
    // record is swapped at most once into its final position.
    private void SortContactsBySegment()
    {
        var active = memContacts.Active;
        int n = active.Length;
        if (sortKeys.Length < n)
        {
            int capacity = Math.Max(n, sortKeys.Length * 2);
            sortKeys = new long[capacity];
            sortOrder = new int[capacity];
            sortTarget = new int[capacity];
        }

        Span<int> counts = stackalloc int[SegmentCount];
        counts.Clear();
        for (int i = 0; i < n; i++)
        {
            int segment = SegmentOf(ref active[i]);
            counts[segment]++;
            sortKeys[i] = ((long)segment << 32) | (uint)SortKey(ref active[i]);
            sortOrder[i] = i;
        }

        int offset = 0;
        for (int s = 0; s < SegmentCount; s++)
        {
            segmentStart[s] = offset;
            offset += counts[s];
        }

        Array.Sort(sortKeys, sortOrder, 0, n);
        for (int rank = 0; rank < n; rank++) sortTarget[sortOrder[rank]] = rank;
        for (int i = 0; i < n; i++)
        {
            while (sortTarget[i] != i)
            {
                int j = sortTarget[i];
                memContacts.Swap(i, j);
                (sortTarget[i], sortTarget[j]) = (sortTarget[j], sortTarget[i]);
                contactRecordSwapCount++;
            }
        }
        incrementalChangesSinceSort = 0;
    }

    // Moves a record across the segment boundaries between two segments. Every boundary
    // crossed shifts by one: the record at that boundary takes the vacated slot and the moving
    // record ends up at the last boundary. One rotation copies each record exactly once.
    private void MoveContactToSegment(JHandle<ContactData> handle, int from, int to)
    {
        if (from == to) return;
        Span<int> path = stackalloc int[SegmentCount + 1];
        int length = 0;
        path[length++] = memContacts.GetIndex(handle);
        if (to < from)
        {
            for (int s = from; s > to; s--)
            {
                int first = segmentStart[s];
                if (first != path[length - 1]) path[length++] = first;
                segmentStart[s]++;
            }
        }
        else
        {
            for (int s = from; s < to; s++)
            {
                int last = SegmentEnd(s) - 1;
                if (last != path[length - 1]) path[length++] = last;
                segmentStart[s + 1]--;
            }
        }
        memContacts.Rotate(path[..length]);
        contactRecordSwapCount += length - 1;
    }

    // Picks the first color free of conflicts and reserves it on the writable endpoints.
    private int AssignContactColor(ref ContactGraphEntry entry)
    {
        bool writable1 = contactBodyWritable[entry.BodyId1], writable2 = contactBodyWritable[entry.BodyId2];
        ulong occupied = (writable1 ? bodyContactColors[entry.BodyId1] : 0) |
                         (writable2 ? bodyContactColors[entry.BodyId2] : 0);
        int index = BitOperations.TrailingZeroCount(~occupied);
        if (index >= MaximumContactColors)
        {
            entry.Color = OverflowSegment;
            return OverflowSegment;
        }
        entry.Color = index;
        contactColorCount = Math.Max(contactColorCount, index + 1);
        ulong bit = 1UL << index;
        if (writable1) bodyContactColors[entry.BodyId1] |= bit;
        if (writable2) bodyContactColors[entry.BodyId2] |= bit;
        return index;
    }

    private void AddColoredContact(JHandle<ContactData> handle)
    {
        ref ContactGraphEntry entry = ref contactGraph[handle.Data._internal];
        int from = entry.Color < 0 ? UncoloredSegment : ColorSegment(entry.Color);
        int to = ColorSegment(AssignContactColor(ref entry));
        MoveContactToSegment(handle, from, to);
    }

    private void ClearContactColor(JHandle<ContactData> handle, ref ContactGraphEntry entry)
    {
        if (entry.Color < 0) return;
        if (entry.Color < MaximumContactColors)
        {
            ulong bit = ~(1UL << entry.Color);
            if (contactBodyWritable[entry.BodyId1]) bodyContactColors[entry.BodyId1] &= bit;
            if (contactBodyWritable[entry.BodyId2]) bodyContactColors[entry.BodyId2] &= bit;
        }
        MoveContactToSegment(handle, ColorSegment(entry.Color), UncoloredSegment);
        entry.Color = -1;
    }

    private void RetryOverflowContacts()
    {
        var active = memContacts.Active;
        for (int i = segmentStart[OverflowSegment]; i < segmentStart[UncoloredSegment];)
        {
            var handle = memContacts.GetHandle(ref active[i]);
            ref ContactGraphEntry entry = ref contactGraph[handle.Data._internal];
            int color = AssignContactColor(ref entry);
            if (color == OverflowSegment)
            {
                i++;
                continue;
            }
            // The record from the segment start now occupies index i and is examined next.
            MoveContactToSegment(handle, OverflowSegment, ColorSegment(color));
            if (i < segmentStart[OverflowSegment]) i = segmentStart[OverflowSegment];
        }
    }

    private void RemoveContactBodyReference(int id)
    {
        if (--contactBodyReferences[id] != 0) return;
        bodyContactColors[id] = 0;
        contactBodyWritable[id] = false;
    }

    // Remove before freeing/recycling a handle or moving it into sleeping storage. The record
    // is moved into the uncolored segment first, where the buffer performs those operations.
    // Use cached endpoints: public fields may already refer to different bodies.
    private void RemoveColoredContact(JHandle<ContactData> handle)
    {
        int id = handle.Data._internal;
        if (id >= contactGraph.Length || contactGraph[id].Handle.IsZero) return;
        ref ContactGraphEntry entry = ref contactGraph[id];
        ClearContactColor(handle, ref entry);
        RemoveContactBodyReference(entry.BodyId1);
        RemoveContactBodyReference(entry.BodyId2);
        entry = default;
        contactGraphChanged = true;
    }

    private void RefreshContactColorBundles()
    {
        while (contactColorCount > 0 && SegmentLength(ColorSegment(contactColorCount - 1)) == 0) contactColorCount--;
        for (int c = 0; c < contactColorCount; c++)
        {
            ContactColor color = contactColors[c] ??= new ContactColor();
            // A final partial bundle keeps every contact of a wide color on the SIMD path.
            color.BundleCount = System.Numerics.Vector.IsHardwareAccelerated
                ? (SegmentLength(ColorSegment(c)) + ContactSolverWide.Width - 1) / ContactSolverWide.Width : 0;
            while (color.Bundles.Count < color.BundleCount) color.Bundles.Add(new ContactSolverWide());
        }
    }

    // Decide which colors are worth a lock-free barrier of their own. The plan is kept
    // between the biased solve and the relaxation of the same substep: the bundles of a
    // wide color hold the prepared coefficients until their final write-back.
    private void PlanColoredSolve(bool multiThread)
    {
        wideColors.Clear();
        lockedContactIndices.Clear();
        int threadCount = ThreadPool.Instance.ThreadCount;
        int threshold = multiThread ? WideColorBarrierFactor * threadCount * threadCount : 0;
        for (int c = 0; c < contactColorCount; c++)
        {
            int segment = ColorSegment(c);
            int count = SegmentLength(segment);
            if (count == 0) continue;
            if (contactColors[c]!.BundleCount > 0 && count >= threshold) wideColors.Add(c);
            else AddLockedContactRange(segmentStart[segment], SegmentEnd(segment));
        }
        AddLockedContactRange(segmentStart[OverflowSegment], segmentStart[UncoloredSegment]);
    }

    private void AddLockedContactRange(int start, int end)
    {
        for (int i = start; i < end; i++) lockedContactIndices.Add(i);
    }

    private Span<ContactData> BundleLanes(int color, int bundle)
    {
        int segment = ColorSegment(color);
        int offset = bundle * ContactSolverWide.Width;
        int count = Math.Min(ContactSolverWide.Width, SegmentLength(segment) - offset);
        return memContacts.Active.Slice(segmentStart[segment] + offset, count);
    }

    // Contacts outside the wide colors use the lock-based scalar path. They run in the
    // same pass as the joints, which also take body locks, so a single barrier covers both.
    private bool HasLockedSolverWork => lockedContactIndices.Count != 0 ||
                                        memConstraints.Active.Length != 0 || memSmallConstraints.Active.Length != 0;

    private void PrepareLockedContacts(Parallel.Batch batch)
    {
        var active = memContacts.Active;
        var indices = CollectionsMarshal.AsSpan(lockedContactIndices)[batch.Start..batch.End];
        var localQueue = deferredContacts.Value!;

        for (int i = 0; i < indices.Length; i++)
        {
            ref ContactData contact = ref active[indices[i]];
            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            contact.PrepareForIteration(timeStep);
            UnlockTwoBody(ref b1, ref b2);
        }

        while (localQueue.TryDequeue(out int i))
        {
            ref ContactData contact = ref active[indices[i]];
            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            contact.PrepareForIteration(timeStep);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private void IterateLockedContacts(Parallel.Batch batch, bool applyBias)
    {
        var active = memContacts.Active;
        var indices = CollectionsMarshal.AsSpan(lockedContactIndices)[batch.Start..batch.End];
        var localQueue = deferredContacts.Value!;

        for (int i = 0; i < indices.Length; i++)
        {
            ref ContactData contact = ref active[indices[i]];
            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            contact.Iterate(applyBias);
            UnlockTwoBody(ref b1, ref b2);
        }

        while (localQueue.TryDequeue(out int i))
        {
            ref ContactData contact = ref active[indices[i]];
            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            contact.Iterate(applyBias);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    // ---- Solver schedule -----------------------------------------------------------------
    // One broadcast per substep runs the whole solve on every participating pool thread:
    // preparation, the biased sweeps, velocity integration and the relaxation sweeps. Passes
    // that must not overlap are separated by a spin barrier, which costs far less than
    // dispatching each pass through the pool queues. Every worker owns the same slice of each
    // pass in every sweep, so the bodies and bundles it touches stay in its own caches; a
    // worker that runs out of work steals the tail of another worker's slice.

    private enum SolverPassKind : byte
    {
        PrepareColor,
        IterateColor,
        WriteBackColor,
        PrepareLocked,
        IterateLocked,
        UpdateLocked,
        IntegrateVelocities
    }

    private struct SolverPass
    {
        internal SolverPassKind Kind;
        internal int Color, Total, Grain;
        internal bool ApplyBias, WriteBack, Joints;
        // The final pass over each contact also refreshes its offsets and breaks drifted
        // points, replacing the separate UpdateContacts sweep of the step.
        internal bool UpdateContacts;
    }

    // A locked pass that includes the joints is divided so that every worker's slice holds its
    // share of each list: scalar contacts cost several times a joint, so slicing the lists
    // end to end would hand all contacts to the first workers. Slice w spans
    // [lockedSliceStart[w], lockedSliceStart[w + 1]) of the pass's index space.
    private int[] lockedSliceStart = Array.Empty<int>();

    // Each worker's range is a packed (next, end) pair on its own cache line.
    private const int RangeStride = 8;
    private const int LockedItemsPerWorker = 64;
    private const int BodiesPerWorker = 256;

    private readonly List<SolverPass> solverPasses = new();
    private long[] solverRanges = Array.Empty<long>();
    private int solverWorkerCount;
    private int solverArrivals;
    private int solverEpoch;
    private ExceptionDispatchInfo? solverFailure;
    private Action<int> solverWorkerEntry = null!;

    private void SolveContactsAndJoints(bool multiThread, int iterations, int relaxations, bool integrateVelocities,
        bool updateContacts)
    {
        // Validate after PreSubStep; retain occupancy masks and storage order
        // when endpoints, participation and write modes are unchanged.
        UpdateContactColors();
        PlanColoredSolve(multiThread);

        int workers = 1;
        if (multiThread)
        {
            // Let the largest pass decide how many workers are worth synchronizing.
            int demand = (lockedContactIndices.Count + memConstraints.Active.Length +
                          memSmallConstraints.Active.Length) / LockedItemsPerWorker;
            for (int i = 0; i < wideColors.Count; i++)
                demand = Math.Max(demand, contactColors[wideColors[i]]!.BundleCount / MinimumBundlesPerTask);
            if (integrateVelocities) demand = Math.Max(demand, memRigidBodies.Active.Length / BodiesPerWorker);
            workers = Math.Clamp(demand, 1, ThreadPool.Instance.ThreadCount);
        }

        solverWorkerCount = workers;
        PlanLockedSlices(workers);
        BuildSolverPasses(iterations, relaxations, integrateVelocities, updateContacts);
        if (solverPasses.Count == 0) return;
        if (solverRanges.Length < workers * RangeStride) solverRanges = new long[workers * RangeStride];
        InitializeSolverRanges(solverPasses[0]);
        solverArrivals = workers;
        solverEpoch = 0;
        solverFailure = null;
        if (workers == 1)
        {
            RunSolverProgram(0);
            return;
        }

        ThreadPool.Instance.ExecuteWorkerLoop(solverWorkerEntry);
        solverFailure?.Throw();
    }

    private static int SliceLength(int total, int parts, int part)
    {
        Parallel.GetBounds(total, parts, part, out int start, out int end);
        return end - start;
    }

    private void PlanLockedSlices(int workers)
    {
        if (lockedSliceStart.Length < workers + 1) lockedSliceStart = new int[workers + 1];
        int contacts = lockedContactIndices.Count;
        int constraints = memConstraints.Active.Length, small = memSmallConstraints.Active.Length;
        int offset = 0;
        for (int w = 0; w < workers; w++)
        {
            lockedSliceStart[w] = offset;
            offset += SliceLength(contacts, workers, w) + SliceLength(constraints, workers, w) +
                      SliceLength(small, workers, w);
        }
        lockedSliceStart[workers] = offset;
    }

    private int UncoloredContactCount => memContacts.Active.Length - segmentStart[UncoloredSegment];

    private void BuildSolverPasses(int iterations, int relaxations, bool integrateVelocities, bool updateContacts)
    {
        solverPasses.Clear();
        bool locked = HasLockedSolverWork;
        int lockedTotal = lockedSliceStart[solverWorkerCount];

        foreach (int c in wideColors) AddColorPass(SolverPassKind.PrepareColor, c, false, false, false);
        if (locked) AddPass(SolverPassKind.PrepareLocked, lockedTotal, applyBias: false, joints: true, update: false);

        for (int iter = 0; iter < iterations; iter++)
        {
            foreach (int c in wideColors) AddColorPass(SolverPassKind.IterateColor, c, applyBias: true, false, false);
            if (locked) AddPass(SolverPassKind.IterateLocked, lockedTotal, applyBias: true, joints: true, update: false);
        }

        if (integrateVelocities)
            AddPass(SolverPassKind.IntegrateVelocities, memRigidBodies.Active.Length, false, false, false);

        // Integration changes velocities and transforms, but world inertia is refreshed by
        // UpdateBodies at the end of Step. Keep the prepared anchors and inertia fixed so
        // velocity relaxation operates on the same constraints. The final sweep also publishes
        // the accumulated impulses for warm starting while each bundle is still in cache, and
        // refreshes the contact state when this is the last substep. Contacts outside the graph
        // join that final locked pass for the refresh only.
        for (int iter = 0; iter < relaxations; iter++)
        {
            bool last = iter == relaxations - 1, update = last && updateContacts;
            foreach (int c in wideColors) AddColorPass(SolverPassKind.IterateColor, c, applyBias: false, last, update);
            int total = lockedContactIndices.Count + (update ? UncoloredContactCount : 0);
            if (total != 0) AddPass(SolverPassKind.IterateLocked, total, applyBias: false, joints: false, update);
        }

        if (relaxations != 0) return;
        // Persist warm-start state even when there are no relaxation iterations.
        foreach (int c in wideColors) AddColorPass(SolverPassKind.WriteBackColor, c, false, false, updateContacts);
        int remaining = lockedContactIndices.Count + UncoloredContactCount;
        if (updateContacts && remaining != 0) AddPass(SolverPassKind.UpdateLocked, remaining, false, false, update: true);
    }

    private void AddColorPass(SolverPassKind kind, int color, bool applyBias, bool writeBack, bool update)
    {
        int bundles = contactColors[color]!.BundleCount;
        solverPasses.Add(new SolverPass
        {
            Kind = kind, Color = color, Total = bundles, Grain = Math.Max(1, bundles / (solverWorkerCount * 8)),
            ApplyBias = applyBias, WriteBack = writeBack, UpdateContacts = update
        });
    }

    private void AddPass(SolverPassKind kind, int total, bool applyBias, bool joints, bool update)
    {
        int minimumGrain = kind == SolverPassKind.IntegrateVelocities ? BodiesPerWorker / 8 : LockedItemsPerWorker / 4;
        solverPasses.Add(new SolverPass
        {
            Kind = kind, Total = total, Grain = Math.Max(minimumGrain, total / (solverWorkerCount * 8)),
            ApplyBias = applyBias, Joints = joints, UpdateContacts = update
        });
    }

    private void SolverWorkerEntry(int worker)
    {
        // Workers beyond the planned count only acknowledge the broadcast.
        if (worker >= solverWorkerCount) return;
        try
        {
            RunSolverProgram(worker);
        }
        catch (Exception exception)
        {
            // Peers spinning at a barrier observe the failure and leave. The caller rethrows
            // once every worker has returned.
            Interlocked.CompareExchange(ref solverFailure, ExceptionDispatchInfo.Capture(exception), null);
        }
    }

    // Every participating worker runs the same pass list. The serial case is the same program
    // with a single worker, whose barriers pass immediately.
    private void RunSolverProgram(int worker)
    {
        var passes = CollectionsMarshal.AsSpan(solverPasses);
        for (int k = 0; k < passes.Length; k++)
        {
            RunDistributedPass(worker, in passes[k]);
            if (!SolverBarrier(k)) return;
        }
    }

    private bool SolverBarrier(int pass)
    {
        int epoch = Volatile.Read(ref solverEpoch);
        if (Volatile.Read(ref solverFailure) != null) return false;
        if (Interlocked.Decrement(ref solverArrivals) == 0)
        {
            // Last to arrive: hand out the next pass's slices, rearm, then release the others.
            if (pass + 1 < solverPasses.Count) InitializeSolverRanges(solverPasses[pass + 1]);
            solverArrivals = solverWorkerCount;
            Volatile.Write(ref solverEpoch, epoch + 1);
            return true;
        }

        var spinner = new SpinWait();
        while (Volatile.Read(ref solverEpoch) == epoch)
        {
            if (Volatile.Read(ref solverFailure) != null) return false;
            // Yield under oversubscription, but never sleep for a millisecond.
            spinner.SpinOnce(sleep1Threshold: -1);
        }
        return true;
    }

    private static bool IsInterleaved(in SolverPass pass) =>
        pass.Joints && pass.Kind is SolverPassKind.PrepareLocked or SolverPassKind.IterateLocked;

    private void InitializeSolverRanges(in SolverPass pass)
    {
        bool interleaved = IsInterleaved(in pass);
        for (int w = 0; w < solverWorkerCount; w++)
        {
            int start, end;
            if (interleaved)
            {
                start = lockedSliceStart[w];
                end = lockedSliceStart[w + 1];
            }
            else Parallel.GetBounds(pass.Total, solverWorkerCount, w, out start, out end);
            solverRanges[w * RangeStride] = PackRange(start, end);
        }
    }

    private static long PackRange(int next, int end) => ((long)next << 32) | (uint)end;

    // The owner takes a grain from the front of its range; thieves take half of the remainder
    // from the back. Both go through one compare-exchange on the packed pair, so every item is
    // processed exactly once.
    private static bool ClaimRange(ref long state, int grain, out int start, out int end)
    {
        while (true)
        {
            long current = Volatile.Read(ref state);
            int next = (int)(current >> 32), limit = (int)current;
            if (next >= limit)
            {
                start = end = 0;
                return false;
            }
            int take = Math.Min(grain, limit - next);
            if (Interlocked.CompareExchange(ref state, PackRange(next + take, limit), current) != current) continue;
            start = next;
            end = next + take;
            return true;
        }
    }

    private static bool StealRange(ref long state, out int start, out int end)
    {
        while (true)
        {
            long current = Volatile.Read(ref state);
            int next = (int)(current >> 32), limit = (int)current;
            int remaining = limit - next;
            if (remaining <= 0)
            {
                start = end = 0;
                return false;
            }
            int take = (remaining + 1) / 2;
            if (Interlocked.CompareExchange(ref state, PackRange(next, limit - take), current) != current) continue;
            start = limit - take;
            end = limit;
            return true;
        }
    }

    private void RunDistributedPass(int worker, in SolverPass pass)
    {
        ref long own = ref solverRanges[worker * RangeStride];
        while (ClaimRange(ref own, pass.Grain, out int start, out int end)) RunPassItems(in pass, start, end);
        if (solverWorkerCount == 1) return;

        // Help until every slice is empty. Stolen work is republished as this worker's own
        // slice, so it can be shared again.
        for (int victim = (worker + 1) % solverWorkerCount; victim != worker; victim = (victim + 1) % solverWorkerCount)
        {
            if (!StealRange(ref solverRanges[victim * RangeStride], out int start, out int end)) continue;
            Volatile.Write(ref own, PackRange(start, end));
            while (ClaimRange(ref own, pass.Grain, out start, out end)) RunPassItems(in pass, start, end);
            victim = worker;
        }
    }

    private void RunPassItems(in SolverPass pass, int start, int end)
    {
        switch (pass.Kind)
        {
            case SolverPassKind.PrepareColor:
            {
                ContactColor color = contactColors[pass.Color]!;
                for (int i = start; i < end; i++) color.Bundles[i].Prepare(BundleLanes(pass.Color, i), timeStep);
                break;
            }
            case SolverPassKind.IterateColor:
            {
                ContactColor color = contactColors[pass.Color]!;
                for (int i = start; i < end; i++)
                {
                    color.Bundles[i].Iterate(pass.ApplyBias, pass.WriteBack);
                    if (pass.UpdateContacts) UpdateContactRecords(BundleLanes(pass.Color, i));
                }
                break;
            }
            case SolverPassKind.WriteBackColor:
            {
                ContactColor color = contactColors[pass.Color]!;
                for (int i = start; i < end; i++)
                {
                    color.Bundles[i].WriteBack();
                    if (pass.UpdateContacts) UpdateContactRecords(BundleLanes(pass.Color, i));
                }
                break;
            }
            case SolverPassKind.PrepareLocked:
                RunLockedItems(start, end, prepare: true, applyBias: false, pass.Joints);
                break;
            case SolverPassKind.IterateLocked:
                RunLockedItems(start, end, prepare: false, pass.ApplyBias, pass.Joints);
                if (pass.UpdateContacts) UpdateLockedAndUncoloredContacts(start, end);
                break;
            case SolverPassKind.UpdateLocked:
                UpdateLockedAndUncoloredContacts(start, end);
                break;
            case SolverPassKind.IntegrateVelocities:
                IntegrateVelocities(new Parallel.Batch(start, end));
                break;
        }
    }

    // Items below the locked count are locked contacts; the rest are records outside the graph.
    private void UpdateLockedAndUncoloredContacts(int start, int end)
    {
        var active = memContacts.Active;
        int locked = lockedContactIndices.Count;
        var indices = CollectionsMarshal.AsSpan(lockedContactIndices);
        for (int i = start; i < Math.Min(end, locked); i++) UpdateContactRecords(active.Slice(indices[i], 1));
        if (end > locked)
        {
            int first = segmentStart[UncoloredSegment] + Math.Max(start, locked) - locked;
            UpdateContactRecords(active.Slice(first, end - Math.Max(start, locked)));
        }
    }

    // Contacts outside the wide colors use the lock-based scalar path. They run in the same
    // pass as the joints, which also take body locks, so a single barrier covers both.
    private void RunLockedItems(int start, int end, bool prepare, bool applyBias, bool joints)
    {
        if (!joints)
        {
            // A relaxation pass may extend past the locked contacts to cover uncolored records
            // that only need their state refreshed.
            end = Math.Min(end, lockedContactIndices.Count);
            if (start >= end) return;
            if (prepare) PrepareLockedContacts(new Parallel.Batch(start, end));
            else IterateLockedContacts(new Parallel.Batch(start, end), applyBias);
            return;
        }

        int workers = solverWorkerCount;
        int contacts = lockedContactIndices.Count;
        int constraints = memConstraints.Active.Length, small = memSmallConstraints.Active.Length;
        int w = 0;
        while (lockedSliceStart[w + 1] <= start) w++;
        for (; w < workers && lockedSliceStart[w] < end; w++)
        {
            // Positions within slice w: its share of the contacts, then of the joints, then of
            // the small joints.
            int sliceStart = lockedSliceStart[w];
            int from = Math.Max(start, sliceStart) - sliceStart;
            int to = Math.Min(end, lockedSliceStart[w + 1]) - sliceStart;
            int offset = 0;

            Parallel.GetBounds(contacts, workers, w, out int first, out int last);
            int a = Math.Max(from, offset) - offset, b = Math.Min(to, offset + last - first) - offset;
            if (a < b)
            {
                if (prepare) PrepareLockedContacts(new Parallel.Batch(first + a, first + b));
                else IterateLockedContacts(new Parallel.Batch(first + a, first + b), applyBias);
            }
            offset += last - first;

            Parallel.GetBounds(constraints, workers, w, out first, out last);
            a = Math.Max(from, offset) - offset;
            b = Math.Min(to, offset + last - first) - offset;
            if (a < b)
            {
                if (prepare) PrepareConstraints(new Parallel.Batch(first + a, first + b));
                else IterateConstraints(new Parallel.Batch(first + a, first + b));
            }
            offset += last - first;

            Parallel.GetBounds(small, workers, w, out first, out last);
            a = Math.Max(from, offset) - offset;
            b = Math.Min(to, offset + last - first) - offset;
            if (a < b)
            {
                if (prepare) PrepareSmallConstraints(new Parallel.Batch(first + a, first + b));
                else IterateSmallConstraints(new Parallel.Batch(first + a, first + b));
            }
        }
    }

    private void ClearContactColors()
    {
        Array.Clear(contactColors);
        Array.Clear(segmentStart);
        wideColors.Clear();
        lockedContactIndices.Clear();
        contactColorCount = 0;
        contactGraphValid = false;
        contactGraphChanged = false;
        incrementalChangesSinceSort = 0;
        contactGraph = Array.Empty<ContactGraphEntry>();
        contactBodyReferences = Array.Empty<int>();
        pendingColoredContacts.Clear();
        pendingContactChanges.Clear();
        bodyContactColors = Array.Empty<ulong>();
        contactBodyWritable = Array.Empty<bool>();
    }

    private void TrimContactColors()
    {
        // Scratch is rebuilt on the next solve. Releasing it avoids retaining bundles
        // and index lists belonging to a formerly much larger scene.
        ClearContactColors();
        wideColors.TrimExcess();
        lockedContactIndices.TrimExcess();
        pendingColoredContacts.TrimExcess();
        pendingContactChanges.Trim();
        sortKeys = Array.Empty<long>();
        sortOrder = sortTarget = Array.Empty<int>();
    }
}
