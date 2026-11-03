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
    /// <summary>
    /// Creates a point support primitive at the origin.
    /// </summary>
    public static Point CreatePoint() => default;

    /// <summary>
    /// Creates a sphere support primitive.
    /// </summary>
    /// <param name="radius">The sphere radius.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> is less than or equal to zero or not finite.
    /// </exception>
    public static Sphere CreateSphere(Real radius) => new(radius);

    /// <summary>
    /// Creates a box support primitive.
    /// </summary>
    /// <param name="halfExtents">The positive half-extents of the box.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any component of <paramref name="halfExtents"/> is less than or equal to zero or not finite.
    /// </exception>
    public static Box CreateBox(JVector halfExtents) => new(halfExtents);

    /// <summary>
    /// Creates a capsule support primitive whose symmetry axis is the Y-axis.
    /// </summary>
    /// <param name="radius">The capsule radius.</param>
    /// <param name="halfLength">The non-negative half-length of the capsule segment.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> is less than or equal to zero, when
    /// <paramref name="halfLength"/> is negative, or when either value is not finite.
    /// </exception>
    public static Capsule CreateCapsule(Real radius, Real halfLength) => new(radius, halfLength);

    /// <summary>
    /// Creates a cylinder support primitive whose symmetry axis is the Y-axis.
    /// </summary>
    /// <param name="radius">The cylinder radius.</param>
    /// <param name="halfHeight">The positive half-height of the cylinder.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> or <paramref name="halfHeight"/> is less than or equal to zero
    /// or not finite.
    /// </exception>
    public static Cylinder CreateCylinder(Real radius, Real halfHeight) => new(radius, halfHeight);

    /// <summary>
    /// Creates a cone support primitive whose symmetry axis is the Y-axis.
    /// </summary>
    /// <param name="radius">The cone radius.</param>
    /// <param name="height">The cone height.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> or <paramref name="height"/> is less than or equal to zero
    /// or not finite.
    /// </exception>
    public static Cone CreateCone(Real radius, Real height) => new(radius, height);

    /// <summary>
    /// Represents a point as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Point : ISupportFeature
    {
        public readonly void SupportMap(in JVector direction, out JVector result) => result = JVector.Zero;

        /// <inheritdoc/>
        public readonly int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices)
        {
            SupportMap(direction, out vertices[0]);
            return 1;
        }

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a sphere as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Sphere(Real radius) : ISupportFeature
    {
        private readonly Real radius = ArgumentCheck.Positive(radius, nameof(radius));

        public readonly void SupportMap(in JVector direction, out JVector result) => result = JVector.Normalize(direction) * radius;

        /// <inheritdoc/>
        public readonly int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices)
        {
            SupportMap(direction, out vertices[0]);
            return 1;
        }

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a box as a lightweight support-mapped query primitive.
    /// </summary>
    public readonly struct Box(JVector halfExtents) : ISupportFeature
    {
        private readonly JVector halfExtents = ArgumentCheck.PositiveComponents(halfExtents, nameof(halfExtents));

        public readonly void SupportMap(in JVector direction, out JVector result)
        {
            result.X = MathHelper.SignBit(direction.X) * halfExtents.X;
            result.Y = MathHelper.SignBit(direction.Y) * halfExtents.Y;
            result.Z = MathHelper.SignBit(direction.Z) * halfExtents.Z;
        }

        /// <inheritdoc/>
        public readonly int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices) =>
            SupportFeatureSampler.Sample(this, direction, tolerance, vertices);

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a capsule as a lightweight support-mapped query primitive. The symmetry axis is the Y-axis.
    /// </summary>
    public readonly struct Capsule(Real radius, Real halfLength) : ISupportFeature
    {
        private readonly Real radius = ArgumentCheck.Positive(radius, nameof(radius));
        private readonly Real halfLength = ArgumentCheck.NonNegative(halfLength, nameof(halfLength));

        public readonly void SupportMap(in JVector direction, out JVector result)
        {
            result = JVector.Normalize(direction) * radius;
            result.Y += MathR.Sign(direction.Y) * halfLength;
        }

        /// <inheritdoc/>
        public readonly int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices) =>
            SupportFeatureSampler.Sample(this, direction, tolerance, vertices);

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a cylinder as a lightweight support-mapped query primitive. The symmetry axis is the Y-axis.
    /// </summary>
    public readonly struct Cylinder(Real radius, Real halfHeight) : ISupportFeature
    {
        private readonly Real radius = ArgumentCheck.Positive(radius, nameof(radius));
        private readonly Real halfHeight = ArgumentCheck.Positive(halfHeight, nameof(halfHeight));

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

        /// <inheritdoc/>
        public readonly int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices) =>
            SupportFeatureSampler.Sample(this, direction, tolerance, vertices);

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }

    /// <summary>
    /// Represents a cone as a lightweight support-mapped query primitive. The symmetry axis is the Y-axis.
    /// The cone is centered at its centroid: the base sits at Y = -height/4 and the apex at Y = 3*height/4.
    /// </summary>
    public readonly struct Cone(Real radius, Real height) : ISupportFeature
    {
        private readonly Real radius = ArgumentCheck.Positive(radius, nameof(radius));
        private readonly Real height = ArgumentCheck.Positive(height, nameof(height));

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

        /// <inheritdoc/>
        public readonly int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices) =>
            SupportFeatureSampler.Sample(this, direction, tolerance, vertices);

        public readonly void GetCenter(out JVector point) => point = JVector.Zero;
    }
}
