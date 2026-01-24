/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Defines a filter for modifying or excluding collisions after narrowphase detection.
/// </summary>
/// <remarks>
/// Implement this interface to adjust contact points, normals, or penetration depth,
/// or to discard specific collisions entirely.
/// </remarks>
public interface INarrowPhaseFilter
{
    /// <summary>
    /// Filters or modifies collision data after narrowphase detection.
    /// </summary>
    /// <param name="shapeA">The first shape in the collision.</param>
    /// <param name="shapeB">The second shape in the collision.</param>
    /// <param name="pointA">Contact point on shape A (modifiable).</param>
    /// <param name="pointB">Contact point on shape B (modifiable).</param>
    /// <param name="normal">Collision normal from B to A (modifiable).</param>
    /// <param name="penetration">Penetration depth (modifiable).</param>
    /// <returns><c>true</c> to keep the collision; <c>false</c> to discard it.</returns>
    [CallbackThread(ThreadContext.Any)]
    bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB,
        ref JVector pointA, ref JVector pointB,
        ref JVector normal, ref Real penetration);
}