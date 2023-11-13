using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Raylib_cs
{
    [SuppressUnmanagedCodeSecurity]
    public static unsafe partial class Raylib
    {
        public const string nativeLibName = "raylib";

        /// <summary>Initialize window and OpenGL context</summary>
        [DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitWindow(int width, int height, sbyte* title);

        /// <summary>Check if KEY_ESCAPE pressed or Close icon pressed</summary>
        [DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern CBool WindowShouldClose();

        /// <summary>Setup canvas (framebuffer) to start drawing</summary>
        [DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void BeginDrawing();

        /// <summary>Set background color (framebuffer clear color)</summary>
        [DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearBackground(Color color);

        /// <summary>Draw text (using default font)</summary>
        [DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DrawText(sbyte* text, int posX, int posY, int fontSize, Color color);

        /// <summary>End canvas drawing and swap buffers (double buffering)</summary>
        [DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void EndDrawing();

        /// <summary>Close window and unload OpenGL context</summary>
        [DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseWindow();

    }
}