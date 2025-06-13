/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
    /// <param name="multiThread">Indicates whether multithreading should be utilized. The behavior of the engine can be modified using <see cref="Parallelization.ThreadPool.Instance"/>.</param>
    public void Step(Real dt, bool multiThread = true)
    {
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

        substepDt = dt / substeps;
        stepDt = dt;

        if (multiThread)
        {
            // Signal the thread pool to spin up threads
            ThreadPool.Instance.SignalWait();
        }

        // Start timer
        time = Stopwatch.GetTimestamp();

        PreStep?.Invoke(dt);
        SetTime(Timings.PreStep);

        // Perform narrow phase detection.
        DynamicTree.EnumerateOverlaps(detect, multiThread);
        SetTime(Timings.NarrowPhase);

        HandleDeferredArbiters();
        SetTime(Timings.AddArbiter);

        CheckDeactivation();
        SetTime(Timings.CheckDeactivation);

        // Sub-stepping
        for (int i = 0; i < substeps; i++)
        {
            IntegrateForces(multiThread);                       // FAST SWEEP
            Solve(multiThread, solverIterations);               // FAST SWEEP
            Integrate(multiThread);                             // FAST SWEEP
            RelaxVelocities(multiThread, velocityRelaxations);  // FAST SWEEP
        }

        SetTime(Timings.Solve);

        RemoveBrokenArbiters();
        SetTime(Timings.RemoveArbiter);

        UpdateContacts(multiThread);                            // FAST SWEEP
        SetTime(Timings.UpdateContacts);

        ForeachActiveBody(multiThread);
        SetTime(Timings.UpdateBodies);

        DynamicTree.Update(multiThread, stepDt);
        SetTime(Timings.BroadPhase);

        PostStep?.Invoke(dt);
        SetTime(Timings.PostStep);

        if ((ThreadModel == ThreadModelType.Regular || !multiThread)
            && ThreadPool.InstanceInitialized)
        {
            // Signal the thread pool that threads can go into a wait state.
            ThreadPool.Instance.SignalReset();
        }
    }

    private void UpdateBodies(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            bodies[i].Update(stepDt, substepDt);
        }
    }

    private void PrepareContacts(Parallel.Batch batch)
    {
        Real invStepDt = (Real)1.0 / stepDt;

        var span = memContacts.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];
            ref RigidBodyData b1 = ref c.Body1.Data;
            ref RigidBodyData b2 = ref c.Body2.Data;

            LockTwoBody(ref b1, ref b2);

            // Why step_dt and not substep_dt?
            // The contact uses the time to calculate the bias from dt:
            // bias = bias_factor x constraint_error / dt
            // The contact is solved in such a way that the contact points
            // move with 'bias' velocity along their normal after solving.
            // Since collision detection is happening at a rate of step_dt
            // and not substep_dt the penetration magnitude can be large.
            c.PrepareForIteration(invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private unsafe void PrepareSmallConstraints(Parallel.Batch batch)
    {
        Real invStepDt = (Real)1.0 / stepDt;

        var span = memSmallConstraints.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.PrepareForIteration == null) continue;

            Debug.Assert(!b1.IsStatic || !b2.IsStatic);

            LockTwoBody(ref b1, ref b2);
            constraint.PrepareForIteration(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private unsafe void IterateSmallConstraints(Parallel.Batch batch)
    {
        Real invStepDt = (Real)1.0 / stepDt;

        var span = memSmallConstraints.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.Iterate == null) continue;

            LockTwoBody(ref b1, ref b2);
            constraint.Iterate(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private unsafe void PrepareConstraints(Parallel.Batch batch)
    {
        Real invStepDt = (Real)1.0 / stepDt;

        var span = memConstraints.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.PrepareForIteration == null) continue;

            Debug.Assert(!b1.IsStatic || !b2.IsStatic);

            LockTwoBody(ref b1, ref b2);
            constraint.PrepareForIteration(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private unsafe void IterateConstraints(Parallel.Batch batch)
    {
        Real invStepDt = (Real)1.0 / stepDt;

        var span = memConstraints.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.Iterate == null) continue;

            LockTwoBody(ref b1, ref b2);
            constraint.Iterate(ref constraint, invStepDt);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private void IterateContacts(Parallel.Batch batch)
    {
        var span = memContacts.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];
            ref RigidBodyData b1 = ref c.Body1.Data;
            ref RigidBodyData b2 = ref c.Body2.Data;

            LockTwoBody(ref b1, ref b2);
            c.Iterate(true);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private void RelaxVelocities(Parallel.Batch batch)
    {
        var span = memContacts.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];
            ref RigidBodyData b1 = ref c.Body1.Data;
            ref RigidBodyData b2 = ref c.Body2.Data;

            LockTwoBody(ref b1, ref b2);
            c.Iterate(false);
            UnlockTwoBody(ref b1, ref b2);
        }
    }

    private void AssertNullBody()
    {
        ref RigidBodyData rigidBody = ref NullBody.Data;
        Debug.Assert(rigidBody.IsStatic);
        Debug.Assert(rigidBody.InverseMass < Real.Epsilon);
        Debug.Assert(MathHelper.UnsafeIsZero(ref rigidBody.InverseInertiaWorld));
    }

    private void ForeachActiveBody(bool multiThread)
    {
#if DEBUG
        foreach (var body in bodies)
        {
            if (body.IsStatic)
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
    /// Spin-wait loop to prevent accessing a body from multiple threads.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LockTwoBody(ref RigidBodyData b1, ref RigidBodyData b2)
    {
        if (Unsafe.IsAddressGreaterThan(ref b1, ref b2))
        {
            if (!b1.IsStatic)
                while (Interlocked.CompareExchange(ref b1._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(10);
                }

            if (!b2.IsStatic)
                while (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(10);
                }
        }
        else
        {
            if (!b2.IsStatic)
                while (Interlocked.CompareExchange(ref b2._lockFlag, 1, 0) != 0)
                {
                    Thread.SpinWait(10);
                }

            if (!b1.IsStatic)
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
            if (!b2.IsStatic) Interlocked.Decrement(ref b2._lockFlag);
            if (!b1.IsStatic) Interlocked.Decrement(ref b1._lockFlag);
        }
        else
        {
            if (!b1.IsStatic) Interlocked.Decrement(ref b1._lockFlag);
            if (!b2.IsStatic) Interlocked.Decrement(ref b2._lockFlag);
        }
    }

    private void IntegrateForces(Parallel.Batch batch)
    {
        var span = memRigidBodies.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref RigidBodyData rigidBody = ref span[i];
            if (rigidBody.IsStaticOrInactive) continue;

            rigidBody.AngularVelocity += rigidBody.DeltaAngularVelocity;
            rigidBody.Velocity += rigidBody.DeltaVelocity;
        }
    }

    private void Integrate(Parallel.Batch batch)
    {
        var span = memRigidBodies.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref RigidBodyData rigidBody = ref span[i];

            // Static bodies may also have a velocity which gets integrated.
            // if (rigidBody.IsStatic) continue;

            JVector linearVelocity = rigidBody.Velocity;
            JVector angularVelocity = rigidBody.AngularVelocity;

            rigidBody.Position += linearVelocity * substepDt;

            JQuaternion quat = MathHelper.RotationQuaternion(angularVelocity, substepDt);
            rigidBody.Orientation = JQuaternion.Normalize(quat * rigidBody.Orientation);
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

            if (!deactivateIsland && !island.NeedsUpdate) continue;

            island.NeedsUpdate = false;

            foreach (RigidBody body in island.InternalBodies)
            {
                ref RigidBodyData rigidBody = ref body.Data;

                if (rigidBody.IsActive != deactivateIsland) continue;

                if (deactivateIsland)
                {
                    rigidBody.IsActive = false;

                    memRigidBodies.MoveToInactive(body.Handle);
                    bodies.MoveToInactive(body);

                    if (!body.Data.IsStatic)
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
                    if (rigidBody.IsStatic) continue;

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