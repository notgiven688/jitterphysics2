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
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a sphere.
/// </summary>
public class SphereShape : RigidBodyShape
{
    private Real radius;

    /// <summary>
    /// Gets or sets the radius of the sphere.
    /// </summary>
    public Real Radius
    {
        get => radius;
        set
        {
            radius = value;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SphereShape"/> class with an optional radius parameter.
    /// The default radius is 1.0 units.
    /// </summary>
    /// <param name="radius">The radius of the sphere. Defaults to 1.0f.</param>
    public SphereShape(Real radius = 1.0f)
    {
        this.radius = radius;
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        result = direction;
        result.Normalize();
        JVector.Multiply(result, radius, out result);
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        box.Min.X = -radius;
        box.Min.Y = -radius;
        box.Min.Z = -radius;
        box.Max.X = +radius;
        box.Max.Y = +radius;
        box.Max.Z = +radius;

        JVector.Add(box.Min, position, out box.Min);
        JVector.Add(box.Max, position, out box.Max);
    }

    public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        normal = JVector.Zero;
        lambda = 0.0f;

        Real disq = 1.0f / direction.LengthSquared();
        Real p = JVector.Dot(direction, origin) * disq;
        Real d = p * p - (origin.LengthSquared() - radius * radius) * disq;

        if (d < 0.0f) return false;

        Real sqrtd = MathR.Sqrt(d);

        Real t0 = -p - sqrtd;
        Real t1 = -p + sqrtd;

        if (t0 >= 0.0f)
        {
            lambda = t0;
            JVector.Normalize(origin + t0 * direction, out normal);
            return true;
        }

        return t1 > 0.0f;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        mass = 4.0f / 3.0f * MathR.PI * radius * radius * radius;

        // (0,0,0) is the center of mass
        inertia = JMatrix.Identity;
        inertia.M11 = 2.0f / 5.0f * mass * radius * radius;
        inertia.M22 = 2.0f / 5.0f * mass * radius * radius;
        inertia.M33 = 2.0f / 5.0f * mass * radius * radius;

        com = JVector.Zero;
    }
}