/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a cylinder shape.
/// </summary>
public class CylinderShape : RigidBodyShape
{
    private Real radius;
    private Real height;

    /// <summary>
    /// Gets or sets the radius of the cylinder.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is less than or equal to zero.
    /// </exception>
    public Real Radius
    {
        get => radius;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(Radius));
            radius = value;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Gets or sets the height of the cylinder.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is less than or equal to zero.
    /// </exception>
    public Real Height
    {
        get => height;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(Height));
            height = value;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CylinderShape"/> class, creating a cylinder shape with the specified
    /// height and radius. The symmetry axis of the cylinder is aligned along the y-axis.
    /// </summary>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="radius">The radius of the cylinder at its base.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="height"/> or <paramref name="radius"/> is less than or equal to zero.
    /// </exception>
    public CylinderShape(Real height, Real radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius, nameof(radius));

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
        Real sigma = (Real)Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z);

        if (sigma > (Real)0.0)
        {
            result.X = direction.X / sigma * radius;
            result.Y = Math.Sign(direction.Y) * height * (Real)0.5;
            result.Z = direction.Z / sigma * radius;
        }
        else
        {
            result.X = (Real)0.0;
            result.Y = Math.Sign(direction.Y) * height * (Real)0.5;
            result.Z = (Real)0.0;
        }
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        const Real zeroEpsilon = (Real)1e-12;

        JVector upa = orientation.GetBasisY();

        Real xx = upa.X * upa.X;
        Real yy = upa.Y * upa.Y;
        Real zz = upa.Z * upa.Z;

        Real l1 = yy + zz;
        Real l2 = xx + zz;
        Real l3 = xx + yy;

        Real xext = 0, yext = 0, zext = 0;

        if (l1 > zeroEpsilon)
        {
            Real sl = (Real)1.0 / MathR.Sqrt(l1);
            xext = (yy + zz) * sl * radius;
        }

        if (l2 > zeroEpsilon)
        {
            Real sl = (Real)1.0 / MathR.Sqrt(l2);
            yext = (xx + zz) * sl * radius;
        }

        if (l3 > zeroEpsilon)
        {
            Real sl = (Real)1.0 / MathR.Sqrt(l3);
            zext = (xx + yy) * sl * radius;
        }

        JVector p1 = -(Real)0.5 * height * upa;
        JVector p2 = +(Real)0.5 * height * upa;

        JVector delta = JVector.Max(p1, p2) + new JVector(xext, yext, zext);

        box.Min = position - delta;
        box.Max = position + delta;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        mass = MathR.PI * radius * radius * height;

        inertia = JMatrix.Identity;

        inertia.M11 = (Real)(1.0 / 4.0) * mass * radius * radius + (Real)(1.0 / 12.0) * mass * height * height;
        inertia.M22 = (Real)(1.0 / 2.0) * mass * radius * radius;
        inertia.M33 = (Real)(1.0 / 4.0) * mass * radius * radius + (Real)(1.0 / 12.0) * mass * height * height;

        com = JVector.Zero;
    }
}