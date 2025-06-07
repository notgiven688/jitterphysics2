using System;
using System.Runtime.InteropServices;

namespace JitterDemo.Renderer;

/// <summary>
/// Resolve native libraries from runtime directory.
/// </summary>
public static class ImportResolver
{
    private static string archStr = string.Empty;

    public static void Load()
    {
        if (archStr.Length != 0) return;

        archStr = "_";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) archStr += "w";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) archStr += "l";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) archStr += "o";

        switch (RuntimeInformation.OSArchitecture)
        {
            case Architecture.Arm64:
                archStr += "a";
                break;
            case Architecture.X64:
                archStr += "x";
                break;
            case Architecture.X86:
                archStr += "i";
                break;
        }

        NativeLibrary.SetDllImportResolver(typeof(ImportResolver).Assembly, (libraryName, _, _) =>
        {
            switch (libraryName)
            {
                case DearImGui.ImGuiNative.LIBCIMGUI:
                    return archStr switch
                    {
                        "_wa" => NativeLibrary.Load("runtimes/win-arm64/native/cimgui.dll"),
                        "_wx" => NativeLibrary.Load("runtimes/win-x64/native/cimgui.dll"),
                        "_wi" => NativeLibrary.Load("runtimes/win-x86/native/cimgui.dll"),
                        "_la" => NativeLibrary.Load("runtimes/linux-arm64/native/cimgui.so"),
                        "_lx" => NativeLibrary.Load("runtimes/linux-x64/native/cimgui.so"),
                        "_oa" => NativeLibrary.Load("runtimes/osx-arm64/native/cimgui.dylib"),
                        "_ox" => NativeLibrary.Load("runtimes/osx-x64/native/cimgui.dylib"),
                        _ => throw new NotSupportedException("Operating system/architecture not supported.")
                    };
                case OpenGL.Native.GLFW.LIBGLFW:
                    return archStr switch
                    {
                        "_wa" => NativeLibrary.Load("runtimes/win-arm64/native/glfw3.dll"),
                        "_wx" => NativeLibrary.Load("runtimes/win-x64/native/glfw3.dll"),
                        "_wi" => NativeLibrary.Load("runtimes/win-x86/native/glfw3.dll"),
                        "_la" => NativeLibrary.Load("runtimes/linux-arm64/native/libglfw.so.3"),
                        "_lx" => NativeLibrary.Load("runtimes/linux-x64/native/libglfw.so.3"),
                        "_oa" => NativeLibrary.Load("runtimes/osx-arm64/native/libglfw.3.dylib"),
                        "_ox" => NativeLibrary.Load("runtimes/osx-x64/native/libglfw.3.dylib"),
                        _ => throw new NotSupportedException("Operating system/architecture not supported.")
                    };
                default:
                    throw new InvalidOperationException("Unknown library.");
            }
        });
    }
}