/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

#if USE_DOUBLE_PRECISION
using Real = System.Double;
using MathR = System.Math;
#else
using Real = System.Single;
using MathR = System.MathF;
#endif

namespace Jitter2.SoftBodies;

public class BroadPhaseCollisionFilter : IBroadPhaseFilter
{
    private readonly World world;

    public BroadPhaseCollisionFilter(World world)
    {
        this.world = world;
    }

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

            bool colliding = NarrowPhase.MPREPA(i1, i2,
                JQuaternion.Identity, JVector.Zero,
                out JVector pA, out JVector pB, out JVector normal, out Real penetration);

            if (!colliding) return false;

            var closestA = i1.GetClosest(pA);
            var closestB = i2.GetClosest(pB);

            world.RegisterContact(closestA.RigidBodyId, closestB.RigidBodyId, closestA, closestB,
                pA, pB, normal, penetration);

            return false;
        }

        if (i1 != null)
        {
            var rb = (proxyB as RigidBodyShape)!.RigidBody!;

            if (!i1.SoftBody.IsActive && !rb.Data.IsActive) return false;

            bool colliding = NarrowPhase.MPREPA(i1, (proxyB as RigidBodyShape)!, rb.Orientation, rb.Position,
                out JVector pA, out JVector pB, out JVector normal, out Real penetration);

            if (!colliding) return false;

            var closest = i1.GetClosest(pA);

            world.RegisterContact(closest.RigidBodyId, rb.RigidBodyId, closest, rb,
                pA, pB, normal, penetration);

            return false;
        }

        if (i2 != null)
        {
            var ra = (proxyA as RigidBodyShape)!.RigidBody!;

            if (!i2.SoftBody.IsActive && !ra.Data.IsActive) return false;

            bool colliding = NarrowPhase.MPREPA(i2, (proxyA as RigidBodyShape)!, ra.Orientation, ra.Position,
                out JVector pA, out JVector pB, out JVector normal, out Real penetration);

            if (!colliding) return false;

            var closest = i2.GetClosest(pA);

            world.RegisterContact(closest.RigidBodyId, ra.RigidBodyId, closest, ra,
                pA, pB, normal, penetration);

            return false;
        }

        return true;
    }
}