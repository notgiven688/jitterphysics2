using System;
using System.Runtime.InteropServices;

namespace JitterDemo.Renderer.OpenGL;

[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct Vector3
{
    [FieldOffset(0)] public float X;
    [FieldOffset(4)] public float Y;
    [FieldOffset(8)] public float Z;

    public static readonly Vector3 Zero = new(0, 0, 0);
    public static readonly Vector3 UnitX = new(1, 0, 0);
    public static readonly Vector3 UnitY = new(0, 1, 0);
    public static readonly Vector3 UnitZ = new(0, 0, 1);

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3(double x, double y, double z)
    {
        X = (float)x;
        Y = (float)y;
        Z = (float)z;
    }

    public static Vector3 operator *(Vector3 left, float right)
    {
        return Multiply(left, right);
    }

    public static Vector3 operator *(float left, Vector3 right)
    {
        return Multiply(right, left);
    }

    public static Vector3 operator +(Vector3 left, Vector3 right)
    {
        return Add(in left, right);
    }

    public static Vector3 operator -(Vector3 left, Vector3 right)
    {
        return Subtract(left, right);
    }

    public static Vector3 operator %(Vector3 left, Vector3 right)
    {
        return Cross(left, right);
    }

    public static Vector3 operator -(Vector3 left)
    {
        return Multiply(left, -1.0f);
    }

    public static Vector3 operator +(Vector3 left)
    {
        return Multiply(left, +1.0f);
    }

    public static Vector3 Add(in Vector3 left, in Vector3 right)
    {
        Vector3 result;
        result.X = left.X + right.X;
        result.Y = left.Y + right.Y;
        result.Z = left.Z + right.Z;
        return result;
    }

    public static Vector3 Subtract(in Vector3 left, in Vector3 right)
    {
        Vector3 result;
        result.X = left.X - right.X;
        result.Y = left.Y - right.Y;
        result.Z = left.Z - right.Z;
        return result;
    }

    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z);
    }

    public static float Dot(in Vector3 left, in Vector3 right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    public static Vector3 Normalize(in Vector3 vector)
    {
        float ls = vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z;
        float invNorm = 1.0f / MathF.Sqrt(ls);

        return new Vector3(
            vector.X * invNorm,
            vector.Y * invNorm,
            vector.Z * invNorm);
    }

    public static Vector3 Cross(in Vector3 left, in Vector3 right)
    {
        return new Vector3(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);
    }

    public static Vector3 Multiply(in Vector3 left, float right)
    {
        Vector3 result;
        result.X = left.X * right;
        result.Y = left.Y * right;
        result.Z = left.Z * right;
        return result;
    }

    public static Vector3 Transform(in Vector3 position, in Matrix4 matrix)
    {
        // equivalent to matrix * (position, 1) = (result, *)
        return new Vector3(
            position.X * matrix.M11 + position.Y * matrix.M12 + position.Z * matrix.M13 + matrix.M14,
            position.X * matrix.M21 + position.Y * matrix.M22 + position.Z * matrix.M23 + matrix.M24,
            position.X * matrix.M31 + position.Y * matrix.M32 + position.Z * matrix.M33 + matrix.M34);
    }

    public override string ToString()
    {
        return $"X={X} Y={Y} Z={Z}";
    }
}