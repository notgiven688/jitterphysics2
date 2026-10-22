using System;
using System.IO;
using System.Runtime.InteropServices;

namespace JitterDemo.Renderer;

/// <summary>
/// Resolves bundled native libraries from the application's runtimes directory.
/// </summary>
public static class ImportResolver
{
    // Keep GLFW loaded for the lifetime of its cached P/Invoke entry points.
    private static readonly Lazy<IntPtr> glfwHandle = new(LoadGlfw);

    public static IntPtr Resolve(string libraryName)
    {
        return libraryName == OpenGL.Native.GLFW.LIBGLFW ? glfwHandle.Value : IntPtr.Zero;
    }

    private static IntPtr LoadGlfw()
    {
        (string platform, string fileName) = OperatingSystem.IsWindows() ? ("win", "glfw3.dll") :
            OperatingSystem.IsLinux() ? ("linux", "libglfw.so.3") :
            OperatingSystem.IsMacOS() ? ("osx", "libglfw.3.dylib") :
            throw new PlatformNotSupportedException("No GLFW library is bundled for this operating system.");

        string architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 when platform == "win" => "x86",
            _ => throw new PlatformNotSupportedException("No GLFW library is bundled for this process architecture.")
        };

        string path = Path.Combine(AppContext.BaseDirectory, "runtimes", $"{platform}-{architecture}", "native", fileName);
        return NativeLibrary.Load(path);
    }
}
