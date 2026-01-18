/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision;
using Jitter2.Collision.Shapes;

namespace Jitter2.SoftBodies;

/// <summary>
/// Provides a collision filter that prevents self-collisions within soft bodies
/// and shapes attached to the same rigid body.
/// </summary>
public static class DynamicTreeCollisionFilter
{
    /// <summary>
    /// Filters collision pairs to exclude self-collisions.
    /// </summary>
    /// <param name="proxyA">The first proxy.</param>
    /// <param name="proxyB">The second proxy.</param>
    /// <returns>
    /// <c>true</c> if the pair should be processed for collision; <c>false</c> if it should be skipped.
    /// </returns>
    public static bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        if (proxyA is RigidBodyShape rbsA && proxyB is RigidBodyShape rbsB)
        {
            if (rbsA.RigidBody == rbsB.RigidBody) return false;
        }
        else if (proxyA is SoftBodyShape softBodyShapeA &&
                 proxyB is SoftBodyShape softBodyShapeB)
        {
            SoftBody ta = softBodyShapeA.SoftBody;
            SoftBody tb = softBodyShapeB.SoftBody;
            return ta != tb;
        }

        return true;
    }
}