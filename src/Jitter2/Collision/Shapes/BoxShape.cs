/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
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
    /// Creates a box shape with specified dimensions.
    /// </summary>
    /// <param name="size">The dimensions of the box.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any component of <paramref name="size"/> is less than or equal to zero.
    /// </exception>
    public BoxShape(JVector size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.X, nameof(size));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.Y, nameof(size));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size.Z, nameof(size));

        halfSize = (Real)0.5 * size;
        UpdateWorldBoundingBox();
    }

    /// <summary>
    /// Gets or sets the dimensions of the box.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any component of <paramref name="value"/> is less than or equal to zero.
    /// </exception>
    public JVector Size
    {
        get => (Real)2.0 * halfSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.X, nameof(Size));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.Y, nameof(Size));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.Z, nameof(Size));

            halfSize = value * (Real)0.5;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Creates a cube shape with sides of equal length.
    /// </summary>
    /// <param name="size">The length of each side.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size"/> is less than or equal to zero.
    /// </exception>
    public BoxShape(Real size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        halfSize = new JVector(size * (Real)0.5);
        UpdateWorldBoundingBox();
    }

    /// <summary>
    /// Creates a box shape with the specified length, height, and width.
    /// </summary>
    /// <param name="length">The length of the box.</param>
    /// <param name="height">The height of the box.</param>
    /// <param name="width">The width of the box.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="length"/>, <paramref name="height"/>, or <paramref name="width"/> is less than
    /// or equal to zero.
    /// </exception>
    public BoxShape(Real width, Real height, Real length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);

        halfSize = (Real)0.5 * new JVector(width, height, length);
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        result.X = MathHelper.SignBit(direction.X) * halfSize.X;
        result.Y = MathHelper.SignBit(direction.Y) * halfSize.Y;
        result.Z = MathHelper.SignBit(direction.Z) * halfSize.Z;
    }

    public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        Real epsilon = (Real)1e-22;

        JVector min = -halfSize;
        JVector max = halfSize;

        normal = JVector.Zero;
        lambda = (Real)0.0;

        Real exit = Real.PositiveInfinity;

        if (MathR.Abs(direction.X) > epsilon)
        {
            Real ix = (Real)1.0 / direction.X;
            Real t0 = (min.X - origin.X) * ix;
            Real t1 = (max.X - origin.X) * ix;

            if (t0 > t1) (t0, t1) = (t1, t0);

            if (t0 > exit || t1 < lambda) return false;

            if (t0 > lambda)
            {
                lambda = t0;
                normal = direction.X < (Real)0.0 ? JVector.UnitX : -JVector.UnitX;
            }

            if (t1 < exit) exit = t1;
        }
        else if (origin.X < min.X || origin.X > max.X)
        {
            return false;
        }

        if (MathR.Abs(direction.Y) > epsilon)
        {
            Real iy = (Real)1.0 / direction.Y;
            Real t0 = (min.Y - origin.Y) * iy;
            Real t1 = (max.Y - origin.Y) * iy;

            if (t0 > t1) (t0, t1) = (t1, t0);

            if (t0 > exit || t1 < lambda) return false;

            if (t0 > lambda)
            {
                lambda = t0;
                normal = direction.Y < (Real)0.0 ? JVector.UnitY : -JVector.UnitY;
            }

            if (t1 < exit) exit = t1;
        }
        else if (origin.Y < min.Y || origin.Y > max.Y)
        {
            return false;
        }

        if (MathR.Abs(direction.Z) > epsilon)
        {
            Real iz = (Real)1.0 / direction.Z;
            Real t0 = (min.Z - origin.Z) * iz;
            Real t1 = (max.Z - origin.Z) * iz;

            if (t0 > t1) (t0, t1) = (t1, t0);

            if (t0 > exit || t1 < lambda) return false;

            if (t0 > lambda)
            {
                lambda = t0;
                normal = direction.Z < (Real)0.0 ? JVector.UnitZ : -JVector.UnitZ;
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

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        JMatrix.Absolute(JMatrix.CreateFromQuaternion(orientation), out JMatrix absm);
        var ths = JVector.Transform(halfSize, absm);
        box.Min = position - ths;
        box.Max = position + ths;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        JVector size = halfSize * (Real)2.0;
        mass = size.X * size.Y * size.Z;

        inertia = JMatrix.Identity;
        inertia.M11 = (Real)(1.0 / 12.0) * mass * (size.Y * size.Y + size.Z * size.Z);
        inertia.M22 = (Real)(1.0 / 12.0) * mass * (size.X * size.X + size.Z * size.Z);
        inertia.M33 = (Real)(1.0 / 12.0) * mass * (size.X * size.X + size.Y * size.Y);

        com = JVector.Zero;
    }
}