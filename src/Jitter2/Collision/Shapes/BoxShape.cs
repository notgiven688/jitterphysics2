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
/// Represents a three-dimensional box shape.
/// </summary>
public class BoxShape : RigidBodyShape
{
    private JVector halfSize;

    /// <summary>
    /// Gets or sets the dimensions of the box.
    /// </summary>
    public JVector Size
    {
        get => 2.0 * halfSize;
        set
        {
            halfSize = value * 0.5;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Creates a box shape with specified dimensions.
    /// </summary>
    /// <param name="size">The dimensions of the box.</param>
    public BoxShape(JVector size)
    {
        halfSize = 0.5 * size;
        UpdateWorldBoundingBox();
    }

    /// <summary>
    /// Creates a cube shape with the specified side length.
    /// </summary>
    /// <param name="size">The length of each side of the cube.</param>
    public BoxShape(double size)
    {
        halfSize = new JVector(size * 0.5);
        UpdateWorldBoundingBox();
    }

    /// <summary>
    /// Creates a box shape with the specified length, height, and width.
    /// </summary>
    /// <param name="length">The length of the box.</param>
    /// <param name="height">The height of the box.</param>
    /// <param name="width">The width of the box.</param>
    public BoxShape(double length, double height, double width)
    {
        halfSize = 0.5 * new JVector(length, height, width);
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        result.X = Math.Sign(direction.X) * halfSize.X;
        result.Y = Math.Sign(direction.Y) * halfSize.Y;
        result.Z = Math.Sign(direction.Z) * halfSize.Z;
    }

    public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out double lambda)
    {
        double epsilon = 1e-22;

        JVector min = -halfSize;
        JVector max = halfSize;

        normal = JVector.Zero;
        lambda = 0.0;

        double exit = double.PositiveInfinity;

        if (Math.Abs(direction.X) > epsilon)
        {
            double ix = 1.0 / direction.X;
            double t0 = (min.X - origin.X) * ix;
            double t1 = (max.X - origin.X) * ix;

            if (t0 > t1) (t0, t1) = (t1, t0);

            if (t0 > exit || t1 < lambda) return false;

            if (t0 > lambda)
            {
                lambda = t0;
                normal = direction.X < 0.0 ? JVector.UnitX : -JVector.UnitX;
            }

            if (t1 < exit) exit = t1;
        }
        else if (origin.X < min.X || origin.X > max.X)
        {
            return false;
        }

        if (Math.Abs(direction.Y) > epsilon)
        {
            double iy = 1.0 / direction.Y;
            double t0 = (min.Y - origin.Y) * iy;
            double t1 = (max.Y - origin.Y) * iy;

            if (t0 > t1) (t0, t1) = (t1, t0);

            if (t0 > exit || t1 < lambda) return false;

            if (t0 > lambda)
            {
                lambda = t0;
                normal = direction.Y < 0.0 ? JVector.UnitY : -JVector.UnitY;
            }

            if (t1 < exit) exit = t1;
        }
        else if (origin.Y < min.Y || origin.Y > max.Y)
        {
            return false;
        }

        if (Math.Abs(direction.Z) > epsilon)
        {
            double iz = 1.0 / direction.Z;
            double t0 = (min.Z - origin.Z) * iz;
            double t1 = (max.Z - origin.Z) * iz;

            if (t0 > t1) (t0, t1) = (t1, t0);

            if (t0 > exit || t1 < lambda) return false;

            if (t0 > lambda)
            {
                lambda = t0;
                normal = direction.Z < 0.0 ? JVector.UnitZ : -JVector.UnitZ;
            }
            //if (t1 < exit) exit = t1;
        }
        else if (origin.Z < min.Z || origin.Z > max.Z)
        {
            return false;
        }

        return true;
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        JMatrix.Absolute(JMatrix.CreateFromQuaternion(orientation), out JMatrix absm);
        var ths = JVector.Transform(halfSize, absm);
        box.Min = position - ths;
        box.Max = position + ths;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out double mass)
    {
        JVector size = halfSize * 2.0;
        mass = size.X * size.Y * size.Z;

        inertia = JMatrix.Identity;
        inertia.M11 = 1.0 / 12.0 * mass * (size.Y * size.Y + size.Z * size.Z);
        inertia.M22 = 1.0 / 12.0 * mass * (size.X * size.X + size.Z * size.Z);
        inertia.M33 = 1.0 / 12.0 * mass * (size.X * size.X + size.Y * size.Y);

        com = JVector.Zero;
    }
}