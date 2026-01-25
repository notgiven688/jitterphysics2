/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Provides a hook into the narrowphase collision detection pipeline.
/// </summary>
/// <remarks>
/// Implement this interface to intercept collisions after contact generation.
/// This can be used to modify contact data, implement custom collision responses,
/// or filter out specific collisions.
/// </remarks>
public interface INarrowPhaseFilter
{
    /// <summary>
    /// Called for each detected collision with its contact data.
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