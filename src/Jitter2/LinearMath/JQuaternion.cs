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
/// A structure representing a Quaternion: <c>Q = x*i + y*j + z*k + w</c>.
/// <br/>
/// Uses the Hamilton convention where <c>i² = j² = k² = ijk = -1</c>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 4 * sizeof(Real))]
public partial struct JQuaternion(Real x, Real y, Real z, Real w) : IEquatable<JQuaternion>
{
    [FieldOffset(0 * sizeof(Real))] public Real X = x;
    [FieldOffset(1 * sizeof(Real))] public Real Y = y;
    [FieldOffset(2 * sizeof(Real))] public Real Z = z;
    [FieldOffset(3 * sizeof(Real))] public Real W = w;

    /// <summary>
    /// Gets the vector part <c>(x, y, z)</c> of the quaternion.
    /// </summary>
    public JVector Vector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.As<JQuaternion, JVector>(ref this);
    }

    /// <summary>
    /// Gets the scalar part <c>(w)</c> of the quaternion.
    /// </summary>
    public Real Scalar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => W;
    }

    /// <summary>
    /// Gets the identity quaternion <c>(0, 0, 0, 1)</c>.
    /// </summary>
    public static JQuaternion Identity => new(0, 0, 0, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="JQuaternion"/> struct.
    /// </summary>
    /// <param name="w">The scalar (W) component.</param>
    /// <param name="v">The vector (X, Y, Z) component.</param>
    public JQuaternion(Real w, in JVector v) : this(v.X, v.Y, v.Z, w)
    {
    }

    /// <summary>
    /// Adds two quaternions component-wise.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <returns>The sum of the two quaternions.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Add(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        Add(in quaternion1, in quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Creates a quaternion that rotates the unit vector <paramref name="from"/> into
    /// the unit vector <paramref name="to"/>.
    /// </summary>
    /// <remarks>
    /// This calculation relies on the half-angle formula.
    /// If the vectors are parallel (<c>dot ≈ 1</c>), Identity is returned.
    /// If the vectors are opposite (<c>dot ≈ -1</c>), a rotation of 180° (π radians)
    /// around an arbitrary orthogonal axis is returned.
    /// </remarks>
    /// <param name="from">Source direction (must be unit length).</param>
    /// <param name="to">Target direction (must be unit length).</param>
    /// <returns>A unit quaternion representing the shortest rotation.</returns>
    public static JQuaternion CreateFromToRotation(JVector from, JVector to)
    {
        const Real epsilon = (Real)1e-6;
        Real dot = JVector.Dot(from, to);

        // Vectors are opposite (Singularity)
        if (dot < -(Real)1.0 + epsilon)
        {
            JVector axis = MathHelper.CreateOrthonormal(from);
            return new JQuaternion(axis.X, axis.Y, axis.Z, 0);
        }

        Real s = MathR.Sqrt(((Real)1.0 + dot) * (Real)2.0);
        Real invS = (Real)1.0 / s;

        JVector c = JVector.Cross(from, to);
        return new JQuaternion(s * (Real)0.5, c * invS);
    }

    /// <summary>
    /// Adds two quaternions component-wise.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">Output: The sum of the two quaternions.</param>
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
    /// <remarks>
    /// The conjugate is defined as <c>(-x, -y, -z, w)</c>.
    /// For unit quaternions, the conjugate is equivalent to the inverse.
    /// </remarks>
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
    /// <remarks>
    /// The conjugate is defined as <c>(-x, -y, -z, w)</c>.
    /// </remarks>
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
    /// Returns a string representing the quaternion in the format <c>X=..., Y=..., Z=..., W=...</c>.
    /// </summary>
    public readonly override string ToString()
    {
        return $"X={X:F6}, Y={Y:F6}, Z={Z:F6}, W={W:F6}";
    }

    /// <summary>
    /// Subtracts the second quaternion from the first component-wise.
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
    /// Subtracts the second quaternion from the first component-wise.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">Output: The difference of the two quaternions.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Subtract(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        result.X = quaternion1.X - quaternion2.X;
        result.Y = quaternion1.Y - quaternion2.Y;
        result.Z = quaternion1.Z - quaternion2.Z;
        result.W = quaternion1.W - quaternion2.W;
    }

    /// <summary>
    /// Multiplies two quaternions (Hamilton Product).
    /// </summary>
    /// <remarks>
    /// Non-commutative. <c>Q1 * Q2</c> represents the rotation Q2 followed by Q1 (local frame) or Q1 followed by Q2 (global frame).
    /// </remarks>
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
    /// Calculates the transformation of the X-axis <c>(1, 0, 0)</c> by this quaternion.
    /// </summary>
    /// <remarks>
    /// Mathematically equivalent to <c>q · (1,0,0) · q⁻¹</c>.<br/>
    /// Result: <c>[1 - 2(y² + z²), 2(xy + zw), 2(xz - yw)]</c>
    /// </remarks>
    /// <returns>The transformed vector.</returns>
    public readonly JVector GetBasisX()
    {
        JVector result;
        result.X = (Real)1.0 - (Real)2.0 * (Y * Y + Z * Z);
        result.Y = (Real)2.0 * (X * Y + Z * W);
        result.Z = (Real)2.0 * (X * Z - Y * W);
        return result;
    }

    /// <summary>
    /// Calculates the transformation of the Y-axis <c>(0, 1, 0)</c> by this quaternion.
    /// </summary>
    /// <remarks>
    /// Mathematically equivalent to <c>q · (0,1,0) · q⁻¹</c>.<br/>
    /// Result: <c>[2(xy - zw), 1 - 2(x² + z²), 2(yz + xw)]</c>
    /// </remarks>
    /// <returns>The transformed vector.</returns>
    public readonly JVector GetBasisY()
    {
        JVector result;
        result.X = (Real)2.0 * (X * Y - Z * W);
        result.Y = (Real)1.0 - (Real)2.0 * (X * X + Z * Z);
        result.Z = (Real)2.0 * (Y * Z + X * W);
        return result;
    }

    /// <summary>
    /// Calculates the transformation of the Z-axis <c>(0, 0, 1)</c> by this quaternion.
    /// </summary>
    /// <remarks>
    /// Mathematically equivalent to <c>q · (0,0,1) · q⁻¹</c>.<br/>
    /// Result: <c>[2(xz + yw), 2(yz - xw), 1 - 2(x² + y²)]</c>
    /// </remarks>
    /// <returns>The transformed vector.</returns>
    public readonly JVector GetBasisZ()
    {
        JVector result;
        result.X = (Real)2.0 * (X * Z + Y * W);
        result.Y = (Real)2.0 * (Y * Z - X * W);
        result.Z = (Real)1.0 - (Real)2.0 * (X * X + Y * Y);
        return result;
    }

    /// <summary>
    /// Creates a quaternion representing a rotation around the X-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>The resulting quaternion.</returns>
    public static JQuaternion CreateRotationX(Real radians)
    {
        Real halfAngle = radians * (Real)0.5;
        (Real sha, Real cha) = MathR.SinCos(halfAngle);
        return new JQuaternion(sha, 0, 0, cha);
    }

    /// <summary>
    /// Creates a quaternion representing a rotation around the Y-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>The resulting quaternion.</returns>
    public static JQuaternion CreateRotationY(Real radians)
    {
        Real halfAngle = radians * (Real)0.5;
        (Real sha, Real cha) = MathR.SinCos(halfAngle);
        return new JQuaternion(0, sha, 0, cha);
    }

    /// <summary>
    /// Creates a quaternion representing a rotation around the Z-axis.
    /// </summary>
    /// <param name="radians">The angle of rotation in radians.</param>
    /// <returns>The resulting quaternion.</returns>
    public static JQuaternion CreateRotationZ(Real radians)
    {
        Real halfAngle = radians * (Real)0.5;
        (Real sha, Real cha) = MathR.SinCos(halfAngle);
        return new JQuaternion(0, 0, sha, cha);
    }

    /// <summary>
    /// Creates a quaternion from a unit axis and an angle.
    /// </summary>
    /// <remarks>
    /// The <paramref name="axis"/> must be normalized.
    /// </remarks>
    /// <param name="axis">The unit vector to rotate around (must be normalized).</param>
    /// <param name="angle">The angle of rotation in radians.</param>
    /// <returns>A unit quaternion representing the rotation.</returns>
    public static JQuaternion CreateFromAxisAngle(in JVector axis, Real angle)
    {
        Real halfAngle = angle * (Real)0.5;
        (Real s, Real c) = MathR.SinCos(halfAngle);
        return new JQuaternion(axis.X * s, axis.Y * s, axis.Z * s, c);
    }

    /// <summary>
    /// Decomposes a unit quaternion into an axis and an angle.
    /// </summary>
    /// <remarks>
    /// Assumes <paramref name="quaternion"/> is normalized.
    /// Returns the shortest arc (angle in [0, π]).
    /// </remarks>
    /// <param name="quaternion">The unit quaternion to decompose.</param>
    /// <param name="axis">Output: The unit rotation axis.</param>
    /// <param name="angle">Output: The rotation angle (radians).</param>
    public static void ToAxisAngle(JQuaternion quaternion, out JVector axis, out Real angle)
    {
        Real s = MathR.Sqrt(MathR.Max((Real)0.0, (Real)1.0 - quaternion.W * quaternion.W));

        const Real epsilonSingularity = (Real)1e-6;

        if (s < epsilonSingularity)
        {
            angle = (Real)0.0;
            axis = JVector.UnitX;
            return;
        }

        Real invS = (Real)1.0 / s;
        axis = new JVector(quaternion.X * invS, quaternion.Y * invS, quaternion.Z * invS);
        angle = (Real)2.0 * MathR.Acos(quaternion.W);

        // Enforce the shortest-arc representation (angle between 0 and PI)
        if (angle > MathR.PI)
        {
            angle = (Real)2.0 * MathR.PI - angle;
            axis = -axis;
        }
    }

    /// <summary>
    /// Multiplies two quaternions.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">Output: The product of the two quaternions.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        Real r1 = quaternion1.W;
        Real i1 = quaternion1.X;
        Real j1 = quaternion1.Y;
        Real k1 = quaternion1.Z;

        Real r2 = quaternion2.W;
        Real i2 = quaternion2.X;
        Real j2 = quaternion2.Y;
        Real k2 = quaternion2.Z;

        result.W = r1 * r2 - (i1 * i2 + j1 * j2 + k1 * k2);
        result.X = r1 * i2 + r2 * i1 + j1 * k2 - k1 * j2;
        result.Y = r1 * j2 + r2 * j1 + k1 * i2 - i1 * k2;
        result.Z = r1 * k2 + r2 * k1 + i1 * j2 - j1 * i2;
    }

    /// <summary>
    /// Calculates <c>conjugate(Q1) * Q2</c>.
    /// </summary>
    /// <param name="quaternion1">The quaternion to conjugate and then multiply.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <param name="result">Output: The resulting quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConjugateMultiply(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        Real r1 = quaternion1.W;
        Real i1 = -quaternion1.X;
        Real j1 = -quaternion1.Y;
        Real k1 = -quaternion1.Z;

        Real r2 = quaternion2.W;
        Real i2 = quaternion2.X;
        Real j2 = quaternion2.Y;
        Real k2 = quaternion2.Z;

        result.W = r1 * r2 - (i1 * i2 + j1 * j2 + k1 * k2);
        result.X = r1 * i2 + r2 * i1 + j1 * k2 - k1 * j2;
        result.Y = r1 * j2 + r2 * j1 + k1 * i2 - i1 * k2;
        result.Z = r1 * k2 + r2 * k1 + i1 * j2 - j1 * i2;
    }

    /// <inheritdoc cref="ConjugateMultiply(in JQuaternion, in JQuaternion, out JQuaternion)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion ConjugateMultiply(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        ConjugateMultiply(quaternion1, quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Calculates <c>Q1 * conjugate(Q2)</c>.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The quaternion to conjugate.</param>
    /// <param name="result">Output: The resulting quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MultiplyConjugate(in JQuaternion quaternion1, in JQuaternion quaternion2, out JQuaternion result)
    {
        Real r1 = quaternion1.W;
        Real i1 = quaternion1.X;
        Real j1 = quaternion1.Y;
        Real k1 = quaternion1.Z;

        Real r2 = quaternion2.W;
        Real i2 = -quaternion2.X;
        Real j2 = -quaternion2.Y;
        Real k2 = -quaternion2.Z;

        result.W = r1 * r2 - (i1 * i2 + j1 * j2 + k1 * k2);
        result.X = r1 * i2 + r2 * i1 + j1 * k2 - k1 * j2;
        result.Y = r1 * j2 + r2 * j1 + k1 * i2 - i1 * k2;
        result.Z = r1 * k2 + r2 * k1 + i1 * j2 - j1 * i2;
    }

    /// <inheritdoc cref="MultiplyConjugate(in JQuaternion, in JQuaternion, out JQuaternion)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion MultiplyConjugate(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        MultiplyConjugate(quaternion1, quaternion2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Multiplies a quaternion by a scalar factor.
    /// </summary>
    /// <param name="quaternion1">The quaternion.</param>
    /// <param name="scaleFactor">The scalar factor.</param>
    /// <returns>The scaled quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Multiply(in JQuaternion quaternion1, Real scaleFactor)
    {
        Multiply(in quaternion1, scaleFactor, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Multiplies a quaternion by a scalar factor.
    /// </summary>
    /// <param name="quaternion1">The quaternion.</param>
    /// <param name="scaleFactor">The scalar factor.</param>
    /// <param name="result">Output: The scaled quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Multiply(in JQuaternion quaternion1, Real scaleFactor, out JQuaternion result)
    {
        result.W = quaternion1.W * scaleFactor;
        result.X = quaternion1.X * scaleFactor;
        result.Y = quaternion1.Y * scaleFactor;
        result.Z = quaternion1.Z * scaleFactor;
    }

    /// <summary>
    /// Calculates the Euclidean length of the quaternion.
    /// </summary>
    /// <returns>The length (magnitude).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real Length()
    {
        return (Real)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
    }

    /// <summary>
    /// Calculates the squared Euclidean length of the quaternion.
    /// </summary>
    /// <returns>The squared length.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Real LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    /// <summary>
    /// Normalizes the quaternion to unit length.
    /// </summary>
    [Obsolete($"In-place Normalize() is deprecated; " +
              $"use the static {nameof(JQuaternion.Normalize)} method or {nameof(JQuaternion.NormalizeInPlace)}.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        Real num2 = X * X + Y * Y + Z * Z + W * W;
        Real num = (Real)1.0 / MathR.Sqrt(num2);
        X *= num;
        Y *= num;
        Z *= num;
        W *= num;
    }

    /// <summary>
    /// Normalizes the provided quaternion structure in place.
    /// </summary>
    /// <param name="quaternion">The quaternion to normalize.</param>
    public static void NormalizeInPlace(ref JQuaternion quaternion)
    {
        Real num2 = quaternion.LengthSquared();
        Real num = (Real)1.0 / MathR.Sqrt(num2);
        quaternion.X *= num;
        quaternion.Y *= num;
        quaternion.Z *= num;
        quaternion.W *= num;
    }

    /// <summary>
    /// Returns a normalized version of the quaternion.
    /// </summary>
    /// <param name="value">The source quaternion.</param>
    /// <param name="result">Output: The normalized unit quaternion.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Normalize(in JQuaternion value, out JQuaternion result)
    {
        Real num2 = value.X * value.X + value.Y * value.Y + value.Z * value.Z + value.W * value.W;
        Real num = (Real)1.0 / MathR.Sqrt(num2);
        result.X = value.X * num;
        result.Y = value.Y * num;
        result.Z = value.Z * num;
        result.W = value.W * num;
    }

    /// <inheritdoc cref="Normalize(in JQuaternion, out JQuaternion)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Normalize(in JQuaternion value)
    {
        Normalize(value, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Creates a quaternion from a rotation matrix.
    /// </summary>
    /// <param name="matrix">The rotation matrix.</param>
    /// <returns>The quaternion representing the rotation.</returns>
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
    /// <param name="result">Output: The quaternion representing the rotation.</param>
    public static void CreateFromMatrix(in JMatrix matrix, out JQuaternion result)
    {
        Real t;

        if (matrix.M33 < 0)
        {
            if (matrix.M11 > matrix.M22)
            {
                t = (Real)1.0 + matrix.M11 - matrix.M22 - matrix.M33;
                result = new JQuaternion(t, matrix.M21 + matrix.M12, matrix.M31 + matrix.M13, matrix.M32 - matrix.M23);
            }
            else
            {
                t = (Real)1.0 - matrix.M11 + matrix.M22 - matrix.M33;
                result = new JQuaternion(matrix.M21 + matrix.M12, t, matrix.M32 + matrix.M23, matrix.M13 - matrix.M31);
            }
        }
        else
        {
            if (matrix.M11 < -matrix.M22)
            {
                t = (Real)1.0 - matrix.M11 - matrix.M22 + matrix.M33;
                result = new JQuaternion(matrix.M13 + matrix.M31, matrix.M32 + matrix.M23, t, matrix.M21 - matrix.M12);
            }
            else
            {
                t = (Real)1.0 + matrix.M11 + matrix.M22 + matrix.M33;
                result = new JQuaternion(matrix.M32 - matrix.M23, matrix.M13 - matrix.M31, matrix.M21 - matrix.M12, t);
            }
        }

        t = (Real)(0.5d / Math.Sqrt(t));
        result.X *= t;
        result.Y *= t;
        result.Z *= t;
        result.W *= t;
    }

    /// <summary>
    /// Calculates the dot product of two quaternions.
    /// </summary>
    /// <param name="quaternion1">The first quaternion.</param>
    /// <param name="quaternion2">The second quaternion.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Real Dot(in JQuaternion quaternion1, in JQuaternion quaternion2)
    {
        return quaternion1.X * quaternion2.X +
               quaternion1.Y * quaternion2.Y +
               quaternion1.Z * quaternion2.Z +
               quaternion1.W * quaternion2.W;
    }

    /// <summary>
    /// Returns the inverse of a quaternion.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Conjugate(in JQuaternion)"/>, this handles non-unit quaternions correctly
    /// by dividing by the squared length.
    /// </remarks>
    public static JQuaternion Inverse(in JQuaternion value)
    {
        Real lengthSq = value.LengthSquared();
        if (lengthSq < (Real)1e-12) return Identity;

        Real invLengthSq = (Real)1.0 / lengthSq;
        return new JQuaternion(
            -value.X * invLengthSq,
            -value.Y * invLengthSq,
            -value.Z * invLengthSq,
            value.W * invLengthSq);
    }

    /// <summary>
    /// Linearly interpolates between two quaternions and normalizes the result.
    /// </summary>
    /// <param name="quaternion1">Source quaternion.</param>
    /// <param name="quaternion2">Target quaternion.</param>
    /// <param name="amount">Weight of the interpolation.</param>
    /// <returns>The interpolated unit quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion Lerp(in JQuaternion quaternion1, in JQuaternion quaternion2, Real amount)
    {
        Real inverse = (Real)1.0 - amount;
        JQuaternion result;

        if (Dot(quaternion1, quaternion2) >= (Real)0.0)
        {
            result.X = inverse * quaternion1.X + amount * quaternion2.X;
            result.Y = inverse * quaternion1.Y + amount * quaternion2.Y;
            result.Z = inverse * quaternion1.Z + amount * quaternion2.Z;
            result.W = inverse * quaternion1.W + amount * quaternion2.W;
        }
        else
        {
            // Flip sign of the second quaternion to ensure shortest path
            result.X = inverse * quaternion1.X - amount * quaternion2.X;
            result.Y = inverse * quaternion1.Y - amount * quaternion2.Y;
            result.Z = inverse * quaternion1.Z - amount * quaternion2.Z;
            result.W = inverse * quaternion1.W - amount * quaternion2.W;
        }

        Normalize(result, out result);
        return result;
    }

    /// <summary>
    /// Interpolates between two quaternions using Spherical Linear Interpolation (SLERP).
    /// </summary>
    /// <param name="quaternion1">Source quaternion.</param>
    /// <param name="quaternion2">Target quaternion.</param>
    /// <param name="amount">Weight of the interpolation.</param>
    /// <returns>The interpolated quaternion.</returns>
    public static JQuaternion Slerp(in JQuaternion quaternion1, in JQuaternion quaternion2, Real amount)
    {
        Real dot = Dot(quaternion1, quaternion2);
        JQuaternion target = quaternion2;

        // If the dot product is negative, the quaternions are pointing in opposite directions.
        // Reversing one ensures we take the shortest path around the sphere.
        if (dot < (Real)0.0)
        {
            dot = -dot;
            target = -target;
        }

        const Real epsilon = (Real)1e-6;
        Real scale0, scale1;

        // If the quaternions are very close, linear interpolation is faster and safe.
        if (dot > (Real)1.0 - epsilon)
        {
            scale0 = (Real)1.0 - amount;
            scale1 = amount;
        }
        else
        {
            Real omega = MathR.Acos(dot);
            Real invSinOmega = (Real)1.0 / MathR.Sin(omega);
            scale0 = MathR.Sin(((Real)1.0 - amount) * omega) * invSinOmega;
            scale1 = MathR.Sin(amount * omega) * invSinOmega;
        }

        return new JQuaternion(
            scale0 * quaternion1.X + scale1 * target.X,
            scale0 * quaternion1.Y + scale1 * target.Y,
            scale0 * quaternion1.Z + scale1 * target.Z,
            scale0 * quaternion1.W + scale1 * target.W);
    }

    /// <summary>
    /// Flips the sign of each component of the quaternion.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator -(in JQuaternion value)
    {
        return new JQuaternion(-value.X, -value.Y, -value.Z, -value.W);
    }

    /// <summary>
    /// Multiplies two quaternions (Hamilton Product).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator *(in JQuaternion value1, in JQuaternion value2)
    {
        Multiply(value1, value2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Scales a quaternion by a factor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator *(Real value1, in JQuaternion value2)
    {
        Multiply(value2, value1, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Scales a quaternion by a factor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator *(in JQuaternion value1, Real value2)
    {
        Multiply(value1, value2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Adds two quaternions component-wise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator +(in JQuaternion value1, in JQuaternion value2)
    {
        Add(value1, value2, out JQuaternion result);
        return result;
    }

    /// <summary>
    /// Subtracts the second quaternion from the first component-wise.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion operator -(in JQuaternion value1, in JQuaternion value2)
    {
        Subtract(value1, value2, out JQuaternion result);
        return result;
    }

    public readonly bool Equals(JQuaternion other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is JQuaternion other && Equals(other);
    }

    public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    public static bool operator ==(JQuaternion left, JQuaternion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JQuaternion left, JQuaternion right)
    {
        return !(left == right);
    }
}