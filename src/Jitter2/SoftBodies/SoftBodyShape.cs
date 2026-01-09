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
}