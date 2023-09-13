/*
 * Copyright (c) 2009-2023 Thorben Linneweber and others
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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jitter2.LinearMath;

public static class MathHelper
{
    /*

    // Given two lines return closest points on both lines
    // Line 1: p1 + (p2 - p1)*mua
    // Line 2: p3 + (p4 - p3)*mub

    public static bool LineLineIntersect(in JVector P1, in JVector P2, in JVector P3, in JVector P4,  out JVector Pa, out JVector Pb, out float mua, out float mub)
    {
        const float Epsilon = 1e-12f;

        Pa = Pb = JVector.Zero;
        mua = mub = 0;

        JVector p13 = P1 - P3;
        JVector p43 = P4 - P3;
        JVector p21 = P2 - P1;

        float d1343 = p13 * p43;
        float d4321 = p43 * p21;
        float d1321 = p13 * p21;
        float d4343 = p43 * p43;
        float d2121 = p21 * p21;

        float denom = d2121 * d4343 - d4321 * d4321;
        if (Math.Abs(denom) < Epsilon) return false;

        float numer = d1343 * d4321 - d1321 * d4343;

        mua = numer / denom;
        mub = (d1343 + d4321 * mua) / d4343;

        Pa = P1 + (p21 * mua);
        Pb = P3 + (p43 * mub);

        return true;
    }

    */

    /// <summary>
    /// Calculates an orthonormal vector.
    /// </summary>
    /// <param name="vec">Input vector, does not have to be normalized.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector CreateOrthonormal(in JVector vec)
    {
        JVector result = vec;

        Debug.Assert(!CloseToZero(vec));

        float xa = Math.Abs(vec.X);
        float ya = Math.Abs(vec.Y);
        float za = Math.Abs(vec.Z);

        if ((xa > ya && xa > za) || (ya > xa && ya > za))
        {
            result.X = vec.Y;
            result.Y = -vec.X;
            result.Z = 0;
        }
        else
        {
            result.Y = vec.Z;
            result.Z = -vec.Y;
            result.X = 0;
        }

        result.Normalize();

        Debug.Assert(MathF.Abs(JVector.Dot(result, vec)) < 1e-6f);

        return result;
    }

    /// <summary>
    /// Checks if the columns of the matrix form an orthonormal basis.
    /// </summary>
    public static bool CheckOrthonormalBasis(in JMatrix matrix)
    {
        JMatrix delta = JMatrix.MultiplyTransposed(matrix, matrix) - JMatrix.Identity;
        if (JVector.MaxAbs(delta.UnsafeGet(0)) > 1e-6f) return false;
        if (JVector.MaxAbs(delta.UnsafeGet(1)) > 1e-6f) return false;
        if (JVector.MaxAbs(delta.UnsafeGet(2)) > 1e-6f) return false;
        return true;
    }

    /// <summary>
    /// Checks if the length of a vector is close to zero or zero.
    /// </summary>
    /// <param name="v">The vector to check.</param>
    /// <param name="epsilonSq">The squared magnitude of the vector is compared
    /// to this value.</param>
    public static bool CloseToZero(in JVector v, float epsilonSq = 1e-16f)
    {
        return v.LengthSquared() < epsilonSq;
    }
}