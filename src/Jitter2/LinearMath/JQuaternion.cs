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

namespace Jitter2.LinearMath;

/// <summary>
/// Quaternion Q = Xi + Yj + Zk + W. Uses Hamilton's definition of ij=k.
/// </summary>
public struct JQuaternion
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    /// <summary>
    /// Gets the identity quaternion (0, 0, 0, 1).
    /// </summary>
    public static JQuaternion Identity => new JQuaternion(0, 0, 0, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="JQuaternion"/> struct.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    /// <param name="w">The W component.</param>
    public JQuaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JQuaternion"/> struct.
    /// </summary>
    /// <param name="w">The W component.</param>
    /// <param name="v">The vector component.</param>
    public JQuaternion(float w, in JVector v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
        W = w;
    }

    /// <summary>
    /// Adds two quaternions.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <returns>The sum of the two quaternions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Add(JQuaternion quaternion1, JQuaternion quaternion2)
    {
        Add(in quaternion1, in quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Adds two quaternions.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">When the method completes, contains the sum of the two quaternions.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        result.X = quaternion1.X + quaternion2.X;
        result.Y = quaternion1.Y + quaternion2.Y;
        result.Z = quaternion1.Z + quaternion2.Z;
        result.W = quaternion1.W + quaternion2.W;
    }

    /// <summary>
    /// Returns the conjugate of a quaternion.
    /// </summary>
    /// <param name="value">The quaternion to conjugate.</param>
    /// <returns>The conjugate of the quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Conjugate(in JQuaternion value)
    {
        JQuaternion quaternion;
        quaternion.X = -value.X;
        quaternion.Y = -value.Y;
        quaternion.Z = -value.Z;
        quaternion.W = value.W;
        return quaternion;
    }

    /// <summary>
    /// Returns the conjugate of the quaternion.
    /// </summary>
    /// <returns>The conjugate of the quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly JQuaternion Conjugate()
    {
        JQuaternion quaternion;
        quaternion.X = -X;
        quaternion.Y = -Y;
        quaternion.Z = -Z;
        quaternion.W = W;
        return quaternion;
    }

    /// <summary>
    /// Returns a string that represents the current quaternion.
    /// </summary>
    /// <returns>A string that represents the current quaternion.</returns>
    public readonly override string ToString()
    {
        return $"{X:F6} {Y:F6} {Z:F6} {W:F6}";
    }

    /// <summary>
    /// Subtracts one quaternion from another.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <returns>The difference of the two quaternions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Subtract(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        Subtract(quaternion1, quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Subtracts one quaternion from another.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">When the method completes, contains the difference of the two quaternions.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Subtract(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        result.X = quaternion1.X - quaternion2.X;
        result.Y = quaternion1.Y - quaternion2.Y;
        result.Z = quaternion1.Z - quaternion2.Z;
        result.W = quaternion1.W - quaternion2.W;
    }

    /// <summary>
    /// Multiplies two quaternions.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <returns>The product of the two quaternions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Multiply(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        Multiply(quaternion1, quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Calculates the transformation of (1,0,0)^T by the quaternion.
    /// </summary>
    /// <returns>The transformed vector.</returns>
    public readonly JVector GetBasisX()
    {
        JVector result;

        result.X = 1.0f - 2.0f * (Y * Y + Z * Z);
        result.Y = 2.0f * (X * Y + Z * W);
        result.Z = 2.0f * (X * Z - Y * W);

        return result;
    }

    /// <summary>
    /// Calculates the transformation of (0,1,0)^T by the quaternion.
    /// </summary>
    /// <returns>The transformed vector.</returns>
    public readonly JVector GetBasisY()
    {
        JVector result;

        result.X = 2.0f * (X * Y - Z * W);
        result.Y = 1.0f - 2.0f * (X * X + Z * Z);
        result.Z = 2.0f * (Y * Z + X * W);

        return result;
    }

    /// <summary>
    /// Calculates the transformation of (0,0,1)^T by the quaternion.
    /// </summary>
    /// <returns>The transformed vector.</returns>
    public readonly JVector GetBasisZ()
    {
        JVector result;

        result.X = 2.0f * (X * Z + Y * W);
        result.Y = 2.0f * (Y * Z - X * W);
        result.Z = 1.0f - 2.0f * (X * X + Y * Y);

        return result;
    }

    /// <summary>
    /// Creates a quaternion representing a rotation around the X-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>A quaternion representing the rotation.</returns>
    public static JQuaternion CreateRotationX(float radians)
    {
        float halfAngle = radians * 0.5f;
        (float sha, float cha) = MathF.SinCos(halfAngle);
        return new JQuaternion(sha, 0, 0, cha);
    }

    /// <summary>
    /// Creates a quaternion representing a rotation around the Y-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>A quaternion representing the rotation.</returns>
    public static JQuaternion CreateRotationY(float radians)
    {
        float halfAngle = radians * 0.5f;
        (float sha, float cha) = MathF.SinCos(halfAngle);
        return new JQuaternion(0, sha, 0, cha);
    }

    /// <summary>
    /// Creates a quaternion representing a rotation around the Z-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>A quaternion representing the rotation.</returns>
    public static JQuaternion CreateRotationZ(float radians)
    {
        float halfAngle = radians * 0.5f;
        (float sha, float cha) = MathF.SinCos(halfAngle);
        return new JQuaternion(0, 0, sha, cha);
    }

    /// <summary>
    /// Multiplies two quaternions.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">When the method completes, contains the product of the two quaternions.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        float r1 = quaternion1.W;
        float i1 = quaternion1.X;
        float j1 = quaternion1.Y;
        float k1 = quaternion1.Z;

        float r2 = quaternion2.W;
        float i2 = quaternion2.X;
        float j2 = quaternion2.Y;
        float k2 = quaternion2.Z;

        result.W = r1 * r2 - (i1 * i2 + j1 * j2 + k1 * k2);
        result.X = r1 * i2 + r2 * i1 + j1 * k2 - k1 * j2;
        result.Y = r1 * j2 + r2 * j1 + k1 * i2 - i1 * k2;
        result.Z = r1 * k2 + r2 * k1 + i1 * j2 - j1 * i2;
    }

    /// <summary>
    /// Calculates quaternion1* \times quaternion2.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">When the method completes, contains the product of the conjugate of the first quaternion and the second quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConjugateMultiply(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        float r1 = quaternion1.W;
        float i1 = -quaternion1.X;
        float j1 = -quaternion1.Y;
        float k1 = -quaternion1.Z;

        float r2 = quaternion2.W;
        float i2 = quaternion2.X;
        float j2 = quaternion2.Y;
        float k2 = quaternion2.Z;

        result.W = r1 * r2 - (i1 * i2 + j1 * j2 + k1 * k2);
        result.X = r1 * i2 + r2 * i1 + j1 * k2 - k1 * j2;
        result.Y = r1 * j2 + r2 * j1 + k1 * i2 - i1 * k2;
        result.Z = r1 * k2 + r2 * k1 + i1 * j2 - j1 * i2;
    }

    /// <summary>
    /// Calculates quaternion1* \times quaternion2.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion ConjugateMultiply(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        ConjugateMultiply(quaternion1, quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Calculates quaternion1 \times quaternion2*.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">When the method completes, contains the product of the first quaternion and the conjugate of the second quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyConjugate(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        float r1 = quaternion1.W;
        float i1 = quaternion1.X;
        float j1 = quaternion1.Y;
        float k1 = quaternion1.Z;

        float r2 = quaternion2.W;
        float i2 = -quaternion2.X;
        float j2 = -quaternion2.Y;
        float k2 = -quaternion2.Z;

        result.W = r1 * r2 - (i1 * i2 + j1 * j2 + k1 * k2);
        result.X = r1 * i2 + r2 * i1 + j1 * k2 - k1 * j2;
        result.Y = r1 * j2 + r2 * j1 + k1 * i2 - i1 * k2;
        result.Z = r1 * k2 + r2 * k1 + i1 * j2 - j1 * i2;
    }

    /// <summary>
    /// Calculates quaternion1 \times quaternion2*.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion MultiplyConjugate(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        MultiplyConjugate(quaternion1, quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Multiplies a quaternion by a scalar factor.
    /// </summary>
    /// <param name="quaternion1">The quaternion to multiply.</param>
    /// <param name="scaleFactor">The scalar factor.</param>
    /// <returns>The scaled quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Multiply(in JQuaternion quaternion1, float scaleFactor)
    {
        Multiply(in quaternion1, scaleFactor, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Multiplies a quaternion by a scalar factor.
    /// </summary>
    /// <param name="quaternion1">The quaternion to multiply.</param>
    /// <param name="scaleFactor">The scalar factor.</param>
    /// <param name="result">When the method completes, contains the scaled quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JQuaternion quaternion1, float scaleFactor, out JQuaternion result)
    {
        result.W = quaternion1.W * scaleFactor;
        result.X = quaternion1.X * scaleFactor;
        result.Y = quaternion1.Y * scaleFactor;
        result.Z = quaternion1.Z * scaleFactor;
    }

    /// <summary>
    /// Calculates the length of the quaternion.
    /// </summary>
    /// <returns>The length of the quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
    }

    /// <summary>
    /// Normalizes the quaternion to unit length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        float num2 = X * X + Y * Y + Z * Z + W * W;
        float num = 1f / (float)Math.Sqrt(num2);
        X *= num;
        Y *= num;
        Z *= num;
        W *= num;
    }

    /// <summary>
    /// Creates a quaternion from a rotation matrix.
    /// </summary>
    /// <param name="matrix">The rotation matrix.</param>
    /// <returns>A quaternion representing the rotation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion CreateFromMatrix(in JMatrix matrix)
    {
        CreateFromMatrix(matrix, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Creates a quaternion from a rotation matrix.
    /// </summary>
    /// <param name="matrix">The rotation matrix.</param>
    /// <param name="result">When the method completes, contains the quaternion representing the rotation.</param>
    public static void CreateFromMatrix(in JMatrix matrix, out JQuaternion result)
    {
        float t;

        if (matrix.M33 < 0)
        {
            if (matrix.M11 > matrix.M22)
            {
                t = 1.0f + matrix.M11 - matrix.M22 - matrix.M33;
                result = new JQuaternion(t, matrix.M21 + matrix.M12, matrix.M31 + matrix.M13, matrix.M32 - matrix.M23);
            }
            else
            {
                t = 1.0f - matrix.M11 + matrix.M22 - matrix.M33;
                result = new JQuaternion(matrix.M21 + matrix.M12, t, matrix.M32 + matrix.M23, matrix.M13 - matrix.M31);
            }
        }
        else
        {
            if (matrix.M11 < -matrix.M22)
            {
                t = 1.0f - matrix.M11 - matrix.M22 + matrix.M33;
                result = new JQuaternion(matrix.M13 + matrix.M31, matrix.M32 + matrix.M23, t, matrix.M21 - matrix.M12);
            }
            else
            {
                t = 1.0f + matrix.M11 + matrix.M22 + matrix.M33;
                result = new JQuaternion(matrix.M32 - matrix.M23, matrix.M13 - matrix.M31, matrix.M21 - matrix.M12, t);
            }
        }

        t = (float)(0.5d / Math.Sqrt(t));
        result.X *= t;
        result.Y *= t;
        result.Z *= t;
        result.W *= t;
    }

    /// <summary>
    /// Multiplies two quaternions.
    /// </summary>
    /// <param name="value1">The first quaternion.</param>
    /// <param name="value2">The second quaternion.</param>
    /// <returns>The product of the two quaternions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator *(in JQuaternion value1, in JQuaternion value2)
    {
        Multiply(value1, value2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Multiplies a quaternion by a scalar factor.
    /// </summary>
    /// <param name="value1">The scalar factor.</param>
    /// <param name="value2">The quaternion to multiply.</param>
    /// <returns>The scaled quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator *(float value1, in JQuaternion value2)
    {
        Multiply(value2, value1, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Multiplies a quaternion by a scalar factor.
    /// </summary>
    /// <param name="value1">The quaternion to multiply.</param>
    /// <param name="value2">The scalar factor.</param>
    /// <returns>The scaled quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator *(in JQuaternion value1, float value2)
    {
        Multiply(value1, value2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Adds two quaternions.
    /// </summary>
    /// <param name="value1">The first quaternion.</param>
    /// <param name="value2">The second quaternion.</param>
    /// <returns>The sum of the two quaternions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator +(in JQuaternion value1, in JQuaternion value2)
    {
        Add(value1, value2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Subtracts one quaternion from another.
    /// </summary>
    /// <param name="value1">The first quaternion.</param>
    /// <param name="value2">The second quaternion.</param>
    /// <returns>The result of subtracting the second quaternion from the first.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator -(in JQuaternion value1, in JQuaternion value2)
    {
        Subtract(value1, value2, out JQuaternion result);
        return result;
    }
}