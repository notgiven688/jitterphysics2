/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.SoftBodies;

/// <summary>
/// A broad-phase filter that handles collisions involving soft body shapes.
/// It delegates collision detection to the narrow phase and registers contacts with the
/// closest rigid body vertices of the soft body.
/// </summary>
public class BroadPhaseCollisionFilter : IBroadPhaseFilter
{
    private readonly World world;

    /// <summary>
    /// Initializes a new instance of the <see cref="BroadPhaseCollisionFilter"/> class.
    /// </summary>
    /// <param name="world">The world instance.</param>
    public BroadPhaseCollisionFilter(World world)
    {
        this.world = world;
    }

    /// <inheritdoc/>
    public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        SoftBodyShape? i1 = proxyA as SoftBodyShape;
        SoftBodyShape? i2 = proxyB as SoftBodyShape;

        if (i1 != null && i2 != null)
        {
            if (i2.ShapeId < i1.ShapeId)
            {
                (i1, i2) = (i2, i1);
            }

            if (!i1.SoftBody.IsActive && !i2.SoftBody.IsActive) return false;

            bool colliding = NarrowPhase.MprEpa(i1, i2,
                JQuaternion.Identity, JVector.Zero,
                out JVector pA, out JVector pB, out JVector normal, out _);

            if (!colliding) return false;

            var closestA = i1.GetClosest(pA);
            var closestB = i2.GetClosest(pB);

            world.RegisterContact(closestA.RigidBodyId, closestB.RigidBodyId, closestA, closestB,
                pA, pB, normal);

            return false;
        }

        if (i1 != null)
        {
            var rb = (proxyB as RigidBodyShape)!.RigidBody;

            if (!i1.SoftBody.IsActive && !rb.Data.IsActive) return false;

            bool colliding = NarrowPhase.MprEpa(i1, (proxyB as RigidBodyShape)!, rb.Orientation, rb.Position,
                out JVector pA, out JVector pB, out JVector normal, out _);

            if (!colliding) return false;

            var closest = i1.GetClosest(pA);

            world.RegisterContact(closest.RigidBodyId, rb.RigidBodyId, closest, rb,
                pA, pB, normal, ContactData.SolveMode.AngularBody1);

            return false;
        }

        if (i2 != null)
        {
            var ra = (proxyA as RigidBodyShape)!.RigidBody;

            if (!i2.SoftBody.IsActive && !ra.Data.IsActive) return false;

            bool colliding = NarrowPhase.MprEpa(i2, (proxyA as RigidBodyShape)!, ra.Orientation, ra.Position,
                out JVector pA, out JVector pB, out JVector normal, out _);

            if (!colliding) return false;

            var closest = i2.GetClosest(pA);

            world.RegisterContact(closest.RigidBodyId, ra.RigidBodyId, closest, ra,
                pA, pB, normal, ContactData.SolveMode.AngularBody1);

            return false;
        }

        return true;
    }
}