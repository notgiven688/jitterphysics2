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
/// Abstract base class for shapes used in soft body simulations.
/// </summary>
public abstract class SoftBodyShape : Shape
{
    /// <summary>
    /// Gets the rigid body closest to the specified position.
    /// </summary>
    /// <param name="pos">The position in world coordinates.</param>
    /// <returns>The closest rigid body (vertex) of this shape.</returns>
    public abstract RigidBody GetClosest(in JVector pos);

    /// <summary>
    /// Gets the soft body instance this shape belongs to.
    /// </summary>
    public SoftBody SoftBody { get; internal init; } = null!;

    /// <inheritdoc/>
    public override bool RayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        return NarrowPhase.RayCast(this, origin, direction, out lambda, out normal);
    }

    /// <inheritdoc/>
    public override bool Sweep<T>(in T support, in JQuaternion orientation, in JVector position, in JVector sweep,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
    {
        bool hit = NarrowPhase.Sweep(this, support,
            orientation, position, sweep,
            out pointB, out pointA, out normal, out lambda);

        JVector.NegateInPlace(ref normal);
        return hit;
    }

    /// <inheritdoc/>
    public override bool Distance<T>(in T support, in JQuaternion orientation, in JVector position,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
    {
        return NarrowPhase.Distance(support, this,
            orientation, JQuaternion.Identity,
            position, JVector.Zero,
            out pointA, out pointB, out normal, out distance);
    }
}
