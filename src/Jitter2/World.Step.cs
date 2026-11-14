/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using Jitter2.Collision;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Parallelization;
using Jitter2.Unmanaged;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = System.Object;
#endif

namespace Jitter2;

public sealed partial class World
{
    private static readonly Lock MultiThreadStepLock = new();

    // Note: A SlimBag of the reference type 'Arbiter' does not introduce GC problems (not setting
    // all elements to null when clearing) since the references for Arbiters are pooled anyway.
    private readonly SlimBag<Arbiter> deferredArbiters = [];
    private readonly SlimBag<JHandle<ContactData>> brokenArbiters = [];
    private readonly List<Arbiter> dispatchArbiters = [];
    private readonly List<Arbiter> retiredArbiters = [];

    /// <summary>
    /// Profiling buckets for <see cref="DebugTimings"/>, representing stages of <see cref="Step(Real, bool)"/>.
    /// </summary>
    public enum Timings
    {
        /// <summary>Time spent in <see cref="PreStep"/> callbacks.</summary>
        PreStep,

        /// <summary>Time spent in narrow phase collision detection and contact generation.</summary>
        NarrowPhase,

        /// <summary>Time spent creating deferred arbiters.</summary>
        AddArbiter,

        /// <summary>Time spent reordering contacts (for cache optimization or deterministic results)</summary>
        ReorderContacts,

        /// <summary>Time spent evaluating body deactivation (sleeping).</summary>
        CheckDeactivation,

        /// <summary>Time spent in substeps: force integration, constraint solving, and velocity integration.</summary>
        Solve,

        /// <summary>Time spent removing broken arbiters.</summary>
        RemoveArbiter,

        /// <summary>Time spent updating contact state after solving.</summary>
        UpdateContacts,

        /// <summary>Time spent finalizing body state and broadphase proxy updates.</summary>
        UpdateBodies,

        /// <summary>Time spent updating the dynamic tree (broadphase).</summary>
        BroadPhase,

        /// <summary>Time spent in <see cref="PostStep"/> callbacks.</summary>
        PostStep,

        /// <summary>Sentinel value for array sizing. Not a real timing bucket.</summary>
        Last
    }

    private Action<Parallel.Batch> updateBodies;
    private Action<IDynamicTreeProxy, IDynamicTreeProxy> detect;
    private Action<Parallel.Batch> findCcdHits;

    private void InitParallelCallbacks()
    {
        updateBodies = UpdateBodies;
        detect = Detect;
        findCcdHits = FindCcdHits;
    }

    private readonly double[] debugTimings = new double[(int)Timings.Last];

    /// <summary>
    /// Contains timings for the stages of the last call to <see cref="World.Step(Real, bool)"/>.
    /// Values are in milliseconds. Index using <c>(int)Timings.XYZ</c>.
    /// </summary>
    public ReadOnlySpan<double> DebugTimings => debugTimings;

    /// <summary>
    /// Performs a single simulation step.
    /// </summary>
    /// <param name="dt">The duration of time to simulate in seconds. Should remain fixed and not exceed 1/60 s.</param>
    /// <param name="multiThread">If <see langword="true"/>, uses the internal thread pool for parallel execution.
    /// Set to <see langword="false"/> for single-threaded execution (useful for debugging or platforms without threading).</param>
    /// <remarks>
    /// The step is divided into <see cref="SubstepCount"/> substeps for improved stability.
    /// Do not step or modify this world from another thread while this call is running.
    /// Multithreaded steps in different worlds wait for each other; single-threaded steps can run concurrently.
    /// Step and substep events run on the calling thread. Collision filters may run concurrently
    /// on worker threads when <paramref name="multiThread"/> is true.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="dt"/> is negative or not finite.</exception>
    public void Step(Real dt, bool multiThread = true)
    {
        if (!multiThread)
        {
            StepInternal(dt, false);
            return;
        }

        lock (MultiThreadStepLock)
            StepInternal(dt, true);
    }

    private void StepInternal(Real dt, bool multiThread)
    {
        ThrowIfDisposed();
        AssertNullBody();
        ArgumentCheck.Finite(dt, nameof(dt));

        switch (dt)
        {
            case < (Real)0.0:
                throw new ArgumentException("Time step cannot be negative.", nameof(dt));
            case < Real.Epsilon:
                return; // nothing to do
        }

        try
        {
            Tracer.ProfileBegin(TraceName.Step);

            long time;
            double invFrequency = 1.0d / Stopwatch.Frequency;

            void SetTime(Timings type)
            {
                long ctime = Stopwatch.GetTimestamp();
                double delta = (ctime - time) * 1000.0d;
                debugTimings[(int)type] = delta * invFrequency;
                time = ctime;
            }

            timeStep = new TimeStep(dt, substeps);


            if (multiThread)
            {
                // Signal the thread pool to spin up threads
                ThreadPool.Instance.ResumeWorkers();
            }

            // Start timer
            time = Stopwatch.GetTimestamp();

            Tracer.ProfileScopeBegin();
            PreStep?.Invoke(timeStep);
            Tracer.ProfileScopeEnd(TraceName.PreStep);
            SetTime(Timings.PreStep);

            // Perform narrow phase detection.
            Tracer.ProfileBegin(TraceName.NarrowPhase);
            DynamicTree.EnumerateOverlaps(detect, multiThread);
            DiscoverCcdContacts(multiThread);
            Tracer.ProfileEnd(TraceName.NarrowPhase);
            SetTime(Timings.NarrowPhase);

            Tracer.ProfileBegin(TraceName.AddArbiter);
            HandleDeferredArbiters();
            Tracer.ProfileEnd(TraceName.AddArbiter);
            SetTime(Timings.AddArbiter);

            Tracer.ProfileBegin(TraceName.CheckDeactivation);
            CheckDeactivation();
            Tracer.ProfileEnd(TraceName.CheckDeactivation);
            SetTime(Timings.CheckDeactivation);
            AssertIslandActivationInvariants();

            // An arbiter whose manifold emptied in the previous step is removed only now, after
            // collision detection had its chance to refill it: bodies that stop touching for a
            // single step keep their arbiter and its impulses.
            Tracer.ProfileBegin(TraceName.RemoveArbiter);
            RemoveBrokenArbiters();
            Tracer.ProfileEnd(TraceName.RemoveArbiter);
            SetTime(Timings.RemoveArbiter);
            AssertIslandActivationInvariants();

            Tracer.ProfileBegin(TraceName.Solve);

            // Sub-stepping
            for (int i = 0; i < timeStep.SubstepCount; i++)
            {
                PreSubStep?.Invoke(timeStep);

                // Without substep callbacks nothing can change contacts or joints between substeps.
                // The last substep ends with the contact update of the step.
                bool recolor = i == 0 || PreSubStep != null || PostSubStep != null;
                SolveColored(multiThread, solverIterations, velocityRelaxations, integrate: true, recolor,
                    updateContacts: i == timeStep.SubstepCount - 1);

                PostSubStep?.Invoke(timeStep);
            }

            Tracer.ProfileEnd(TraceName.Solve);
            SetTime(Timings.Solve);

            Tracer.ProfileBegin(TraceName.UpdateBodies);
            ForeachActiveBody(multiThread);
            Tracer.ProfileEnd(TraceName.UpdateBodies);
            SetTime(Timings.UpdateBodies);

            Tracer.ProfileBegin(TraceName.BroadPhase);
            DynamicTree.Update(multiThread, timeStep.StepDt);
            broadphaseStepDt = timeStep.StepDt;
            Tracer.ProfileEnd(TraceName.BroadPhase);
            SetTime(Timings.BroadPhase);

            Tracer.ProfileScopeBegin();
            PostStep?.Invoke(timeStep);
            Tracer.ProfileScopeEnd(TraceName.PostStep);
            SetTime(Timings.PostStep);
        }
        finally
        {
            PauseWorkers(multiThread);
            Tracer.ProfileEnd(TraceName.Step);
        }
    }

    private void PauseWorkers(bool multiThread)
    {
        if ((ThreadModel == ThreadModelType.Regular || !multiThread)
            && ThreadPool.InstanceInitialized)
        {
            // Signal the thread pool that threads can go into a wait state.
            ThreadPool.Instance.PauseWorkers();
        }
    }

    /// <summary>
    /// Adjusts velocities to satisfy existing contacts and constraints without moving the bodies.
    /// </summary>
    /// <param name="dt">The reference timestep in seconds used to scale bias and softness terms.</param>
    /// <param name="solverIterations">The number of solver iterations to execute.</param>
    /// <param name="relaxationIterations">The number of relaxation iterations to execute after solving.</param>
    /// <param name="multiThread">If <see langword="true"/>, uses the internal thread pool for parallel execution.
    /// Multithreaded calls are internally serialized across all worlds because the worker pool is process-wide.</param>
    /// <remarks>
    /// Useful after restoring a saved scene, before resuming <see cref="Step"/>.
    /// Only active contacts and constraints are solved. No new collisions are detected,
    /// forces are not applied, and positions and orientations are left unchanged.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="dt"/> is negative or not finite, <paramref name="solverIterations"/>
    /// is less than 1, or <paramref name="relaxationIterations"/> is negative.
    /// </exception>
    public void Stabilize(Real dt, int solverIterations, int relaxationIterations = 0, bool multiThread = true)
    {
        if (!multiThread)
        {
            StabilizeInternal(dt, solverIterations, relaxationIterations, false);
            return;
        }

        lock (MultiThreadStepLock)
            StabilizeInternal(dt, solverIterations, relaxationIterations, true);
    }

    private void StabilizeInternal(Real dt, int solverIterations, int relaxationIterations, bool multiThread)
    {
        ThrowIfDisposed();
        AssertNullBody();
        ArgumentCheck.Finite(dt, nameof(dt));

        if (dt < (Real)0.0)
        {
            throw new ArgumentException("Time step cannot be negative.", nameof(dt));
        }

        if (solverIterations < 1)
        {
            throw new ArgumentException("Solver iterations cannot be smaller than one.", nameof(solverIterations));
        }

        if (relaxationIterations < 0)
        {
            throw new ArgumentException("Relaxation iterations cannot be smaller than zero.", nameof(relaxationIterations));
        }

        if (dt < Real.Epsilon) return; // nothing to do

        try
        {
            timeStep = new TimeStep(dt, substeps);

            if (multiThread)
            {
                ThreadPool.Instance.ResumeWorkers();
            }

            CheckDeactivation();


            for (int i = 0; i < timeStep.SubstepCount; i++)
            {
                SolveColored(multiThread, solverIterations, relaxationIterations, integrate: false);
            }
        }
        finally
        {
            PauseWorkers(multiThread);
        }
    }

    private void UpdateBodies(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            bodies[i].Update(timeStep);
        }
    }

    private void AssertNullBody()
    {
        ref RigidBodyData rigidBody = ref NullBody.Data;
        Debug.Assert(rigidBody.Position == JVector.Zero, "World.NullBody must remain at the world origin.");
        Debug.Assert(rigidBody.MotionType == MotionType.Static);
        Debug.Assert(rigidBody.InverseInertiaWorld == JSymmetricMatrix.Zero);
        Debug.Assert(rigidBody.InverseMassVector == JVector.Zero);
    }

    private void ForeachActiveBody(bool multiThread)
    {
#if DEBUG
        foreach (var body in bodies)
        {
            if (body.Data.MotionType != MotionType.Dynamic)
            {
                Debug.Assert(body.Data.InverseInertiaWorld == JSymmetricMatrix.Zero);
                Debug.Assert(body.Data.InverseMassVector == JVector.Zero);
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

    // Removes the arbiters whose manifold emptied in the previous step and was not refilled in this one.
    private void RemoveBrokenArbiters()
    {
        foreach (var handle in brokenArbiters)
        {
            if ((handle.Data.UsageMask & ContactData.MaskContactAll) != 0) continue;
            var arb = arbiters[handle.Data.Key];

            WakeIslandNextStep(arb.Body1.InternalIsland);
            WakeIslandNextStep(arb.Body2.InternalIsland);

            IslandHelper.ArbiterRemoved(islands, islandPool, arb);
            arbiters.Remove(handle.Data.Key);

            arb.Body1.RaiseEndCollide(arb);
            arb.Body2.RaiseEndCollide(arb);

            memContacts.Free(handle);
            ReturnArbiter(arb);
        }

        brokenArbiters.Clear();
    }

    private void HandleDeferredArbiters()
    {
        while (deferredArbiters.Count > 0)
        {
            try
            {
                foreach (var arb in deferredArbiters)
                {
                    IslandHelper.ArbiterCreated(islands, islandPool, arb);

                    WakeIslandNextStep(arb.Body1.InternalIsland);
                    WakeIslandNextStep(arb.Body2.InternalIsland);

                    dispatchArbiters.Add(arb);
                }

                // All contacts must be linked and no longer pending before handlers can remove them.
                deferredArbiters.Clear();

                foreach (var arb in dispatchArbiters)
                {
                    // Removed arbiters cannot be reused until this batch finishes, so a zero handle
                    // also invalidates queued notifications, even if a handler creates new contacts.
                    if (!arb.Handle.IsZero) arb.Body1.RaiseBeginCollide(arb);
                    if (!arb.Handle.IsZero) arb.Body2.RaiseBeginCollide(arb);
                }
            }
            finally
            {
                dispatchArbiters.Clear();
                foreach (var arb in retiredArbiters) arbiterPool.Push(arb);
                retiredArbiters.Clear();
            }
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
    private static JVector SolveGyroscopic(in JSymmetricMatrix inertiaWorld, in JVector omega, Real dt)
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
        // The two approaches are algebraically equivalent:
        // - Catto keeps I_b fixed and rotates omega' with R_n.
        // - This method keeps I_w fixed (= R_n I_b R_n^T) while solving.
        // Both introduce the same first-order O(h) approximation: either freeze
        // the inertia tensor or rotate omega' with an orientation that is one step out of date.

        JVector f = dt * (omega % JVector.Transform(omega, inertiaWorld));

        JMatrix inertiaMatrix = inertiaWorld.ToMatrix();
        JMatrix jacobian = inertiaMatrix + dt * (JMatrix.CreateCrossProduct(omega) * inertiaMatrix -
                                               JMatrix.CreateCrossProduct(JVector.Transform(omega, inertiaWorld)));

        if (!JMatrix.Inverse(jacobian, out var invJacobian)) return omega;

        return omega - JVector.Transform(f, invJacobian);
    }

    private void IntegrateVelocities(Parallel.Batch batch)
    {
        var span = memRigidBodies.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref RigidBodyData rigidBody = ref span[i];

            // Only dynamic and kinematic objects have a velocity.
            if (rigidBody.MotionType == MotionType.Static) continue;

            JVector linearVelocity = rigidBody.Velocity;
            JVector angularVelocity = rigidBody.AngularVelocity;

            rigidBody.Position += linearVelocity * timeStep.SubstepDt;

            JQuaternion quat = MathHelper.RotationQuaternion(angularVelocity, timeStep.SubstepDt);
            rigidBody.Orientation = JQuaternion.Normalize(quat * rigidBody.Orientation);

            if (!rigidBody.EnableGyroscopicForces || rigidBody.HasMotionLocks) continue;

            // Note: We do not perform a symplectic Euler update here (i.e., we calculate the new orientation
            // from the *old* angular velocity), since the gyroscopic term does introduce instabilities.
            // We handle the gyroscopic term with implicit Euler. This is known as the symplectic splitting method.
            JSymmetricMatrix.Inverse(rigidBody.InverseInertiaWorld, out var inertiaWorld);
            rigidBody.AngularVelocity = SolveGyroscopic(inertiaWorld, angularVelocity, timeStep.SubstepDt);
        }
    }

    private readonly List<Island> islandsToSleep = [];

    private static void ClearMotionForSleep(RigidBody body)
    {
        ref RigidBodyData rigidBody = ref body.Data;

        rigidBody.Velocity = JVector.Zero;
        rigidBody.AngularVelocity = JVector.Zero;
        rigidBody.DeltaVelocity = JVector.Zero;
        rigidBody.DeltaAngularVelocity = JVector.Zero;

        body.Force = JVector.Zero;
        body.Torque = JVector.Zero;
        body.InternalSleepTime = Real.PositiveInfinity;
    }

    // The two functions below are the only places that change whether a body, its contacts,
    // constraints and proxies are awake. Static bodies never wake.

    private void WakeBody(RigidBody body)
    {
        Debug.Assert(body.MotionType != MotionType.Static);

        body.Data.IsActive = true;
        body.InternalSleepTime = 0;

        memRigidBodies.MoveToActive(body.Handle);
        bodies.MoveToActive(body);

        foreach (var c in body.InternalContacts)
        {
            memContacts.MoveToActive(c.Handle);
        }

        foreach (var c in body.InternalConstraints)
        {
            UpdateConstraintActivation(c);
        }

        foreach (var s in body.InternalShapes)
        {
            DynamicTree.ActivateProxy(s);
        }
    }

    private void SleepBody(RigidBody body)
    {
        body.Data.IsActive = false;

        memRigidBodies.MoveToInactive(body.Handle);
        bodies.MoveToInactive(body);

        // Static bodies have contacts and constraints, but they do not form
        // collision islands. Do not deactivate their contacts or constraints
        // when a connected dynamic island goes to sleep.
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

    private void SleepIsland(Island island)
    {
        bool deactivatedBody = false;

        foreach (RigidBody body in island.InternalBodies)
        {
            deactivatedBody |= body.Data.IsActive && body.MotionType != MotionType.Static;
            SleepBody(body);
        }

        island.WakePending = false;
        islands.MoveToInactive(island);

        if (deactivatedBody) IslandDeactivated?.Invoke(island);
    }

    private void CheckDeactivation()
    {
        for (int i = 0; i < islands.ActiveCount; i++)
        {
            Island island = islands[i];

            // Votes are collected again until the next check.
            bool keepAwake = island.KeepAwake || !AllowDeactivation;
            island.KeepAwake = false;

            if (!keepAwake)
            {
                // Moving the island out of the active partition here would reorder the loop.
                islandsToSleep.Add(island);
                continue;
            }

            if (!island.WakePending) continue;
            island.WakePending = false;

            bool activatedBody = false;

            foreach (RigidBody body in island.InternalBodies)
            {
                if (body.Data.IsActive || body.MotionType == MotionType.Static) continue;
                WakeBody(body);
                activatedBody = true;
            }

            if (activatedBody) IslandActivated?.Invoke(island);
        }

        for (int i = islandsToSleep.Count - 1; i >= 0; i--)
        {
            SleepIsland(islandsToSleep[i]);
        }

        islandsToSleep.Clear();
    }
}
