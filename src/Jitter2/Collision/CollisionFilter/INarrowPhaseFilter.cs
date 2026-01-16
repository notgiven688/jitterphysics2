/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Interface to facilitate the implementation of a generic filter. This filter can either exclude certain pairs of shapes or modify collision
/// information subsequent to Jitter's execution of narrow phase collision detection between the shapes.
/// </summary>
public interface INarrowPhaseFilter
{
    /// <summary>
    /// Invoked following the narrow phase of collision detection in Jitter. This allows for the modification of collision information.
    /// Refer to the corresponding <see cref="NarrowPhase"/> methods for details on the parameters.
    /// </summary>
    /// <returns>False if the collision should be filtered out, true otherwise.</returns>
    [CallbackThread(ThreadContext.Any)]
    bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB,
        ref JVector pointA, ref JVector pointB,
        ref JVector normal, ref Real penetration);
}