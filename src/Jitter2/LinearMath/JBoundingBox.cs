/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>
/// Represents an axis-aligned bounding box (AABB), a rectangular bounding box whose edges are parallel to the coordinate axes.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 6 * sizeof(Real))]
public struct JBoundingBox(JVector min, JVector max) : IEquatable<JBoundingBox>
{
    public const Real Epsilon = (Real)1e-12;

    /// <summary>
    /// Describes how one bounding box relates to another spatially.
    /// </summary>
    public enum ContainmentType
    {
        /// <summary>
        /// The two boxes are completely separated and do not touch or overlap.
        /// </summary>
        Disjoint,

        /// <summary>
        /// The other box is completely inside this box.
        /// </summary>
        Contains,

        /// <summary>
        /// The boxes overlap, but neither completely contains the other.
        /// </summary>
        Intersects
    }

    /// <summary>
    /// The minimum corner of the bounding box (smallest X, Y, Z coordinates).
    /// </summary>
    [FieldOffset(0 * sizeof(Real))]
    public JVector Min = min;

    /// <summary>
    /// The maximum corner of the bounding box (largest X, Y, Z coordinates).
    /// </summary>
    [FieldOffset(3 * sizeof(Real))]
    public JVector Max = max;

    /// <summary>
    /// A bounding box covering the entire valid range of coordinates.
    /// </summary>
    public static readonly JBoundingBox LargeBox;

    /// <summary>
    /// An inverted bounding box initialized with Min > Max, useful for growing a box from scratch.
    /// </summary>
    public static readonly JBoundingBox SmallBox;

    static JBoundingBox()
    {
        LargeBox.Min = new JVector(Real.MinValue);
        LargeBox.Max = new JVector(Real.MaxValue);
        SmallBox.Min = new JVector(Real.MaxValue);
        SmallBox.Max = new JVector(Real.MinValue);
    }

    /// <summary>
    /// Returns a string representation of the <see cref="JBoundingBox"/>.
    /// </summary>
    public readonly override string ToString()
    {
        return $"Min={{{Min}}}, Max={{{Max}}}";
    }

    // Prefer using CreateTransformed for clarity and to avoid mutations.
    [Obsolete($"Use static {nameof(CreateTransformed)} instead.")]
    public void Transform(in JMatrix orientation)
    {
        JVector halfExtents = (Real)0.5 * (Max - Min);
        JVector center = (Real)0.5 * (Max + Min);

        JVector.Transform(center, orientation, out center);

        JMatrix.Absolute(orientation, out var abs);
        JVector.Transform(halfExtents, abs, out halfExtents);

        Max = center + halfExtents;
        Min = center - halfExtents;
    }

    /// <summary>
    /// Creates a new AABB that encloses the original box after it has been rotated by the given orientation matrix.
    /// </summary>
    /// <remarks>
    /// Rotating an AABB usually results in a larger AABB to fit the rotated geometry.
    /// </remarks>
    /// <param name="box">The original bounding box.</param>
    /// <param name="orientation">The rotation matrix to apply.</param>
    /// <returns>A new AABB enclosing the rotated box.</returns>
    public static JBoundingBox CreateTransformed(in JBoundingBox box, in JMatrix orientation)
    {
        JVector halfExtents = (Real)0.5 * (box.Max - box.Min);
        JVector center = (Real)0.5 * (box.Max + box.Min);

        JVector.Transform(center, orientation, out center);

        JMatrix.Absolute(orientation, out var abs);
        JVector.Transform(halfExtents, abs, out halfExtents);

        JBoundingBox result;
        result.Max = center + halfExtents;
        result.Min = center - halfExtents;

        return result;
    }

    private static bool Intersect1D(Real start, Real dir, Real min, Real max,
        ref Real enter, ref Real exit)
    {
        if (dir * dir < Epsilon * Epsilon) return start >= min && start <= max;

        Real t0 = (min - start) / dir;
        Real t1 = (max - start) / dir;

        if (t0 > t1)
        {
            (t0, t1) = (t1, t0);
        }

        if (t0 > exit || t1 < enter) return false;

        if (t0 > enter) enter = t0;
        if (t1 < exit) exit = t1;
        return true;
    }

    /// <summary>
    /// Checks if a finite line segment intersects this bounding box.
    /// </summary>
    /// <param name="origin">The start point of the segment.</param>
    /// <param name="direction">The vector from start to end (End = Origin + Direction).</param>
    /// <returns><c>true</c> if the segment passes through the box; otherwise, <c>false</c>.</returns>
    public readonly bool SegmentIntersect(in JVector origin, in JVector direction)
    {
        Real enter = (Real)0.0, exit = (Real)1.0;

        if (!Intersect1D(origin.X, direction.X, Min.X, Max.X, ref enter, ref exit))
            return false;

        if (!Intersect1D(origin.Y, direction.Y, Min.Y, Max.Y, ref enter, ref exit))
            return false;

        if (!Intersect1D(origin.Z, direction.Z, Min.Z, Max.Z, ref enter, ref exit))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if an infinite ray intersects this bounding box.
    /// </summary>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray (not necessarily normalized).</param>
    /// <returns><c>true</c> if the ray intersects the box; otherwise, <c>false</c>.</returns>
    public readonly bool RayIntersect(in JVector origin, in JVector direction)
    {
        Real enter = (Real)0.0, exit = Real.MaxValue;

        if (!Intersect1D(origin.X, direction.X, Min.X, Max.X, ref enter, ref exit))
            return false;

        if (!Intersect1D(origin.Y, direction.Y, Min.Y, Max.Y, ref enter, ref exit))
            return false;

        if (!Intersect1D(origin.Z, direction.Z, Min.Z, Max.Z, ref enter, ref exit))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if an infinite ray intersects this bounding box and calculates the entry distance.
    /// </summary>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray (not necessarily normalized).</param>
    /// <param name="enter">Outputs the distance along the direction vector where the ray enters the box. Returns 0 if the origin is inside.</param>
    /// <returns><c>true</c> if the ray intersects the box; otherwise, <c>false</c>.</returns>
    public readonly bool RayIntersect(in JVector origin, in JVector direction, out Real enter)
    {
        enter = (Real)0.0;
        Real exit = Real.MaxValue;

        if (!Intersect1D(origin.X, direction.X, Min.X, Max.X, ref enter, ref exit))
            return false;

        if (!Intersect1D(origin.Y, direction.Y, Min.Y, Max.Y, ref enter, ref exit))
            return false;

        if (!Intersect1D(origin.Z, direction.Z, Min.Z, Max.Z, ref enter, ref exit))
            return false;

        return true;
    }

    /// <summary>
    /// Determines whether the bounding box contains the specified point.
    /// </summary>
    public readonly bool Contains(in JVector point)
    {
        return Min.X <= point.X && point.X <= Max.X &&
               Min.Y <= point.Y && point.Y <= Max.Y &&
               Min.Z <= point.Z && point.Z <= Max.Z;
    }

    /// <summary>
    /// Retrieves the 8 corners of the bounding box.
    /// </summary>
    /// <param name="destination">A span of at least 8 JVectors to hold the corners.</param>
    public readonly void GetCorners(Span<JVector> destination)
    {
        destination[0] = new(Min.X, Max.Y, Max.Z);
        destination[1] = new(Max.X, Max.Y, Max.Z);
        destination[2] = new(Max.X, Min.Y, Max.Z);
        destination[3] = new(Min.X, Min.Y, Max.Z);
        destination[4] = new(Min.X, Max.Y, Min.Z);
        destination[5] = new(Max.X, Max.Y, Min.Z);
        destination[6] = new(Max.X, Min.Y, Min.Z);
        destination[7] = new(Min.X, Min.Y, Min.Z);
    }

    // Marked obsolete to guide users toward the preferred static method.
    [Obsolete($"Use static {nameof(AddPointInPlace)} instead.")]
    public void AddPoint(in JVector point)
    {
        JVector.Max(Max, point, out Max);
        JVector.Min(Min, point, out Min);
    }

    /// <summary>
    /// Expands the bounding box to include the specified point.
    /// </summary>
    /// <param name="box">The bounding box to expand.</param>
    /// <param name="point">The point to include.</param>
    public static void AddPointInPlace(ref JBoundingBox box, in JVector point)
    {
        JVector.Max(box.Max, point, out box.Max);
        JVector.Min(box.Min, point, out box.Min);
    }

    /// <summary>
    /// Creates a bounding box that exactly encompasses a collection of points.
    /// </summary>
    /// <param name="points">The collection of points to encompass.</param>
    /// <returns>A bounding box containing all the points.</returns>
    public static JBoundingBox CreateFromPoints(IEnumerable<JVector> points)
    {
        JBoundingBox box = SmallBox;

        foreach (var point in points)
        {
            AddPointInPlace(ref box, point);
        }

        return box;
    }



    /// <summary>
    /// Determines the relationship between this box and another box.
    /// </summary>
    /// <param name="box">The other bounding box to test.</param>
    /// <returns>
    /// <see cref="ContainmentType.Disjoint"/> if they do not touch.<br/>
    /// <see cref="ContainmentType.Contains"/> if <paramref name="box"/> is strictly inside this box.<br/>
    /// <see cref="ContainmentType.Intersects"/> if they overlap but one does not strictly contain the other.
    /// </returns>
    public readonly ContainmentType Contains(in JBoundingBox box)
    {
        ContainmentType result = ContainmentType.Disjoint;
        if (Max.X >= box.Min.X && Min.X <= box.Max.X && Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
            Max.Z >= box.Min.Z && Min.Z <= box.Max.Z)
        {
            result = Min.X <= box.Min.X && box.Max.X <= Max.X && Min.Y <= box.Min.Y && box.Max.Y <= Max.Y &&
                     Min.Z <= box.Min.Z && box.Max.Z <= Max.Z
                ? ContainmentType.Contains
                : ContainmentType.Intersects;
        }

        return result;
    }

    /// <summary>
    /// Determines whether the two boxes intersect or overlap.
    /// </summary>
    /// <returns><c>true</c> if the boxes overlap; <c>false</c> if they are disjoint.</returns>
    [Obsolete($"Use !{nameof(Disjoint)} instead.")]
    public static bool NotDisjoint(in JBoundingBox left, in JBoundingBox right)
    {
        return left.Max.X >= right.Min.X && left.Min.X <= right.Max.X && left.Max.Y >= right.Min.Y && left.Min.Y <= right.Max.Y &&
               left.Max.Z >= right.Min.Z && left.Min.Z <= right.Max.Z;
    }

    /// <summary>
    /// Determines whether the two boxes are completely separated (disjoint).
    /// </summary>
    /// <param name="left">The first bounding box.</param>
    /// <param name="right">The second bounding box.</param>
    /// <returns><see langword="true"/> if there is a gap between the boxes on at least one axis; otherwise, <see langword="false"/>.</returns>
    public static bool Disjoint(in JBoundingBox left, in JBoundingBox right)
    {
        return left.Max.X < right.Min.X || left.Min.X > right.Max.X || left.Max.Y < right.Min.Y || left.Min.Y > right.Max.Y ||
               left.Max.Z < right.Min.Z || left.Min.Z > right.Max.Z;
    }

    /// <summary>
    /// Determines whether the <paramref name="outer"/> box completely contains the <paramref name="inner"/> box.
    /// </summary>
    /// <param name="outer">The outer bounding box.</param>
    /// <param name="inner">The inner bounding box to test.</param>
    /// <returns><see langword="true"/> if <paramref name="inner"/> is entirely within the boundaries of <paramref name="outer"/>; otherwise, <see langword="false"/>.</returns>
    public static bool Contains(in JBoundingBox outer, in JBoundingBox inner)
    {
        return outer.Min.X <= inner.Min.X && outer.Max.X >= inner.Max.X && outer.Min.Y <= inner.Min.Y && outer.Max.Y >= inner.Max.Y &&
               outer.Min.Z <= inner.Min.Z && outer.Max.Z >= inner.Max.Z;
    }

    /// <summary>
    /// Determines whether the <paramref name="outer"/> box completely contains the <paramref name="inner"/> box.
    /// </summary>
    /// <remarks>This is an alias for <see cref="Contains(in JBoundingBox, in JBoundingBox)"/>.</remarks>
    [Obsolete($"Use {nameof(Contains)} instead.")]
    public static bool Encompasses(in JBoundingBox outer, in JBoundingBox inner)
    {
        return outer.Min.X <= inner.Min.X && outer.Max.X >= inner.Max.X && outer.Min.Y <= inner.Min.Y && outer.Max.Y >= inner.Max.Y &&
               outer.Min.Z <= inner.Min.Z && outer.Max.Z >= inner.Max.Z;
    }

    /// <summary>
    /// Creates a new bounding box that is the union of two other bounding boxes.
    /// </summary>
    /// <param name="original">The first bounding box.</param>
    /// <param name="additional">The second bounding box.</param>
    /// <returns>A bounding box encompassing both inputs.</returns>
    public static JBoundingBox CreateMerged(in JBoundingBox original, in JBoundingBox additional)
    {
        CreateMerged(original, additional, out JBoundingBox result);
        return result;
    }

    /// <summary>
    /// Creates a new bounding box that is the union of two other bounding boxes.
    /// </summary>
    /// <param name="original">The first bounding box.</param>
    /// <param name="additional">The second bounding box.</param>
    /// <param name="result">Output: A bounding box encompassing both inputs.</param>
    public static void CreateMerged(in JBoundingBox original, in JBoundingBox additional, out JBoundingBox result)
    {
        JVector.Min(original.Min, additional.Min, out result.Min);
        JVector.Max(original.Max, additional.Max, out result.Max);
    }

    /// <summary>
    /// Gets the center point of the bounding box.
    /// </summary>
    public readonly JVector Center => (Min + Max) * ((Real)(1.0 / 2.0));

    /// <summary>
    /// Calculates the volume of the bounding box.
    /// </summary>
    public readonly Real GetVolume()
    {
        JVector len = Max - Min;
        return len.X * len.Y * len.Z;
    }

    /// <summary>
    /// Calculates the surface area of the bounding box.
    /// </summary>
    public readonly Real GetSurfaceArea()
    {
        JVector len = Max - Min;
        return (Real)2.0 * (len.X * len.Y + len.Y * len.Z + len.Z * len.X);
    }

    public readonly bool Equals(JBoundingBox other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is JBoundingBox other && Equals(other);
    }

    public readonly override int GetHashCode() => HashCode.Combine(Min, Max);

    public static bool operator ==(JBoundingBox left, JBoundingBox right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JBoundingBox left, JBoundingBox right)
    {
        return !(left == right);
    }
}