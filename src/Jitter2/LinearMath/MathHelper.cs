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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jitter2.LinearMath;

public static class MathHelper
{
    /*

    // Given two lines return closest points on both lines
    // Line 1: p1 + (p2 - p1)*mua
    // Line 2: p3 + (p4 - p3)*mub

    public static bool LineLineIntersect(in JVector p1, in JVector p2, in JVector p3, in JVector P4,  out JVector Pa, out JVector Pb, out double mua, out double mub)
    {
        const double Epsilon = 1e-12;

        Pa = Pb = JVector.Zero;
        mua = mub = 0;

        JVector p13 = p1 - p3;
        JVector p43 = P4 - p3;
        JVector p21 = p2 - p1;

        double d1343 = p13 * p43;
        double d4321 = p43 * p21;
        double d1321 = p13 * p21;
        double d4343 = p43 * p43;
        double d2121 = p21 * p21;

        double denom = d2121 * d4343 - d4321 * d4321;
        if (Math.Abs(denom) < Epsilon) return false;

        double numer = d1343 * d4321 - d1321 * d4343;

        mua = numer / denom;
        mub = (d1343 + d4321 * mua) / d4343;

        Pa = p1 + (p21 * mua);
        Pb = p3 + (p43 * mub);

        return true;
    }

    */

    /// <summary>
    /// Checks if matrix is a pure rotation matrix.
    /// </summary>
    public static bool IsRotationMatrix(in JMatrix matrix, double epsilon = 1e-06)
    {
        JMatrix delta = JMatrix.MultiplyTransposed(matrix, matrix) - JMatrix.Identity;

        if (!UnsafeIsZero(ref delta, epsilon))
        {
            return false;
        }

        return Math.Abs(matrix.Determinant() - 1.0) < epsilon;
    }

    /// <summary>
    /// Checks if all entries of a vector are close to zero.
    /// </summary>
    public static bool IsZero(in JVector vector, double epsilon = 1e-6)
    {
        double x = Math.Abs(vector.X);
        double y = Math.Abs(vector.Y);
        double z = Math.Abs(vector.Z);

        return Math.Max(x, Math.Max(y, z)) < epsilon;
    }

    /// <summary>
    /// Checks if all entries of a matrix are close to zero.
    /// </summary>
    public static bool UnsafeIsZero(ref JMatrix matrix, double epsilon = 1e-6)
    {
        if (!IsZero(matrix.UnsafeGet(0), epsilon)) return false;
        if (!IsZero(matrix.UnsafeGet(1), epsilon)) return false;
        if (!IsZero(matrix.UnsafeGet(2), epsilon)) return false;
        return true;
    }

    /// <summary>
    /// Calculates (M^T \times M)^(-1/2) using Jacobi iterations.
    /// </summary>
    /// <param name="sweeps">The number of Jacobi iterations.</param>
    public static JMatrix InverseSquareRoot(JMatrix m, int sweeps = 2)
    {
        double phi, cp, sp;
        Unsafe.SkipInit(out JMatrix r);

        JMatrix rotation = JMatrix.Identity;

        for (int i = 0; i < sweeps; i++)
        {
            // M32
            if (Math.Abs(m.M23) > 1e-6)
            {
                phi = Math.Atan2(1, (m.M33 - m.M22) / (2.0 * m.M23)) / 2.0;
                (sp, cp) = Math.SinCos(phi);
                r = new JMatrix(1, 0, 0, 0, cp, sp, 0, -sp, cp);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }

            // M21
            if (Math.Abs(m.M21) > 1e-6)
            {
                phi = Math.Atan2(1, (m.M22 - m.M11) / (2.0 * m.M21)) / 2.0;
                (sp, cp) = Math.SinCos(phi);
                r = new JMatrix(cp, sp, 0, -sp, cp, 0, 0, 0, 1);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }

            // M31
            if (Math.Abs(m.M31) > 1e-6)
            {
                phi = Math.Atan2(1, (m.M33 - m.M11) / (2.0 * m.M31)) / 2.0;
                (sp, cp) = Math.SinCos(phi);
                r = new JMatrix(cp, 0, sp, 0, 1, 0, -sp, 0, cp);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }
        }

        JMatrix d = new JMatrix(1.0 / Math.Sqrt(m.M11), 0, 0,
            0, 1.0 / Math.Sqrt(m.M22), 0,
            0, 0, 1.0 / Math.Sqrt(m.M33));

        return rotation * d * JMatrix.Transpose(rotation);
    }

    /// <summary>
    /// Calculates an orthonormal vector to the given vector.
    /// </summary>
    /// <param name="vec">The input vector, which does not need to be normalized.</param>
    /// <returns>An orthonormal vector to the input vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector CreateOrthonormal(in JVector vec)
    {
        JVector result = vec;

        Debug.Assert(!CloseToZero(vec));

        double xa = Math.Abs(vec.X);
        double ya = Math.Abs(vec.Y);
        double za = Math.Abs(vec.Z);

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

        Debug.Assert(Math.Abs(JVector.Dot(result, vec)) < 1e-6);

        return result;
    }

    /// <summary>
    /// Verifies whether the columns of the given matrix constitute an orthonormal basis.
    /// An orthonormal basis means that the columns are mutually perpendicular and have unit length.
    /// </summary>
    /// <param name="matrix">The input matrix to check for an orthonormal basis.</param>
    /// <returns>True if the columns of the matrix form an orthonormal basis; otherwise, false.</returns>
    public static bool CheckOrthonormalBasis(in JMatrix matrix, double epsilon = 1e-6)
    {
        JMatrix delta = JMatrix.MultiplyTransposed(matrix, matrix) - JMatrix.Identity;
        return UnsafeIsZero(ref delta, epsilon);
    }

    /// <summary>
    /// Determines whether the length of the given vector is zero or close to zero.
    /// </summary>
    /// <param name="v">The vector to evaluate.</param>
    /// <param name="epsilonSq">A threshold value below which the squared magnitude of the vector
    /// is considered to be zero or close to zero.</param>
    /// <returns>True if the vector is close to zero; otherwise, false.</returns>
    public static bool CloseToZero(in JVector v, double epsilonSq = 1e-16)
    {
        return v.LengthSquared() < epsilonSq;
    }
}