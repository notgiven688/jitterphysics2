using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterDemo;


/// <summary>
/// A simple implementation of Iterative Speculative Contact Discovery (ISCD).
/// This is more a proof-of-concept than a production-ready solution. There is no multithreading support
/// and the solver is not very efficient. For very few shapes (~10), it works well.
/// </summary>
public class CcdSolver
{
    private const int SelfConsistencyIterations = 4;

    private readonly World world;

    private readonly List<RigidBody> bodies = new();
    private readonly List<IDynamicTreeProxy> overlaps = new();
    private readonly List<Arbiter> discoveredArbiters = new();

    public CcdSolver(World world)
    {
        this.world = world;
        world.PreStep += PreStep;
    }

    public bool Enabled { get; set; } = true;

    private void PreStep(float dt)
    {
        if (!Enabled) return;

        discoveredArbiters.Clear();

        try
        {
            var spanBodies = CollectionsMarshal.AsSpan(bodies);

            for (int iter = 0; iter < SelfConsistencyIterations; iter++)
            {

                // Go through all rigid bodies which have been registered with the ccd-solver.
                for (int i = 0; i < spanBodies.Length; i++)
                {
                    ref var body = ref spanBodies[i];

                    if (body.Handle.IsZero)
                    {
                        throw new InvalidOperationException("RigidBody has been removed from the world, " +
                                                            "but is still registered with the CCD solver.");
                    }

                    foreach (var shape in body.Shapes)
                    {
                        // Find the first future collision among the candidates returned
                        // by the broad phase and register its contact.
                        CreateContact(shape, dt);
                    }
                }

                // The discovery solve starts with zero accumulated impulses. Later
                // impulses are already reflected in body velocities, so preparation
                // only recomputes the contact data.
                for (int i = 0; i < discoveredArbiters.Count; i++)
                {
                    ref var contact = ref discoveredArbiters[i].Handle.Data;
                    contact.SkipWarmStart();
                    contact.PrepareForIteration((float)1.0 / dt);
                }

                for (int i = 0; i < discoveredArbiters.Count; i++)
                {
                    ref var contact = ref discoveredArbiters[i].Handle.Data;
                    contact.Iterate(false);
                }
            }
        }
        finally
        {
            // Discovery impulses are already reflected in body velocities. Keep
            // them for the world solve, but do not replay them on its first prepare.
            for (int i = 0; i < discoveredArbiters.Count; i++)
            {
                ref var contact = ref discoveredArbiters[i].Handle.Data;
                contact.SkipWarmStart();
            }

            discoveredArbiters.Clear();
            overlaps.Clear();
        }
    }

    private void CreateContact(RigidBodyShape shape, float dt)
    {
        JBoundingBox sweptBox = CalculateSweptBoundingBox(shape, dt, out float extentA);
        overlaps.Clear();
        world.DynamicTree.Query(overlaps, sweptBox);

        // Find the candidate which collides with 'shape' at the smallest time of impact (TOI).

        RigidBodyShape otherShape = null!;

        Unsafe.SkipInit(out JVector bestpA);
        Unsafe.SkipInit(out JVector bestpB);
        Unsafe.SkipInit(out JVector bestNormal);

        float smallestToi = float.MaxValue;

        for (int i = 0; i < overlaps.Count; i++)
        {
            if (overlaps[i] is not RigidBodyShape candidate) continue;

            if (candidate.RigidBody == shape.RigidBody) continue;
            if (world.BroadPhaseFilter != null && !world.BroadPhaseFilter.Filter(shape, candidate)) continue;

            ref var data = ref shape.RigidBody.Data;
            ref var pdata = ref candidate.RigidBody.Data;
            float extentB = CalculateAngularExtent(candidate);

            bool success = NarrowPhase.Sweep(shape, candidate, data.Orientation, pdata.Orientation,
                data.Position, pdata.Position, data.Velocity, pdata.Velocity,
                data.AngularVelocity, pdata.AngularVelocity, extentA, extentB,
                out JVector pA, out JVector pB, out JVector normal, out float toi);

            if (!success || toi > dt || toi == (float)0.0) continue;

            if (world.NarrowPhaseFilter != null)
            {
                // The filter contract expects signed separation, not time of impact.
                // Keep TOI exclusively for ordering the swept hits.
                float separation = JVector.Dot(normal, pA - pB) * world.SpeculativeRelaxationFactor;
                bool result = world.NarrowPhaseFilter.Filter(shape, candidate,
                    ref pA, ref pB, ref normal, ref separation);
                if (!result) continue;
            }

            if (toi < smallestToi)
            {
                smallestToi = toi;
                bestpA = pA;
                bestpB = pB;
                bestNormal = normal;
                otherShape = candidate;
            }
        }

        if (!(smallestToi < float.MaxValue)) return;

        // Create an arbiter and register the contact. All discovered arbiters are
        // prepared and iterated together after this detection pass.

        Arbiter arbiter;

        if (shape.ShapeId < otherShape.ShapeId)
        {
            world.GetOrCreateArbiter(shape.ShapeId, otherShape.ShapeId, shape.RigidBody, otherShape.RigidBody, out arbiter);
            world.RegisterContact(arbiter, bestpA, bestpB, bestNormal);
        }
        else
        {
            world.GetOrCreateArbiter(otherShape.ShapeId, shape.ShapeId, otherShape.RigidBody, shape.RigidBody, out arbiter);
            world.RegisterContact(arbiter, bestpB, bestpA, -bestNormal);
        }

        // This proof of concept explores motion created by contacts only. Constraints
        // are intentionally left to the normal world solver after discovery.
        if (!discoveredArbiters.Contains(arbiter))
        {
            discoveredArbiters.Add(arbiter);
            // Ignore impulses cached by an earlier world step during discovery.
            arbiter.Handle.Data.ResetImpulses();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JBoundingBox CalculateSweptBoundingBox(RigidBodyShape shape, float dt, out float extent)
    {
        ref var data = ref shape.RigidBody.Data;
        shape.CalculateBoundingBox(data.Orientation, data.Position, out JBoundingBox currentBox);

        extent = CalculateAngularExtent(currentBox, data.Position);

        JVector translation = data.Velocity * dt;
        JBoundingBox endBox = new(currentBox.Min + translation, currentBox.Max + translation);
        JBoundingBox result = JBoundingBox.CreateMerged(currentBox, endBox);

        // Every point moves at most |omega| * radius * dt due to rotation.
        // Expanding the translational sweep by that distance covers all
        // intermediate orientations without using an excessively large sphere.
        JVector angularExpansion = new(data.AngularVelocity.Length() * extent * dt);
        result.Min -= angularExpansion;
        result.Max += angularExpansion;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateAngularExtent(RigidBodyShape shape)
    {
        ref var data = ref shape.RigidBody.Data;
        shape.CalculateBoundingBox(data.Orientation, data.Position, out JBoundingBox currentBox);

        return CalculateAngularExtent(currentBox, data.Position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalculateAngularExtent(in JBoundingBox box, in JVector position)
    {
        JVector fromMin = JVector.Abs(box.Min - position);
        JVector fromMax = JVector.Abs(box.Max - position);
        return JVector.Max(fromMin, fromMax).Length();
    }

    public void Destroy()
    {
        world.PreStep -= PreStep;
        bodies.Clear();
        overlaps.Clear();
        discoveredArbiters.Clear();
    }

    public void Remove(RigidBody body) => bodies.Remove(body);

    public void Add(RigidBody body) => bodies.Add(body);
}
