using System;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public static class ColorGenerator
{
    private const int NumColors = 127;

    // Surprisingly buffering the colors is about 20 times
    // faster than generating them on the fly.
    private static readonly Vector3[] buffer = new Vector3[NumColors];

    static ColorGenerator()
    {
        for (int i = 0; i < NumColors; i++)
        {
            buffer[i] = ColorFromHSV((float)i / NumColors, 1, 0.6f);
        }
    }

    private static Vector3 ColorFromHSV(float h, float s, float v)
    {
        float Gen(float n)
        {
            float k = (n + h * 6) % 6;
            return v - v * s * Math.Max(0.0f, Math.Min(Math.Min(k, 4.0f - k), 1.0f));
        }

        return new Vector3(Gen(5), Gen(3), Gen(1));
    }

    public static Vector3 GetColor(int seed)
    {
        return buffer[(uint)seed % NumColors];
    }
}