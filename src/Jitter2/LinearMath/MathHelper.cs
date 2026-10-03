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
    /// Calculates the rotation quaternion corresponding to the given angular velocity using
    /// deterministic trigonometric approximations.
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
            // Exact rotation: (omega * sin(theta/2) / |omega|, cos(theta/2)).
            // For small theta, the vector scale is dt/2 * (1 - theta^2/24)
            // and the scalar part is 1 - theta^2/8 (Taylor expansions).
            // Using theta^2 avoids forming dt^3, which can overflow in a long sweep.
            Real theta2 = theta * theta;
            Real smallAngleScale = (Real)0.5 * dt * ((Real)1.0 - theta2 / (Real)24.0);
            JVector.Multiply(omega, smallAngleScale, out var smallAngleAxis);

            Real cos = (Real)1.0 - ((Real)1.0 / (Real)8.0) * theta2;

            JQuaternion smallAngleResult = new JQuaternion(smallAngleAxis.X, smallAngleAxis.Y, smallAngleAxis.Z, cos);
            Debug.Assert(MathHelper.IsZero(smallAngleResult.Length() - 1, (Real)1e-2));
            return smallAngleResult;
        }

        Real halfAngleDt = (Real)0.5 * angle * dt;
        (Real sinD, Real cosD) = StableMath.SinCos(halfAngleDt);

        Real scale = sinD / angle;
        JVector.Multiply(omega, scale, out var axis);

        JQuaternion result = new JQuaternion(axis.X, axis.Y, axis.Z, cosD);
        Debug.Assert(MathHelper.IsZero(result.Length() - 1, (Real)1e-2));
        return result;
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

            // M23
            if (MathR.Abs(m.M23) > (Real)1e-6)
            {
                phi = StableMath.Atan2((Real)1.0, (m.M33 - m.M22) / ((Real)2.0 * m.M23)) / (Real)2.0;
                (sp, cp) = StableMath.SinCos(phi);
                r = new JMatrix(1, 0, 0, 0, cp, sp, 0, -sp, cp);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }

            // M21
            if (MathR.Abs(m.M21) > (Real)1e-6)
            {
                phi = StableMath.Atan2((Real)1.0, (m.M22 - m.M11) / ((Real)2.0 * m.M21)) / (Real)2.0;
                (sp, cp) = StableMath.SinCos(phi);
                r = new JMatrix(cp, sp, 0, -sp, cp, 0, 0, 0, 1);
                JMatrix.Multiply(m, r, out m);
                JMatrix.TransposedMultiply(r, m, out m);
                JMatrix.Multiply(rotation, r, out rotation);
            }

            // M31
            if (MathR.Abs(m.M31) > (Real)1e-6)
            {
                phi = StableMath.Atan2((Real)1.0, (m.M33 - m.M11) / ((Real)2.0 * m.M31)) / (Real)2.0;
                (sp, cp) = StableMath.SinCos(phi);
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

        Real ax = MathR.Abs(vec.X);
        Real ay = MathR.Abs(vec.Y);
        Real az = MathR.Abs(vec.Z);

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
    /// Computes an approximate Moore-Penrose pseudoinverse of a symmetric,
    /// positive-semidefinite 3x3 matrix, such as a joint's inverse effective mass.
    /// </summary>
    /// <param name="matrix">A finite, symmetric, positive-semidefinite matrix.</param>
    /// <returns>The inverse on responsive directions, with zero response on discarded directions.</returns>
    /// <remarks>
    /// Uses the ordinary inverse when the normalized determinant is sufficiently large.
    /// Otherwise, Jacobi diagonalization retains eigenvalues greater than a normalized
    /// tolerance of 1e-6 in single precision or 1e-12 in double precision. The normalization
    /// uses the largest absolute diagonal entry. A zero matrix produces a zero pseudoinverse.
    /// Symmetry and positive semidefiniteness are assumed and are not validated.
    /// Compute this during constraint preparation and cache the result for solver iterations.
    /// </remarks>
    public static JMatrix PseudoInverseSymmetric(in JMatrix matrix)
    {
        Real scale = MathR.Max(MathR.Abs(matrix.M11),
            MathR.Max(MathR.Abs(matrix.M22), MathR.Abs(matrix.M33)));
        if (!(scale > 0) || !Real.IsFinite(scale)) return JMatrix.Zero;

        JSymmetricMatrix normalized = new(matrix.M11 / scale, matrix.M12 / scale, matrix.M13 / scale,
            matrix.M22 / scale, matrix.M23 / scale, matrix.M33 / scale);
        Real tolerance = Precision.IsDoublePrecision ? (Real)1e-12 : (Real)1e-6;
        if (MathR.Abs(normalized.Determinant()) > tolerance && JMatrix.Inverse(matrix, out JMatrix inverse))
        {
            return inverse;
        }

        Span<Real> a = stackalloc Real[9]
        {
            normalized.M11, normalized.M12, normalized.M13,
            normalized.M12, normalized.M22, normalized.M23,
            normalized.M13, normalized.M23, normalized.M33
        };
        Span<Real> basis = stackalloc Real[9] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };

        // Jacobi rotations diagonalize the symmetric matrix without trigonometric functions.
        for (int sweep = 0; sweep < 8; sweep++)
        {
            bool changed = JacobiRotate(a, basis, 0, 1, tolerance);
            changed |= JacobiRotate(a, basis, 0, 2, tolerance);
            changed |= JacobiRotate(a, basis, 1, 2, tolerance);
            if (!changed) break;
        }

        JSymmetricMatrix result = JSymmetricMatrix.Zero;
        for (int i = 0; i < 3; i++)
        {
            Real eigenvalue = a[3 * i + i];
            if (!(eigenvalue > tolerance)) continue;
            Real weight = ((Real)1.0 / eigenvalue) / scale;
            JVector axis = new(basis[i], basis[3 + i], basis[6 + i]);
            result += weight * new JSymmetricMatrix(axis.X * axis.X, axis.X * axis.Y, axis.X * axis.Z,
                axis.Y * axis.Y, axis.Y * axis.Z, axis.Z * axis.Z);
        }

        return result.ToMatrix();
    }

    private static bool JacobiRotate(Span<Real> a, Span<Real> basis, int p, int q, Real tolerance)
    {
        Real offDiagonal = a[3 * p + q];
        if (MathR.Abs(offDiagonal) <= tolerance) return false;

        Real tau = (a[3 * q + q] - a[3 * p + p]) / ((Real)2.0 * offDiagonal);
        Real tangent = MathR.CopySign((Real)1.0, tau) /
                       (MathR.Abs(tau) + MathR.Sqrt((Real)1.0 + tau * tau));
        Real cosine = (Real)1.0 / MathR.Sqrt((Real)1.0 + tangent * tangent);
        Real sine = tangent * cosine;

        a[3 * p + p] -= tangent * offDiagonal;
        a[3 * q + q] += tangent * offDiagonal;
        a[3 * p + q] = a[3 * q + p] = 0;

        for (int k = 0; k < 3; k++)
        {
            if (k != p && k != q)
            {
                Real akp = a[3 * k + p];
                Real akq = a[3 * k + q];
                a[3 * k + p] = a[3 * p + k] = cosine * akp - sine * akq;
                a[3 * k + q] = a[3 * q + k] = sine * akp + cosine * akq;
            }

            Real vkp = basis[3 * k + p];
            Real vkq = basis[3 * k + q];
            basis[3 * k + p] = cosine * vkp - sine * vkq;
            basis[3 * k + q] = sine * vkp + cosine * vkq;
        }

        return true;
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
