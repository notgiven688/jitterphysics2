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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>
/// 3x3 matrix of 32 bit float values in column major format.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct JMatrix
{
    [FieldOffset(0)]
    public Matrix4x4 matrix;

    [FieldOffset(0)] public float M11;
    [FieldOffset(4)] public float M21;
    [FieldOffset(8)] public float M31;

    [FieldOffset(12)] public float M41;
    [FieldOffset(16)] public float M12;
    [FieldOffset(20)] public float M22;
    [FieldOffset(24)] public float M32;
    [FieldOffset(28)] public float M42;
    [FieldOffset(32)] public float M13;
    [FieldOffset(36)] public float M23;
    [FieldOffset(40)] public float M33;
    [FieldOffset(44)] public float M43;
    [FieldOffset(48)] public float M14;
    [FieldOffset(52)] public float M24;
    [FieldOffset(56)] public float M34;
    [FieldOffset(60)] public float M44;

    public static readonly JMatrix Identity;
    public static readonly JMatrix Zero;

    static JMatrix()
    {
        Zero = new JMatrix();

        Identity = new JMatrix
        {
            M11 = 1.0f,
            M22 = 1.0f,
            M33 = 1.0f,
            M44 = 1.0f
        };
    }

    public JMatrix(float m11, float m12, float m13, float m21, float m22, float m23, float m31, float m32, float m33)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M21 = m21;
        M22 = m22;
        M23 = m23;
        M31 = m31;
        M32 = m32;
        M33 = m33;
        M44 = 1;
    }

    public static JMatrix FromColumns(in JVector col1, in JVector col2, in JVector col3)
    {
        Unsafe.SkipInit(out JMatrix res);
        res.UnsafeGet(0) = col1;
        res.UnsafeGet(1) = col2;
        res.UnsafeGet(2) = col3;
        res.UnsafeGet(4) = JVector.UnitW;

        return res;
    }

    public unsafe ref JVector UnsafeGet(int index)
    {
        JVector* ptr = (JVector*)Unsafe.AsPointer(ref this);
        return ref ptr[index];
    }

    public unsafe JVector GetColumn(int index)
    {
        fixed (float* ptr = &M11)
        {
            JVector* vptr = (JVector*)ptr;
            return vptr[index];
        }
    }

    public static JMatrix Multiply(in JMatrix matrix1, in JMatrix matrix2)
    {
        Multiply(matrix1, matrix2, out JMatrix result);
        return result;
    }

    public static JMatrix MultiplyTransposed(in JMatrix matrix1, in JMatrix matrix2)
    {
        MultiplyTransposed(matrix1, matrix2, out JMatrix result);
        return result;
    }

    public static JMatrix TransposedMultiply(in JMatrix matrix1, in JMatrix matrix2)
    {
        TransposedMultiply(matrix1, matrix2, out JMatrix result);
        return result;
    }

    public static JMatrix CreateRotationMatrix(JVector axis, float angle)
    {
        float c = MathF.Cos(angle / 2.0f);
        float s = MathF.Sin(angle / 2.0f);
        axis *= s;
        JQuaternion jq = new(axis.X, axis.Y, axis.Z, c);
        CreateFromQuaternion(in jq, out JMatrix result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        result.matrix = Matrix4x4.Multiply(matrix1.matrix, matrix2.matrix);
    }

    public static JMatrix Add(JMatrix matrix1, JMatrix matrix2)
    {
        Add(matrix1, matrix2, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Calculates matrix1 \times matrix2^\mathrm{T}.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyTransposed(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        result.matrix = Matrix4x4.Multiply(matrix1.matrix, Matrix4x4.Transpose(matrix2.matrix));
    }

    public static JMatrix CreateRotationX(float radians)
    {
        JMatrix result = Identity;

        float c = (float)Math.Cos(radians);
        float s = (float)Math.Sin(radians);

        // [  1  0  0  0 ]
        // [  0  c -s  0 ]
        // [  0  s  c  0 ]
        // [  0  0  0  1 ]
        result.M22 = c;
        result.M23 = -s;
        result.M32 = s;
        result.M33 = c;

        return result;
    }

    public static JMatrix CreateRotationY(float radians)
    {
        JMatrix result = Identity;

        float c = (float)Math.Cos(radians);
        float s = (float)Math.Sin(radians);

        // [  c  0  s  0 ]
        // [  0  1  0  0 ]
        // [ -s  0  c  0 ]
        // [  0  0  0  1 ]
        result.M11 = c;
        result.M13 = s;
        result.M31 = -s;
        result.M33 = c;

        return result;
    }

    public static JMatrix CreateRotationZ(float radians)
    {
        JMatrix result = Identity;

        float c = (float)Math.Cos(radians);
        float s = (float)Math.Sin(radians);

        // [  c -s  0  0 ]
        // [  s  c  0  0 ]
        // [  0  0  1  0 ]
        // [  0  0  0  1 ]
        result.M11 = c;
        result.M12 = -s;
        result.M21 = s;
        result.M22 = c;

        return result;
    }

    /// <summary>
    /// Create a scaling matrix.
    /// </summary>
    /// <returns></returns>
    public static JMatrix CreateScale(in JVector scale)
    {
        JMatrix result = Zero;

        result.M11 = scale.X;
        result.M22 = scale.Y;
        result.M33 = scale.Z;
        result.M44 = 1;

        return result;
    }

    /// <summary>
    /// Create a scaling matrix.
    /// </summary>
    /// <returns></returns>
    public static JMatrix CreateScale(float x, float y, float z)
    {
        return CreateScale(new JVector(x, y, z));
    }

    /// <summary>
    /// Calculates matrix1^\mathrm{T} \times matrix2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TransposedMultiply(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        result.matrix = Matrix4x4.Multiply(Matrix4x4.Transpose(matrix1.matrix), matrix2.matrix);
    }

    public static void Add(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        result.matrix = Matrix4x4.Add(matrix1.matrix, matrix2.matrix);
    }

    public static void Subtract(in JMatrix matrix1, in JMatrix matrix2, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        result.matrix = Matrix4x4.Subtract(matrix1.matrix, matrix2.matrix);
    }
    public readonly float Determinant()
    {
        return matrix.GetDeterminant();
    }

    public static bool Inverse(in JMatrix matrix, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        return Matrix4x4.Invert(matrix.matrix, out result.matrix);
    }

    public static JMatrix Multiply(JMatrix matrix1, float scaleFactor)
    {
        Multiply(in matrix1, scaleFactor, out JMatrix result);
        return result;
    }

    public static void Multiply(in JMatrix matrix1, float scaleFactor, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        result.matrix = Matrix4x4.Multiply(matrix1.matrix, scaleFactor);
    }

    public static JMatrix CreateFromQuaternion(JQuaternion quaternion)
    {
        CreateFromQuaternion(quaternion, out JMatrix result);
        return result;
    }

    public static void Absolute(in JMatrix matrix, out JMatrix result)
    {
        Unsafe.SkipInit(out result);

        result.M11 = Math.Abs(matrix.M11);
        result.M12 = Math.Abs(matrix.M12);
        result.M13 = Math.Abs(matrix.M13);
        result.M14 = Math.Abs(matrix.M14);
        result.M21 = Math.Abs(matrix.M21);
        result.M22 = Math.Abs(matrix.M22);
        result.M23 = Math.Abs(matrix.M23);
        result.M24 = Math.Abs(matrix.M24);
        result.M31 = Math.Abs(matrix.M31);
        result.M32 = Math.Abs(matrix.M32);
        result.M33 = Math.Abs(matrix.M33);
        result.M34 = Math.Abs(matrix.M34);
        result.M41 = Math.Abs(matrix.M41);
        result.M42 = Math.Abs(matrix.M42);
        result.M43 = Math.Abs(matrix.M43);
        result.M44 = Math.Abs(matrix.M44);
    }

    public static void CreateFromQuaternion(in JQuaternion quaternion, out JMatrix result)
    {
        Unsafe.SkipInit(out result);

        float r = quaternion.W;
        float i = quaternion.X;
        float j = quaternion.Y;
        float k = quaternion.Z;

        result.M11 = 1.0f - 2.0f * (j * j + k * k);
        result.M12 = 2.0f * (i * j - k * r);
        result.M13 = 2.0f * (i * k + j * r);
        result.M21 = 2.0f * (i * j + k * r);
        result.M22 = 1.0f - 2.0f * (i * i + k * k);
        result.M23 = 2.0f * (j * k - i * r);
        result.M31 = 2.0f * (i * k - j * r);
        result.M32 = 2.0f * (j * k + i * r);
        result.M33 = 1.0f - 2.0f * (i * i + j * j);

        result.M14 = 0;
        result.M24 = 0;
        result.M34 = 0;
        result.M41 = 0;
        result.M42 = 0;
        result.M43 = 0;
        result.M44 = 1;
    }

    public static JMatrix Transpose(in JMatrix matrix)
    {
        Transpose(in matrix, out JMatrix result);
        return result;
    }

    /// <summary>
    /// Returns JMatrix(0, -vec.Z, vec.Y, vec.Z, 0, -vec.X, -vec.Y, vec.X, 0)-
    /// </summary>
    public static JMatrix CreateCrossProduct(in JVector vec)
    {
        return new JMatrix(0, -vec.Z, vec.Y, vec.Z, 0, -vec.X, -vec.Y, vec.X, 0);
    }

    private static void Transpose(in JMatrix matrix, out JMatrix result)
    {
        Unsafe.SkipInit(out result);
        result.matrix = Matrix4x4.Transpose(matrix.matrix);
    }

    public static JMatrix operator *(in JMatrix matrix1, in JMatrix matrix2)
    {
        Unsafe.SkipInit(out JMatrix result);
        result.matrix = Matrix4x4.Multiply(matrix1.matrix, matrix2.matrix);
        return result;
    }

    public float Trace()
    {
        return M11 + M22 + M33;
    }

    public static JMatrix operator *(float factor, in JMatrix matrix)
    {
        Multiply(matrix, factor, out JMatrix result);
        return result;
    }

    public static JMatrix operator *(in JMatrix matrix, float factor)
    {
        Multiply(matrix, factor, out JMatrix result);
        return result;
    }

    public static JMatrix operator +(in JMatrix value1, in JMatrix value2)
    {
        Add(value1, value2, out JMatrix result);
        return result;
    }

    public static JMatrix operator -(in JMatrix value1, in JMatrix value2)
    {
        Subtract(value1, value2, out JMatrix result);
        return result;
    }
}