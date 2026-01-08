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
/// A simple continuous collision detection (CCD) solver based on speculative contacts.
/// This is more a proof-of-concept than a production-ready solution. There is no multithreading support
/// and the solver is not very efficient. For very few shapes (~10), it works well.
/// There is not really a 'right way' to implement CCD, so this is just one of many possible approaches.
/// </summary>
public class CcdSolver
{
    private const int SelfConsistencyIterations = 4;

    private readonly World world;

    private readonly List<RigidBody> bodies = new();
    private readonly List<IDynamicTreeProxy> overlapList = new();

    public CcdSolver(World world)
    {
        this.world = world;
        world.PreStep += PreStep;
    }

    public bool Enabled { get; set; } = true;

    private void PreStep(double dt)
    {
        if (!Enabled) return;

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

                ref var data = ref body.Data;

                // Predict the position and orientation of the rigid body after the time step.
                body.PredictPose(dt, out var predPos, out var predOri);

                foreach (var shape in body.Shapes)
                {
                    // Use the predicted position and orientation to calculate the future bounding box of the shape.
                    // Then merge this box with the current bounding box of the shape.
                    shape.CalculateBoundingBox(predOri, predPos, out var predBox);
                    var box = JBoundingBox.CreateMerged(shape.WorldBoundingBox, predBox);

                    // Query all tree proxies (read: shapes) which overlap with this extended bounding box.
                    overlapList.Clear();
                    world.DynamicTree.Query(overlapList, box);

                    // Find the first future collision of the shape with any of the overlapping shapes.
                    // If a collision is found, create an arbiter and solve it.
                    CreateAndSolve(overlapList, shape, dt);
                }
            }
        }
    }

    private void CreateAndSolve(List<IDynamicTreeProxy> proxies, RigidBodyShape shape, double dt)
    {
        // Within proxies find the one which collides with 'shape' and has the smallest time of impact (TOI).

        RigidBodyShape otherShape = null!;

        Unsafe.SkipInit(out JVector bestpA);
        Unsafe.SkipInit(out JVector bestpB);
        Unsafe.SkipInit(out JVector bestNormal);

        double smallestToi = double.MaxValue;

        for (int i = 0; i < proxies.Count; i++)
        {
            var proxy = proxies[i];

            if (proxy is not RigidBodyShape pshape) continue;
            if (pshape.RigidBody == shape.RigidBody) continue;

            ref var data = ref shape.RigidBody.Data;
            ref var pdata = ref pshape.RigidBody.Data;

            double extentA = Math.Max((shape.WorldBoundingBox.Max - shape.RigidBody.Position).Length(),
                (shape.WorldBoundingBox.Min - shape.RigidBody.Position).Length());

            double extentB = Math.Max((pshape.WorldBoundingBox.Max - pshape.RigidBody.Position).Length(),
                (pshape.WorldBoundingBox.Min - pshape.RigidBody.Position).Length());

            bool success = NarrowPhase.Sweep(shape, pshape, data.Orientation, pdata.Orientation,
                data.Position, pdata.Position, data.Velocity, pdata.Velocity,
                data.AngularVelocity, pdata.AngularVelocity, extentA, extentB,
                out JVector pA, out JVector pB, out JVector normal, out double toi);

            if (!success || toi > dt || toi == (double)0.0) continue;

            if (world.NarrowPhaseFilter != null)
            {
                bool result = world.NarrowPhaseFilter.Filter(shape, pshape, ref pA, ref pB, ref normal, ref toi);
                if (!result) continue;
            }

            if (toi < smallestToi)
            {
                smallestToi = toi;
                bestpA = pA;
                bestpB = pB;
                bestNormal = normal;
                otherShape = pshape;
            }
        }

        if (!(smallestToi < double.MaxValue)) return;

        // Create an arbiter and register the contact. Perform one iteration of the solver.
        // This updates the velocities of the rigid bodies and prepares them for the next iteration.

        Arbiter arbiter;

        if(shape.ShapeId < otherShape.ShapeId)
        {
            world.GetOrCreateArbiter(shape.ShapeId, otherShape.ShapeId, shape.RigidBody, otherShape.RigidBody, out arbiter);
            world.RegisterContact(arbiter, bestpA, bestpB, bestNormal);
        }
        else
        {
            world.GetOrCreateArbiter(otherShape.ShapeId, shape.ShapeId, otherShape.RigidBody, shape.RigidBody, out arbiter);
            world.RegisterContact(arbiter, bestpB, bestpA, -bestNormal);
        }

        arbiter.Handle.Data.PrepareForIteration((double)1.0 / dt);
        arbiter.Handle.Data.Iterate(false);
    }

    public void Destroy()
    {
        world.PreStep -= PreStep;
        bodies.Clear();
    }

    public void Remove(RigidBody body) => bodies.Remove(body);

    public void Add(RigidBody body) => bodies.Add(body);
}