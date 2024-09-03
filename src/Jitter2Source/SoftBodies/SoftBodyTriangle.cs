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

    private float halfThickness = 0.05f;

    public float Thickness
    {
        get => halfThickness * 2.0f;
        set => halfThickness = value * 0.5f;
    }

    public SoftBodyTriangle(SoftBody body, RigidBody v1, RigidBody v2, RigidBody v3)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;

        SoftBody = body;
        UpdateWorldBoundingBox();
    }

    public override JVector Velocity => 1.0f / 3.0f * (v1.Data.Velocity + v2.Data.Velocity + v3.Data.Velocity);

    public override RigidBody GetClosest(in JVector pos)
    {
        float len1 = (pos - v1.Position).LengthSquared();
        float len2 = (pos - v2.Position).LengthSquared();
        float len3 = (pos - v3.Position).LengthSquared();

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

    public override void UpdateWorldBoundingBox(float dt = 0.0f)
    {
        float extraMargin = MathF.Max(halfThickness, 0.01f);

        JBBox box = JBBox.SmallBox;

        box.AddPoint(Vertex1.Position);
        box.AddPoint(Vertex2.Position);
        box.AddPoint(Vertex3.Position);

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

        float min = JVector.Dot(a, direction);
        float dot = JVector.Dot(b, direction);

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
        point = (1.0f / 3.0f) * (Vertex1.Position + Vertex2.Position + Vertex3.Position);
    }
}