/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Jitter2.Collision;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Parallelization;
using Jitter2.Unmanaged;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2;

public sealed partial class World
{
    // Note: A SlimBag of the reference type 'Arbiter' does not introduce GC problems (not setting
    // all elements to null when clearing) since the references for Arbiters are pooled anyway.
    private readonly SlimBag<Arbiter> deferredArbiters = [];
    private readonly SlimBag<JHandle<ContactData>> brokenArbiters = [];

    public enum Timings
    {
        PreStep,
        NarrowPhase,
        AddArbiter,
        ReorderContacts,
        CheckDeactivation,
        Solve,
        RemoveArbiter,
        UpdateContacts,
        UpdateBodies,
        BroadPhase,
        PostStep,
        Last
    }

    private Action<Parallel.Batch> integrate;
    private Action<Parallel.Batch> integrateForces;
    private Action<Parallel.Batch> prepareContacts;
    private Action<Parallel.Batch> iterateContacts;
    private Action<Parallel.Batch> relaxVelocities;
    private Action<Parallel.Batch> updateContacts;
    private Action<Parallel.Batch> prepareConstraints;
    private Action<Parallel.Batch> iterateConstraints;
    private Action<Parallel.Batch> prepareSmallConstraints;
    private Action<Parallel.Batch> iterateSmallConstraints;
    private Action<Parallel.Batch> updateBodies;
    private Action<IDynamicTreeProxy, IDynamicTreeProxy> detect;

    private void InitParallelCallbacks()
    {
        integrate = Integrate;
        integrateForces = IntegrateForces;
        prepareContacts = PrepareContacts;
        iterateContacts = IterateContacts;
        relaxVelocities = RelaxVelocities;
        prepareConstraints = PrepareConstraints;
        iterateConstraints = IterateConstraints;
        prepareSmallConstraints = PrepareSmallConstraints;
        iterateSmallConstraints = IterateSmallConstraints;
        updateContacts = UpdateContacts;
        updateBodies = UpdateBodies;
        detect = Detect;
    }

    /// <summary>
    /// Contains timings for the stages of the last call to <see cref="World.Step(Real, bool)"/>.
    /// Array elements correspond to the enums in <see cref="Timings"/>. It can be used to identify
    /// bottlenecks.
    /// </summary>
    public double[] DebugTimings { get; } = new double[(int)Timings.Last];

    /// <summary>
    /// Performs a single simulation step.
    /// </summary>
    /// <param name="dt">The duration of time to simulate. This should remain fixed and not exceed 1/60 of a second.</param>
    /// <param name="multiThread">Indicates whether multithreading should be used. The behavior of the engine can be modified using <see cref="Parallelization.ThreadPool.Instance"/>.</param>
    public void Step(Real dt, bool multiThread = true)
    {
        Tracer.ProfileBegin(TraceName.Step);

        AssertNullBody();

        switch (dt)
        {
            case < (Real)0.0:
                throw new ArgumentException("Time step cannot be negative.", nameof(dt));
            case < Real.Epsilon:
                return; // nothing to do
        }

        long time;
        double invFrequency = 1.0d / Stopwatch.Frequency;

        void SetTime(Timings type)
        {
            long ctime = Stopwatch.GetTimestamp();
            double delta = (ctime - time) * 1000.0d;
            DebugTimings[(int)type] = delta * invFrequency;
            time = ctime;
        }

        invStepDt = (Real)1.0 / stepDt;
        substepDt = dt / substeps;
        stepDt = dt;

        if (multiThread)
        {
            // Signal the thread pool to spin up threads
            ThreadPool.Instance.ResumeWorkers();
        }

        // Start timer
        time = Stopwatch.GetTimestamp();

        Tracer.ProfileScopeBegin();
        PreStep?.Invoke(dt);
        Tracer.ProfileScopeEnd(TraceName.PreStep);
        SetTime(Timings.PreStep);

        // Perform narrow phase detection.
        Tracer.ProfileBegin(TraceName.NarrowPhase);
        DynamicTree.EnumerateOverlaps(detect, multiThread);
        Tracer.ProfileEnd(TraceName.NarrowPhase);
        SetTime(Timings.NarrowPhase);

        Tracer.ProfileBegin(TraceName.AddArbiter);
        HandleDeferredArbiters();
        Tracer.ProfileEnd(TraceName.AddArbiter);
        SetTime(Timings.AddArbiter);

        Tracer.ProfileBegin(TraceName.ReorderContacts);
        ReorderContacts();
        Tracer.ProfileEnd(TraceName.ReorderContacts);
        SetTime(Timings.ReorderContacts);

        Tracer.ProfileBegin(TraceName.CheckDeactivation);
        CheckDeactivation();
        Tracer.ProfileEnd(TraceName.CheckDeactivation);
        SetTime(Timings.CheckDeactivation);

        Tracer.ProfileBegin(TraceName.Solve);

        // Sub-stepping
        for (int i = 0; i < substeps; i++)
        {
            PreSubStep?.Invoke(substepDt);
            IntegrateForces(multiThread);                       // FAST SWEEP
            Solve(multiThread, solverIterations);               // FAST SWEEP
            Integrate(multiThread);                             // FAST SWEEP
            RelaxVelocities(multiThread, velocityRelaxations);  // FAST SWEEP
            PostSubStep?.Invoke(substepDt);
        }

        Tracer.ProfileEnd(TraceName.Solve);
        SetTime(Timings.Solve);

        Tracer.ProfileBegin(TraceName.RemoveArbiter);
        RemoveBrokenArbiters();
        Tracer.ProfileEnd(TraceName.RemoveArbiter);
        SetTime(Timings.RemoveArbiter);

        Tracer.ProfileBegin(TraceName.UpdateContacts);
        UpdateContacts(multiThread);                            // FAST SWEEP
        Tracer.ProfileEnd(TraceName.UpdateContacts);
        SetTime(Timings.UpdateContacts);

        Tracer.ProfileBegin(TraceName.UpdateBodies);
        ForeachActiveBody(multiThread);
        Tracer.ProfileEnd(TraceName.UpdateBodies);
        SetTime(Timings.UpdateBodies);

        Tracer.ProfileBegin(TraceName.BroadPhase);
        DynamicTree.Update(multiThread, stepDt);
        Tracer.ProfileEnd(TraceName.BroadPhase);
        SetTime(Timings.BroadPhase);

        Tracer.ProfileScopeBegin();
        PostStep?.Invoke(dt);
        Tracer.ProfileScopeEnd(TraceName.PostStep);
        SetTime(Timings.PostStep);

        if ((ThreadModel == ThreadModelType.Regular || !multiThread)
            && ThreadPool.InstanceInitialized)
        {
            // Signal the thread pool that threads can go into a wait state.
            ThreadPool.Instance.PauseWorkers();
        }

        Tracer.ProfileEnd(TraceName.Step);
    }

    #region Prepare and Solve Contacts and Constraints

    private readonly ThreadLocal<Queue<int>> deferredContacts = new(() => new Queue<int>());
    private readonly ThreadLocal<Queue<int>> deferredConstraints = new(() => new Queue<int>());
    private readonly ThreadLocal<Queue<int>> deferredSmallConstraints = new(() => new Queue<int>());

    private void PrepareContacts(Parallel.Batch batch)
    {
        var span = memContacts.Active[batch.Start..batch.End];
        var localQueue = deferredContacts.Value!;

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData contact = ref span[i];
            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            contact.PrepareForIteration(invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }

        Tracer.ProfileScopeBegin();

        while (localQueue.TryDequeue(out int i))
        {
            ref ContactData contact = ref span[i];
            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            contact.PrepareForIteration(invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }

        Tracer.ProfileScopeEnd(TraceName.Queue, TraceCategory.General, 10);
    }

    private void IterateContacts(Parallel.Batch batch)
    {
        var span = memContacts.Active[batch.Start..batch.End];
        var localQueue = deferredContacts.Value!;

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];
            ref RigidBodyData b1 = ref c.Body1.Data;
            ref RigidBodyData b2 = ref c.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            c.Iterate(true);
            UnlockTwoBody(ref b1, ref b2);
        }

        Tracer.ProfileScopeBegin();

        while (localQueue.TryDequeue(out int i))
        {
            ref ContactData c = ref span[i];
            ref RigidBodyData b1 = ref c.Body1.Data;
            ref RigidBodyData b2 = ref c.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            c.Iterate(true);
            UnlockTwoBody(ref b1, ref b2);
        }

        Tracer.ProfileScopeEnd(TraceName.Queue, TraceCategory.General, 10);
    }

    private unsafe void PrepareSmallConstraints(Parallel.Batch batch)
    {
        var span = memSmallConstraints.Active[batch.Start..batch.End];
        var localQueue = deferredSmallConstraints.Value!;

        for (int i = 0; i < span.Length; i++)
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.PrepareForIteration == null) continue;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.PrepareForIteration(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }

        while (localQueue.TryDequeue(out int i))
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.PrepareForIteration(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private unsafe void IterateSmallConstraints(Parallel.Batch batch)
    {
        var span = memSmallConstraints.Active[batch.Start..batch.End];
        var localQueue = deferredSmallConstraints.Value!;

        for (int i = 0; i < span.Length; i++)
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.Iterate == null) continue;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.Iterate(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }

        while (localQueue.TryDequeue(out int i))
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.Iterate(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }


    private unsafe void PrepareConstraints(Parallel.Batch batch)
    {
        var span = memConstraints.Active[batch.Start..batch.End];
        var localQueue = deferredConstraints.Value!;

        for (int i = 0; i < span.Length; i++)
        {
            ref ConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.PrepareForIteration == null) continue;

            Debug.Assert(b1.MotionType == MotionType.Dynamic || b2.MotionType == MotionType.Dynamic,
                "Invalid constraint: both bodies are non-dynamic.");

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.PrepareForIteration(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }

        while (localQueue.TryDequeue(out int i))
        {
            ref ConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.PrepareForIteration(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private unsafe void IterateConstraints(Parallel.Batch batch)
    {
        var span = memConstraints.Active[batch.Start..batch.End];
        var localQueue = deferredConstraints.Value!;

        for (int i = 0; i < span.Length; i++)
        {
            ref ConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.Iterate == null) continue;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.Iterate(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }

        while (localQueue.TryDequeue(out int i))
        {
            ref ConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            constraint.Iterate(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private void RelaxVelocities(Parallel.Batch batch)
    {
        var span = memContacts.Active[batch.Start..batch.End];
        var localQueue = deferredContacts.Value!;

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];
            ref RigidBodyData b1 = ref c.Body1.Data;
            ref RigidBodyData b2 = ref c.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            c.Iterate(false);
            UnlockTwoBody(ref b1, ref b2);
        }

        Tracer.ProfileScopeBegin();

        while (localQueue.TryDequeue(out int i))
        {
            ref ContactData c = ref span[i];
            ref RigidBodyData b1 = ref c.Body1.Data;
            ref RigidBodyData b2 = ref c.Body2.Data;

            if (!TryLockTwoBody(ref b1, ref b2))
            {
                localQueue.Enqueue(i);
                continue;
            }

            c.Iterate(false);
            UnlockTwoBody(ref b1, ref b2);
        }

        Tracer.ProfileScopeEnd(TraceName.Queue, TraceCategory.General, 10);
    }

    #endregion

    private void UpdateBodies(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            bodies[i].Update(stepDt, substepDt);
        }
    }

    private JHandle<RigidBodyData> lastVisited = JHandle<RigidBodyData>.Zero;
    private int sortCounter;

    /// <summary>
    /// This method gradually improves the memory layout of <see cref="memContacts"/>, moving connected contacts
    /// next to each other. This improves cache coherence.
    /// </summary>
    private void ReorderContacts()
    {
        int activeCount = memContacts.Active.Length;

        // Don't even bother with sorting contacts when the set is small.
        if (activeCount < 1024) return;

        const int fraction = 1024;

        // The basic idea here is to gradually improve the spatial locality of the contact array.
        // Each frame, only a small fraction of contacts (1/1024th of the total) are visited.
        // For each visited contact, the algorithm checks whether it is already adjacent to another
        // contact that shares one of its bodies. If not, it attempts to move contacts that
        // involve the same bodies forward in memory, grouping connected contacts together.

        int iterations = activeCount / fraction;

        for (int iter = 0; iter < iterations; iter++)
        {
            if (sortCounter > activeCount - 2) sortCounter = 0;

            ref var current = ref memContacts.Active[sortCounter];
            ref var next = ref memContacts.Active[sortCounter + 1];

            if (current.Body1 == next.Body1 || current.Body1 == next.Body2)
            {
                lastVisited = current.Body1;
                sortCounter += 1;
                continue;
            }

            if (current.Body2 == next.Body1 || current.Body2 == next.Body2)
            {
                lastVisited = current.Body2;
                sortCounter += 1;
                continue;
            }

            int swaps = 0;

            if (arbiters.TryGetValue(current.Key, out var arbiter))
            {
                if (lastVisited != arbiter.Body1.Handle)
                {
                    foreach (var arb in arbiter.Body1.Contacts)
                    {
                        int index = memContacts.GetIndex(arb.Handle);
                        if (index <= sortCounter || index >= activeCount) continue;

                        swaps++;
                        memContacts.Swap(sortCounter + swaps, index);
                    }

                    lastVisited = arbiter.Body1.Handle;
                }
                else
                {
                    foreach (var arb in arbiter.Body2.Contacts)
                    {
                        int index = memContacts.GetIndex(arb.Handle);
                        if (index <= sortCounter || index >= activeCount) continue;

                        swaps++;
                        memContacts.Swap(sortCounter + swaps, index);
                    }

                    lastVisited = arbiter.Body2.Handle;
                }
            }

            sortCounter += Math.Max(1, swaps);
        }
    }

    private void AssertNullBody()
    {
        ref RigidBodyData rigidBody = ref NullBody.Data;
        Debug.Assert(rigidBody.MotionType == MotionType.Static);
        Debug.Assert(rigidBody.InverseMass < Real.Epsilon);
        Debug.Assert(MathHelper.UnsafeIsZero(ref rigidBody.InverseInertiaWorld));
    }

    private void ForeachActiveBody(bool multiThread)
    {
#if DEBUG
        foreach (var body in bodies)
        {
            if (body.Data.MotionType != MotionType.Dynamic)
            {
                Debug.Assert(MathHelper.UnsafeIsZero(ref body.Data.InverseInertiaWorld));
                Debug.Assert(body.Data.InverseMass < Real.Epsilon);
            }
        }
#endif

        if (multiThread)
        {
            bodies.ParallelForBatch(256, updateBodies);
        }
        else
        {
            Parallel.Batch batch = new(0, bodies.ActiveCount);
            UpdateBodies(batch);
        }
    }

    private void RemoveBrokenArbiters()
    {
        for (int i = 0; i < brokenArbiters.Count; i++)
        {
            var handle = brokenArbiters[i];
            if ((handle.Data.UsageMask & ContactData.MaskContactAll) == 0)
            {
                Arbiter arb = arbiters[handle.Data.Key];

                AddToActiveList(arb.Body1.InternalIsland);
                AddToActiveList(arb.Body2.InternalIsland);

                memContacts.Free(handle);
                IslandHelper.ArbiterRemoved(islands, arb);
                arbiters.Remove(handle.Data.Key);

                arb.Body1.RaiseEndCollide(arb);
                arb.Body2.RaiseEndCollide(arb);

                Arbiter.Pool.Push(arb);
                arb.Handle = JHandle<ContactData>.Zero;
            }
        }

        brokenArbiters.Clear();
    }

    private void UpdateContacts(Parallel.Batch batch)
    {
        var span = memContacts.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            // get rid of broken contacts
            ref ContactData cq = ref span[i];
            cq.UpdatePosition();

            if ((cq.UsageMask & ContactData.MaskContactAll) == 0)
            {
                var h = memContacts.GetHandle(ref cq);
                brokenArbiters.ConcurrentAdd(h);
            }
        }
    }

    private void HandleDeferredArbiters()
    {
        for (int i = 0; i < deferredArbiters.Count; i++)
        {
            Arbiter arb = deferredArbiters[i];
            IslandHelper.ArbiterCreated(islands, arb);

            AddToActiveList(arb.Body1.InternalIsland);
            AddToActiveList(arb.Body2.InternalIsland);

            arb.Body1.RaiseBeginCollide(arb);
            arb.Body2.RaiseBeginCollide(arb);
        }

        deferredArbiters.Clear();
    }

    /// <summary>
    /// Attempts to lock two bodies. Briefly waits on contention, then backs off if unsuccessful.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryLockTwoBody(ref RigidBodyData b1, ref RigidBodyData b2)
    {
        const int spinCount = 10;

        if (Unsafe.IsAddressGreaterThan(ref b1, ref b2))
        {
            if (b1.MotionType == MotionType.Dynamic)
            {
                if (Interlocked.CompareExchange(ref b1._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(spinCount);
                    if (Interlocked.CompareExchange(ref b1._lockFlag, 1, 0) != 0)
                    {
                        return false;
                    }
                }
            }

            if (b2.MotionType == MotionType.Dynamic)
            {
                if (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(spinCount);
                    if (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                    {
                        // back off
                        Volatile.Write(ref b1._lockFlag, 0);
                        return false;
                    }
                }
            }
        }
        else
        {
            if (b2.MotionType == MotionType.Dynamic)
            {
                if (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(spinCount);
                    if (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                    {
                        return false;
                    }
                }
            }

            if (b1.MotionType == MotionType.Dynamic)
            {
                if (Interlocked.CompareExchange(ref b1._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(spinCount);
                    if (Interlocked.CompareExchange(ref b1._lockFlag, 1, 0) != 0)
                    {
                        // back off
                        Volatile.Write(ref b2._lockFlag, 0);
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Spin-wait loop to prevent accessing a body from multiple threads.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LockTwoBody(ref RigidBodyData b1, ref RigidBodyData b2)
    {
        if (Unsafe.IsAddressGreaterThan(ref b1, ref b2))
        {
            if (b1.MotionType == MotionType.Dynamic)
                while (Interlocked.CompareExchange(ref b1._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(10);
                }

            if (b2.MotionType == MotionType.Dynamic)
                while (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(10);
                }
        }
        else
        {
            if (b2.MotionType == MotionType.Dynamic)
                while (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(10);
                }

            if (b1.MotionType == MotionType.Dynamic)
                while (Interlocked.CompareExchange(ref b1._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(10);
                }
        }
    }

    /// <summary>
    /// Spin-wait loop to prevent accessing a body from multiple threads.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnlockTwoBody(ref RigidBodyData b1, ref RigidBodyData b2)
    {
        if (Unsafe.IsAddressGreaterThan(ref b1, ref b2))
        {
            if (b2.MotionType == MotionType.Dynamic) Interlocked.Decrement(ref b2._lockFlag);
            if (b1.MotionType == MotionType.Dynamic) Interlocked.Decrement(ref b1._lockFlag);
        }
        else
        {
            if (b1.MotionType == MotionType.Dynamic) Interlocked.Decrement(ref b1._lockFlag);
            if (b2.MotionType == MotionType.Dynamic) Interlocked.Decrement(ref b2._lockFlag);
        }
    }

    private void IntegrateForces(Parallel.Batch batch)
    {
        var span = memRigidBodies.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref RigidBodyData rigidBody = ref span[i];
            if (rigidBody.MotionType != MotionType.Dynamic) continue;

            rigidBody.AngularVelocity += rigidBody.DeltaAngularVelocity;
            rigidBody.Velocity += rigidBody.DeltaVelocity;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JVector SolveGyroscopic(in JMatrix inertiaWorld, in JVector omega, Real dt)
    {
        // The equation which we solve for ω_{n+1} in this method with a single Newton iteration:
        // I_{n+1}(ω_{n+1} - ω_{n}) + h ω_{n+1} x (I_{n+1}ω_{n+1})=0

        // Based on Erin Catto’s “Numerical Methods” (GDC 2015) slides.
        // Catto integrates the gyroscopic term in **body space** where the inertia
        // tensor I_b is constant, then transforms the updated angular velocity ω′
        // back to world space with the *previous* orientation R_n.

        // In this implementation, we keep only the world-space inverse inertia, so
        // we solve the same implicit equation directly in **world space** using
        //   I_w = R_n I_b R_nᵀ (assembled from the orientation at t_n).
        //
        // The two approaches are algebraically equivalent:           -
        //   • Catto:  keep I_b fixed, rotate ω′ with R_n             |
        //   • Here:   keep I_w fixed (= R_n I_b R_nᵀ) while solving  |
        // Both introduce the same first-order O(h) approximation - either “freeze”
        // the inertia tensor (our method) or rotate ω′ with an orientation that is one
        // step out of date (Catto).

        JVector f = dt * (omega % JVector.Transform(omega, inertiaWorld));

        JMatrix jacobian = inertiaWorld + dt * (JMatrix.CreateCrossProduct(omega) * inertiaWorld -
                                               JMatrix.CreateCrossProduct(JVector.Transform(omega, inertiaWorld)));

        if (!JMatrix.Inverse(jacobian, out var invJacobian)) return omega;

        return omega - JVector.Transform(f, invJacobian);
    }

    private void Integrate(Parallel.Batch batch)
    {
        var span = memRigidBodies.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref RigidBodyData rigidBody = ref span[i];

            // only dynamic and kinematic objects have a velocity
            if(rigidBody.MotionType == MotionType.Static) continue;

            JVector linearVelocity = rigidBody.Velocity;
            JVector angularVelocity = rigidBody.AngularVelocity;

            rigidBody.Position += linearVelocity * substepDt;

            JQuaternion quat = MathHelper.RotationQuaternion(angularVelocity, substepDt);
            rigidBody.Orientation = JQuaternion.Normalize(quat * rigidBody.Orientation);

            if (!rigidBody.EnableGyroscopicForces) continue;

            // Note: We do not perform a symplectic Euler update here (i.e., we calculate the new orientation
            // from the *old* angular velocity), since the gyroscopic term does introduce instabilities.
            // We handle the gyroscopic term with implicit Euler. This is known as the symplectic splitting method.
            JMatrix.Inverse(rigidBody.InverseInertiaWorld, out var inertiaWorld);
            rigidBody.AngularVelocity = SolveGyroscopic(inertiaWorld, angularVelocity, substepDt);
        }
    }

    private void RelaxVelocities(bool multiThread, int iterations)
    {
        if (multiThread)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                memContacts.ParallelForBatch(64, relaxVelocities);
            }
        }
        else
        {
            var batchContacts = new Parallel.Batch(0, memContacts.Active.Length);

            for (int iter = 0; iter < iterations; iter++)
            {
                RelaxVelocities(batchContacts);
            }
        }
    }

    private void Solve(bool multiThread, int iterations)
    {
        if (multiThread)
        {
            memContacts.ParallelForBatch(64, prepareContacts, false);
            memConstraints.ParallelForBatch(64, prepareConstraints, false);
            memSmallConstraints.ParallelForBatch(64, prepareSmallConstraints, false);

            ThreadPool.Instance.Execute();

            for (int iter = 0; iter < iterations; iter++)
            {
                memContacts.ParallelForBatch(64, iterateContacts, false);
                memConstraints.ParallelForBatch(64, iterateConstraints, false);
                memSmallConstraints.ParallelForBatch(64, iterateSmallConstraints, false);

                ThreadPool.Instance.Execute();
            }
        }
        else
        {
            Parallel.Batch batchContacts = new(0, memContacts.Active.Length);
            Parallel.Batch batchConstraints = new(0, memConstraints.Active.Length);
            Parallel.Batch batchSmallConstraints = new(0, memSmallConstraints.Active.Length);

            PrepareContacts(batchContacts);
            PrepareConstraints(batchConstraints);
            PrepareSmallConstraints(batchSmallConstraints);

            for (int iter = 0; iter < iterations; iter++)
            {
                IterateContacts(batchContacts);
                IterateConstraints(batchConstraints);
                IterateSmallConstraints(batchSmallConstraints);
            }
        }
    }

    private void UpdateContacts(bool multiThread)
    {
        if (multiThread)
        {
            memContacts.ParallelForBatch(256, updateContacts);
        }
        else
        {
            Parallel.Batch batch = new(0, memContacts.Active.Length);
            UpdateContacts(batch);
        }
    }

    private void IntegrateForces(bool multiThread)
    {
        if (multiThread)
        {
            memRigidBodies.ParallelForBatch(256, integrateForces);
        }
        else
        {
            IntegrateForces(new Parallel.Batch(0, memRigidBodies.Active.Length));
        }
    }

    private void Integrate(bool multiThread)
    {
        if (multiThread)
        {
            memRigidBodies.ParallelForBatch(256, integrate);
        }
        else
        {
            Integrate(new Parallel.Batch(0, memRigidBodies.Active.Length));
        }
    }

    private readonly Stack<Island> inactivateIslands = new();

    private void CheckDeactivation()
    {
        for (int i = 0; i < islands.ActiveCount; i++)
        {
            Island island = islands[i];

            bool deactivateIsland = !island.MarkedAsActive;
            if (!AllowDeactivation) deactivateIsland = false;

            // Mark the island as inactive
            // Next frame one active body will be enough to set
            // MarkedAsActive back to true;
            island.MarkedAsActive = false;

            bool needsUpdate = island.NeedsUpdate;
            island.NeedsUpdate = false;

            if (!deactivateIsland && !needsUpdate) continue;

            foreach (RigidBody body in island.InternalBodies)
            {
                ref RigidBodyData rigidBody = ref body.Data;

                if (rigidBody.IsActive != deactivateIsland)
                {
                    if (!needsUpdate) break;
                    continue;
                }

                if (deactivateIsland)
                {
                    rigidBody.IsActive = false;

                    memRigidBodies.MoveToInactive(body.Handle);
                    bodies.MoveToInactive(body);

                    // Static bodies have contacts and constraints, but they do not form
                    // collision islands. Do not deactivate contacts or constraint
                    // of static bodies, as the island of the static body goes to sleep.
                    if (body.MotionType != MotionType.Static)
                    {
                        foreach (var c in body.InternalContacts)
                        {
                            memContacts.MoveToInactive(c.Handle);
                        }

                        foreach (var c in body.InternalConstraints)
                        {
                            if (c.IsSmallConstraint)
                            {
                                memSmallConstraints.MoveToInactive(c.SmallHandle);
                            }
                            else
                            {
                                memConstraints.MoveToInactive(c.Handle);
                            }
                        }
                    }

                    foreach (var s in body.InternalShapes)
                    {
                        DynamicTree.DeactivateProxy(s);
                    }
                }
                else
                {
                    // Don't activate static bodies at all.
                    if (rigidBody.MotionType == MotionType.Static) continue;

                    rigidBody.IsActive = true;

                    body.InternalSleepTime = 0;

                    memRigidBodies.MoveToActive(body.Handle);
                    bodies.MoveToActive(body);

                    foreach (var c in body.InternalContacts)
                    {
                        memContacts.MoveToActive(c.Handle);
                    }

                    foreach (var c in body.InternalConstraints)
                    {
                        if (c.IsSmallConstraint)
                        {
                            memSmallConstraints.MoveToActive(c.SmallHandle);
                        }
                        else
                        {
                            memConstraints.MoveToActive(c.Handle);
                        }
                    }

                    foreach (var s in body.InternalShapes)
                    {
                        DynamicTree.ActivateProxy(s);
                    }
                }
            }

            if (deactivateIsland)
            {
                inactivateIslands.Push(island);
            }
        }

        while (inactivateIslands.Count > 0)
        {
            islands.MoveToInactive(inactivateIslands.Pop());
        }
    }
}