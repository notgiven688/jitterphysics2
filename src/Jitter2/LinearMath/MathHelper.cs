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
    /// <summary>
    /// Gets the sign of <paramref name="value"/> purely from its IEEE-754 sign bit.
    /// </summary>
    /// <param name="value">The number to test.</param>
    /// <returns>
    /// <c>+1</c> when the sign bit is clear (positive, +0, or a positive-sign NaN),
    /// <c>-1</c> when the sign bit is set (negative, −0, or a negative-sign NaN).
    /// Never returns <c>0</c>, unlike <see cref="Math.Sign(float)"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SignBit(float value)
    {
        return 1 | (BitConverter.SingleToInt32Bits(value) >> 31);
    }

    /// <inheritdoc cref="SignBit(float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SignBit(double value)
    {
        return 1 | (int)(BitConverter.DoubleToInt64Bits(value) >> 63);
    }

    /// <summary>
    /// Calculates the rotation quaternion corresponding to the given (constant) angular
    /// velocity vector and time step.
    /// </summary>
    public static JQuaternion RotationQuaternion(in JVector omega, Real dt)
    {
        Real angle = omega.Length();
        Real theta = angle * dt;

        if (theta < (Real)1e-3)
        {
            Real dt3 = dt * dt * dt;
            Real angle2 = angle * angle;

            Real scale = (Real)0.5 * dt - ((Real)1.0 / (Real)48.0) * dt3 * angle2;
            JVector.Multiply(omega, scale, out var axis);

            Real cos = (Real)1.0 - ((Real)1.0/(Real)8.0) * theta * theta;

            JQuaternion res = new JQuaternion(axis.X, axis.Y, axis.Z, cos);
            Debug.Assert(MathHelper.IsZero(res.Length() - 1, (Real)1e-2));
            return res;
        }
        else
        {
            Real halfAngleDt = (Real)0.5 * angle * dt;
            (Real sinD, Real cosD) = MathR.SinCos(halfAngleDt);

            Real scale = sinD / angle;
            JVector.Multiply(omega, scale, out var axis);

            JQuaternion res = new JQuaternion(axis.X, axis.Y, axis.Z, cosD);
            Debug.Assert(MathHelper.IsZero(res.Length() - 1, (Real)1e-2));
            return res;
        }
    }

    /// <summary>
    /// Checks if matrix is a pure rotation matrix.
    /// </summary>
    public static bool IsRotationMatrix(in JMatrix matrix, Real epsilon = (Real)1e-06)
    {
        JMatrix delta = JMatrix.MultiplyTransposed(matrix, matrix) - JMatrix.Identity;

        if (!UnsafeIsZero(ref delta, epsilon))
        {
            return false;
        }

        return MathR.Abs(matrix.Determinant() - (Real)1.0) < epsilon;
    }

    /// <summary>
    /// Checks if all entries of a vector are close to zero.
    /// </summary>
    public static bool IsZero(in JVector vector, Real epsilon = (Real)1e-6)
    {
        return !(MathR.Abs(vector.X) >= epsilon) &&
               !(MathR.Abs(vector.Y) >= epsilon) &&
               !(MathR.Abs(vector.Z) >= epsilon);
    }

    /// <summary>
    /// Checks if a value is close to zero.
    /// </summary>
    public static bool IsZero(Real value, Real epsilon = (Real)1e-6)
    {
        return MathR.Abs(value) < epsilon;
    }

    /// <summary>
    /// Checks if all entries of a matrix are close to zero.
    /// </summary>
    public static bool UnsafeIsZero(ref JMatrix matrix, Real epsilon = (Real)1e-6)
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
        Unsafe.SkipInit(out JMatrix r);

        JMatrix rotation = JMatrix.Identity;

        for (int i = 0; i < sweeps; i++)
        {
            Real phi, cp, sp;

            // M32
            if (MathR.Abs(m.M23) > (Real)1e-6)
            {
                phi = MathR.Atan2(1, (m.M33 - m.M22) / ((Real)2.0 * m.M23)) / (Real)2.0;
                (sp, cp) = MathR.SinCos(phi);
                r = new JMatrix(1, 0, 0, 0, cp, sp, 0, -sp, cp);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }

            // M21
            if (MathR.Abs(m.M21) > (Real)1e-6)
            {
                phi = MathR.Atan2(1, (m.M22 - m.M11) / ((Real)2.0 * m.M21)) / (Real)2.0;
                (sp, cp) = MathR.SinCos(phi);
                r = new JMatrix(cp, sp, 0, -sp, cp, 0, 0, 0, 1);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }

            // M31
            if (MathR.Abs(m.M31) > (Real)1e-6)
            {
                phi = MathR.Atan2(1, (m.M33 - m.M11) / ((Real)2.0 * m.M31)) / (Real)2.0;
                (sp, cp) = MathR.SinCos(phi);
                r = new JMatrix(cp, 0, sp, 0, 1, 0, -sp, 0, cp);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }
        }

        JMatrix d = new((Real)1.0 / MathR.Sqrt(m.M11), 0, 0,
            0, (Real)1.0 / MathR.Sqrt(m.M22), 0,
            0, 0, (Real)1.0 / MathR.Sqrt(m.M33));

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

        Real xa = Math.Abs(vec.X);
        Real ya = Math.Abs(vec.Y);
        Real za = Math.Abs(vec.Z);

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

        JVector.NormalizeInPlace(ref result);

        Debug.Assert(MathR.Abs(JVector.Dot(result, vec)) < (Real)1e-6);

        return result;
    }

    /// <summary>
    /// Verifies whether the columns of the given matrix constitute an orthonormal basis.
    /// An orthonormal basis means that the columns are mutually perpendicular and have unit length.
    /// </summary>
    /// <param name="matrix">The input matrix to check for an orthonormal basis.</param>
    /// <returns>True if the columns of the matrix form an orthonormal basis; otherwise, false.</returns>
    public static bool CheckOrthonormalBasis(in JMatrix matrix, Real epsilon = (Real)1e-6)
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
    public static bool CloseToZero(in JVector v, Real epsilonSq = (Real)1e-16)
    {
        return v.LengthSquared() < epsilonSq;
    }
}