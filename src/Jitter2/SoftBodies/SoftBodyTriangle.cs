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

using System;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.SoftBodies;

public class SoftBodyTriangle : SoftBodyShape
{
    private readonly RigidBody v1;
    private readonly RigidBody v2;
    private readonly RigidBody v3;

    public RigidBody Vertex1 => v1;
    public RigidBody Vertex2 => v2;
    public RigidBody Vertex3 => v3;

    private Real halfThickness = (Real)0.05;

    public Real Thickness
    {
        get => halfThickness * (Real)2.0;
        set => halfThickness = value * (Real)0.5;
    }

    public SoftBodyTriangle(SoftBody body, RigidBody v1, RigidBody v2, RigidBody v3)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;

        SoftBody = body;
        UpdateWorldBoundingBox();
    }

    public override JVector Velocity => (Real)(1.0 / 3.0) * (v1.Data.Velocity + v2.Data.Velocity + v3.Data.Velocity);

    public override RigidBody GetClosest(in JVector pos)
    {
        Real len1 = (pos - v1.Position).LengthSquared();
        Real len2 = (pos - v2.Position).LengthSquared();
        Real len3 = (pos - v3.Position).LengthSquared();

        if (len1 < len2 && len1 < len3)
        {
            return v1;
        }

        if (len2 < len3 && len2 <= len1)
        {
            return v2;
        }

        return v3;
    }

    public override void UpdateWorldBoundingBox(Real dt = (Real)0.0)
    {
        Real extraMargin = MathR.Max(halfThickness, (Real)0.01);

        JBBox box = JBBox.SmallBox;

        JBBox.AddPointInPlace(ref box, Vertex1.Position);
        JBBox.AddPointInPlace(ref box, Vertex2.Position);
        JBBox.AddPointInPlace(ref box, Vertex3.Position);

        // prevent a degenerate bounding box
        JVector extra = new JVector(extraMargin);
        box.Min -= extra;
        box.Max += extra;

        WorldBoundingBox = box;
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        JVector a = v1.Position;
        JVector b = v2.Position;
        JVector c = v3.Position;

        Real min = JVector.Dot(a, direction);
        Real dot = JVector.Dot(b, direction);

        result = a;

        if (dot > min)
        {
            min = dot;
            result = b;
        }

        dot = JVector.Dot(c, direction);

        if (dot > min)
        {
            result = c;
        }

        result += JVector.Normalize(direction) * halfThickness;
    }

    public override void GetCenter(out JVector point)
    {
        point = ((Real)(1.0 / 3.0)) * (Vertex1.Position + Vertex2.Position + Vertex3.Position);
    }
}