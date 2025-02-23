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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>
/// Represents a three-dimensional vector with components of type <see cref="Real"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 3*sizeof(Real))]
public partial struct JVector : IEquatable<JVector>
{
    internal static JVector InternalZero;
    internal static JVector Arbitrary;

    [FieldOffset(0*sizeof(Real))] public Real X;
    [FieldOffset(1*sizeof(Real))] public Real Y;
    [FieldOffset(2*sizeof(Real))] public Real Z;

    public static readonly JVector Zero;
    public static readonly JVector UnitX;
    public static readonly JVector UnitY;
    public static readonly JVector UnitZ;
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
        MinValue = new JVector(Real.MinValue);
        MaxValue = new JVector(Real.MaxValue);
        Arbitrary = new JVector(1, 1, 1);
        InternalZero = Zero;
    }

    public JVector(Real x, Real y, Real z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Set(Real x, Real y, Real z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public JVector(Real xyz)
    {
        X = xyz;
        Y = xyz;
        Z = xyz;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref Real UnsafeGet(int index)
    {
        Real* ptr = (Real*)Unsafe.AsPointer(ref this);
        return ref ptr[index];
    }

    public unsafe Real this[int i]
    {
        get
        {
            fixed (Real* ptr = &X)
            {
                return ptr[i];
            }
        }
        set
        {
            fixed (Real* ptr = &X)
            {
                ptr[i] = value;
            }
        }
    }

    /// <summary>
    /// Returns a string representation of the <see cref="JVector"/>.
    /// </summary>
    public readonly override string ToString()
    {
        return $"X={X:F6}, Y={Y:F6}, Z={Z:F6}";
    }

    public override bool Equals(object? obj)
    {
        return obj is JVector other && Equals(other);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Min(in JVector value1, in JVector value2)
    {
        Min(value1, value2, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Min(in JVector value1, in JVector value2, out JVector result)
    {
        result.X = value1.X < value2.X ? value1.X : value2.X;
        result.Y = value1.Y < value2.Y ? value1.Y : value2.Y;
        result.Z = value1.Z < value2.Z ? value1.Z : value2.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Max(in JVector value1, in JVector value2)
    {
        Max(value1, value2, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Abs(in JVector value1)
    {
        return new JVector(MathR.Abs(value1.X), MathR.Abs(value1.Y), MathR.Abs(value1.Z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Real MaxAbs(in JVector value1)
    {
        JVector abs = Abs(value1);
        return MathR.Max(MathR.Max(abs.X, abs.Y), abs.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Max(in JVector value1, in JVector value2, out JVector result)
    {
        result.X = value1.X > value2.X ? value1.X : value2.X;
        result.Y = value1.Y > value2.Y ? value1.Y : value2.Y;
        result.Z = value1.Z > value2.Z ? value1.Z : value2.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakeZero()
    {
        X = (Real)0.0;
        Y = (Real)0.0;
        Z = (Real)0.0;
    }

    /// <summary>
    /// Calculates matrix \times vector, where vector is a column vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Transform(in JVector vector, in JMatrix matrix)
    {
        Transform(vector, matrix, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Transform(in JVector vector, in JQuaternion quat)
    {
        Transform(vector, quat, out JVector result);
        return result;
    }

    /// <summary>
    /// Calculates matrix^\mathrf{T} \times vector, where vector is a column vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector TransposedTransform(in JVector vector, in JMatrix matrix)
    {
        TransposedTransform(vector, matrix, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector TransposedTransform(in JVector vector, in JQuaternion quat)
    {
        ConjugatedTransform(vector, quat, out JVector result);
        return result;
    }

    /// <summary>
    /// Calculates matrix \times vector, where vector is a column vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Transform(in JVector vector, in JMatrix matrix, out JVector result)
    {
        Real num0 = vector.X * matrix.M11 + vector.Y * matrix.M12 + vector.Z * matrix.M13;
        Real num1 = vector.X * matrix.M21 + vector.Y * matrix.M22 + vector.Z * matrix.M23;
        Real num2 = vector.X * matrix.M31 + vector.Y * matrix.M32 + vector.Z * matrix.M33;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
    }

    /// <summary>
    /// Calculates matrix^\mathrf{T} \times vector, where vector is a column vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TransposedTransform(in JVector vector, in JMatrix matrix, out JVector result)
    {
        Real num0 = vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31;
        Real num1 = vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32;
        Real num2 = vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
    }

    /// <summary>
    /// Transforms the vector by a quaternion.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Transform(in JVector vector, in JQuaternion quaternion, out JVector result)
    {
        Real numx = (Real)2.0 * (quaternion.Y * vector.Z - quaternion.Z * vector.Y);
        Real numy = (Real)2.0 * (quaternion.Z * vector.X - quaternion.X * vector.Z);
        Real numz = (Real)2.0 * (quaternion.X * vector.Y - quaternion.Y * vector.X);

        Real num00 = quaternion.Y * numz - quaternion.Z * numy;
        Real num11 = quaternion.Z * numx - quaternion.X * numz;
        Real num22 = quaternion.X * numy - quaternion.Y * numx;

        result.X = vector.X + quaternion.W * numx + num00;
        result.Y = vector.Y + quaternion.W * numy + num11;
        result.Z = vector.Z + quaternion.W * numz + num22;
    }

    /// <summary>
    /// Transforms the vector by a conjugated quaternion.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConjugatedTransform(in JVector vector, in JQuaternion quaternion, out JVector result)
    {
        Real numx = (Real)2.0 * (quaternion.Z * vector.Y - quaternion.Y * vector.Z);
        Real numy = (Real)2.0 * (quaternion.X * vector.Z - quaternion.Z * vector.X);
        Real numz = (Real)2.0 * (quaternion.Y * vector.X - quaternion.X * vector.Y);

        Real num00 = quaternion.Z * numy - quaternion.Y * numz;
        Real num11 = quaternion.X * numz - quaternion.Z * numx;
        Real num22 = quaternion.Y * numx - quaternion.X * numy;

        result.X = vector.X + quaternion.W * numx + num00;
        result.Y = vector.Y + quaternion.W * numy + num11;
        result.Z = vector.Z + quaternion.W * numz + num22;
    }

    /// <summary>
    /// Calculates the outer product.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JMatrix Outer(in JVector u, in JVector v)
    {
        JMatrix result;
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
    public static Real Dot(in JVector vector1, in JVector vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Add(in JVector value1, in JVector value2)
    {
        Add(value1, value2, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(in JVector value1, in JVector value2, out JVector result)
    {
        result.X = value1.X + value2.X;
        result.Y = value1.Y + value2.Y;
        result.Z = value1.Z + value2.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Subtract(JVector value1, JVector value2)
    {
        Subtract(value1, value2, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Subtract(in JVector value1, in JVector value2, out JVector result)
    {
        Real num0 = value1.X - value2.X;
        Real num1 = value1.Y - value2.Y;
        Real num2 = value1.Z - value2.Z;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Cross(in JVector vector1, in JVector vector2)
    {
        Cross(vector1, vector2, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Cross(in JVector vector1, in JVector vector2, out JVector result)
    {
        Real num0 = vector1.Y * vector2.Z - vector1.Z * vector2.Y;
        Real num1 = vector1.Z * vector2.X - vector1.X * vector2.Z;
        Real num2 = vector1.X * vector2.Y - vector1.Y * vector2.X;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Negate()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Negate(in JVector value)
    {
        Negate(value, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Negate(in JVector value, out JVector result)
    {
        Real num0 = -value.X;
        Real num1 = -value.Y;
        Real num2 = -value.Z;

        result.X = num0;
        result.Y = num1;
        result.Z = num2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Normalize(in JVector value)
    {
        Normalize(value, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        Real num2 = X * X + Y * Y + Z * Z;
        Real num = (Real)1.0 / MathR.Sqrt(num2);
        X *= num;
        Y *= num;
        Z *= num;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Normalize(in JVector value, out JVector result)
    {
        Real num2 = value.X * value.X + value.Y * value.Y + value.Z * value.Z;
        Real num = (Real)1.0 / MathR.Sqrt(num2);
        result.X = value.X * num;
        result.Y = value.Y * num;
        result.Z = value.Z * num;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real Length()
    {
        return MathR.Sqrt(X * X + Y * Y + Z * Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap(ref JVector vector1, ref JVector vector2)
    {
        (vector2, vector1) = (vector1, vector2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector Multiply(in JVector value1, Real scaleFactor)
    {
        Multiply(value1, scaleFactor, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Multiply(Real factor)
    {
        X *= factor;
        Y *= factor;
        Z *= factor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JVector value1, Real scaleFactor, out JVector result)
    {
        result.X = value1.X * scaleFactor;
        result.Y = value1.Y * scaleFactor;
        result.Z = value1.Z * scaleFactor;
    }

    /// <summary>
    /// Calculates the cross product.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector operator %(in JVector vector1, in JVector vector2)
    {
        JVector result;
        result.X = vector1.Y * vector2.Z - vector1.Z * vector2.Y;
        result.Y = vector1.Z * vector2.X - vector1.X * vector2.Z;
        result.Z = vector1.X * vector2.Y - vector1.Y * vector2.X;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Real operator *(in JVector vector1, in JVector vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector operator *(in JVector value1, Real value2)
    {
        JVector result;
        result.X = value1.X * value2;
        result.Y = value1.Y * value2;
        result.Z = value1.Z * value2;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector operator *(Real value1, in JVector value2)
    {
        JVector result;
        result.X = value2.X * value1;
        result.Y = value2.Y * value1;
        result.Z = value2.Z * value1;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector operator -(in JVector value1, in JVector value2)
    {
        JVector result;
        result.X = value1.X - value2.X;
        result.Y = value1.Y - value2.Y;
        result.Z = value1.Z - value2.Z;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector operator -(in JVector left)
    {
        return Multiply(left, -(Real)1.0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector operator +(in JVector left)
    {
        return left;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector operator +(in JVector value1, in JVector value2)
    {
        JVector result;
        result.X = value1.X + value2.X;
        result.Y = value1.Y + value2.Y;
        result.Z = value1.Z + value2.Z;

        return result;
    }

    public bool Equals(JVector other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }
}