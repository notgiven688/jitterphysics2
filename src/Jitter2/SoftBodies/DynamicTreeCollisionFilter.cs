/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision;
using Jitter2.Collision.Shapes;

namespace Jitter2.SoftBodies;

/// <summary>
/// Provides a collision filter that rejects pairs of soft-body shapes belonging to the same
/// soft body and pairs of rigid-body shapes attached to the same rigid body. Mixed pairs pass.
/// </summary>
public static class DynamicTreeCollisionFilter
{
    /// <summary>
    /// Filters matching soft-body and rigid-body shape pairs that share an owner.
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
            SoftBody softBodyA = softBodyShapeA.SoftBody;
            SoftBody softBodyB = softBodyShapeB.SoftBody;
            return softBodyA != softBodyB;
        }

        return true;
    }
}
