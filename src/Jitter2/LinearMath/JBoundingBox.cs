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
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>
/// Represents an axis-aligned bounding box (AABB), a rectangular bounding box whose edges are parallel to the coordinate axes.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 6*sizeof(Real))]
public struct JBoundingBox : IEquatable<JBoundingBox>
{
    public const Real Epsilon = (Real)1e-12;

    public enum ContainmentType
    {
        Disjoint,
        Contains,
        Intersects
    }

    [FieldOffset(0*sizeof(Real))]
    public JVector Min;

    [FieldOffset(3*sizeof(Real))]
    public JVector Max;

    public static readonly JBoundingBox LargeBox;

    public static readonly JBoundingBox SmallBox;

    static JBoundingBox()
    {
        LargeBox.Min = new JVector(Real.MinValue);
        LargeBox.Max = new JVector(Real.MaxValue);
        SmallBox.Min = new JVector(Real.MaxValue);
        SmallBox.Max = new JVector(Real.MinValue);
    }

    public JBoundingBox(JVector min, JVector max)
    {
        Min = min;
        Max = max;
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

    public readonly bool Contains(in JVector point)
    {
        return Min.X <= point.X && point.X <= Max.X &&
               Min.Y <= point.Y && point.Y <= Max.Y &&
               Min.Z <= point.Z && point.Z <= Max.Z;
    }

    public readonly void GetCorners(JVector[] corners)
    {
        corners[0].Set(Min.X, Max.Y, Max.Z);
        corners[1].Set(Max.X, Max.Y, Max.Z);
        corners[2].Set(Max.X, Min.Y, Max.Z);
        corners[3].Set(Min.X, Min.Y, Max.Z);
        corners[4].Set(Min.X, Max.Y, Min.Z);
        corners[5].Set(Max.X, Max.Y, Min.Z);
        corners[6].Set(Max.X, Min.Y, Min.Z);
        corners[7].Set(Min.X, Min.Y, Min.Z);
    }

    // Marked obsolete to guide users toward the preferred static method.
    [Obsolete($"Use static {nameof(AddPointInPlace)} instead.")]
    public void AddPoint(in JVector point)
    {
        JVector.Max(Max, point, out Max);
        JVector.Min(Min, point, out Min);
    }

    public static void AddPointInPlace(ref JBoundingBox box, in JVector point)
    {
        JVector.Max(box.Max, point, out box.Max);
        JVector.Min(box.Min, point, out box.Min);
    }

    public static JBoundingBox CreateFromPoints(JVector[] points)
    {
        JVector vector3 = new (Real.MaxValue);
        JVector vector2 = new (Real.MinValue);

        for (int i = 0; i < points.Length; i++)
        {
            vector3 = JVector.Min(vector3, points[i]);
            vector2 = JVector.Max(vector2, points[i]);
        }

        return new JBoundingBox(vector3, vector2);
    }

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

    public static bool NotDisjoint(in JBoundingBox left, in JBoundingBox right)
    {
        return left.Max.X >= right.Min.X && left.Min.X <= right.Max.X && left.Max.Y >= right.Min.Y && left.Min.Y <= right.Max.Y &&
               left.Max.Z >= right.Min.Z && left.Min.Z <= right.Max.Z;
    }

    public static bool Disjoint(in JBoundingBox left, in JBoundingBox right)
    {
        return left.Max.X < right.Min.X || left.Min.X > right.Max.X || left.Max.Y < right.Min.Y || left.Min.Y > right.Max.Y ||
               left.Max.Z < right.Min.Z || left.Min.Z > right.Max.Z;
    }

    public static bool Encompasses(in JBoundingBox outer, in JBoundingBox inner)
    {
        return outer.Min.X <= inner.Min.X && outer.Max.X >= inner.Max.X && outer.Min.Y <= inner.Min.Y && outer.Max.Y >= inner.Max.Y &&
               outer.Min.Z <= inner.Min.Z && outer.Max.Z >= inner.Max.Z;
    }

    public static JBoundingBox CreateMerged(in JBoundingBox original, in JBoundingBox additional)
    {
        CreateMerged(original, additional, out JBoundingBox result);
        return result;
    }

    public static void CreateMerged(in JBoundingBox original, in JBoundingBox additional, out JBoundingBox result)
    {
        JVector.Min(original.Min, additional.Min, out result.Min);
        JVector.Max(original.Max, additional.Max, out result.Max);
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