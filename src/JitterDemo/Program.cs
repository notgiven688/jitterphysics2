using System;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public static class Program
{
    private static void PrintException(Exception ex, string info)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{ex.GetType()}");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(info);
        Console.ResetColor();
        Console.WriteLine(ex.Message);
    }

    public static float GenerateRandom(ulong seed)
    {
        const uint A = 21_687_443;
        const uint B = 35_253_893;

        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;

        uint randomBits = (uint)seed * A + B;
        return MathF.Abs((float)randomBits / uint.MaxValue);
    }

    public static void Main()
    {
        CreationSettings cs = new(1200, 800, "JitterDemo");

        try
        {
            new Playground().Open(cs);
        }
        catch (DllNotFoundException ex)
        {
            PrintException(ex, "Make sure GLFW (https://www.glfw.org/) is installed and available on your system.");
        }
        catch (OpenGLVersionNotSupportedException ex)
        {
            PrintException(ex, "The OpenGL version available on this system is too old.");
        }
    }
}