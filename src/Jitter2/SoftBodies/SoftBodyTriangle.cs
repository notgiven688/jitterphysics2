/*
 * Copyright (c) 2009-2023 Thorben Linneweber and others
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
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.SoftBodies;

public class SoftBodyTriangle : Shape, ISoftBodyShape
{
    private readonly RigidBody p2;
    private readonly RigidBody p3;
    private readonly RigidBody p1;

    public RigidBody Body1 => p1;
    public RigidBody Body2 => p2;
    public RigidBody Body3 => p3;

    public float Thickness { get; set; } = 0.05f;

    public SoftBodyTriangle(SoftBody body, RigidBody p1, RigidBody p2, RigidBody p3)
    {
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;

        SoftBody = body;
        UpdateShape();
    }

    public override JVector Velocity => 1.0f / 3.0f * (p1.Data.Velocity + p2.Data.Velocity + p3.Data.Velocity);

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)
    {
        inertia = JMatrix.Identity;
        mass = 1;
        com = 1.0f / 3.0f * (p1.Position + p2.Position + p3.Position);
    }

    public RigidBody GetClosest(in JVector pos)
    {
        float len1 = (pos - p1.Position).LengthSquared();
        float len2 = (pos - p2.Position).LengthSquared();
        float len3 = (pos - p3.Position).LengthSquared();

        if (len1 < len2 && len1 < len3)
        {
            return p1;
        }

        if (len2 < len3 && len2 <= len1)
        {
            return p2;
        }

        return p3;
    }

    public SoftBody SoftBody { get; }

    public override void UpdateWorldBoundingBox()
    {
        float extraMargin = MathF.Max(Thickness, 0.01f);

        var box = JBBox.SmallBox;
        box.AddPoint(p1.Position);
        box.AddPoint(p2.Position);
        box.AddPoint(p3.Position);

        box.Min -= JVector.One * extraMargin;
        box.Max += JVector.One * extraMargin;

        WorldBoundingBox = box;

        GeometricCenter = 1.0f / 3.0f * (p1.Position + p2.Position + p3.Position);
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        JVector a = p1.Position;
        JVector b = p2.Position;
        JVector c = p3.Position;

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

        result += JVector.Normalize(direction) * Thickness;
    }
}