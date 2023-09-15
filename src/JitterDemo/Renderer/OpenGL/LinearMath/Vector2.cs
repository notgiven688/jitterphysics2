using System;
using System.Runtime.InteropServices;

namespace JitterDemo.Renderer.OpenGL;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct Vector2
{
    [FieldOffset(0)] public float X;
    [FieldOffset(4)] public float Y;

    public static Vector2 Zero { get; } = new(0, 0);

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 operator *(Vector2 left, float right)
    {
        return Multiply(left, right);
    }

    public static Vector2 operator *(float left, Vector2 right)
    {
        return Multiply(right, left);
    }

    public static Vector2 operator +(Vector2 left, Vector2 right)
    {
        return Add(left, right);
    }

    public static Vector2 operator -(Vector2 left, Vector2 right)
    {
        return Subtract(left, right);
    }

    public static Vector2 operator -(Vector2 left)
    {
        return Multiply(left, -1.0f);
    }

    public static Vector2 Add(in Vector2 left, in Vector2 right)
    {
        Vector2 result;
        result.X = left.X + right.X;
        result.Y = left.Y + right.Y;
        return result;
    }

    public static Vector2 Subtract(in Vector2 left, in Vector2 right)
    {
        Vector2 result;
        result.X = left.X - right.X;
        result.Y = left.Y - right.Y;
        return result;
    }

    public static Vector2 Normalize(in Vector2 vector)
    {
        float ls = vector.X * vector.X + vector.Y * vector.Y;
        float invNorm = 1.0f / (float)Math.Sqrt(ls);

        return new Vector2(
            vector.X * invNorm,
            vector.Y * invNorm);
    }

    public float LengthSquared()
    {
        return X * X + Y * Y;
    }

    public float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y);
    }

    public static float Dot(in Vector2 left, in Vector2 right)
    {
        return left.X * right.X +
               left.Y * right.Y;
    }

    public static Vector2 Multiply(in Vector2 left, float right)
    {
        Vector2 result;
        result.X = left.X * right;
        result.Y = left.Y * right;
        return result;
    }

    public override string ToString()
    {
        return $"X={X} Y={Y}";
    }
}