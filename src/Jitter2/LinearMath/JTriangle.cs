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
/// Represents a triangle defined by three vertices.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 9*sizeof(Real))]
public struct JTriangle : IEquatable<JTriangle>
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

    [FieldOffset(0*sizeof(Real))]
    public JVector V0;
    [FieldOffset(3*sizeof(Real))]
    public JVector V1;
    [FieldOffset(6*sizeof(Real))]
    public JVector V2;

    /// <summary>
    /// Initializes a new instance of the <see cref="JTriangle"/> structure with the specified vertices.
    /// </summary>
    /// <param name="v0">The first vertex of the triangle.</param>
    /// <param name="v1">The second vertex of the triangle.</param>
    /// <param name="v2">The third vertex of the triangle.</param>
    public JTriangle(in JVector v0, in JVector v1, in JVector v2)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
    }

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