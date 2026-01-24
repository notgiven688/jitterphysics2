/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a capsule shape defined by a radius and the length of its cylindrical section.
/// </summary>
public class CapsuleShape : RigidBodyShape
{
    private Real radius;
    private Real halfLength;

    /// <summary>
    /// Gets or sets the radius of the capsule.
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
    /// Gets or sets the length of the cylindrical part of the capsule, excluding the half-spheres on both ends.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is negative.
    /// </exception>
    public Real Length
    {
        get => (Real)2.0 * halfLength;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(Length));
            halfLength = value / (Real)2.0;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Initializes a new instance of the CapsuleShape class with the specified radius and length. The symmetry axis of the capsule is aligned along the Y-axis.
    /// </summary>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="length">The length of the cylindrical part of the capsule, excluding the half-spheres at both ends.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> is less than or equal to zero or when <paramref name="length"/> is negative.
    /// </exception>
    public CapsuleShape(Real radius = (Real)0.5, Real length = (Real)1.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius, nameof(radius));
        ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));

        this.radius = radius;
        halfLength = (Real)0.5 * length;
        UpdateWorldBoundingBox();
    }

    /// <inheritdoc/>
    public override void SupportMap(in JVector direction, out JVector result)
    {
        // capsule = segment + sphere

        // sphere
        result = JVector.Normalize(direction) * radius;

        // two endpoints of the segment are
        // p_1 = (0, +length/2, 0)
        // p_2 = (0, -length/2, 0)

        // we have to calculate the dot-product with the direction
        // vector to decide whether p_1 or p_2 is the correct support point
        result.Y += MathR.Sign(direction.Y) * halfLength;
    }

    /// <inheritdoc/>
    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    /// <inheritdoc/>
    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        JVector delta = halfLength * orientation.GetBasisY();

        box.Max.X = +radius + MathR.Abs(delta.X);
        box.Max.Y = +radius + MathR.Abs(delta.Y);
        box.Max.Z = +radius + MathR.Abs(delta.Z);

        box.Min = -box.Max;

        box.Min += position;
        box.Max += position;
    }

    /// <inheritdoc/>
    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        Real length = (Real)2.0 * halfLength;

        Real massSphere = (Real)(4.0 / 3.0) * MathR.PI * radius * radius * radius;
        Real massCylinder = MathR.PI * radius * radius * length;

        inertia = JMatrix.Identity;

        inertia.M11 = massCylinder * ((Real)(1.0 / 12.0) * length * length + (Real)(1.0 / 4.0) * radius * radius) + massSphere *
            ((Real)(2.0 / 5.0) * radius * radius + (Real)(1.0 / 4.0) * length * length + (Real)(3.0 / 8.0) * length * radius);
        inertia.M22 = (Real)(1.0 / 2.0) * massCylinder * radius * radius + (Real)(2.0 / 5.0) * massSphere * radius * radius;
        inertia.M33 = massCylinder * ((Real)(1.0 / 12.0) * length * length + (Real)(1.0 / 4.0) * radius * radius) + massSphere *
            ((Real)(2.0 / 5.0) * radius * radius + (Real)(1.0 / 4.0) * length * length + (Real)(3.0 / 8.0) * length * radius);

        mass = massCylinder + massSphere;
        com = JVector.Zero;
    }
}