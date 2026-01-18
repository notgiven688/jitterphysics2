/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Represents an axis-aligned bounding box with SIMD-friendly memory layout, used for spatial partitioning in
/// acceleration structures such as <see cref="DynamicTree"/>.
/// </summary>
/// <remarks>
/// The struct is explicitly laid out to occupy exactly 8 <see cref="Real"/> fields, enabling efficient SIMD operations and
/// binary comparisons. It contains both minimum and maximum corners of the box as <see cref="JVector"/>, along with
/// unused W components (<c>MinW</c>, <c>MaxW</c>) to match memory alignment.
/// </remarks>
/// <seealso cref="JBoundingBox"/>
/// <seealso cref="JVector"/>
[StructLayout(LayoutKind.Explicit, Size = 8 * sizeof(Real))]
public struct TreeBox : IEquatable<TreeBox>
{
    /// <summary>
    /// Small epsilon value used for ray-box intersection tests.
    /// </summary>
    public const Real Epsilon = (Real)1e-12;

    /// <summary>The minimum corner of the bounding box.</summary>
    [FieldOffset(0 * sizeof(Real))] public JVector Min;

    /// <summary>Padding for SIMD alignment. Not used directly.</summary>
    [FieldOffset(3 * sizeof(Real))] public Real MinW;

    /// <summary>The maximum corner of the bounding box.</summary>
    [FieldOffset(4 * sizeof(Real))] public JVector Max;

    /// <summary>Padding for SIMD alignment. Not used directly.</summary>
    [FieldOffset(7 * sizeof(Real))] public Real MaxW;

    /// <summary>
    /// Returns a <see cref="VectorReal"/> view of the <see cref="Min"/> vector,
    /// reinterpreted as a SIMD vector for efficient processing.
    /// This enables vectorized operations on the bounding box's minimum corner.
    /// </summary>
    public readonly ref VectorReal VectorMin => ref Unsafe.As<JVector, VectorReal>(ref Unsafe.AsRef(in this.Min));

    /// <summary>
    /// Returns a <see cref="VectorReal"/> view of the <see cref="Max"/> vector,
    /// reinterpreted as a SIMD vector for efficient processing.
    /// This enables vectorized operations on the bounding box's maximum corner.
    /// </summary>
    public readonly ref VectorReal VectorMax => ref Unsafe.As<JVector, VectorReal>(ref Unsafe.AsRef(in this.Max));

    /// <summary>
    /// Creates a new <see cref="TreeBox"/> from minimum and maximum corner vectors.
    /// </summary>
    public TreeBox(in JVector min, in JVector max)
    {
        this.Min = min;
        this.Max = max;
        this.MinW = 0;
        this.MaxW = 0;
    }

    /// <summary>
    /// Creates a new <see cref="TreeBox"/> from an existing <see cref="JBoundingBox"/>.
    /// </summary>
    public TreeBox(in JBoundingBox box)
    {
        this.Min = box.Min;
        this.Max = box.Max;
        this.MinW = 0;
        this.MaxW = 0;
    }

    /// <summary>
    /// Converts this <see cref="TreeBox"/> to a <see cref="JBoundingBox"/>.
    /// </summary>
    public readonly JBoundingBox AsJBoundingBox() => new(Min, Max);

    // ─── Helper functions 1:1 like in JBox ───────────────────────────

    /// <summary>
    /// Determines whether this box contains the specified point.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns><c>true</c> if the point is within the boundaries of this box; otherwise, <c>false</c>.</returns>
    public readonly bool Contains(in JVector point)
    {
        return Min.X <= point.X && point.X <= Max.X &&
               Min.Y <= point.Y && point.Y <= Max.Y &&
               Min.Z <= point.Z && point.Z <= Max.Z;
    }

    /// <summary>
    /// Gets the center point of the bounding box.
    /// </summary>
    public readonly JVector Center => (Min + Max) * ((Real)(1.0 / 2.0));

    /// <summary>
    /// Determines whether this box intersects with or overlaps the specified box.
    /// </summary>
    /// <param name="box">The box to check against.</param>
    /// <returns><c>true</c> if the boxes overlap; <c>false</c> if they are completely separated.</returns>
    [Obsolete($"Use !{nameof(Disjoint)} instead.")]
    public readonly bool NotDisjoint(in JBoundingBox box)
    {
        return Max.X >= box.Min.X && Min.X <= box.Max.X && Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
               Max.Z >= box.Min.Z && Min.Z <= box.Max.Z;
    }

    /// <summary>
    /// Determines whether this box is completely separated from (does not overlap) the specified box.
    /// </summary>
    /// <param name="box">The box to check against.</param>
    /// <returns><c>true</c> if the boxes are separated by a gap on at least one axis; otherwise, <c>false</c>.</returns>
    public readonly bool Disjoint(in JBoundingBox box)
    {
        return Max.X < box.Min.X || Min.X > box.Max.X || Max.Y < box.Min.Y || Min.Y > box.Max.Y ||
               Max.Z < box.Min.Z || Min.Z > box.Max.Z;
    }

    /// <summary>
    /// Determines whether this box completely encloses the specified box.
    /// </summary>
    /// <param name="box">The box to check for containment.</param>
    /// <returns><c>true</c> if the <paramref name="box"/> is entirely inside this box; otherwise, <c>false</c>.</returns>
    public readonly bool Contains(in JBoundingBox box)
    {
        return Min.X <= box.Min.X && Max.X >= box.Max.X && Min.Y <= box.Min.Y && Max.Y >= box.Max.Y &&
               Min.Z <= box.Min.Z && Max.Z >= box.Max.Z;
    }

    /// <summary>
    /// Determines whether this box completely encloses the specified box.
    /// </summary>
    /// <remarks>This is an alias for <see cref="Contains(in JBoundingBox)"/>.</remarks>
    [Obsolete($"Use {nameof(Contains)} instead.")]
    public readonly bool Encompasses(in JBoundingBox box)
    {
        return Min.X <= box.Min.X && Max.X >= box.Max.X && Min.Y <= box.Min.Y && Max.Y >= box.Max.Y &&
               Min.Z <= box.Min.Z && Max.Z >= box.Max.Z;
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
    /// <param name="direction">The vector defining the direction and length of the segment (End Point = Origin + Direction).</param>
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
    /// <param name="enter">Outputs the scalar distance along the direction vector where the ray enters the box. If inside, this is 0.</param>
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

    public readonly override string ToString()
    {
        return $"Min={{{Min}}}, Max={{{Max}}}";
    }

    // ─── Helper functions with SIMD support ───────────────────────────

    /// <summary>
    /// Determines whether this box is completely separated from the specified <see cref="TreeBox"/>.
    /// </summary>
    /// <param name="box">The box to check against.</param>
    /// <returns><c>true</c> if the boxes do not overlap; <c>false</c> if they touch or overlap.</returns>
    public readonly bool Disjoint(in TreeBox box) => TreeBox.Disjoint(this, box);

    /// <summary>
    /// Determines whether this box completely encloses the specified <see cref="TreeBox"/>.
    /// </summary>
    /// <param name="box">The box to check for containment.</param>
    /// <returns><c>true</c> if the <paramref name="box"/> is entirely inside this box.</returns>
    public readonly bool Contains(in TreeBox box) => TreeBox.Contains(this, box);

    /// <summary>
    /// Calculates the surface area of the bounding box.
    /// </summary>
    /// <returns>The total surface area ($2(xy + yz + zx)$).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double GetSurfaceArea()
    {
        var extent = Vector.Subtract(VectorMax, VectorMin);

        double ex = extent.GetElement(0);
        double ey = extent.GetElement(1);
        double ez = extent.GetElement(2);

        return 2.0 * (ex * ey + ey * ez + ez * ex);
    }

    /// <summary>
    /// Calculates the surface area of a hypothetical box that would result from merging the two specified boxes.
    /// </summary>
    /// <param name="first">The first box.</param>
    /// <param name="second">The second box.</param>
    /// <returns>The surface area of the union box. Used for SAH (Surface Area Heuristic) costs.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MergedSurface(in TreeBox first, in TreeBox second)
    {
        var vMin = Vector.Min(first.VectorMin, second.VectorMin);
        var vMax = Vector.Max(first.VectorMax, second.VectorMax);
        var extent = Vector.Subtract(vMax, vMin);

        double ex = extent.GetElement(0);
        double ey = extent.GetElement(1);
        double ez = extent.GetElement(2);

        return 2.0d * (ex * ey + ex * ez + ey * ez);
    }

    /// <summary>
    /// Determines whether the outer box completely contains the inner box using SIMD instructions.
    /// </summary>
    /// <param name="outer">The potential container box.</param>
    /// <param name="inner">The box to check for inclusion.</param>
    /// <returns><c>true</c> if <paramref name="inner"/> is fully inside <paramref name="outer"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(in TreeBox outer, in TreeBox inner)
    {
        var leMin = Vector.LessThanOrEqual(outer.VectorMin, inner.VectorMin);
        var geMax = Vector.GreaterThanOrEqual(outer.VectorMax, inner.VectorMax);

        var mask = Vector.BitwiseAnd(leMin, geMax);
        return Vector.EqualsAll(mask.AsInt32(), Vector.Create(-1));
    }

    /// <summary>
    /// Determines whether two boxes overlap using SIMD instructions.
    /// </summary>
    /// <param name="first">The first box.</param>
    /// <param name="second">The second box.</param>
    /// <returns><c>true</c> if the boxes intersect; <c>false</c> if they are disjoint.</returns>
    [Obsolete($"Use !{nameof(Disjoint)} instead.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NotDisjoint(in TreeBox first, in TreeBox second)
    {
        var geMin = Vector.GreaterThanOrEqual(first.VectorMax, second.VectorMin);
        var leMax = Vector.LessThanOrEqual(first.VectorMin, second.VectorMax);

        var mask = Vector.BitwiseAnd(geMin, leMax);
        return Vector.EqualsAll(mask.AsInt32(), Vector.Create(-1));
    }

    /// <summary>
    /// Determines whether two boxes are completely separated (disjoint) using SIMD instructions.
    /// </summary>
    /// <param name="first">The first box.</param>
    /// <param name="second">The second box.</param>
    /// <returns><c>true</c> if the boxes do not overlap on any axis; <c>false</c> if they touch or intersect.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Disjoint(in TreeBox first, in TreeBox second)
    {
        // If first.Max < second.Min OR first.Min > second.Max on any axis,
        // the two boxes cannot overlap.
        var ltMin = Vector.LessThan(first.VectorMax, second.VectorMin);
        var gtMax = Vector.GreaterThan(first.VectorMin, second.VectorMax);

        var mask = Vector.BitwiseOr(ltMin, gtMax);
        return !Vector.EqualsAll(mask.AsInt32(), Vector.Create(0));
    }

    /// <summary>
    /// Creates a new box that encompasses both input boxes.
    /// </summary>
    /// <param name="first">The first box.</param>
    /// <param name="second">The second box.</param>
    /// <param name="result">The resulting union box.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateMerged(in TreeBox first, in TreeBox second, out TreeBox result)
    {
        Unsafe.SkipInit(out result);
        result.VectorMin = Vector.Min(first.VectorMin, second.VectorMin);
        result.VectorMax = Vector.Max(first.VectorMax, second.VectorMax);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(in TreeBox first, in TreeBox second)
    {
        var a = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in first), 1));
        var b = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in second), 1));
        return a.SequenceEqual(b); // SIMD-accelerated in .NET ≥ 5
    }

    public readonly bool Equals(TreeBox other)
    {
        return Equals(this, other);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is TreeBox other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(Min, MinW, Max, MaxW);
    }

    public static bool operator ==(TreeBox left, TreeBox right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TreeBox left, TreeBox right)
    {
        return !(left == right);
    }
}
