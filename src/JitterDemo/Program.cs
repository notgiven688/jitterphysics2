using System;
using Jitter2.Dynamics;
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