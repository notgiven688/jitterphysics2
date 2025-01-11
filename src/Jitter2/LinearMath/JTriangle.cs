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

    /// <summary>
    /// Returns a string representation of the <see cref="JTriangle"/>.
    /// </summary>
    public override string ToString()
    {
        return $"V0={{{V0}}}, V1={{{V1}}}, V2={{{V2}}}";
    }

    public override int GetHashCode()
    {
        return V0.GetHashCode() ^ V1.GetHashCode() ^ V2.GetHashCode();
    }

    public bool Equals(JTriangle other)
    {
        return V0.Equals(other.V0) && V1.Equals(other.V1) && V2.Equals(other.V2);
    }

    public override bool Equals(object? obj)
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