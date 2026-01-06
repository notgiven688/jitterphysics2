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
/// Represents a 3x3 matrix with components of type <see cref="Real"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 9 * sizeof(Real))]
public struct JMatrix(
    Real m11, Real m12, Real m13,
    Real m21, Real m22, Real m23,
    Real m31, Real m32, Real m33) : IEquatable<JMatrix>
{
    [FieldOffset(0 * sizeof(Real))] public Real M11 = m11;
    [FieldOffset(1 * sizeof(Real))] public Real M21 = m21;
    [FieldOffset(2 * sizeof(Real))] public Real M31 = m31;
    [FieldOffset(3 * sizeof(Real))] public Real M12 = m12;
    [FieldOffset(4 * sizeof(Real))] public Real M22 = m22;
    [FieldOffset(5 * sizeof(Real))] public Real M32 = m32;
    [FieldOffset(6 * sizeof(Real))] public Real M13 = m13;
    [FieldOffset(7 * sizeof(Real))] public Real M23 = m23;
    [FieldOffset(8 * sizeof(Real))] public Real M33 = m33;

    /// <summary>
    /// The identity matrix.
    /// </summary>
    public static readonly JMatrix Identity;

    /// <summary>
    /// The zero matrix.
    /// </summary>
    public static readonly JMatrix Zero;

    static JMatrix()
    {
        Zero = new JMatrix();

        Identity = new JMatrix
        {
            M11 = (Real)1.0,
            M22 = (Real)1.0,
            M33 = (Real)1.0
        };
    }

    /// <summary>
    /// Creates a matrix from three column vectors.
    /// </summary>
    /// <param name="col1">The first column vector.</param>
    /// <param name="col2">The second column vector.</param>
    /// <param name="col3">The third column vector.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix FromColumns(in JVector col1, in JVector col2, in JVector col3)
    {
        Unsafe.SkipInit(out JMatrix res);
        res.UnsafeGet(0) = col1;
        res.UnsafeGet(1) = col2;
        res.UnsafeGet(2) = col3;
        return res;
    }

    /// <summary>
    /// Gets a reference to a column vector by index using unsafe pointer arithmetic.
    /// </summary>
    /// <param name="index">The zero-based index of the column (0, 1, or 2).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref JVector UnsafeGet(int index)
    {
        JVector* ptr = (JVector*)Unsafe.AsPointer(ref this);
        return ref ptr[index];
    }

    /// <summary>
    /// Gets a column vector by index.
    /// </summary>
    /// <param name="index">The zero-based index of the column (0, 1, or 2).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe JVector GetColumn(int index)
    {
        fixed (Real* ptr = &M11)
        {
            JVector* vptr = (JVector*)ptr;
            return vptr[index];
        }
    }

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>The product of the two matrices.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix Multiply(in JMatrix matrix1, in JMatrix matrix2)
    {
        Multiply(matrix1, matrix2, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Calculates <c>matrix1 * transpose(matrix2)</c>.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix (which will be transposed during multiplication).</param>
    /// <returns>The result of the multiplication.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix MultiplyTransposed(in JMatrix matrix1, in JMatrix matrix2)
    {
        MultiplyTransposed(matrix1, matrix2, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Calculates <c>transpose(matrix1) * matrix2</c>.
    /// </summary>
    /// <param name="matrix1">The first matrix (which will be transposed during multiplication).</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>The result of the multiplication.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix TransposedMultiply(in JMatrix matrix1, in JMatrix matrix2)
    {
        TransposedMultiply(matrix1, matrix2, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Creates a rotation matrix from an axis and an angle.
    /// </summary>
    /// <param name="axis">The axis to rotate around.</param>
    /// <param name="angle">The angle of rotation in radians.</param>
    /// <returns>The rotation matrix.</returns>
    public static JMatrix CreateRotationMatrix(JVector axis, Real angle)
    {
        Real c = MathR.Cos(angle / (Real)2.0);
        Real s = MathR.Sin(angle / (Real)2.0);
        axis *= s;
        JQuaternion jq = new(axis.X, axis.Y, axis.Z, c);
        CreateFromQuaternion(in jq, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <param name="result">Output: The product of the two matrices.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Real num0 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31;
        Real num1 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32;
        Real num2 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33;
        Real num3 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31;
        Real num4 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32;
        Real num5 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33;
        Real num6 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31;
        Real num7 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32;
        Real num8 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33;

        result.M11 = num0;
        result.M12 = num1;
        result.M13 = num2;
        result.M21 = num3;
        result.M22 = num4;
        result.M23 = num5;
        result.M31 = num6;
        result.M32 = num7;
        result.M33 = num8;
    }

    /// <summary>
    /// Adds two matrices.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <returns>The sum of the two matrices.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix Add(JMatrix matrix1, JMatrix matrix2)
    {
        Add(matrix1, matrix2, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Calculates <c>matrix1 * matrix2ᵀ</c> (multiplying matrix1 by the transpose of matrix2).
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix (transposed during operation).</param>
    /// <param name="result">Output: The result of the multiplication.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyTransposed(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Real num0 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M12 + matrix1.M13 * matrix2.M13;
        Real num1 = matrix1.M11 * matrix2.M21 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M23;
        Real num2 = matrix1.M11 * matrix2.M31 + matrix1.M12 * matrix2.M32 + matrix1.M13 * matrix2.M33;
        Real num3 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M12 + matrix1.M23 * matrix2.M13;
        Real num4 = matrix1.M21 * matrix2.M21 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M23;
        Real num5 = matrix1.M21 * matrix2.M31 + matrix1.M22 * matrix2.M32 + matrix1.M23 * matrix2.M33;
        Real num6 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M12 + matrix1.M33 * matrix2.M13;
        Real num7 = matrix1.M31 * matrix2.M21 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M23;
        Real num8 = matrix1.M31 * matrix2.M31 + matrix1.M32 * matrix2.M32 + matrix1.M33 * matrix2.M33;

        result.M11 = num0;
        result.M12 = num1;
        result.M13 = num2;
        result.M21 = num3;
        result.M22 = num4;
        result.M23 = num5;
        result.M31 = num6;
        result.M32 = num7;
        result.M33 = num8;
    }

    /// <summary>
    /// Creates a rotation matrix around the X-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>The rotation matrix.</returns>
    public static JMatrix CreateRotationX(Real radians)
    {
        JMatrix result = Identity;

        Real c = MathR.Cos(radians);
        Real s = MathR.Sin(radians);

        // [  1  0  0  ]
        // [  0  c -s  ]
        // [  0  s  c  ]
        result.M22 = c;
        result.M23 = -s;
        result.M32 = s;
        result.M33 = c;

        return result;
    }

    /// <summary>
    /// Creates a rotation matrix around the Y-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>The rotation matrix.</returns>
    public static JMatrix CreateRotationY(Real radians)
    {
        JMatrix result = Identity;

        Real c = MathR.Cos(radians);
        Real s = MathR.Sin(radians);

        // [  c  0  s  ]
        // [  0  1  0  ]
        // [ -s  0  c  ]
        result.M11 = c;
        result.M13 = s;
        result.M31 = -s;
        result.M33 = c;

        return result;
    }

    /// <summary>
    /// Creates a rotation matrix around the Z-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>The rotation matrix.</returns>
    public static JMatrix CreateRotationZ(Real radians)
    {
        JMatrix result = Identity;

        Real c = MathR.Cos(radians);
        Real s = MathR.Sin(radians);

        // [  c -s  0  ]
        // [  s  c  0  ]
        // [  0  0  1  ]
        result.M11 = c;
        result.M12 = -s;
        result.M21 = s;
        result.M22 = c;

        return result;
    }

    /// <summary>
    /// Creates a scaling matrix.
    /// </summary>
    /// <param name="scale">The scaling vector.</param>
    /// <returns>The scaling matrix.</returns>
    public static JMatrix CreateScale(in JVector scale)
    {
        JMatrix result = Zero;

        result.M11 = scale.X;
        result.M22 = scale.Y;
        result.M33 = scale.Z;

        return result;
    }

    /// <summary>
    /// Creates a scaling matrix.
    /// </summary>
    /// <param name="x">Scaling factor on the X-axis.</param>
    /// <param name="y">Scaling factor on the Y-axis.</param>
    /// <param name="z">Scaling factor on the Z-axis.</param>
    /// <returns>The scaling matrix.</returns>
    public static JMatrix CreateScale(Real x, Real y, Real z)
    {
        return CreateScale(new JVector(x, y, z));
    }

    /// <summary>
    /// Calculates <c>matrix1ᵀ * matrix2</c> (multiplying the transpose of matrix1 by matrix2).
    /// </summary>
    /// <param name="matrix1">The first matrix (transposed during operation).</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <param name="result">Output: The result of the multiplication.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TransposedMultiply(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Real num0 = matrix1.M11 * matrix2.M11 + matrix1.M21 * matrix2.M21 + matrix1.M31 * matrix2.M31;
        Real num1 = matrix1.M11 * matrix2.M12 + matrix1.M21 * matrix2.M22 + matrix1.M31 * matrix2.M32;
        Real num2 = matrix1.M11 * matrix2.M13 + matrix1.M21 * matrix2.M23 + matrix1.M31 * matrix2.M33;
        Real num3 = matrix1.M12 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M32 * matrix2.M31;
        Real num4 = matrix1.M12 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M32 * matrix2.M32;
        Real num5 = matrix1.M12 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M32 * matrix2.M33;
        Real num6 = matrix1.M13 * matrix2.M11 + matrix1.M23 * matrix2.M21 + matrix1.M33 * matrix2.M31;
        Real num7 = matrix1.M13 * matrix2.M12 + matrix1.M23 * matrix2.M22 + matrix1.M33 * matrix2.M32;
        Real num8 = matrix1.M13 * matrix2.M13 + matrix1.M23 * matrix2.M23 + matrix1.M33 * matrix2.M33;

        result.M11 = num0;
        result.M12 = num1;
        result.M13 = num2;
        result.M21 = num3;
        result.M22 = num4;
        result.M23 = num5;
        result.M31 = num6;
        result.M32 = num7;
        result.M33 = num8;
    }

    /// <summary>
    /// Adds two matrices component-wise.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <param name="result">Output: The sum of the two matrices.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        result.M11 = matrix1.M11 + matrix2.M11;
        result.M12 = matrix1.M12 + matrix2.M12;
        result.M13 = matrix1.M13 + matrix2.M13;
        result.M21 = matrix1.M21 + matrix2.M21;
        result.M22 = matrix1.M22 + matrix2.M22;
        result.M23 = matrix1.M23 + matrix2.M23;
        result.M31 = matrix1.M31 + matrix2.M31;
        result.M32 = matrix1.M32 + matrix2.M32;
        result.M33 = matrix1.M33 + matrix2.M33;
    }

    /// <summary>
    /// Subtracts the second matrix from the first component-wise.
    /// </summary>
    /// <param name="matrix1">The first matrix.</param>
    /// <param name="matrix2">The second matrix.</param>
    /// <param name="result">Output: The difference of the two matrices.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Subtract(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        result.M11 = matrix1.M11 - matrix2.M11;
        result.M12 = matrix1.M12 - matrix2.M12;
        result.M13 = matrix1.M13 - matrix2.M13;
        result.M21 = matrix1.M21 - matrix2.M21;
        result.M22 = matrix1.M22 - matrix2.M22;
        result.M23 = matrix1.M23 - matrix2.M23;
        result.M31 = matrix1.M31 - matrix2.M31;
        result.M32 = matrix1.M32 - matrix2.M32;
        result.M33 = matrix1.M33 - matrix2.M33;
    }

    /// <summary>
    /// Calculates the determinant of the matrix.
    /// </summary>
    /// <returns>The determinant.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real Determinant()
    {
        return M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32 -
               M31 * M22 * M13 - M32 * M23 * M11 - M33 * M21 * M12;
    }

    /// <summary>
    /// Calculates the inverse of the matrix.
    /// </summary>
    /// <param name="matrix">The matrix to invert.</param>
    /// <param name="result">Output: The inverted matrix, or a zero matrix if the determinant is zero.</param>
    /// <returns><c>true</c> if the matrix can be inverted; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Inverse(in JMatrix matrix, out JMatrix result)
    {
        Real idet = (Real)1.0 / matrix.Determinant();

        if (!Real.IsNormal(idet))
        {
            result = new JMatrix();
            return false;
        }

        Real num11 = matrix.M22 * matrix.M33 - matrix.M23 * matrix.M32;
        Real num12 = matrix.M13 * matrix.M32 - matrix.M12 * matrix.M33;
        Real num13 = matrix.M12 * matrix.M23 - matrix.M22 * matrix.M13;

        Real num21 = matrix.M23 * matrix.M31 - matrix.M33 * matrix.M21;
        Real num22 = matrix.M11 * matrix.M33 - matrix.M31 * matrix.M13;
        Real num23 = matrix.M13 * matrix.M21 - matrix.M23 * matrix.M11;

        Real num31 = matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22;
        Real num32 = matrix.M12 * matrix.M31 - matrix.M32 * matrix.M11;
        Real num33 = matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12;

        result.M11 = num11 * idet;
        result.M12 = num12 * idet;
        result.M13 = num13 * idet;
        result.M21 = num21 * idet;
        result.M22 = num22 * idet;
        result.M23 = num23 * idet;
        result.M31 = num31 * idet;
        result.M32 = num32 * idet;
        result.M33 = num33 * idet;

        return true;
    }

    /// <summary>
    /// Multiplies a matrix by a scalar factor.
    /// </summary>
    /// <param name="matrix1">The matrix.</param>
    /// <param name="scaleFactor">The scalar factor.</param>
    /// <returns>The scaled matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix Multiply(JMatrix matrix1, Real scaleFactor)
    {
        Multiply(in matrix1, scaleFactor, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Multiplies a matrix by a scalar factor.
    /// </summary>
    /// <param name="matrix1">The matrix.</param>
    /// <param name="scaleFactor">The scalar factor.</param>
    /// <param name="result">Output: The scaled matrix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JMatrix matrix1, Real scaleFactor, out JMatrix result)
    {
        Real num = scaleFactor;
        result.M11 = matrix1.M11 * num;
        result.M12 = matrix1.M12 * num;
        result.M13 = matrix1.M13 * num;
        result.M21 = matrix1.M21 * num;
        result.M22 = matrix1.M22 * num;
        result.M23 = matrix1.M23 * num;
        result.M31 = matrix1.M31 * num;
        result.M32 = matrix1.M32 * num;
        result.M33 = matrix1.M33 * num;
    }

    /// <summary>
    /// Creates a rotation matrix from a quaternion.
    /// </summary>
    /// <param name="quaternion">The quaternion representing the rotation.</param>
    /// <returns>The rotation matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix CreateFromQuaternion(JQuaternion quaternion)
    {
        CreateFromQuaternion(quaternion, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Creates a matrix where each component is the absolute value of the input matrix component.
    /// </summary>
    /// <param name="matrix">The input matrix.</param>
    /// <param name="result">Output: The absolute matrix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Absolute(in JMatrix matrix, out JMatrix result)
    {
        result.M11 = Math.Abs(matrix.M11);
        result.M12 = Math.Abs(matrix.M12);
        result.M13 = Math.Abs(matrix.M13);
        result.M21 = Math.Abs(matrix.M21);
        result.M22 = Math.Abs(matrix.M22);
        result.M23 = Math.Abs(matrix.M23);
        result.M31 = Math.Abs(matrix.M31);
        result.M32 = Math.Abs(matrix.M32);
        result.M33 = Math.Abs(matrix.M33);
    }

    /// <summary>
    /// Creates a rotation matrix from a quaternion.
    /// </summary>
    /// <param name="quaternion">The quaternion representing the rotation.</param>
    /// <param name="result">Output: The rotation matrix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreateFromQuaternion(in JQuaternion quaternion, out JMatrix result)
    {
        Real r = quaternion.W;
        Real i = quaternion.X;
        Real j = quaternion.Y;
        Real k = quaternion.Z;

        result.M11 = (Real)1.0 - (Real)2.0 * (j * j + k * k);
        result.M12 = (Real)2.0 * (i * j - k * r);
        result.M13 = (Real)2.0 * (i * k + j * r);
        result.M21 = (Real)2.0 * (i * j + k * r);
        result.M22 = (Real)1.0 - (Real)2.0 * (i * i + k * k);
        result.M23 = (Real)2.0 * (j * k - i * r);
        result.M31 = (Real)2.0 * (i * k - j * r);
        result.M32 = (Real)2.0 * (j * k + i * r);
        result.M33 = (Real)1.0 - (Real)2.0 * (i * i + j * j);
    }

    /// <summary>
    /// Transposes a matrix.
    /// </summary>
    /// <param name="matrix">The matrix to transpose.</param>
    /// <returns>The transposed matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix Transpose(in JMatrix matrix)
    {
        Transpose(in matrix, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Creates a skew-symmetric matrix from a vector, representing the cross product operation.
    /// </summary>
    /// <remarks>
    /// Result is equivalent to:
    /// <code>
    /// [  0  -z   y ]
    /// [  z   0  -x ]
    /// [ -y   x   0 ]
    /// </code>
    /// </remarks>
    /// <param name="vec">The vector.</param>
    /// <returns>The skew-symmetric matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix CreateCrossProduct(in JVector vec)
    {
        return new JMatrix(0, -vec.Z, vec.Y, vec.Z, 0, -vec.X, -vec.Y, vec.X, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Transpose(in JMatrix matrix, out JMatrix result)
    {
        result.M11 = matrix.M11;
        result.M12 = matrix.M21;
        result.M13 = matrix.M31;
        result.M21 = matrix.M12;
        result.M22 = matrix.M22;
        result.M23 = matrix.M32;
        result.M31 = matrix.M13;
        result.M32 = matrix.M23;
        result.M33 = matrix.M33;
    }

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix operator *(in JMatrix matrix1, in JMatrix matrix2)
    {
        JMatrix result;
        result.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31;
        result.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32;
        result.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33;
        result.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31;
        result.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32;
        result.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33;
        result.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31;
        result.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32;
        result.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33;
        return result;
    }

    /// <summary>
    /// Calculates the trace (sum of diagonal elements) of the matrix.
    /// </summary>
    /// <returns>The trace of the matrix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real Trace()
    {
        return M11 + M22 + M33;
    }

    /// <summary>
    /// Scales a matrix by a factor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix operator *(Real factor, in JMatrix matrix)
    {
        Multiply(matrix, factor, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Scales a matrix by a factor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix operator *(in JMatrix matrix, Real factor)
    {
        Multiply(matrix, factor, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Adds two matrices.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix operator +(in JMatrix value1, in JMatrix value2)
    {
        Add(value1, value2, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Subtracts the second matrix from the first.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix operator -(in JMatrix value1, in JMatrix value2)
    {
        Subtract(value1, value2, out JMatrix result);
        return result;
    }

    public readonly bool Equals(JMatrix other)
    {
        return M11.Equals(other.M11) && M21.Equals(other.M21) &&
               M31.Equals(other.M31) && M12.Equals(other.M12) &&
               M22.Equals(other.M22) && M32.Equals(other.M32) &&
               M13.Equals(other.M13) && M23.Equals(other.M23) &&
               M33.Equals(other.M33);
    }

    public readonly override string ToString()
    {
        return $"M11={M11:F6}, M12={M12:F6}, M13={M13:F6}, " +
               $"M21={M21:F6}, M22={M22:F6}, M23={M23:F6}, " +
               $"M31={M31:F6}, M32={M32:F6}, M33={M33:F6}";
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is JMatrix other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(M11); hc.Add(M21); hc.Add(M31);
        hc.Add(M12); hc.Add(M22); hc.Add(M32);
        hc.Add(M13); hc.Add(M23); hc.Add(M33);
        return hc.ToHashCode();
    }

    public static bool operator ==(JMatrix left, JMatrix right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JMatrix left, JMatrix right)
    {
        return !(left == right);
    }
}