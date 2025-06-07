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
public struct JBBox : IEquatable<JBBox>
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

    public static readonly JBBox LargeBox;

    public static readonly JBBox SmallBox;

    static JBBox()
    {
        LargeBox.Min = new JVector(Real.MinValue);
        LargeBox.Max = new JVector(Real.MaxValue);
        SmallBox.Min = new JVector(Real.MaxValue);
        SmallBox.Max = new JVector(Real.MinValue);
    }

    /// <summary>
    /// Returns a string representation of the <see cref="JBBox"/>.
    /// </summary>
    public override string ToString()
    {
        return $"Min={{{Min}}}, Max={{{Max}}}";
    }

    public JBBox(JVector min, JVector max)
    {
        Min = min;
        Max = max;
    }

    internal void InverseTransform(ref JVector position, ref JMatrix orientation)
    {
        JVector.Subtract(Max, position, out Max);
        JVector.Subtract(Min, position, out Min);

        JVector.Add(Max, Min, out JVector center);
        center.X *= (Real)0.5;
        center.Y *= (Real)0.5;
        center.Z *= (Real)0.5;

        JVector.Subtract(Max, Min, out JVector halfExtents);
        halfExtents.X *= (Real)0.5;
        halfExtents.Y *= (Real)0.5;
        halfExtents.Z *= (Real)0.5;

        JVector.TransposedTransform(center, orientation, out center);

        JMatrix.Absolute(orientation, out JMatrix abs);
        JVector.TransposedTransform(halfExtents, abs, out halfExtents);

        JVector.Add(center, halfExtents, out Max);
        JVector.Subtract(center, halfExtents, out Min);
    }

    public void Transform(ref JMatrix orientation)
    {
        JVector halfExtents = (Real)0.5 * (Max - Min);
        JVector center = (Real)0.5 * (Max + Min);

        JVector.Transform(center, orientation, out center);

        JMatrix.Absolute(orientation, out var abs);
        JVector.Transform(halfExtents, abs, out halfExtents);

        Max = center + halfExtents;
        Min = center - halfExtents;
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

    public ContainmentType Contains(in JVector point)
    {
        return Min.X <= point.X && point.X <= Max.X &&
               Min.Y <= point.Y && point.Y <= Max.Y &&
               Min.Z <= point.Z && point.Z <= Max.Z
            ? ContainmentType.Contains
            : ContainmentType.Disjoint;
    }

    public void GetCorners(JVector[] corners)
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

    public void AddPoint(in JVector point)
    {
        JVector.Max(Max, point, out Max);
        JVector.Min(Min, point, out Min);
    }

    public static JBBox CreateFromPoints(JVector[] points)
    {
        JVector vector3 = new JVector(Real.MaxValue);
        JVector vector2 = new JVector(Real.MinValue);

        for (int i = 0; i < points.Length; i++)
        {
            JVector.Min(vector3, points[i], out vector3);
            JVector.Max(vector2, points[i], out vector2);
        }

        return new JBBox(vector3, vector2);
    }

    public readonly ContainmentType Contains(in JBBox box)
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

    public readonly bool NotDisjoint(in JBBox box)
    {
        return Max.X >= box.Min.X && Min.X <= box.Max.X && Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
               Max.Z >= box.Min.Z && Min.Z <= box.Max.Z;
    }

    public readonly bool Disjoint(in JBBox box)
    {
        return !(Max.X >= box.Min.X && Min.X <= box.Max.X && Max.Y >= box.Min.Y && Min.Y <= box.Max.Y &&
                 Max.Z >= box.Min.Z && Min.Z <= box.Max.Z);
    }

    public readonly bool Encompasses(in JBBox box)
    {
        return Min.X <= box.Min.X && Max.X >= box.Max.X &&
               Min.Y <= box.Min.Y && Max.Y >= box.Max.Y &&
               Min.Z <= box.Min.Z && Max.Z >= box.Max.Z;
    }

    public static JBBox CreateMerged(in JBBox original, in JBBox additional)
    {
        CreateMerged(original, additional, out JBBox result);
        return result;
    }

    public static void CreateMerged(in JBBox original, in JBBox additional, out JBBox result)
    {
        JVector.Min(original.Min, additional.Min, out result.Min);
        JVector.Max(original.Max, additional.Max, out result.Max);
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

    public bool Equals(JBBox other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }

    public override bool Equals(object? obj)
    {
        return obj is JBBox other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Min.GetHashCode() ^ Max.GetHashCode();
    }
}