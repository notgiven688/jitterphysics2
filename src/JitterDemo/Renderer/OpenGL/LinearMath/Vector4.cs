using System;
using System.Runtime.InteropServices;

namespace JitterDemo.Renderer.OpenGL;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct Vector4
{
    [FieldOffset(0)] public float X;
    [FieldOffset(4)] public float Y;
    [FieldOffset(8)] public float Z;
    [FieldOffset(12)] public float W;

    public static Vector4 Zero { get; } = new(0, 0, 0, 0);

    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public Vector4(Vector3 vec, float w)
    {
        X = vec.X;
        Y = vec.Y;
        Z = vec.Z;
        W = w;
    }

    public static Vector4 operator *(Matrix4 matrix, Vector4 vector)
    {
        return Multiply(matrix, vector);
    }

    public static Vector4 operator *(Vector4 left, float right)
    {
        return Multiply(left, right);
    }

    public static Vector4 operator *(float left, Vector4 right)
    {
        return Multiply(right, left);
    }

    public static Vector4 operator +(Vector4 left, Vector4 right)
    {
        return Add(left, right);
    }

    public static Vector4 operator -(Vector4 left, Vector4 right)
    {
        return Subtract(left, right);
    }

    public static Vector4 operator -(Vector4 left)
    {
        return Multiply(left, -1.0f);
    }

    public static Vector4 Add(in Vector4 left, in Vector4 right)
    {
        Vector4 result;
        result.X = left.X + right.X;
        result.Y = left.Y + right.Y;
        result.Z = left.Z + right.Z;
        result.W = left.W + right.W;
        return result;
    }

    public static Vector4 Subtract(in Vector4 left, in Vector4 right)
    {
        Vector4 result;
        result.X = left.X - right.X;
        result.Y = left.Y - right.Y;
        result.Z = left.Z - right.Z;
        result.W = left.W - right.W;
        return result;
    }

    public Vector3 XYZ => new(X, Y, Z);

    public static Vector4 Normalize(in Vector4 vector)
    {
        float ls = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z + vector.W * vector.W;
        float invNorm = 1.0f / (float)Math.Sqrt(ls);

        return new Vector4(
            vector.X * invNorm,
            vector.Y * invNorm,
            vector.Z * invNorm,
            vector.W * invNorm);
    }

    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
    }

    public static float Dot(in Vector4 left, in Vector4 right)
    {
        return left.X * right.X +
               left.Y * right.Y +
               left.Z * right.Z +
               left.W * right.W;
    }

    public static Vector4 Multiply(in Matrix4 left, in Vector4 right)
    {
        Vector4 result = new()
        {
            X = left.M11 * right.X + left.M12 * right.Y + left.M13 * right.Z + left.M14 * right.W,
            Y = left.M21 * right.X + left.M22 * right.Y + left.M23 * right.Z + left.M24 * right.W,
            Z = left.M31 * right.X + left.M32 * right.Y + left.M33 * right.Z + left.M34 * right.W,
            W = left.M41 * right.X + left.M42 * right.Y + left.M43 * right.Z + left.M44 * right.W
        };
        return result;
    }

    public static Vector4 Multiply(in Vector4 left, float right)
    {
        Vector4 result;
        result.X = left.X * right;
        result.Y = left.Y * right;
        result.Z = left.Z * right;
        result.W = left.W * right;
        return result;
    }

    public override string ToString()
    {
        return $"X={X} Y={Y} Z={Z} W={W}";
    }
}