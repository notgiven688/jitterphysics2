/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Jitter2.Dynamics;
using Jitter2.Parallelization;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2;

// The graph-colored solver. Contacts are the edges of a graph whose nodes are the dynamic bodies.
// Contacts of one color share no dynamic body, so they can be solved at the same time without
// locks, and eight at once as the lanes of a SIMD bundle.
//
// Nothing is kept between solves: every solve colors the active contacts from scratch and
// gathers the bundles from the contact records, which stay where and what they are for the
// rest of the engine.
public sealed partial class World
{
    // Contacts that find none of these colors free are solved with body locks.
    private const int MaximumContactColors = 64;

    // A lock-free color ends with a barrier whose cost grows with the worker count while the
    // work per worker shrinks, so the contact count at which a color pays for its barrier grows
    // with the square of the worker count. Smaller colors are solved with body locks, in the
    // pass that also solves the joints.
    private const int WideColorBarrierFactor = 2;

    // Take the most workers that keep all but this fraction of the contacts in lock-free colors.
    // Adding workers wakes parked pool threads, so the previous choice is kept while it stays
    // within the looser bound and is raised only once more workers were justified for a number
    // of solves in a row; contact churn would otherwise change the count every few steps.
    private const int LockedFractionShift = 4, LockedFractionShiftKeep = 3;
    private const int WorkerRaiseDelay = 32;

    // Joints are always solved with locks and gain from every worker. Their count sets a floor
    // under the choice made for the contacts.
    private const int JointsPerWorker = 512;

    private const int MinimumBundlesPerWorker = 2;
    private const int LockedItemsPerWorker = 64;
    private const int BodiesPerWorker = 256;

    // The dynamic bodies of a contact by RigidBodyData._index, -1 for a body the solver only reads.
    private struct ContactEndpoints
    {
        internal int Body1, Body2;

        // An empty manifold is left out of the solve.
        internal const int Unsolved = int.MinValue;

        // The body whose index orders the contacts: the lower of the two it writes. Contacts
        // that write no body come first.
        internal readonly int Key => Body1 < 0 ? Body2 + 1 : Body2 < 0 ? Body1 + 1 : Math.Min(Body1, Body2) + 1;
    }

    // Per-solve scratch, indexed by the position of a contact in the active contact span.
    private ContactEndpoints[] contactEndpoints = [];
    private int[] contactOrder = [], coloredContacts = [];
    private byte[] contactColor = [];
    // Per-solve scratch, indexed by RigidBodyData._index.
    private ulong[] bodyColors = [];
    private int[] bodyBucket = [];

    private readonly int[] colorStart = new int[MaximumContactColors + 1];
    private readonly int[] colorBundleStart = new int[MaximumContactColors];
    private int contactColorCount;
    private readonly List<ContactSolverWide> contactBundles = [];
    private readonly List<int> wideColors = [];
    private readonly List<int> lockedContacts = [];
    private readonly List<int> unsolvedContacts = [];

    // The colors index the active contacts by position and are valid for this state of the buffer.
    private int coloredVersion, coloredCount = -1;

    private const byte UncoloredContact = byte.MaxValue;

    // Assigns every active contact the first color that none of its dynamic bodies uses yet.
    // The contacts are visited ordered by the body they write, the lower one when they write
    // both, and each color keeps that order: a worker's contiguous share of every color then
    // covers the same bodies, which stay in its caches across the color passes.
    private void ColorContacts(bool multiThread)
    {
        int count = memContacts.Active.Length;
        coloredVersion = memContacts.Version;
        coloredCount = count;

        if (contactColor.Length < count)
        {
            int capacity = Math.Max(count, contactColor.Length * 2);
            contactEndpoints = new ContactEndpoints[capacity];
            contactOrder = new int[capacity];
            coloredContacts = new int[capacity];
            contactColor = new byte[capacity];
        }

        // Reading the endpoints touches every contact record and its bodies, which is most of
        // the cost of coloring. The workers of the last solve share it; the rest works on the
        // compact arrays filled here.
        int readers = multiThread && count >= ParallelColoringThreshold
            ? Math.Min(solverWorkerCount, ThreadPool.Instance.ThreadCount) : 1;
        if (readers > 1)
        {
            endpointReaders = readers;
            readContactEndpoints ??= worker =>
            {
                Parallel.GetBounds(memContacts.Active.Length, endpointReaders, worker, out int start, out int end);
                ReadContactEndpoints(start, end);
            };
            ThreadPool.Instance.ExecuteWorkerLoop(readContactEndpoints, readers);
        }
        else
        {
            ReadContactEndpoints(0, count);
        }

        var endpoints = contactEndpoints.AsSpan(0, count);
        int bodyLimit = 0;
        foreach (ref readonly ContactEndpoints e in endpoints) bodyLimit = Math.Max(bodyLimit, Math.Max(e.Body1, e.Body2) + 1);

        // Bucket zero holds the contacts without a dynamic body, bucket b + 1 those keyed by body b.
        if (bodyColors.Length < bodyLimit + 2)
        {
            bodyColors = new ulong[Math.Max(bodyLimit + 2, bodyColors.Length * 2)];
            bodyBucket = new int[bodyColors.Length];
        }

        Array.Clear(bodyColors, 0, bodyLimit);
        Array.Clear(bodyBucket, 0, bodyLimit + 2);
        unsolvedContacts.Clear();

        for (int i = 0; i < count; i++)
        {
            if (endpoints[i].Body1 == ContactEndpoints.Unsolved) unsolvedContacts.Add(i);
            else bodyBucket[endpoints[i].Key + 1]++;
        }

        for (int b = 0; b <= bodyLimit; b++) bodyBucket[b + 1] += bodyBucket[b];
        int solved = bodyBucket[bodyLimit + 1];

        for (int i = 0; i < count; i++)
        {
            if (endpoints[i].Body1 != ContactEndpoints.Unsolved) contactOrder[bodyBucket[endpoints[i].Key]++] = i;
        }

        Span<int> colorCount = stackalloc int[MaximumContactColors];
        colorCount.Clear();
        lockedContacts.Clear();

        for (int k = 0; k < solved; k++)
        {
            int i = contactOrder[k];
            int id1 = endpoints[i].Body1, id2 = endpoints[i].Body2;
            ulong occupied = (id1 >= 0 ? bodyColors[id1] : 0) | (id2 >= 0 ? bodyColors[id2] : 0);
            int color = BitOperations.TrailingZeroCount(~occupied);
            if (color >= MaximumContactColors)
            {
                contactColor[i] = UncoloredContact;
                lockedContacts.Add(i);
                continue;
            }

            if (id1 >= 0) bodyColors[id1] |= 1UL << color;
            if (id2 >= 0) bodyColors[id2] |= 1UL << color;
            contactColor[i] = (byte)color;
            colorCount[color]++;
        }

        contactColorCount = 0;
        int offset = 0;
        for (int c = 0; c < MaximumContactColors; c++)
        {
            colorStart[c] = offset;
            offset += colorCount[c];
            if (colorCount[c] != 0) contactColorCount = c + 1;
            colorCount[c] = colorStart[c];
        }

        colorStart[MaximumContactColors] = offset;

        for (int k = 0; k < solved; k++)
        {
            int i = contactOrder[k];
            int color = contactColor[i];
            if (color != UncoloredContact) coloredContacts[colorCount[color]++] = i;
        }
    }

    // A body is a node of the graph only if the solver writes it. Static and kinematic bodies
    // are read by any number of contacts at once and are recorded as -1. Empty manifolds are
    // left out of the solve.
    private void ReadContactEndpoints(int start, int end)
    {
        var active = memContacts.Active;
        for (int i = start; i < end; i++)
        {
            ref ContactData contact = ref active[i];
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0)
            {
                contactEndpoints[i].Body1 = contactEndpoints[i].Body2 = ContactEndpoints.Unsolved;
                continue;
            }

            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;
            contactEndpoints[i].Body1 = b1.MotionType == MotionType.Dynamic ? b1._index : -1;
            contactEndpoints[i].Body2 = b2.MotionType == MotionType.Dynamic ? b2._index : -1;
        }
    }

    private const int ParallelColoringThreshold = 4096;
    private int endpointReaders;
    private Action<int>? readContactEndpoints;

    private int ColorLength(int color) => colorStart[color + 1] - colorStart[color];

    // Decides how many workers solve and which colors are worth a lock-free barrier of their own.
    private void PlanColoredSolve(bool multiThread)
    {
        solverWorkerLimit = multiThread ? ChooseSolverWorkerLimit(ThreadPool.Instance.ThreadCount) : 1;
        int threshold = multiThread ? WideColorBarrierFactor * solverWorkerLimit * solverWorkerLimit : 0;

        wideColors.Clear();
        int bundles = 0;
        for (int c = 0; c < contactColorCount; c++)
        {
            int length = ColorLength(c);
            if (length == 0) continue;

            if (Vector.IsHardwareAccelerated && length >= threshold)
            {
                wideColors.Add(c);
                colorBundleStart[c] = bundles;
                bundles += (length + ContactSolverWide.Width - 1) / ContactSolverWide.Width;
            }
            else
            {
                for (int k = colorStart[c]; k < colorStart[c + 1]; k++) lockedContacts.Add(coloredContacts[k]);
            }
        }

        while (contactBundles.Count < bundles) contactBundles.Add(new ContactSolverWide());
    }

    private int ChooseSolverWorkerLimit(int threadCount)
    {
        if (!Vector.IsHardwareAccelerated) return threadCount;

        int colored = colorStart[MaximumContactColors];
        int previous = solverWorkerLimit > 0 ? Math.Min(solverWorkerLimit, threadCount) : threadCount;
        int workers = previous;

        if (LockedColoredContacts(workers) <= colored >> LockedFractionShiftKeep)
        {
            while (workers < threadCount && LockedColoredContacts(workers + 1) <= colored >> LockedFractionShift) workers++;
        }
        else
        {
            while (workers > 1 && LockedColoredContacts(workers) > colored >> LockedFractionShift) workers--;
        }

        if (workers > previous && solverWorkerLimit != 0 && ++solverWorkerRaiseStreak < WorkerRaiseDelay) workers = previous;
        else solverWorkerRaiseStreak = 0;

        int joints = memConstraints.Active.Length + memSmallConstraints.Active.Length;
        return Math.Max(workers, Math.Min(threadCount, joints / JointsPerWorker));
    }

    // Contacts of colors too small to be solved lock-free by the given number of workers.
    private int LockedColoredContacts(int workers)
    {
        int threshold = WideColorBarrierFactor * workers * workers, locked = 0;
        for (int c = 0; c < contactColorCount; c++)
        {
            int length = ColorLength(c);
            if (length < threshold) locked += length;
        }

        return locked;
    }

    // ---- Solver program ---------------------------------------------------------------------
    // One worker loop per substep runs the whole solve on the participating pool threads: force
    // integration, preparation, the biased sweeps, velocity integration and the relaxation
    // sweeps. Passes that must not overlap are separated by a spin barrier, which costs far less
    // than dispatching each pass through the task queues. Every worker owns the same share of
    // each pass in every sweep.

    private enum SolverPassKind : byte
    {
        IntegrateForces,
        PrepareColor,
        IterateColor,
        WriteBackColor,
        PrepareLocked,
        IterateLocked,
        IntegrateVelocities,
        UpdateRemainingContacts
    }

    private struct SolverPass
    {
        internal SolverPassKind Kind;
        internal int Color, Total;
        internal bool ApplyBias, WriteBack, Joints, UpdateContacts;
    }

    private readonly List<SolverPass> solverPasses = [];
    // The most workers the current plan may use, and the count it does use.
    private int solverWorkerLimit;
    private int solverWorkerCount;
    private int solverWorkerRaiseStreak;
    private int solverArrivals;
    private int solverEpoch;
    private ExceptionDispatchInfo? solverFailure;
    private Action<int>? solverWorkerEntry;

    // A locked pass that includes the joints gives every worker its share of each list: a contact
    // costs several times a joint, so slicing the lists end to end would hand all contacts to the
    // first workers. Slice w spans [lockedSliceStart[w], lockedSliceStart[w + 1]).
    private int[] lockedSliceStart = [];

    /// <summary>
    /// Solves contacts and joints for one substep. With <paramref name="integrate"/> set, forces
    /// are integrated before and velocities after the biased sweeps.
    /// </summary>
    /// <param name="multiThread">Whether to use the thread pool.</param>
    /// <param name="iterations">The number of biased sweeps.</param>
    /// <param name="relaxations">The number of relaxation sweeps.</param>
    /// <param name="integrate">Whether this is a full substep rather than a velocity-only solve.</param>
    /// <param name="recolor">Whether contacts may have changed since the previous solve.</param>
    /// <param name="updateContacts">
    /// Whether to finish with the contact update of the step, see <see cref="RemoveBrokenArbiters"/>.
    /// </param>
    private void SolveColored(bool multiThread, int iterations, int relaxations, bool integrate,
        bool recolor = true, bool updateContacts = false)
    {
        // The colors of the previous substep are reused only when the caller knows that nothing
        // could have added, removed or edited a contact since.
        if (recolor || !ColorsAreCurrent)
        {
            ColorContacts(multiThread);
            PlanColoredSolve(multiThread);
        }

        int joints = memConstraints.Active.Length + memSmallConstraints.Active.Length;
        int workers = 1;
        if (multiThread)
        {
            // Let the largest pass decide how many workers are worth synchronizing.
            int demand = (lockedContacts.Count + joints) / LockedItemsPerWorker;
            foreach (int c in wideColors)
                demand = Math.Max(demand, ColorLength(c) / (ContactSolverWide.Width * MinimumBundlesPerWorker));
            if (integrate) demand = Math.Max(demand, memRigidBodies.Active.Length / BodiesPerWorker);
            workers = Math.Clamp(demand, 1, solverWorkerLimit);
        }

        solverWorkerCount = workers;

        if (lockedSliceStart.Length < workers + 1) lockedSliceStart = new int[workers + 1];
        int offset = 0;
        for (int w = 0; w < workers; w++)
        {
            lockedSliceStart[w] = offset;
            offset += SliceLength(lockedContacts.Count, workers, w) +
                      SliceLength(memConstraints.Active.Length, workers, w) +
                      SliceLength(memSmallConstraints.Active.Length, workers, w);
        }

        lockedSliceStart[workers] = offset;

        BuildSolverPasses(iterations, relaxations, integrate, offset, updateContacts);
        if (solverPasses.Count == 0) return;
        if (updateContacts) FlagRetiringArbiters();

        solverArrivals = workers;
        solverEpoch = 0;
        solverFailure = null;

        if (workers == 1)
        {
            RunSolverProgram(0);
        }
        else
        {
            solverWorkerEntry ??= SolverWorkerEntry;
            ThreadPool.Instance.ExecuteWorkerLoop(solverWorkerEntry, workers);
        }

        solverFailure?.Throw();
    }

    private bool ColorsAreCurrent => coloredVersion == memContacts.Version && coloredCount == memContacts.Active.Length;

    // The contact update of the step, run by the solver on the records it has just written while
    // they and their bodies are still in the worker's cache. It runs before the arbiters that
    // broke in the previous step are removed, so those are flagged first: their empty manifolds
    // must not be reported as broken a second time.
    private void FlagRetiringArbiters()
    {
        foreach (var handle in brokenArbiters)
        {
            ref ContactData contact = ref handle.Data;
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0) contact.UsageMask = ContactData.MaskRetiring;
        }
    }

    private void UpdateColoredContacts(ReadOnlySpan<int> indices)
    {
        var active = memContacts.Active;
        for (int i = 0; i < indices.Length; i++)
        {
            ref ContactData contact = ref active[indices[i]];
            if (contact.UsageMask == ContactData.MaskRetiring) continue;
            contact.UpdatePosition();

            if ((contact.UsageMask & ContactData.MaskContactAll) == 0)
                pendingBrokenArbiters.ConcurrentAdd(memContacts.GetHandle(ref contact));
        }
    }

    private static int SliceLength(int total, int parts, int part)
    {
        Parallel.GetBounds(total, parts, part, out int start, out int end);
        return end - start;
    }

    private void BuildSolverPasses(int iterations, int relaxations, bool integrate, int lockedTotal, bool updateContacts)
    {
        solverPasses.Clear();
        bool locked = lockedTotal != 0;

        if (integrate) AddPass(SolverPassKind.IntegrateForces, memRigidBodies.Active.Length);

        foreach (int c in wideColors) AddColorPass(SolverPassKind.PrepareColor, c);
        if (locked) AddPass(SolverPassKind.PrepareLocked, lockedTotal, joints: true);

        for (int iter = 0; iter < iterations; iter++)
        {
            foreach (int c in wideColors) AddColorPass(SolverPassKind.IterateColor, c, applyBias: true);
            if (locked) AddPass(SolverPassKind.IterateLocked, lockedTotal, applyBias: true, joints: true);
        }

        if (integrate) AddPass(SolverPassKind.IntegrateVelocities, memRigidBodies.Active.Length);

        // Relaxation solves the contacts only, on the anchors and masses prepared before the
        // bodies moved. Its last sweep also publishes the impulses while each bundle is in cache.
        for (int iter = 0; iter < relaxations; iter++)
        {
            bool last = iter == relaxations - 1;
            foreach (int c in wideColors)
                AddColorPass(SolverPassKind.IterateColor, c, writeBack: last, updateContacts: last && updateContacts);
            if (lockedContacts.Count != 0) AddPass(SolverPassKind.IterateLocked, lockedContacts.Count);
        }

        if (relaxations == 0)
        {
            foreach (int c in wideColors) AddColorPass(SolverPassKind.WriteBackColor, c, updateContacts: updateContacts);
        }

        if (updateContacts && lockedContacts.Count + unsolvedContacts.Count != 0)
            AddPass(SolverPassKind.UpdateRemainingContacts, solverWorkerCount);
    }

    private void AddColorPass(SolverPassKind kind, int color, bool applyBias = false, bool writeBack = false,
        bool updateContacts = false)
    {
        int bundles = (ColorLength(color) + ContactSolverWide.Width - 1) / ContactSolverWide.Width;
        solverPasses.Add(new SolverPass
        {
            Kind = kind, Color = color, Total = bundles, ApplyBias = applyBias, WriteBack = writeBack,
            UpdateContacts = updateContacts
        });
    }

    private void AddPass(SolverPassKind kind, int total, bool applyBias = false, bool joints = false)
    {
        solverPasses.Add(new SolverPass { Kind = kind, Total = total, ApplyBias = applyBias, Joints = joints });
    }

    private void SolverWorkerEntry(int worker)
    {
        try
        {
            RunSolverProgram(worker);
        }
        catch (Exception exception)
        {
            // The others see the failure at their next barrier and leave. The caller rethrows
            // once every worker has returned.
            Interlocked.CompareExchange(ref solverFailure, ExceptionDispatchInfo.Capture(exception), null);
        }
    }

    // Every participating worker runs the same pass list. A single worker passes each barrier at once.
    private void RunSolverProgram(int worker)
    {
        var passes = CollectionsMarshal.AsSpan(solverPasses);
        for (int k = 0; k < passes.Length; k++)
        {
            ref readonly SolverPass pass = ref passes[k];
            int start, end;
            if (pass.Joints)
            {
                start = lockedSliceStart[worker];
                end = lockedSliceStart[worker + 1];
            }
            else
            {
                Parallel.GetBounds(pass.Total, solverWorkerCount, worker, out start, out end);
            }

            if (start < end) RunPass(in pass, worker, start, end);
            if (!SolverBarrier()) return;
        }
    }

    private bool SolverBarrier()
    {
        int epoch = Volatile.Read(ref solverEpoch);
        if (Volatile.Read(ref solverFailure) != null) return false;
        if (Interlocked.Decrement(ref solverArrivals) == 0)
        {
            // Last to arrive: rearm, then release the others.
            solverArrivals = solverWorkerCount;
            Volatile.Write(ref solverEpoch, epoch + 1);
            return true;
        }

        var spinner = new SpinWait();
        while (Volatile.Read(ref solverEpoch) == epoch)
        {
            if (Volatile.Read(ref solverFailure) != null) return false;
            // Yield when the machine is oversubscribed, but never sleep for a millisecond.
            spinner.SpinOnce(sleep1Threshold: -1);
        }

        return true;
    }

    private void RunPass(in SolverPass pass, int worker, int start, int end)
    {
        switch (pass.Kind)
        {
            case SolverPassKind.IntegrateForces:
                IntegrateForces(new Parallel.Batch(start, end));
                break;
            case SolverPassKind.IntegrateVelocities:
                IntegrateVelocities(new Parallel.Batch(start, end));
                break;
            case SolverPassKind.PrepareColor:
            {
                var records = memContacts.Active;
                var lanes = coloredContacts.AsSpan(colorStart[pass.Color], ColorLength(pass.Color));
                int first = colorBundleStart[pass.Color];
                for (int i = start; i < end; i++)
                {
                    int from = i * ContactSolverWide.Width;
                    int length = Math.Min(ContactSolverWide.Width, lanes.Length - from);
                    contactBundles[first + i].Prepare(records, lanes.Slice(from, length), timeStep);
                }

                break;
            }
            case SolverPassKind.IterateColor:
            case SolverPassKind.WriteBackColor:
            {
                var lanes = coloredContacts.AsSpan(colorStart[pass.Color], ColorLength(pass.Color));
                int first = colorBundleStart[pass.Color];
                for (int i = start; i < end; i++)
                {
                    ContactSolverWide bundle = contactBundles[first + i];
                    if (pass.Kind == SolverPassKind.IterateColor) bundle.Iterate(pass.ApplyBias, pass.WriteBack);
                    else bundle.WriteBack();
                    if (pass.UpdateContacts) UpdateColoredContacts(lanes.Slice(i * ContactSolverWide.Width, bundle.Count));
                }

                break;
            }
            case SolverPassKind.UpdateRemainingContacts:
            {
                Parallel.GetBounds(lockedContacts.Count, solverWorkerCount, worker, out int first, out int last);
                UpdateColoredContacts(CollectionsMarshal.AsSpan(lockedContacts)[first..last]);
                Parallel.GetBounds(unsolvedContacts.Count, solverWorkerCount, worker, out first, out last);
                UpdateColoredContacts(CollectionsMarshal.AsSpan(unsolvedContacts)[first..last]);
                break;
            }
            case SolverPassKind.PrepareLocked:
            case SolverPassKind.IterateLocked:
            {
                bool prepare = pass.Kind == SolverPassKind.PrepareLocked;
                // A single worker owns every body, so the body locks are skipped.
                if (solverWorkerCount == 1) RunLockedItems<NoBodyLock>(pass.Joints, worker, start, end, prepare, pass.ApplyBias);
                else RunLockedItems<BodyLock>(pass.Joints, worker, start, end, prepare, pass.ApplyBias);
                break;
            }
        }
    }

    // Without joints the range indexes the locked contacts. With joints it is the worker's whole
    // slice: its share of the locked contacts, then of the joints, then of the small joints.
    private void RunLockedItems<TLock>(bool joints, int worker, int start, int end, bool prepare, bool applyBias)
        where TLock : struct, IBodyLock
    {
        if (!joints)
        {
            SolveLockedContacts<TLock>(start, end, prepare, applyBias);
            return;
        }

        int workers = solverWorkerCount;
        Parallel.GetBounds(lockedContacts.Count, workers, worker, out int first, out int last);
        if (first < last) SolveLockedContacts<TLock>(first, last, prepare, applyBias);

        Parallel.GetBounds(memConstraints.Active.Length, workers, worker, out first, out last);
        if (first < last)
        {
            if (prepare) PrepareConstraints<TLock>(new Parallel.Batch(first, last));
            else IterateConstraints<TLock>(new Parallel.Batch(first, last));
        }

        Parallel.GetBounds(memSmallConstraints.Active.Length, workers, worker, out first, out last);
        if (first < last)
        {
            if (prepare) PrepareSmallConstraints<TLock>(new Parallel.Batch(first, last));
            else IterateSmallConstraints<TLock>(new Parallel.Batch(first, last));
        }
    }

    private void SolveLockedContacts<TLock>(int start, int end, bool prepare, bool applyBias)
        where TLock : struct, IBodyLock
    {
        var active = memContacts.Active;
        var indices = CollectionsMarshal.AsSpan(lockedContacts)[start..end];
        var localQueue = deferredContacts.Value!;

        for (int i = 0; i < indices.Length; i++)
        {
            if (!TrySolveLockedContact<TLock>(ref active[indices[i]], prepare, applyBias)) localQueue.Enqueue(i);
        }

        while (localQueue.TryDequeue(out int i))
        {
            if (!TrySolveLockedContact<TLock>(ref active[indices[i]], prepare, applyBias)) localQueue.Enqueue(i);
        }
    }

    private bool TrySolveLockedContact<TLock>(ref ContactData contact, bool prepare, bool applyBias)
        where TLock : struct, IBodyLock
    {
        ref RigidBodyData b1 = ref contact.Body1.Data;
        ref RigidBodyData b2 = ref contact.Body2.Data;
        if (!TLock.TryLock(ref b1, ref b2)) return false;

        if (prepare) contact.PrepareForIteration(timeStep);
        else contact.Iterate(applyBias);
        TLock.Unlock(ref b1, ref b2);
        return true;
    }
}
