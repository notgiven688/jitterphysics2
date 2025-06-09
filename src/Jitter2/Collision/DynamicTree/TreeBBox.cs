/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
/// <seealso cref="JBBox"/>
/// <seealso cref="JVector"/>
[StructLayout(LayoutKind.Explicit, Size = 8*sizeof(Real))]
public struct TreeBBox
{
    public const Real Epsilon = (Real)1e-12;

    [FieldOffset(0 * sizeof(Real))] public JVector Min;
    [FieldOffset(3 * sizeof(Real))] public Real MinW;

    [FieldOffset(4 * sizeof(Real))] public JVector Max;
    [FieldOffset(7 * sizeof(Real))] public Real MaxW;

    public TreeBBox(in JVector min, in JVector max)
    {
        this.Min = min;
        this.Max = max;
        this.MinW = 0;
        this.MaxW = 0;
    }

    public TreeBBox(in JBBox box)
    {
        this.Min = box.Min;
        this.Max = box.Max;
        this.MinW = 0;
        this.MaxW = 0;
    }

    public JBBox AsJBBox() => new JBBox(Min, Max);

    public static TreeBBox FromJBBox(JBBox box) => new TreeBBox(box.Min, box.Max);

    // ─── Helper functions 1:1 like in JBBox ───────────────────────────

    public bool Contains(in JVector point)
    {
        return Min.X <= point.X && point.X <= Max.X &&
               Min.Y <= point.Y && point.Y <= Max.Y &&
               Min.Z <= point.Z && point.Z <= Max.Z;
    }

    public readonly JVector Center => (Min + Max) * ((Real)(1.0 / 2.0));

    public Real GetVolume()
    {
        JVector len = Max - Min;
        return len.X * len.Y * len.Z;
    }

    public Real GetSurfaceArea()
    {
        JVector len = Max - Min;
        return (Real)2.0 * (len.X * len.Y + len.Y * len.Z + len.Z * len.X);
    }

    public readonly bool NotDisjoint(in JBBox box)
    {
        return Max.X >= box.Min.X && Min.X <= box.Max.X && Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
               Max.Z >= box.Min.Z && Min.Z <= box.Max.Z;
    }

    public readonly bool Disjoint(in JBBox box)
    {
        return Max.X < box.Min.X || Min.X > box.Max.X || Max.Y < box.Min.Y || Min.Y > box.Max.Y ||
               Max.Z < box.Min.Z || Min.Z > box.Max.Z;
    }

    public readonly bool Encompasses(in JBBox box)
    {
        return Min.X <= box.Min.X && Max.X >= box.Max.X && Min.Y <= box.Min.Y && Max.Y >= box.Max.Y &&
               Min.Z <= box.Min.Z && Max.Z >= box.Max.Z;
    }

    private bool Intersect1D(Real start, Real dir, Real min, Real max,
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

    public bool SegmentIntersect(in JVector origin, in JVector direction)
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

    public bool RayIntersect(in JVector origin, in JVector direction)
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

    public bool RayIntersect(in JVector origin, in JVector direction, out Real enter)
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

    public override string ToString()
    {
        return $"Min={{{Min}}}, Max={{{Max}}}";
    }

    // ─── Helper functions with SIMD support ───────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static VectorReal AsVectorReal(in JVector v) => Unsafe.As<JVector, VectorReal>(ref Unsafe.AsRef(in v));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MergedSurface(in TreeBBox first, in TreeBBox second)
    {
        var vMin = Vector.Min(AsVectorReal(first.Min), AsVectorReal(second.Min));
        var vMax = Vector.Max(AsVectorReal(first.Max), AsVectorReal(second.Max));
        var extent = Vector.Subtract(vMax, vMin);

        var ex = extent.GetElement(0);
        var ey = extent.GetElement(1);
        var ez = extent.GetElement(2);

        return (Real)2.0 * (ex * ey + ex * ez + ey * ez);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Encompasses(in TreeBBox outer, in TreeBBox inner)
    {
        var leMin = Vector.LessThanOrEqual(AsVectorReal(outer.Min), AsVectorReal(inner.Min));
        var geMax = Vector.GreaterThanOrEqual(AsVectorReal(outer.Max), AsVectorReal(inner.Max));

        var mask = Vector.BitwiseAnd(leMin, geMax);
        return Vector.EqualsAll(mask.AsInt32(), Vector.Create(-1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NotDisjoint(in TreeBBox first, in TreeBBox second)
    {
        var geMin = Vector.GreaterThanOrEqual(AsVectorReal(first.Max), AsVectorReal(second.Min));
        var leMax = Vector.LessThanOrEqual(AsVectorReal(first.Min), AsVectorReal(second.Max));

        var mask = Vector.BitwiseAnd(geMin, leMax);
        return Vector.EqualsAll(mask.AsInt32(), Vector.Create(-1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateMerged(in TreeBBox first, in TreeBBox second, out TreeBBox result)
    {
        Unsafe.SkipInit(out result);
        ref var min = ref Unsafe.As<JVector, VectorReal>(ref Unsafe.AsRef(in result.Min));
        ref var max = ref Unsafe.As<JVector, VectorReal>(ref Unsafe.AsRef(in result.Max));

        min = Vector.Min(AsVectorReal(first.Min), AsVectorReal(second.Min));
        max = Vector.Max(AsVectorReal(first.Max), AsVectorReal(second.Max));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(in TreeBBox first, in TreeBBox second)
    {
        var a = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in first), 1));
        var b = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in second), 1));
        return a.SequenceEqual(b); // SIMD-accelerated in .NET ≥ 5
    }
}
