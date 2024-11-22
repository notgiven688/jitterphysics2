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
/// Represents a cone shape.
/// </summary>
public class ConeShape : RigidBodyShape
{
    private double radius;
    private double height;

    /// <summary>
    /// Gets or sets the radius of the cone at its base.
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
    /// Gets or sets the height of the cone.
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
    /// Initializes a new instance of the ConeShape class with specified radius and height. The symmetry axis of the cone is aligned along the Y-axis.
    /// </summary>
    /// <param name="radius">The radius of the cone at its base.</param>
    /// <param name="height">The height of the cone.</param>
    public ConeShape(double radius = 0.5, double height = 1.0)
    {
        this.radius = radius;
        this.height = height;
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        const double ZeroEpsilon = 1e-12;
        // cone = disk + point

        // center of mass of a cone is at 0.25 height
        JVector ndir = direction;
        ndir.Y = 0.0;
        double ndir2 = ndir.LengthSquared();

        if (ndir2 > ZeroEpsilon)
        {
            ndir *= radius / Math.Sqrt(ndir2);
        }

        ndir.Y = -0.25 * height;

        // disk support point vs (0, 0.75 * height, 0)
        if (JVector.Dot(direction, ndir) >= direction.Y * 0.75 * height)
        {
            result = ndir;
        }
        else
        {
            result = new JVector(0, 0.75 * height, 0);
        }
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        const double ZeroEpsilon = 1e-12;

        JVector upa = orientation.GetBasisY();

        double xx = upa.X * upa.X;
        double yy = upa.Y * upa.Y;
        double zz = upa.Z * upa.Z;

        double l1 = yy + zz;
        double l2 = xx + zz;
        double l3 = xx + yy;

        double xext = 0, yext = 0, zext = 0;

        if (l1 > ZeroEpsilon)
        {
            double sl = 1.0 / Math.Sqrt(l1);
            xext = (yy + zz) * sl * radius;
        }

        if (l2 > ZeroEpsilon)
        {
            double sl = 1.0 / Math.Sqrt(l2);
            yext = (xx + zz) * sl * radius;
        }

        if (l3 > ZeroEpsilon)
        {
            double sl = 1.0 / Math.Sqrt(l3);
            zext = (xx + yy) * sl * radius;
        }

        JVector p1 = -0.25 * height * upa;
        JVector p2 = +0.75 * height * upa;

        box.Min = p1 - new JVector(xext, yext, zext);
        box.Max = p1 + new JVector(xext, yext, zext);

        box.AddPoint(p2);

        box.Min += position;
        box.Max += position;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out double mass)
    {
        mass = 1.0 / 3.0 * Math.PI * radius * radius * height;

        inertia = JMatrix.Identity;
        inertia.M11 = mass * (3.0 / 20.0 * radius * radius + 3.0 / 80.0 * height * height);
        inertia.M22 = 3.0 / 10.0 * mass * radius * radius;
        inertia.M33 = mass * (3.0 / 20.0 * radius * radius + 3.0 / 80.0 * height * height);

        com = JVector.Zero;
    }
}