/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a cone shape.
/// </summary>
public class ConeShape : RigidBodyShape
{
    private Real radius;
    private Real height;

    /// <summary>
    /// Gets or sets the radius of the cone at its base.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when radius is less than or equal to zero.
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
    /// Gets or sets the height of the cone.
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
    /// Initializes a new instance of the ConeShape class with specified radius and height. The symmetry axis of the cone is aligned along the Y-axis.
    /// </summary>
    /// <param name="radius">The radius of the cone at its base.</param>
    /// <param name="height">The height of the cone.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    public ConeShape(Real radius = (Real)0.5, Real height = (Real)1.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius, nameof(radius));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));

        this.radius = radius;
        this.height = height;
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        const Real zeroEpsilon = (Real)1e-12;
        // cone = disk + point

        // The center of mass is at 0.25 height.
        JVector baseDir = new JVector(direction.X, (Real)0.0, direction.Z);
        baseDir = JVector.NormalizeSafe(baseDir, zeroEpsilon) * radius;

        baseDir.Y = -(Real)0.25 * height;

        // disk support point vs. (0, 0.75 * height, 0)
        if (JVector.Dot(direction, baseDir) >= direction.Y * (Real)0.75 * height)
        {
            result = baseDir;
        }
        else
        {
            result = new JVector(0, (Real)0.75 * height, 0);
        }
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        JVector upa = orientation.GetBasisY();

        Real xx = upa.X * upa.X;
        Real yy = upa.Y * upa.Y;
        Real zz = upa.Z * upa.Z;

        Real xext = MathR.Sqrt(yy + zz) * radius;
        Real yext = MathR.Sqrt(xx + zz) * radius;
        Real zext = MathR.Sqrt(xx + yy) * radius;

        JVector p1 = -(Real)0.25 * height * upa;
        JVector p2 = +(Real)0.75 * height * upa;

        box.Min = p1 - new JVector(xext, yext, zext);
        box.Max = p1 + new JVector(xext, yext, zext);

        JBoundingBox.AddPointInPlace(ref box, p2);

        box.Min += position;
        box.Max += position;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        mass = (Real)(1.0 / 3.0) * MathR.PI * radius * radius * height;

        inertia = JMatrix.Identity;
        inertia.M11 = mass * ((Real)(3.0 / 20.0) * radius * radius + (Real)(3.0 / 80.0) * height * height);
        inertia.M22 = (Real)(3.0 / 10.0) * mass * radius * radius;
        inertia.M33 = mass * ((Real)(3.0 / 20.0) * radius * radius + (Real)(3.0 / 80.0) * height * height);

        com = JVector.Zero;
    }
}