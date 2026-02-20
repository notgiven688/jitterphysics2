/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jitter2.LinearMath;

/// <summary>
/// Provides mathematical helper methods for linear algebra and physics calculations.
/// </summary>
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
    /// <param name="omega">The angular velocity vector in radians per second.</param>
    /// <param name="dt">The time step in seconds.</param>
    /// <returns>A unit quaternion representing the rotation.</returns>
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
    /// <param name="matrix">The matrix to check.</param>
    /// <param name="epsilon">The tolerance for floating-point comparisons.</param>
    /// <returns><see langword="true"/> if the matrix is orthonormal with determinant 1; otherwise, <see langword="false"/>.</returns>
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
    /// <param name="vector">The vector to check.</param>
    /// <param name="epsilon">The tolerance for each component.</param>
    /// <returns><see langword="true"/> if all components are within epsilon of zero; otherwise, <see langword="false"/>.</returns>
    public static bool IsZero(in JVector vector, Real epsilon = (Real)1e-6)
    {
        return !(MathR.Abs(vector.X) >= epsilon) &&
               !(MathR.Abs(vector.Y) >= epsilon) &&
               !(MathR.Abs(vector.Z) >= epsilon);
    }

    /// <summary>
    /// Checks if a value is close to zero.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="epsilon">The tolerance.</param>
    /// <returns><see langword="true"/> if the absolute value is less than epsilon; otherwise, <see langword="false"/>.</returns>
    public static bool IsZero(Real value, Real epsilon = (Real)1e-6)
    {
        return MathR.Abs(value) < epsilon;
    }

    /// <summary>
    /// Checks if all entries of a matrix are close to zero.
    /// </summary>
    /// <param name="matrix">The matrix to check.</param>
    /// <param name="epsilon">The tolerance for each element.</param>
    /// <returns><see langword="true"/> if all elements are within epsilon of zero; otherwise, <see langword="false"/>.</returns>
    public static bool UnsafeIsZero(ref JMatrix matrix, Real epsilon = (Real)1e-6)
    {
        if (!IsZero(matrix.UnsafeGet(0), epsilon)) return false;
        if (!IsZero(matrix.UnsafeGet(1), epsilon)) return false;
        if (!IsZero(matrix.UnsafeGet(2), epsilon)) return false;
        return true;
    }

    /// <summary>
    /// Calculates <c>(MᵀM)^(-1/2)</c> using Jacobi iterations.
    /// </summary>
    /// <param name="m">The input matrix.</param>
    /// <param name="sweeps">The number of Jacobi iterations.</param>
    /// <returns>The inverse square root of <c>MᵀM</c>.</returns>
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
    /// <remarks>
    /// The input vector must be non-zero. Debug builds assert this condition.
    /// </remarks>
    /// <param name="vec">The input vector (must be non-zero, does not need to be normalized).</param>
    /// <returns>A unit vector orthogonal to the input.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector CreateOrthonormal(in JVector vec)
    {
        Debug.Assert(!CloseToZero(vec), "Cannot create orthonormal of a zero vector");

        Real ax = Math.Abs(vec.X);
        Real ay = Math.Abs(vec.Y);
        Real az = Math.Abs(vec.Z);

        JVector r;

        if (ax <= ay && ax <= az)
        {
            // (0, z, -y)
            Real y = vec.Z;
            Real z = -vec.Y;

            // invLen = 1 / sqrt(y*y + z*z)
            Real invLen = (Real)1.0 / MathR.Sqrt(y * y + z * z);

            r.X = 0;
            r.Y = y * invLen;
            r.Z = z * invLen;
        }
        else if (ay <= az)
        {
            // (-z, 0, x)
            Real x = -vec.Z;
            Real z = vec.X;

            Real invLen = (Real)1.0 / MathR.Sqrt(x * x + z * z);

            r.X = x * invLen;
            r.Y = 0;
            r.Z = z * invLen;
        }
        else
        {
            // (y, -x, 0)
            Real x = vec.Y;
            Real y = -vec.X;

            Real invLen = (Real)1.0 / MathR.Sqrt(x * x + y * y);

            r.X = x * invLen;
            r.Y = y * invLen;
            r.Z = 0;
        }

        Debug.Assert(MathR.Abs(JVector.Dot(r, vec)) < (Real)1e-6);
        return r;
    }

    /// <summary>
    /// Verifies whether the columns of the given matrix constitute an orthonormal basis.
    /// </summary>
    /// <param name="matrix">The input matrix to check.</param>
    /// <param name="epsilon">The tolerance for floating-point comparisons.</param>
    /// <returns><see langword="true"/> if the columns are mutually perpendicular and have unit length; otherwise, <see langword="false"/>.</returns>
    public static bool CheckOrthonormalBasis(in JMatrix matrix, Real epsilon = (Real)1e-6)
    {
        JMatrix delta = JMatrix.MultiplyTransposed(matrix, matrix) - JMatrix.Identity;
        return UnsafeIsZero(ref delta, epsilon);
    }

    /// <summary>
    /// Determines whether the length of the given vector is zero or close to zero.
    /// </summary>
    /// <param name="v">The vector to evaluate.</param>
    /// <param name="epsilonSq">Threshold for squared magnitude.</param>
    /// <returns><see langword="true"/> if the squared length is less than <paramref name="epsilonSq"/>; otherwise, <see langword="false"/>.</returns>
    public static bool CloseToZero(in JVector v, Real epsilonSq = (Real)1e-16)
    {
        return v.LengthSquared() < epsilonSq;
    }
}