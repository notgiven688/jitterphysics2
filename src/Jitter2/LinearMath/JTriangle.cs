/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>
/// Represents a triangle defined by three vertices.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 9*sizeof(Real))]
public struct JTriangle(in JVector v0, in JVector v1, in JVector v2) : IEquatable<JTriangle>
{
    /// <summary>
    /// Specifies the face culling mode for triangles based on their winding order.
    /// Counter-clockwise (CCW) winding is considered front-facing.
    /// </summary>
    public enum CullMode
    {
        /// <summary>
        /// Cull triangles that are front-facing.
        /// A triangle is front-facing if its vertices are ordered counter-clockwise (CCW).
        /// </summary>
        FrontFacing,

        /// <summary>
        /// Cull triangles that are back-facing.
        /// A triangle is back-facing if its vertices are ordered clockwise (CW).
        /// This is the most common culling mode.
        /// </summary>
        BackFacing,

        /// <summary>
        /// Do not perform face culling; both front- and back-facing triangles are processed.
        /// </summary>
        None
    }

    /// <summary>The first vertex of the triangle.</summary>
    [FieldOffset(0 * sizeof(Real))] public JVector V0 = v0;

    /// <summary>The second vertex of the triangle.</summary>
    [FieldOffset(3 * sizeof(Real))] public JVector V1 = v1;

    /// <summary>The third vertex of the triangle.</summary>
    [FieldOffset(6 * sizeof(Real))] public JVector V2 = v2;

    /// <summary>
    /// Checks if a ray intersects the triangle.
    /// </summary>
    /// <param name="origin">The starting point (origin) of the ray.</param>
    /// <param name="direction">The direction vector of the ray.</param>
    /// <param name="cullMode">Determines whether to ignore triangles based on their winding order (Front/Back facing).</param>
    /// <param name="normal">Output: The normalized surface normal at the point of intersection.</param>
    /// <param name="lambda">Output: The distance along the <paramref name="direction"/> vector where the intersection occurs (hit point = origin + lambda * direction).</param>
    /// <returns><c>true</c> if the ray intersects the triangle; otherwise, <c>false</c>.</returns>
    public readonly bool RayIntersect(in JVector origin, in JVector direction, CullMode cullMode,
        out JVector normal, out Real lambda)
    {
        JVector u = V0 - V1;
        JVector v = V0 - V2;

        normal = u % v;
        Real it = (Real)1.0 / normal.LengthSquared();

        Real denominator = JVector.Dot(direction, normal);

        if (Math.Abs(denominator) < (Real)1e-06)
        {
            // triangle and ray are parallel
            goto return_false;
        }

        lambda = JVector.Dot(V0 - origin, normal);

        switch (cullMode)
        {
            case CullMode.FrontFacing when lambda < (Real)0.0:
            case CullMode.BackFacing when lambda > (Real)0.0:
                goto return_false;
        }

        lambda /= denominator;

        // point where the ray intersects the plane of the triangle.
        JVector hitPoint = origin + lambda * direction;

        // check if the point is inside the triangle
        JVector at = V0 - hitPoint;
        JVector.Cross(u, at, out JVector tmp);
        Real gamma = JVector.Dot(tmp, normal) * it;
        JVector.Cross(at, v, out tmp);
        Real beta = JVector.Dot(tmp, normal) * it;
        Real alpha = (Real)1.0 - gamma - beta;

        if (alpha > 0 && beta > 0 && gamma > 0)
        {
            normal *= -MathHelper.SignBit(denominator) * MathR.Sqrt(it);
            return true;
        }

        return_false:

        lambda = Real.MaxValue; normal = JVector.Zero;
        return false;
    }

    /// <summary>
    /// Returns a string representation of the <see cref="JTriangle"/>.
    /// </summary>
    public readonly override string ToString()
    {
        return $"V0={{{V0}}}, V1={{{V1}}}, V2={{{V2}}}";
    }

    /// <summary>
    /// Calculates the face normal of the triangle.
    /// The direction follows the Right-Hand Rule (counter-clockwise winding).
    /// </summary>
    /// <returns>The non-normalized normal vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly JVector GetNormal()
    {
        return (V1 - V0) % (V2 - V0);
    }

    /// <summary>
    /// Calculates the geometric center (centroid) of the triangle.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly JVector GetCenter()
    {
        return (V0 + V1 + V2) * (Real)(1.0 / 3.0);
    }

    /// <summary>
    /// Calculates the area of the triangle.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real GetArea()
    {
        return GetNormal().Length() * (Real)0.5;
    }

    /// <summary>
    /// Calculates the axis-aligned bounding box (AABB) of this triangle.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly JBoundingBox GetBoundingBox()
    {
        JBoundingBox box = JBoundingBox.SmallBox;
        JBoundingBox.AddPointInPlace(ref box, V0);
        JBoundingBox.AddPointInPlace(ref box, V1);
        JBoundingBox.AddPointInPlace(ref box, V2);
        return box;
    }

    /// <summary>
    /// Finds the closest point on the triangle surface to a specified point.
    /// </summary>
    /// <param name="point">The query point.</param>
    /// <returns>The point on the triangle closest to the query point.</returns>
    public readonly JVector ClosestPoint(JVector point)
    {
        JVector ab = V1 - V0;
        JVector ac = V2 - V0;
        JVector ap = point - V0;

        Real d1 = JVector.Dot(ab, ap);
        Real d2 = JVector.Dot(ac, ap);

        // Vertex region V0
        if (d1 <= 0 && d2 <= 0) return V0;

        JVector bp = point - V1;
        Real d3 = JVector.Dot(ab, bp);
        Real d4 = JVector.Dot(ac, bp);

        // Vertex region V1
        if (d3 >= 0 && d4 <= d3) return V1;

        // Edge region V0-V1
        Real vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            Real v = d1 / (d1 - d3);
            return V0 + v * ab;
        }

        JVector cp = point - V2;
        Real d5 = JVector.Dot(ab, cp);
        Real d6 = JVector.Dot(ac, cp);

        // Vertex region V2
        if (d6 >= 0 && d5 <= d6) return V2;

        // Edge region V0-V2
        Real vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            Real w = d2 / (d2 - d6);
            return V0 + w * ac;
        }

        // Edge region V1-V2
        Real va = d3 * d6 - d5 * d4;
        if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
        {
            Real w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return V1 + w * (V2 - V1);
        }

        // Face region
        Real denom = (Real)1.0 / (va + vb + vc);
        Real vbn = vb * denom;
        Real wn = vc * denom;

        return V0 + ab * vbn + ac * wn;
    }

    public readonly override int GetHashCode() => HashCode.Combine(V0, V1, V2);

    public readonly bool Equals(JTriangle other)
    {
        return V0.Equals(other.V0) && V1.Equals(other.V1) && V2.Equals(other.V2);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is JTriangle other && Equals(other);
    }

    public static bool operator ==(JTriangle left, JTriangle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JTriangle left, JTriangle right)
    {
        return !(left == right);
    }
}