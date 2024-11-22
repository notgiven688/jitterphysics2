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
/// Represents a cylinder shape.
/// </summary>
public class CylinderShape : RigidBodyShape
{
    private double radius;
    private double height;

    /// <summary>
    /// Gets or sets the radius of the cylinder.
    /// </summary>
    public double Radius
    {
        get => radius;
        set
        {
            radius = value;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Gets or sets the height of the cylinder.
    /// </summary>
    public double Height
    {
        get => height;
        set
        {
            height = value;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CylinderShape"/> class, creating a cylinder shape with the specified height and radius. The symmetry axis of the cylinder is aligned along the y-axis.
    /// </summary>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="radius">The radius of the cylinder at its base.</param>
    public CylinderShape(double height, double radius)
    {
        this.radius = radius;
        this.height = height;
        UpdateWorldBoundingBox();
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        double sigma = (double)Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z);

        if (sigma > 0.0)
        {
            result.X = direction.X / sigma * radius;
            result.Y = Math.Sign(direction.Y) * height * 0.5;
            result.Z = direction.Z / sigma * radius;
        }
        else
        {
            result.X = 0.0;
            result.Y = Math.Sign(direction.Y) * height * 0.5;
            result.Z = 0.0;
        }
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        const double zeroEpsilon = 1e-12;

        JVector upa = orientation.GetBasisY();

        double xx = upa.X * upa.X;
        double yy = upa.Y * upa.Y;
        double zz = upa.Z * upa.Z;

        double l1 = yy + zz;
        double l2 = xx + zz;
        double l3 = xx + yy;

        double xext = 0, yext = 0, zext = 0;

        if (l1 > zeroEpsilon)
        {
            double sl = 1.0 / Math.Sqrt(l1);
            xext = (yy + zz) * sl * radius;
        }

        if (l2 > zeroEpsilon)
        {
            double sl = 1.0 / Math.Sqrt(l2);
            yext = (xx + zz) * sl * radius;
        }

        if (l3 > zeroEpsilon)
        {
            double sl = 1.0 / Math.Sqrt(l3);
            zext = (xx + yy) * sl * radius;
        }

        JVector p1 = -0.5 * height * upa;
        JVector p2 = +0.5 * height * upa;

        JVector delta = JVector.Max(p1, p2) + new JVector(xext, yext, zext);

        box.Min = position - delta;
        box.Max = position + delta;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out double mass)
    {
        mass = Math.PI * radius * radius * height;

        inertia = JMatrix.Identity;

        inertia.M11 = 1.0 / 4.0 * mass * radius * radius + 1.0 / 12.0 * mass * height * height;
        inertia.M22 = 1.0 / 2.0 * mass * radius * radius;
        inertia.M33 = 1.0 / 4.0 * mass * radius * radius + 1.0 / 12.0 * mass * height * height;

        com = JVector.Zero;
    }
}