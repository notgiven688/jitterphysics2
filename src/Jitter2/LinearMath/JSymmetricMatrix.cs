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
/// Represents a symmetric 3x3 matrix using six components of type <see cref="Real"/>.
/// </summary>
/// <remarks>
/// Only the upper triangle is stored. The lower-triangle properties access the same components.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 6 * sizeof(Real))]
public struct JSymmetricMatrix(
    Real m11, Real m12, Real m13,
    Real m22, Real m23,
    Real m33) : IEquatable<JSymmetricMatrix>
{
    [FieldOffset(0 * sizeof(Real))] public Real M11 = m11;
    [FieldOffset(1 * sizeof(Real))] public Real M12 = m12;
    [FieldOffset(2 * sizeof(Real))] public Real M13 = m13;
    [FieldOffset(3 * sizeof(Real))] public Real M22 = m22;
    [FieldOffset(4 * sizeof(Real))] public Real M23 = m23;
    [FieldOffset(5 * sizeof(Real))] public Real M33 = m33;

    /// <summary>The component shared with <see cref="M12"/>.</summary>
    public Real M21
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => M12;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => M12 = value;
    }

    /// <summary>The component shared with <see cref="M13"/>.</summary>
    public Real M31
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => M13;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => M13 = value;
    }

    /// <summary>The component shared with <see cref="M23"/>.</summary>
    public Real M32
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => M23;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => M23 = value;
    }


    /// <summary>The identity matrix.</summary>
    public static readonly JSymmetricMatrix Identity = new(1, 0, 0, 1, 0, 1);

    /// <summary>The zero matrix.</summary>
    public static readonly JSymmetricMatrix Zero;

    /// <summary>Expands the matrix into a full 3x3 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly JMatrix ToMatrix()
    {
        return new JMatrix(M11, M12, M13, M12, M22, M23, M13, M23, M33);
    }

    /// <summary>Creates a diagonal scaling matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix CreateScale(in JVector scale)
    {
        return new JSymmetricMatrix(scale.X, 0, 0, scale.Y, 0, scale.Z);
    }

    /// <summary>Creates a diagonal scaling matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix CreateScale(Real x, Real y, Real z)
    {
        return new JSymmetricMatrix(x, 0, 0, y, 0, z);
    }

    /// <summary>Calculates the symmetric outer product <c>vector * vectorᵀ</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix Outer(in JVector vector)
    {
        return new JSymmetricMatrix(vector.X * vector.X, vector.X * vector.Y, vector.X * vector.Z,
            vector.Y * vector.Y, vector.Y * vector.Z, vector.Z * vector.Z);
    }

    /// <summary>Calculates the trace (sum of diagonal components).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real Trace()
    {
        return M11 + M22 + M33;
    }

    /// <summary>Calculates the determinant of the matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real Determinant()
    {
        return M11 * (M22 * M33 - M23 * M23) +
               M12 * (M23 * M13 - M12 * M33) +
               M13 * (M12 * M23 - M22 * M13);
    }

    /// <summary>Calculates the inverse of a symmetric matrix.</summary>
    /// <param name="matrix">The matrix to invert.</param>
    /// <param name="result">Output: The inverse, or zero if inversion fails.</param>
    /// <returns>
    /// <c>true</c> if the reciprocal determinant is a normal finite number;
    /// otherwise, <c>false</c>, matching <see cref="JMatrix.Inverse"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Inverse(in JSymmetricMatrix matrix, out JSymmetricMatrix result)
    {
        Real idet = (Real)1.0 / matrix.Determinant();

        if (!Real.IsNormal(idet))
        {
            result = Zero;
            return false;
        }

        // Read every input before writing the result, allowing in-place inversion.
        Real m11 = matrix.M22 * matrix.M33 - matrix.M23 * matrix.M23;
        Real m12 = matrix.M13 * matrix.M23 - matrix.M12 * matrix.M33;
        Real m13 = matrix.M12 * matrix.M23 - matrix.M22 * matrix.M13;
        Real m22 = matrix.M11 * matrix.M33 - matrix.M13 * matrix.M13;
        Real m23 = matrix.M13 * matrix.M12 - matrix.M23 * matrix.M11;
        Real m33 = matrix.M11 * matrix.M22 - matrix.M12 * matrix.M12;

        result = new JSymmetricMatrix(m11 * idet, m12 * idet, m13 * idet,
            m22 * idet, m23 * idet, m33 * idet);
        return true;
    }

    /// <summary>Adds two matrices component-wise.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix Add(in JSymmetricMatrix matrix1, in JSymmetricMatrix matrix2)
    {
        Add(matrix1, matrix2, out JSymmetricMatrix result);
        return result;
    }

    /// <summary>Adds two matrices component-wise.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(in JSymmetricMatrix matrix1, in JSymmetricMatrix matrix2,
        out JSymmetricMatrix result)
    {
        result = new JSymmetricMatrix(matrix1.M11 + matrix2.M11, matrix1.M12 + matrix2.M12,
            matrix1.M13 + matrix2.M13, matrix1.M22 + matrix2.M22,
            matrix1.M23 + matrix2.M23, matrix1.M33 + matrix2.M33);
    }

    /// <summary>Subtracts the second matrix from the first component-wise.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix Subtract(in JSymmetricMatrix matrix1, in JSymmetricMatrix matrix2)
    {
        Subtract(matrix1, matrix2, out JSymmetricMatrix result);
        return result;
    }

    /// <summary>Subtracts the second matrix from the first component-wise.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Subtract(in JSymmetricMatrix matrix1, in JSymmetricMatrix matrix2,
        out JSymmetricMatrix result)
    {
        result = new JSymmetricMatrix(matrix1.M11 - matrix2.M11, matrix1.M12 - matrix2.M12,
            matrix1.M13 - matrix2.M13, matrix1.M22 - matrix2.M22,
            matrix1.M23 - matrix2.M23, matrix1.M33 - matrix2.M33);
    }

    /// <summary>Multiplies a matrix by a scalar factor.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix Multiply(in JSymmetricMatrix matrix, Real scaleFactor)
    {
        Multiply(matrix, scaleFactor, out JSymmetricMatrix result);
        return result;
    }

    /// <summary>Multiplies a matrix by a scalar factor.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JSymmetricMatrix matrix, Real scaleFactor, out JSymmetricMatrix result)
    {
        result = new JSymmetricMatrix(matrix.M11 * scaleFactor, matrix.M12 * scaleFactor,
            matrix.M13 * scaleFactor, matrix.M22 * scaleFactor,
            matrix.M23 * scaleFactor, matrix.M33 * scaleFactor);
    }

    /// <summary>Calculates <c>transform * matrix * transformᵀ</c>.</summary>
    /// <remarks>
    /// Computes only the six independent components. A rotation transforms an inertia tensor
    /// into the rotated frame; general transforms are supported as well.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix Transform(in JSymmetricMatrix matrix, in JMatrix transform)
    {
        Transform(matrix, transform, out JSymmetricMatrix result);
        return result;
    }

    /// <summary>Calculates <c>transform * matrix * transformᵀ</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Transform(in JSymmetricMatrix matrix, in JMatrix transform,
        out JSymmetricMatrix result)
    {
        JVector row1 = new(transform.M11, transform.M12, transform.M13);
        JVector row2 = new(transform.M21, transform.M22, transform.M23);
        JVector row3 = new(transform.M31, transform.M32, transform.M33);

        JVector product1 = JVector.Transform(row1, matrix);
        JVector product2 = JVector.Transform(row2, matrix);
        JVector product3 = JVector.Transform(row3, matrix);

        result = new JSymmetricMatrix(product1 * row1, product1 * row2, product1 * row3,
            product2 * row2, product2 * row3, product3 * row3);
    }

    /// <summary>Expands the symmetric matrix into a full matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator JMatrix(in JSymmetricMatrix matrix) => matrix.ToMatrix();

    /// <summary>Adds two matrices.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix operator +(in JSymmetricMatrix matrix1, in JSymmetricMatrix matrix2)
        => Add(matrix1, matrix2);

    /// <summary>Subtracts the second matrix from the first.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix operator -(in JSymmetricMatrix matrix1, in JSymmetricMatrix matrix2)
        => Subtract(matrix1, matrix2);

    /// <summary>Scales a matrix by a factor.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix operator *(in JSymmetricMatrix matrix, Real factor)
        => Multiply(matrix, factor);

    /// <summary>Scales a matrix by a factor.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix operator *(Real factor, in JSymmetricMatrix matrix)
        => Multiply(matrix, factor);

    public readonly bool Equals(JSymmetricMatrix other)
    {
        return M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) &&
               M22.Equals(other.M22) && M23.Equals(other.M23) && M33.Equals(other.M33);
    }

    public readonly override bool Equals(object? obj) => obj is JSymmetricMatrix other && Equals(other);

    public readonly override int GetHashCode() => HashCode.Combine(M11, M12, M13, M22, M23, M33);

    public readonly override string ToString()
    {
        return $"M11={M11:F6}, M12={M12:F6}, M13={M13:F6}, " +
               $"M22={M22:F6}, M23={M23:F6}, M33={M33:F6}";
    }

    public static bool operator ==(JSymmetricMatrix left, JSymmetricMatrix right) => left.Equals(right);

    public static bool operator !=(JSymmetricMatrix left, JSymmetricMatrix right) => !left.Equals(right);
}
