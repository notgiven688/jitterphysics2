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
using Jitter2.Collision.Shapes;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Parallelization;
using Jitter2.UnmanagedMemory;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2;

public partial class World
{
    // Note: A SlimBag of the reference type 'Arbiter' does not introduce GC problems (not setting
    // all elements to null when clearing) since the references for Arbiters are pooled anyway.
    private readonly SlimBag<Arbiter> deferredArbiters = new();
    private readonly SlimBag<JHandle<ContactData>> brokenArbiters = new();

    private Action<Parallel.Batch> integrate;
    private Action<Parallel.Batch> integrateForces;
    private Action<Parallel.Batch> updateBodies;
    private Action<Parallel.Batch> updateBoundingBoxes;
    private Action<Parallel.Batch> detectCollisions;

    private Action<Parallel.ColoredBatch> prepareContacts;
    private Action<Parallel.ColoredBatch> iterateContacts;
    private Action<Parallel.ColoredBatch> relaxVelocities;
    private Action<Parallel.ColoredBatch> updateContacts;
    private Action<Parallel.ColoredBatch> prepareConstraints;
    private Action<Parallel.ColoredBatch> iterateConstraints;
    private Action<Parallel.ColoredBatch> prepareSmallConstraints;
    private Action<Parallel.ColoredBatch> iterateSmallConstraints;

    private void InitParallelCallbacks()
    {
        integrate = IntegrateCallback;
        integrateForces = IntegrateForcesCallback;
        prepareContacts = PrepareContactsCallback;
        iterateContacts = IterateContactsCallback;
        relaxVelocities = RelaxVelocitiesCallback;
        prepareConstraints = PrepareConstraintsCallback;
        iterateConstraints = IterateConstraintsCallback;
        prepareSmallConstraints = PrepareSmallConstraintsCallback;
        iterateSmallConstraints = IterateSmallConstraintsCallback;
        updateContacts = UpdateContactsCallback;
        updateBodies = UpdateBodiesCallback;
        updateBoundingBoxes = UpdateBoundingBoxesCallback;
        detectCollisions = DetectCollisionsCallback;
    }

    public enum Timings
    {
        UpdateBodies,
        CollisionDetect1,
        CollisionDetect2,
        RemoveArbiter,
        AddArbiter,
        SolveContacts,
        UpdateContacts,
        Integrate,
        TrimPotentialPairs,
        CheckDeactivation,
        Last
    }

    /// <summary>
    /// Contains timings for the stages of the last call to <see cref="World.Step(float, bool)"/>.
    /// Array elements correspond to the enums in <see cref="Timings"/>. Can be used to identify
    /// bottlenecks.
    /// </summary>
    public double[] DebugTimings { get; } = new double[(int)Timings.Last];

    /// <summary>
    /// Performs a single simulation step.
    /// </summary>
    /// <param name="dt">The duration of time to simulate. This should remain fixed and not exceed 1/60 of a second.</param>
    /// <param name="multiThread">Indicates whether multithreading should be utilized. The behavior of the engine can be modified using <see cref="Parallelization.ThreadPool.Instance"/>.</param>
    public void Step(float dt, bool multiThread = true)
    {
        AssertNullBody();

        if (dt < 0.0f)
        {
            throw new ArgumentException("Time step cannot be negative.", nameof(dt));
        }

        if (dt == 0.0f) return; // nothing to do

        long time = Stopwatch.GetTimestamp();
        double invFrequency = 1.0d / Stopwatch.Frequency;

        void SetTime(Timings type)
        {
            long ctime = Stopwatch.GetTimestamp();
            double delta = (ctime - time) * 1000.0d;
            DebugTimings[(int)type] = delta * invFrequency;
            time = ctime;
        }

        int ssp1 = substeps;
        substep_dt = dt / ssp1;
        step_dt = dt;

        SetTime(Timings.Integrate);

        // Signal the thread pool to spin up threads
        if (multiThread)
        {
            ThreadPool.Instance.SignalWait();
        }

        PreStep?.Invoke(dt);

        SetTime(Timings.Integrate);

        DetectCollisions(multiThread);

        SetTime(Timings.CollisionDetect1);

        HandleDeferredArbiters();
        SetTime(Timings.AddArbiter);

        CheckDeactivation();
        SetTime(Timings.CheckDeactivation);

        // substep_dt = -dt;
        // Integrate(multiThread);

        // Add the new arbiters to their respective rigid body.

        // Go through potential pairs in the collision system and remove
        // pairs which are inactive. This speeds up the enumeration of all
        // collisions of interest.
        DynamicTree.TrimInactivePairs();

        SetTime(Timings.TrimPotentialPairs);

        // Sub-stepping
        // TODO: comment...
        // -> prepare for iteration does calculate new positions, but only linear
        // -> inertia is not transformed in the substeps.
        //
        for (int i = 0; i < ssp1; i++)
        {
            // we need to apply the forces each substep. we can not apply
            // them all at once since this would mess with the warm starting
            // of the solver
            IntegrateForces(multiThread); // FAST SWEEP
            Solve(multiThread, solverIterations); // FAST SWEEP
            Integrate(multiThread); // FAST SWEEP
            RelaxVelocities(multiThread, velocityRelaxations); // FAST SWEEP
        }

        SetTime(Timings.SolveContacts);

        RemoveBrokenArbiters();
        SetTime(Timings.RemoveArbiter);

        UpdateContacts(multiThread); // FAST SWEEP
        SetTime(Timings.UpdateContacts);

        // substep_dt = +dt;
        // Integrate(multiThread);
        ForeachActiveBody(multiThread);
        ForeachActiveShape(multiThread);
        SetTime(Timings.UpdateBodies);

        // Perform collision detection.
        // In the callback:
        // If both bodies are static we do nothing.
        // If both bodies are inactive we do nothing.
        // We perform narrow phase detection.
        // New arbiters are added to deferredArbiters
        DynamicTree.Update(multiThread);
        SetTime(Timings.CollisionDetect2);

        PostStep?.Invoke(dt);

        // Signal the thread pool that threads can go into a wait state.
        if ((ThreadModel == ThreadModelType.Regular || !multiThread)
            && ThreadPool.InstanceInitialized)
        {
            ThreadPool.Instance.SignalReset();
        }
    }

    private void UpdateBoundingBoxesCallback(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            var proxy = DynamicTree.Proxies[i];
            if (proxy is IUpdatableBoundingBox sh) sh.UpdateWorldBoundingBox(step_dt);
        }
    }

    private void UpdateBodiesCallback(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            RigidBody body = bodies[i];
            ref RigidBodyData rigidBody = ref body.Data;

            if (rigidBody.AngularVelocity.LengthSquared() < body.inactiveThresholdAngularSq &&
                rigidBody.Velocity.LengthSquared() < body.inactiveThresholdLinearSq)
            {
                body.sleepTime += step_dt;
            }
            else
            {
                body.sleepTime = 0;
            }

            if (body.sleepTime < body.deactivationTimeThreshold)
            {
                body.island.MarkedAsActive = true;
            }

            if (!rigidBody.IsStatic && rigidBody.IsActive)
            {
                rigidBody.AngularVelocity *= body.angularDampingMultiplier;
                rigidBody.Velocity *= body.linearDampingMultiplier;

                rigidBody.DeltaVelocity = body.Force * rigidBody.InverseMass * substep_dt;
                rigidBody.DeltaAngularVelocity = JVector.Transform(body.Torque, rigidBody.InverseInertiaWorld) * substep_dt;

                if (body.AffectedByGravity)
                {
                    rigidBody.DeltaVelocity += gravity * substep_dt;
                }

                body.Force = JVector.Zero;
                body.Torque = JVector.Zero;

                var bodyOrientation = JMatrix.CreateFromQuaternion(rigidBody.Orientation);

                JMatrix.Multiply(bodyOrientation, body.inverseInertia, out rigidBody.InverseInertiaWorld);
                JMatrix.MultiplyTransposed(rigidBody.InverseInertiaWorld, bodyOrientation, out rigidBody.InverseInertiaWorld);

                rigidBody.InverseMass = body.inverseMass;
            }
        }
    }

    private void PrepareContactsCallback(Parallel.ColoredBatch batch)
    {
        float istep_dt = 1.0f / step_dt;

        var span = memContacts.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];


            // Why step_dt and not substep_dt?
            // The contact uses the time to calculate the bias from dt:
            // bias = bias_factor x constraint_error / dt
            // The contact is solved in such a way that the contact points
            // move with 'bias' velocity along their normal after solving.
            // Since collision detection is happening at a rate of step_dt
            // and not substep_dt the penetration magnitude can be large.
            c.PrepareForIteration(istep_dt);
        }
    }

    private unsafe void PrepareSmallConstraintsCallback(Parallel.ColoredBatch batch)
    {
        float istep_dt = 1.0f / step_dt;

        var span = memSmallConstraints.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.PrepareForIteration == null) continue;

            Debug.Assert(!b1.IsStatic || !b2.IsStatic);

            constraint.PrepareForIteration(ref constraint, istep_dt);
        }
    }

    private unsafe void IterateSmallConstraintsCallback(Parallel.ColoredBatch batch)
    {
        float istep_dt = 1.0f / step_dt;

        var span = memSmallConstraints.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref SmallConstraintData constraint = ref span[i];
            ref RigidBodyData b1 = ref constraint.Body1.Data;
            ref RigidBodyData b2 = ref constraint.Body2.Data;

            if (constraint.Iterate == null) continue;

            constraint.Iterate(ref constraint, istep_dt);
        }
    }

    private unsafe void PrepareConstraintsCallback(Parallel.ColoredBatch batch)
    {
        float istep_dt = 1.0f / step_dt;

        var span = memConstraints.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ConstraintData constraint = ref span[i];
            if (constraint.PrepareForIteration == null) continue;
            constraint.PrepareForIteration(ref constraint, istep_dt);
        }
    }

    private unsafe void IterateConstraintsCallback(Parallel.ColoredBatch batch)
    {
        float istep_dt = 1.0f / step_dt;

        var span = memConstraints.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ConstraintData constraint = ref span[i];
            if (constraint.Iterate == null) continue;
            constraint.Iterate(ref constraint, istep_dt);
        }
    }

    private void IterateContactsCallback(Parallel.ColoredBatch batch)
    {
        var span = memContacts.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];
            c.Iterate(true);
        }
    }


    private void RelaxVelocitiesCallback(Parallel.ColoredBatch batch)
    {
        var span = memContacts.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref ContactData c = ref span[i];
            c.Iterate(false);
        }
    }


    private void DetectCollisionsCallback(Parallel.Batch batch)
    {
        PairHashSet phs = DynamicTree.PotentialPairs;

        for (int e = batch.Start; e < batch.End; e++)
        {
            var node = phs.Slots[e];
            if (node.ID == 0) continue;

            var proxyA = DynamicTree.Nodes[node.ID1].Proxy;
            var proxyB = DynamicTree.Nodes[node.ID2].Proxy;

            if (!proxyA.WorldBoundingBox.Disjoint(proxyB.WorldBoundingBox))
            {
                Detect(proxyA, proxyB);
            }
        }
    }

    private void AssertNullBody()
    {
        ref RigidBodyData rigidBody = ref NullBody.Data;
        Debug.Assert(rigidBody.IsStatic);
        Debug.Assert(rigidBody.InverseMass == 0.0f);
        Debug.Assert(MathHelper.UnsafeIsZero(rigidBody.InverseInertiaWorld));
    }

    private void ForeachActiveShape(bool multiThread)
    {
        if (multiThread)
        {
            DynamicTree.Proxies.ParallelForBatch(256, updateBoundingBoxes);
        }
        else
        {
            Parallel.Batch batch = new(0, DynamicTree.Proxies.Active);
            UpdateBoundingBoxesCallback(batch);
        }
    }

    private void ForeachActiveBody(bool multiThread)
    {
#if DEBUG
        foreach (var body in bodies)
        {
            if (body.IsStatic)
            {
                System.Diagnostics.Debug.Assert(MathHelper.UnsafeIsZero(body.Data.InverseInertiaWorld));
                System.Diagnostics.Debug.Assert(body.Data.InverseMass == 0.0f);
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
            UpdateBodiesCallback(batch);
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

                AddToActiveList(arb.Body1.island);
                AddToActiveList(arb.Body2.island);

                UnColor(ref arb.Body1.Data, ref arb.Body2.Data, memContacts.GetColor(handle));

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

    private void UpdateContactsCallback(Parallel.ColoredBatch batch)
    {
        var span = memContacts.Active(batch.Color)[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            // get rid of broken contacts
            ref ContactData cq = ref span[i];
            cq.UpdatePosition();

            if ((cq.UsageMask & ContactData.MaskContactAll) == 0)
            {
                var h = memContacts.GetHandle(ref cq);
                lock (brokenArbiters)
                {
                    brokenArbiters.Add(h);
                }
            }
        }
    }

    private void HandleDeferredArbiters()
    {
        for (int i = 0; i < deferredArbiters.Count; i++)
        {
            Arbiter arb = deferredArbiters[i];
            IslandHelper.ArbiterCreated(islands, arb);

            AddToActiveList(arb.Body1.island);
            AddToActiveList(arb.Body2.island);

            arb.Body1.RaiseBeginCollide(arb);
            arb.Body2.RaiseBeginCollide(arb);
        }

        deferredArbiters.Clear();
    }

    private void IntegrateForcesCallback(Parallel.Batch batch)
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

    private void IntegrateCallback(Parallel.Batch batch)
    {
        var span = memRigidBodies.Active[batch.Start..batch.End];

        for (int i = 0; i < span.Length; i++)
        {
            ref RigidBodyData rigidBody = ref span[i];

            if (rigidBody.IsStatic) continue;

            JVector lvel = rigidBody.Velocity;
            JVector avel = rigidBody.AngularVelocity;

            rigidBody.Position += lvel * substep_dt;

            float angle = avel.Length();
            JVector axis;

            if (angle < 0.001f)
            {
                // use Taylor's expansions of sync function
                // axis = body.angularVelocity * (0.5f * timestep - (timestep * timestep * timestep) * (0.020833333333f) * angle * angle);
                JVector.Multiply(avel,
                    0.5f * substep_dt - substep_dt * substep_dt * substep_dt * 0.020833333333f * angle * angle,
                    out axis);
            }
            else
            {
                // sync(fAngle) = sin(c*fAngle)/t
                JVector.Multiply(avel, (float)Math.Sin(0.5f * angle * substep_dt) / angle, out axis);
            }

            JQuaternion dorn = new(axis.X, axis.Y, axis.Z, (float)Math.Cos(angle * substep_dt * 0.5f));
            //JQuaternion.CreateFromMatrix(rigidBody.Orientation, out JQuaternion ornA);
            JQuaternion ornA = rigidBody.Orientation;

            JQuaternion.Multiply(dorn, ornA, out dorn);

            dorn.Normalize();
            //JMatrix.CreateFromQuaternion(dorn, out rigidBody.Orientation);
            rigidBody.Orientation = dorn;
        }
    }


    private void RelaxVelocities(bool multiThread, int iterations)
    {
        if (multiThread)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
                {
                    int taskThreshold = (color == UnmanagedColoredActiveList<ContactData>.ColorCount - 1) ? int.MaxValue : 64;
                    memContacts.Active(color).ParallelForBatch(taskThreshold, color, relaxVelocities, true);
                }
            }
        }
        else
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
                {
                    RelaxVelocitiesCallback(new (0, memContacts.Active(color).Length, color));
                }
            }
        }
    }


    private void Solve(bool multiThread, int iterations)
    {
        if (multiThread)
        {
            for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
            {
                int taskThreshold = (color == UnmanagedColoredActiveList<ContactData>.ColorCount - 1) ? int.MaxValue : 24;

                memContacts.Active(color).ParallelForBatch(taskThreshold, color, prepareContacts, false);
                memConstraints.Active(color).ParallelForBatch(taskThreshold, color, prepareConstraints, false);
                memSmallConstraints.Active(color).ParallelForBatch(taskThreshold, color, prepareSmallConstraints, false);
                ThreadPool.Instance.Execute();
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
                {
                    int taskThreshold = (color == UnmanagedColoredActiveList<ContactData>.ColorCount - 1) ? int.MaxValue : 24;

                    memContacts.Active(color).ParallelForBatch(taskThreshold, color, iterateContacts, false);
                    memConstraints.Active(color).ParallelForBatch(taskThreshold, color, iterateConstraints, false);
                    memSmallConstraints.Active(color).ParallelForBatch(taskThreshold, color, iterateSmallConstraints, false);
                    ThreadPool.Instance.Execute();
                }
            }
        }
        else
        {
            for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
            {
                PrepareContactsCallback(new (0, memContacts.Active(color).Length, color));
                PrepareConstraintsCallback(new (0, memConstraints.Active(color).Length, color));
                PrepareSmallConstraintsCallback(new (0, memSmallConstraints.Active(color).Length, color));
            }

            for (int iter = 0; iter < iterations; iter++)
            {
                for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
                {
                    IterateContactsCallback(new (0, memContacts.Active(color).Length, color));
                    IterateConstraintsCallback(new (0, memConstraints.Active(color).Length, color));
                    IterateSmallConstraintsCallback(new (0, memSmallConstraints.Active(color).Length, color));
                }
            }
        }
    }

    private void UpdateContacts(bool multiThread)
    {
        if (multiThread)
        {
            for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
            {
                int taskThreshold = (color == UnmanagedColoredActiveList<ContactData>.ColorCount - 1) ? int.MaxValue : 256;
                memContacts.Active(color).ParallelForBatch(taskThreshold, color, updateContacts, true);
            }
        }
        else
        {
            for (ushort color = 0; color < UnmanagedColoredActiveList<ContactData>.ColorCount; color++)
            {
                UpdateContactsCallback(new (0, memContacts.Active(color).Length, color));
            }
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
            IntegrateForcesCallback(new Parallel.Batch(0, memRigidBodies.Active.Length));
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
            IntegrateCallback(new Parallel.Batch(0, memRigidBodies.Active.Length));
        }
    }

    private void DetectCollisions(bool multiThread)
    {
        if (multiThread)
        {
            DynamicTree.PotentialPairs.Slots.ParallelForBatch<PairHashSet.Pair>(1024, detectCollisions);
        }
        else
        {
            DetectCollisionsCallback(new Parallel.Batch(0, DynamicTree.PotentialPairs.Slots.Length));
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

            foreach (RigidBody body in island.bodies)
            {
                ref RigidBodyData rigidBody = ref body.Data;

                if (rigidBody.IsActive != deactivateIsland) continue;

                if (deactivateIsland)
                {
                    rigidBody.IsActive = false;

                    memRigidBodies.MoveToInactive(body.handle);
                    bodies.MoveToInactive(body);

                    if (!body.Data.IsStatic)
                    {
                        foreach (var c in body.contacts)
                        {
                            memContacts.MoveToInactive(c.Handle);
                        }

                        foreach (var c in body.constraints)
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

                    foreach (var s in body.shapes)
                    {
                        DynamicTree.Deactivate(s);
                    }
                }
                else
                {
                    if (rigidBody.IsStatic) continue;

                    rigidBody.IsActive = true;

                    body.sleepTime = 0;

                    memRigidBodies.MoveToActive(body.handle);
                    bodies.MoveToActive(body);

                    foreach (var c in body.contacts)
                    {
                        memContacts.MoveToActive(c.Handle);
                    }

                    foreach (var c in body.constraints)
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

                    foreach (var s in body.shapes)
                    {
                        DynamicTree.Activate(s);
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