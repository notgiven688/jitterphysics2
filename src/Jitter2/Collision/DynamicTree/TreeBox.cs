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
/// Represents an axis-aligned bounding box with SIMD-friendly memory layout,used for spatial partitioning in
/// acceleration structures such as <see cref="DynamicTree"/>.
/// </summary>
/// <remarks>
/// The struct is explicitly laid out to occupy exactly 8 <see cref="Real"/> fields, enabling efficient SIMD operations and
/// binary comparisons. It contains both minimum and maximum corners of the box as <see cref="JVector"/>, along with
/// unused W components (<c>MinW</c>, <c>MaxW</c>) to match memory alignment.
/// </remarks>
/// <seealso cref="JBoundingBox"/>
/// <seealso cref="JVector"/>
[StructLayout(LayoutKind.Explicit, Size = 8*sizeof(Real))]
public struct TreeBox : IEquatable<TreeBox>
{
    public const Real Epsilon = (Real)1e-12;

    [FieldOffset(0 * sizeof(Real))] public JVector Min;
    [FieldOffset(3 * sizeof(Real))] public Real MinW;

    [FieldOffset(4 * sizeof(Real))] public JVector Max;
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

    public TreeBox(in JVector min, in JVector max)
    {
        this.Min = min;
        this.Max = max;
        this.MinW = 0;
        this.MaxW = 0;
    }

    public TreeBox(in JBoundingBox box)
    {
        this.Min = box.Min;
        this.Max = box.Max;
        this.MinW = 0;
        this.MaxW = 0;
    }

    public readonly JBoundingBox AsJBoundingBox() => new(Min, Max);

    // ─── Helper functions 1:1 like in JBox ───────────────────────────

    public readonly bool Contains(in JVector point)
    {
        return Min.X <= point.X && point.X <= Max.X &&
               Min.Y <= point.Y && point.Y <= Max.Y &&
               Min.Z <= point.Z && point.Z <= Max.Z;
    }

    public readonly JVector Center => (Min + Max) * ((Real)(1.0 / 2.0));

    public readonly Real GetVolume()
    {
        JVector len = Max - Min;
        return len.X * len.Y * len.Z;
    }

    public readonly Real GetSurfaceArea()
    {
        JVector len = Max - Min;
        return (Real)2.0 * (len.X * len.Y + len.Y * len.Z + len.Z * len.X);
    }

    public readonly bool NotDisjoint(in JBoundingBox box)
    {
        return Max.X >= box.Min.X && Min.X <= box.Max.X && Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
               Max.Z >= box.Min.Z && Min.Z <= box.Max.Z;
    }

    public readonly bool Disjoint(in JBoundingBox box)
    {
        return Max.X < box.Min.X || Min.X > box.Max.X || Max.Y < box.Min.Y || Min.Y > box.Max.Y ||
               Max.Z < box.Min.Z || Min.Z > box.Max.Z;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MergedSurface(in TreeBox first, in TreeBox second)
    {
        var vMin = Vector.Min(first.VectorMin, second.VectorMin);
        var vMax = Vector.Max(first.VectorMax, second.VectorMax);
        var extent = Vector.Subtract(vMax, vMin);

        var ex = extent.GetElement(0);
        var ey = extent.GetElement(1);
        var ez = extent.GetElement(2);

        return (Real)2.0 * (ex * ey + ex * ez + ey * ez);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Encompasses(in TreeBox outer, in TreeBox inner)
    {
        var leMin = Vector.LessThanOrEqual(outer.VectorMin, inner.VectorMin);
        var geMax = Vector.GreaterThanOrEqual(outer.VectorMax, inner.VectorMax);

        var mask = Vector.BitwiseAnd(leMin, geMax);
        return Vector.EqualsAll(mask.AsInt32(), Vector.Create(-1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NotDisjoint(in TreeBox first, in TreeBox second)
    {
        var geMin = Vector.GreaterThanOrEqual(first.VectorMax, second.VectorMin);
        var leMax = Vector.LessThanOrEqual(first.VectorMin, second.VectorMax);

        var mask = Vector.BitwiseAnd(geMin, leMax);
        return Vector.EqualsAll(mask.AsInt32(), Vector.Create(-1));
    }

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
