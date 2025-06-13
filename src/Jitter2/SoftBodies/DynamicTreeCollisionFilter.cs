/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Collision;
using Jitter2.Collision.Shapes;

namespace Jitter2.SoftBodies;

public static class DynamicTreeCollisionFilter
{
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