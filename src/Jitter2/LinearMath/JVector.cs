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
/// Represents a three-dimensional vector with components of type <see cref="Real"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 3*sizeof(Real))]
public partial struct JVector(Real x, Real y, Real z) : IEquatable<JVector>
{
    internal static JVector InternalZero;
    internal static JVector Arbitrary;

    [FieldOffset(0*sizeof(Real))] public Real X = x;
    [FieldOffset(1*sizeof(Real))] public Real Y = y;
    [FieldOffset(2*sizeof(Real))] public Real Z = z;

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

    public void Set(Real x, Real y, Real z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public JVector(Real xyz) : this(xyz, xyz, xyz)
    {
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

    public readonly override bool Equals(object? obj)
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
    /// Calculates transposed(matrix) times vector, where vector is a column vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector TransposedTransform(in JVector vector, in JMatrix matrix)
    {
        TransposedTransform(vector, matrix, out JVector result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector ConjugatedTransform(in JVector vector, in JQuaternion quat)
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
    /// Calculates transposed(matrix) times vector, where vector is a column vector.
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
        Real num0 = (Real)2.0 * (quaternion.Y * vector.Z - quaternion.Z * vector.Y);
        Real num1 = (Real)2.0 * (quaternion.Z * vector.X - quaternion.X * vector.Z);
        Real num2 = (Real)2.0 * (quaternion.X * vector.Y - quaternion.Y * vector.X);

        Real num00 = quaternion.Y * num2 - quaternion.Z * num1;
        Real num11 = quaternion.Z * num0 - quaternion.X * num2;
        Real num22 = quaternion.X * num1 - quaternion.Y * num0;

        result.X = vector.X + quaternion.W * num0 + num00;
        result.Y = vector.Y + quaternion.W * num1 + num11;
        result.Z = vector.Z + quaternion.W * num2 + num22;
    }

    /// <summary>
    /// Transforms the vector by a conjugated quaternion.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConjugatedTransform(in JVector vector, in JQuaternion quaternion, out JVector result)
    {
        Real num0 = (Real)2.0 * (quaternion.Z * vector.Y - quaternion.Y * vector.Z);
        Real num1 = (Real)2.0 * (quaternion.X * vector.Z - quaternion.Z * vector.X);
        Real num2 = (Real)2.0 * (quaternion.Y * vector.X - quaternion.X * vector.Y);

        Real num00 = quaternion.Z * num1 - quaternion.Y * num2;
        Real num11 = quaternion.X * num2 - quaternion.Z * num0;
        Real num22 = quaternion.Y * num0 - quaternion.X * num1;

        result.X = vector.X + quaternion.W * num0 + num00;
        result.Y = vector.Y + quaternion.W * num1 + num11;
        result.Z = vector.Z + quaternion.W * num2 + num22;
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
    public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);

    [Obsolete($"Use static {nameof(NegateInPlace)} instead.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Negate()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NegateInPlace(ref JVector vector)
    {
        vector.X = -vector.X;
        vector.Y = -vector.Y;
        vector.Z = -vector.Z;
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


    /// <summary>
    /// Normalizes <paramref name="value"/>; returns <see cref="JVector.Zero"/> when its squared-length is below <paramref name="epsilonSquared"/>.
    /// </summary>
    /// <param name="value">Vector to normalize.</param>
    /// <param name="epsilonSquared">Cut-off for <c>‖value‖²</c>; default is <c>1 × 10⁻¹⁶</c>.</param>
    /// <returns>Unit vector or zero.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector NormalizeSafe(in JVector value, Real epsilonSquared = (Real)1e-16)
    {
        Real len2 = value.X * value.X + value.Y * value.Y + value.Z * value.Z;
        if (len2 < epsilonSquared) return JVector.Zero;

        return ((Real)1.0 / MathR.Sqrt(len2)) * value;
    }

    [Obsolete($"In-place Normalize() is deprecated; " +
              $"use the static {nameof(JVector.Normalize)} method or {nameof(JVector.NormalizeInPlace)}.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        Real num2 = X * X + Y * Y + Z * Z;
        Real num = (Real)1.0 / MathR.Sqrt(num2);
        X *= num;
        Y *= num;
        Z *= num;
    }

    public static void NormalizeInPlace(ref JVector toNormalize)
    {
        Real num2 = toNormalize.LengthSquared();
        Real num = (Real)1.0 / MathR.Sqrt(num2);
        toNormalize.X *= num;
        toNormalize.Y *= num;
        toNormalize.Z *= num;
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

    public readonly bool Equals(JVector other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }
}