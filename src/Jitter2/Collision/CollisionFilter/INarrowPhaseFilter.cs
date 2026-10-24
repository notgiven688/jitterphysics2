/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Allows detected contacts to be modified or discarded before they affect the bodies.
/// </summary>
/// <remarks>Assign an implementation to <see cref="World.NarrowPhaseFilter"/>.</remarks>
public interface INarrowPhaseFilter
{
    /// <summary>
    /// Called for each detected collision with its contact data.
    /// </summary>
    /// <param name="shapeA">The first shape in the collision.</param>
    /// <param name="shapeB">The second shape in the collision.</param>
    /// <param name="pointA">Contact point on shape A in world space. May be modified.</param>
    /// <param name="pointB">Contact point on shape B in world space. May be modified.</param>
    /// <param name="normal">Collision normal in world space. May be modified. The contact pushes
    /// shape B along this direction and shape A against it.</param>
    /// <param name="penetration">Signed penetration depth. Changing this value alone does not change
    /// the contact response; that is determined by the contact points and normal.</param>
    /// <returns><c>true</c> to keep the collision; <c>false</c> to discard it.</returns>
    [CallbackThread(ThreadContext.Any)]
    bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB,
        ref JVector pointA, ref JVector pointB,
        ref JVector normal, ref Real penetration);
}
