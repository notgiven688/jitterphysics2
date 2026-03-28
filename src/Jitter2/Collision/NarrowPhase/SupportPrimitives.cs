/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Provides built-in primitive support-mapped query types and helper methods for constructing them.
/// </summary>
public static class SupportPrimitives
{
    public static Point CreatePoint() => default;

    public static Sphere CreateSphere(Real radius) => new(radius);

    public static Box CreateBox(JVector halfExtents) => new(halfExtents);

    public static Capsule CreateCapsule(Real radius, Real halfLength) => new(radius, halfLength);

    public static Cylinder CreateCylinder(Real radius, Real halfHeight) => new(radius, halfHeight);

    public static Cone CreateCone(Real radius, Real height) => new(radius, height);

    /// <summary>
    /// Represents a point as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Point : ISupportMappable
    {
        public readonly void SupportMap(in JVector direction, out JVector result) => result = JVector.Zero;

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a sphere as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Sphere(Real radius) : ISupportMappable
    {
        private readonly Real radius = radius > (Real)0.0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));

        public readonly void SupportMap(in JVector direction, out JVector result) => result = JVector.Normalize(direction) * radius;

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a box as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Box(JVector halfExtents) : ISupportMappable
    {
        private readonly JVector halfExtents = halfExtents.X > (Real)0.0 &&
                                               halfExtents.Y > (Real)0.0 &&
                                               halfExtents.Z > (Real)0.0
            ? halfExtents
            : throw new ArgumentOutOfRangeException(nameof(halfExtents));

        public readonly void SupportMap(in JVector direction, out JVector result)
        {
            result.X = MathHelper.SignBit(direction.X) * halfExtents.X;
            result.Y = MathHelper.SignBit(direction.Y) * halfExtents.Y;
            result.Z = MathHelper.SignBit(direction.Z) * halfExtents.Z;
        }

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a Y-axis capsule as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Capsule(Real radius, Real halfLength) : ISupportMappable
    {
        private readonly Real radius = radius > (Real)0.0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));
        private readonly Real halfLength = halfLength >= (Real)0.0 ? halfLength : throw new ArgumentOutOfRangeException(nameof(halfLength));

        public readonly void SupportMap(in JVector direction, out JVector result)
        {
            result = JVector.Normalize(direction) * radius;
            result.Y += MathR.Sign(direction.Y) * halfLength;
        }

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a Y-axis cylinder as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Cylinder(Real radius, Real halfHeight) : ISupportMappable
    {
        private readonly Real radius = radius > (Real)0.0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));
        private readonly Real halfHeight = halfHeight > (Real)0.0 ? halfHeight : throw new ArgumentOutOfRangeException(nameof(halfHeight));

        public readonly void SupportMap(in JVector direction, out JVector result)
        {
            Real sigma = MathR.Sqrt(direction.X * direction.X + direction.Z * direction.Z);

            if (sigma > (Real)0.0)
            {
                result.X = direction.X / sigma * radius;
                result.Y = MathR.Sign(direction.Y) * halfHeight;
                result.Z = direction.Z / sigma * radius;
            }
            else
            {
                result.X = (Real)0.0;
                result.Y = MathR.Sign(direction.Y) * halfHeight;
                result.Z = (Real)0.0;
            }
        }

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a Y-axis cone as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Cone(Real radius, Real height) : ISupportMappable
    {
        private readonly Real radius = radius > (Real)0.0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));
        private readonly Real height = height > (Real)0.0 ? height : throw new ArgumentOutOfRangeException(nameof(height));

        public readonly void SupportMap(in JVector direction, out JVector result)
        {
            const Real zeroEpsilon = (Real)1e-12;

            JVector baseDir = new(direction.X, (Real)0.0, direction.Z);
            baseDir = JVector.NormalizeSafe(baseDir, zeroEpsilon) * radius;
            baseDir.Y = -(Real)0.25 * height;

            if (JVector.Dot(direction, baseDir) >= direction.Y * (Real)0.75 * height)
            {
                result = baseDir;
            }
            else
            {
                result = new JVector((Real)0.0, (Real)0.75 * height, (Real)0.0);
            }
        }

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }
}
