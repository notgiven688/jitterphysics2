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

    public static void Main()
    {
        Jitter2.Logger.Listener = (level, message) => Console.WriteLine($"{level}: {message}");

        CreationSettings cs = new(1200, 800, "JitterDemo");

        try
        {
            new Playground().Open(cs);
        }
        catch (DllNotFoundException ex)
        {
            PrintException(ex, "Unable to load library.");
        }
        catch (OpenGLVersionNotSupportedException ex)
        {
            PrintException(ex, "The OpenGL version available on this system is too old.");
        }
    }
}