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
/// Represents a three-dimensional vector using three floating-point numbers.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct JVector
{
    internal static JVector InternalZero;
    internal static JVector Arbitrary;

    [FieldOffset(0)]
    public Vector4 vector;

    [FieldOffset(0)]
    public float X;

    [FieldOffset(4)]
    public float Y;

    [FieldOffset(8)]
    public float Z;

    [FieldOffset(12)]
    public float W;

    public static readonly JVector Zero;
    public static readonly JVector UnitX;
    public static readonly JVector UnitY;
    public static readonly JVector UnitZ;
    public static readonly JVector UnitW;
    public static readonly JVector One;
    public static readonly JVector MinValue;
    public static readonly JVector MaxValue;

    static JVector()
    {
        One = new JVector(1, 1, 1);
        Zero = new JVector(0, 0, 0);
        UnitX = new JVector(1, 0, 0);
        UnitY = new JVector(0, 1, 0);
        UnitZ = new JVector(0, 0, 1);
        UnitW.vector.W = 1;
        MinValue = new JVector(float.MinValue);
        MaxValue = new JVector(float.MaxValue);
        Arbitrary = new JVector(1, 1, 1);
        InternalZero = Zero;
    }

    public JVector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Set(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public JVector(float xyz)
    {
        X = xyz;
        Y = xyz;
        Z = xyz;
    }

    public unsafe ref float UnsafeGet(int index)
    {
        float* ptr = (float*)Unsafe.AsPointer(ref this);
        return ref ptr[index];
    }

    public unsafe float this[int i]
    {
        get
        {
            fixed (float* ptr = &X)
            {
                return ptr[i];
            }
        }
        set
        {
            fixed (float* ptr = &X)
            {
                ptr[i] = value;
            }
        }
    }

    public readonly override string ToString()
    {
        return $"{X:F6} {Y:F6} {Z:F6}";
    }

    public readonly override bool Equals(object? obj)
    {
        if (obj is not JVector) return false;
        JVector other = (JVector)obj;

        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public static bool operator ==(JVector value1, JVector value2)
    {
        return value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
    }

    public static bool operator !=(JVector value1, JVector value2)
    {
        if (value1.X == value2.X && value1.Y == value2.Y)
        {
            return value1.Z != value2.Z;
        }

        return true;
    }

    public static JVector Min(in JVector value1, in JVector value2)
    {
        Min(value1, value2, out JVector result);
        return result;
    }

    public static void Min(in JVector value1, in JVector value2, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Min(value1.vector, value2.vector);
    }

    public static JVector Max(in JVector value1, in JVector value2)
    {
        Max(value1, value2, out JVector result);
        return result;
    }

    public static JVector Abs(in JVector value1)
    {
        return new JVector(MathF.Abs(value1.X), MathF.Abs(value1.Y), MathF.Abs(value1.Z));
    }

    public static float MaxAbs(in JVector value1)
    {
        JVector abs = Abs(value1);
        return MathF.Max(MathF.Max(abs.X, abs.Y), abs.Z);
    }

    public static void Max(in JVector value1, in JVector value2, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Max(value1.vector, value2.vector);
    }

    public void MakeZero()
    {
        X = 0.0f;
        Y = 0.0f;
        Z = 0.0f;
        W = 0.0f;
    }

    /// <summary>
    /// Calculates matrix \times vector, where vector is a column vector.
    /// </summary>
    public static JVector Transform(in JVector vector, in JMatrix matrix)
    {
        Unsafe.SkipInit(out JVector result);
        result.vector = Vector4.Transform(vector.vector, (matrix.matrix));
        return result;
    }

    /// <summary>
    /// Calculates matrix^\mathrf{T} \times vector, where vector is a column vector.
    /// </summary>
    public static JVector TransposedTransform(in JVector vector, in JMatrix matrix)
    {
        Unsafe.SkipInit(out JVector result);
        result.vector = Vector4.Transform(vector.vector, Matrix4x4.Transpose(matrix.matrix));
        return result;
    }

    /// <summary>
    /// Calculates matrix \times vector, where vector is a column vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Transform(in JVector vector, in JMatrix matrix, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Transform(vector.vector, (matrix.matrix));
    }

    /// <summary>
    /// Calculates matrix^\mathrf{T} \times vector, where vector is a column vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TransposedTransform(in JVector vector, in JMatrix matrix, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Transform(vector.vector, Matrix4x4.Transpose(matrix.matrix));
    }

    /// <summary>
    /// Calculates the outer product.
    /// </summary>
    public static JMatrix Outer(in JVector u, in JVector v)
    {
        JMatrix result = JMatrix.Zero;
        result.M11 = u.X * v.X;
        result.M12 = u.X * v.Y;
        result.M13 = u.X * v.Z;
        result.M21 = u.Y * v.X;
        result.M22 = u.Y * v.Y;
        result.M23 = u.Y * v.Z;
        result.M31 = u.Z * v.X;
        result.M32 = u.Z * v.Y;
        result.M33 = u.Z * v.Z;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(in JVector vector1, in JVector vector2)
    {
        return Vector4.Dot(vector1.vector, vector2.vector);
    }

    public static JVector Add(in JVector value1, in JVector value2)
    {
        Add(value1, value2, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(in JVector value1, in JVector value2, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Add(value1.vector, value2.vector);
    }

    public static JVector Subtract(JVector value1, JVector value2)
    {
        Unsafe.SkipInit(out JVector result);
        result.vector = Vector4.Subtract(value1.vector, value2.vector);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Subtract(in JVector value1, in JVector value2, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Subtract(value1.vector, value2.vector);
    }

    public static JVector Cross(in JVector vector1, in JVector vector2)
    {
        Cross(vector1, vector2, out JVector result);
        return result;
    }

    public static void Cross(in Vector4 vector1, in Vector4 vector2, out Vector4 result)
    {
        Unsafe.SkipInit(out result);

        float num0 = vector1.Y * vector2.Z - vector1.Z * vector2.Y;
        float num1 = vector1.Z * vector2.X - vector1.X * vector2.Z;
        float num2 = vector1.X * vector2.Y - vector1.Y * vector2.X;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
        result.W = vector1.W * vector2.W;
    }

    public static Vector4 Cross(in Vector4 vector1, in Vector4 vector2)
    {
        Unsafe.SkipInit(out Vector4 result);

        float num0 = vector1.Y * vector2.Z - vector1.Z * vector2.Y;
        float num1 = vector1.Z * vector2.X - vector1.X * vector2.Z;
        float num2 = vector1.X * vector2.Y - vector1.Y * vector2.X;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
        result.W = vector1.W * vector2.W;

        return result;
    }

    public static void Cross(in JVector vector1, in JVector vector2, out JVector result)
    {
        Unsafe.SkipInit(out result);

        float num0 = vector1.Y * vector2.Z - vector1.Z * vector2.Y;
        float num1 = vector1.Z * vector2.X - vector1.X * vector2.Z;
        float num2 = vector1.X * vector2.Y - vector1.Y * vector2.X;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
        result.W = vector1.W * vector2.W;
    }

    public readonly override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
    }

    public void Negate()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
        W = -W;
    }

    public static JVector Negate(in JVector value)
    {
        Negate(value, out JVector result);
        return result;
    }

    public static void Negate(in JVector value, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Negate(value.vector);
    }

    public static JVector Normalize(in JVector value)
    {
        Normalize(value, out JVector result);
        return result;
    }

    public void Normalize()
    {
        this.vector = Vector4.Normalize(vector);
    }

    public static void Normalize(in JVector value, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Normalize(value.vector);
    }

    public readonly float LengthSquared()
    {
        return Vector4.Dot(vector, vector);
    }

    public readonly float Length()
    {
        return vector.Length();
    }

    public static void Swap(ref JVector vector1, ref JVector vector2)
    {
        (vector2, vector1) = (vector1, vector2);
    }

    public static JVector Multiply(in JVector value1, float scaleFactor)
    {
        Multiply(value1, scaleFactor, out JVector result);
        return result;
    }

    public void Multiply(float factor)
    {
        vector = Vector4.Multiply(vector, factor);
    }

    public static void Multiply(in JVector value1, float scaleFactor, out JVector result)
    {
        Unsafe.SkipInit(out result);
        result.vector = Vector4.Multiply(value1.vector, scaleFactor);
    }

    /// <summary>
    /// Calculates the cross product.
    /// </summary>
    public static JVector operator %(in JVector vector1, in JVector vector2)
    {
        Unsafe.SkipInit(out JVector result);
        result.X = vector1.Y * vector2.Z - vector1.Z * vector2.Y;
        result.Y = vector1.Z * vector2.X - vector1.X * vector2.Z;
        result.Z = vector1.X * vector2.Y - vector1.Y * vector2.X;
        result.W = vector1.W * vector2.W;
        return result;
    }

    public static float operator *(in JVector vector1, in JVector vector2)
    {
        return Vector4.Dot(vector1.vector, vector2.vector);
    }

    public static JVector operator *(in JVector value1, float value2)
    {
        Unsafe.SkipInit(out JVector result);
        result.vector = Vector4.Multiply(value2, value1.vector);
        return result;
    }

    public static JVector operator *(float value1, in JVector value2)
    {
        Unsafe.SkipInit(out JVector result);
        result.vector = Vector4.Multiply(value1, value2.vector);
        return result;
    }

    public static JVector operator -(in JVector value1, in JVector value2)
    {
        Unsafe.SkipInit(out JVector result);
        result.vector = Vector4.Subtract(value1.vector, value2.vector);
        return result;
    }

    public static JVector operator -(JVector left)
    {
        return Multiply(left, -1.0f);
    }

    public static JVector operator +(in JVector value1, in JVector value2)
    {
        Unsafe.SkipInit(out JVector result);
        result.vector = Vector4.Add(value1.vector, value2.vector);
        return result;
    }
}