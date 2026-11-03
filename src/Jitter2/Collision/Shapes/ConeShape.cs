/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a cone shape defined by a base radius and height.
/// </summary>
public class ConeShape : RigidBodyShape
{
    private Real radius;
    private Real height;

    /// <inheritdoc/>
    protected internal override void DebugDrawLocal<TSink>(ref TSink triangleSink, in DebugDrawSettings settings)
    {
        ShapeDebugDraw.Cone(ref triangleSink, radius, height, settings.Segments);
    }

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
            radius = ArgumentCheck.Positive(value, nameof(Radius));
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
            height = ArgumentCheck.Positive(value, nameof(Height));
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Creates a cone with its symmetry axis along the local Y-axis.
    /// </summary>
    /// <param name="radius">The radius of the cone at its base.</param>
    /// <param name="height">The height of the cone.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    public ConeShape(Real radius = (Real)0.5, Real height = (Real)1.0)
    {
        this.radius = ArgumentCheck.Positive(radius, nameof(radius));
        this.height = ArgumentCheck.Positive(height, nameof(height));
        UpdateWorldBoundingBox();
    }

    /// <inheritdoc/>
    public override void SupportMap(in JVector direction, out JVector result)
    {
        const Real zeroEpsilon = (Real)1e-12;
        // cone = convex hull of disk and point

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

    private const Real Sqrt1Over2 = (Real)0.70710678118654752;

    // Unit circle in the XZ plane, eight points in winding order.
    private static readonly Real[] rim = [(Real)1.0, (Real)0.0, Sqrt1Over2, Sqrt1Over2, (Real)0.0, (Real)1.0,
        -Sqrt1Over2, Sqrt1Over2, -(Real)1.0, (Real)0.0, -Sqrt1Over2, -Sqrt1Over2, (Real)0.0, -(Real)1.0,
        Sqrt1Over2, -Sqrt1Over2];

    /// <inheritdoc/>
    public override int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices)
    {
        Real sideSq = direction.X * direction.X + direction.Z * direction.Z;
        Real toleranceSq = tolerance * tolerance * direction.LengthSquared();
        Real baseY = -(Real)0.25 * height;

        // Almost along the axis below the cone, it stands on its base, approximated by an octagon
        // on the rim.
        if (direction.Y < (Real)0.0 && sideSq < toleranceSq)
        {
            for (int i = 0; i < 8; i++)
            {
                vertices[i] = new JVector(rim[2 * i] * radius, baseY, rim[2 * i + 1] * radius);
            }

            return 8;
        }

        // The line from the apex to the rim facing the direction has the outward normal
        // (height, radius) in the plane of the axis and the direction. Almost along that normal,
        // the cone rests on this line.
        if (sideSq > (Real)0.0)
        {
            Real side = MathR.Sqrt(sideSq);
            Real cross = side * radius - direction.Y * height;
            Real dot = side * height + direction.Y * radius;

            if (dot > (Real)0.0 && cross * cross < toleranceSq * (height * height + radius * radius))
            {
                Real scale = radius / side;
                vertices[0] = new JVector(0, (Real)0.75 * height, 0);
                vertices[1] = new JVector(direction.X * scale, baseY, direction.Z * scale);
                return 2;
            }
        }

        // Otherwise it touches at its apex or at a single point of the rim.
        SupportMap(direction, out vertices[0]);
        return 1;
    }

    /// <inheritdoc/>
    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override void CalculateMassInertia(out JSymmetricMatrix inertia, out JVector com, out Real mass)
    {
        mass = (Real)(1.0 / 3.0) * MathR.PI * radius * radius * height;

        inertia = JSymmetricMatrix.Identity;
        inertia.M11 = mass * ((Real)(3.0 / 20.0) * radius * radius + (Real)(3.0 / 80.0) * height * height);
        inertia.M22 = (Real)(3.0 / 10.0) * mass * radius * radius;
        inertia.M33 = mass * ((Real)(3.0 / 20.0) * radius * radius + (Real)(3.0 / 80.0) * height * height);

        com = JVector.Zero;
    }
}
