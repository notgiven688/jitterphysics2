/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.SoftBodies;

public sealed class SoftBodyTetrahedron : SoftBodyShape
{
    public SoftBodyTetrahedron(SoftBody body, RigidBody v1, RigidBody v2, RigidBody v3, RigidBody v4)
    {
        Vertices[0] = v1;
        Vertices[1] = v2;
        Vertices[2] = v3;
        Vertices[3] = v4;

        SoftBody = body;

        UpdateWorldBoundingBox();
    }

    public RigidBody[] Vertices { get; } = new RigidBody[4];

    public override JVector Velocity
    {
        get
        {
            JVector vel = JVector.Zero;

            for (int i = 0; i < 4; i++)
            {
                vel += Vertices[i].Velocity;
            }

            vel *= (Real)0.25;

            return vel;
        }
    }

    public override RigidBody GetClosest(in JVector pos)
    {
        Real dist = Real.MaxValue;
        int closest = 0;

        for (int i = 0; i < 4; i++)
        {
            Real len = (pos - Vertices[i].Position).LengthSquared();
            if (len < dist)
            {
                dist = len;
                closest = i;
            }
        }

        return Vertices[closest];
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        Real maxDot = Real.MinValue;
        int furthest = 0;

        for (int i = 0; i < 4; i++)
        {
            Real dot = JVector.Dot(direction, Vertices[i].Position);
            if (dot > maxDot)
            {
                maxDot = dot;
                furthest = i;
            }
        }

        result = Vertices[furthest].Position;
    }

    public override void GetCenter(out JVector point)
    {
        point = (Real)0.25 * (Vertices[0].Position + Vertices[1].Position +
                         Vertices[2].Position + Vertices[3].Position);
    }

    public override void UpdateWorldBoundingBox(Real dt = (Real)0.0)
    {
        const Real extraMargin = (Real)0.01;

        JBoundingBox box = JBoundingBox.SmallBox;
        JBoundingBox.AddPointInPlace(ref box, Vertices[0].Position);
        JBoundingBox.AddPointInPlace(ref box, Vertices[1].Position);
        JBoundingBox.AddPointInPlace(ref box, Vertices[2].Position);
        JBoundingBox.AddPointInPlace(ref box, Vertices[3].Position);

        box.Min -= JVector.One * extraMargin;
        box.Max += JVector.One * extraMargin;

        WorldBoundingBox = box;
    }
}