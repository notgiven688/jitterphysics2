/* Copyright <2022> <Thorben Linneweber>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace JitterDemo.Renderer.OpenGL.Native;

[SuppressMessage("Usage", "CA1401:P/Invokes should not be visible",
    Justification = "Part of the 'Native' namespace.")]
public static class GLFW
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GLFWImage
    {
        public int Width;
        public int Height;
        public IntPtr Pixels;
    }

#if Windows
    public const string LIBGLFW = "glfw3.dll";
#elif OSX
    public const string LIBGLFW = "libglfw.3.dylib";
#else
    public const string LIBGLFW = "libglfw.so.3";
#endif

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void GLProcDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ErrorDelegate(int errorcode, string description);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WindowsPosDelegate(IntPtr window, int posx, int posy);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WindowSizeDelegate(IntPtr window, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WindowCloseDelegate(IntPtr window);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WindowRefreshDelegate(IntPtr window);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WindowFocusDelegate(IntPtr window, int focused);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void WindowIconifyDelegate(IntPtr window, int iconified);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FrameBufferDelegate(IntPtr window, int width, int height);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MouseButtonDelegate(IntPtr window, int button, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CursorPosDelegate(IntPtr window, double mousex, double mousey);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CursorEnterDelegate(IntPtr window, int entered);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ScrollDelegate(IntPtr window, double xoffset, double yoffset);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void KeyDelegate(IntPtr window, int key, int scancode, int action, int mods);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CharDelegate(IntPtr window, uint codepoint);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CharModDelegate(IntPtr window, int codepoint, int mods);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DropDelegate(IntPtr window, int count, string[] paths);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MonitorDelegate(IntPtr window, int monitorevent);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void JoystickDelegate(int jid, int ev);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetJoystickName", ExactSpelling = true)]
    public static extern IntPtr GetGamepadName(int jid);

    [DllImport(LIBGLFW, EntryPoint = "glfwJoystickIsGamepad", ExactSpelling = true)]
    public static extern int JoystickIsGamepad(int jid);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetWindowUserPointer", ExactSpelling = true)]
    public static extern IntPtr GetWindowUserPointer(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetVersion", ExactSpelling = true)]
    public static extern void GetVersion([Out] out int major, [Out] out int minor, [Out] out int rev);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetMonitorPos", ExactSpelling = true)]
    public static extern void GetMonitorPos(IntPtr monitor, ref int xpos, ref int ypos);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetMonitorPhysicalSize", ExactSpelling = true)]
    public static extern void GetMonitorPhysicalSize(IntPtr monitor, ref int widthMM, ref int heightMM);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetWindowPos", ExactSpelling = true)]
    public static extern void GetWindowPos(IntPtr window, ref int xpos, ref int ypos);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetWindowSize", ExactSpelling = true)]
    public static extern void GetWindowSize(IntPtr window, out int width, out int height);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetFramebufferSize", ExactSpelling = true)]
    public static extern void GetFramebufferSize(IntPtr window, ref int width, ref int height);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetWindowFrameSize", ExactSpelling = true)]
    public static extern void GetWindowFrameSize(IntPtr window, ref int left, ref int top, ref int right,
        ref int bottom);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetCursorPos", ExactSpelling = true)]
    public static extern void GetCursorPos(IntPtr window, ref double xpos, ref double ypos);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetGamma", ExactSpelling = true)]
    public static extern void SetGamma(IntPtr monitor, float gamma);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetGammaRamp", ExactSpelling = true)]
    public static extern void SetGammaRamp(IntPtr monitor, long ramp);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowShouldClose", ExactSpelling = true)]
    public static extern void SetWindowShouldClose(IntPtr window, int value);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowPos", ExactSpelling = true)]
    public static extern void SetWindowPos(IntPtr window, int xpos, int ypos);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowSize", ExactSpelling = true)]
    public static extern void SetWindowSize(IntPtr window, int width, int height);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowUserPointer", ExactSpelling = true)]
    public static extern void SetWindowUserPointer(IntPtr window, IntPtr pointer);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetCursorPos", ExactSpelling = true)]
    public static extern void SetCursorPos(IntPtr window, double xpos, double ypos);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetCursor", ExactSpelling = true)]
    public static extern void SetCursor(IntPtr window, long cursor);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetClipboardString", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern void SetClipboardString(IntPtr window, string @string);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetTime", ExactSpelling = true)]
    public static extern void SetTime(double time);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetInputMode", ExactSpelling = true)]
    public static extern void SetInputMode(IntPtr window, int mode, int value);

    [DllImport(LIBGLFW, EntryPoint = "glfwTerminate", ExactSpelling = true)]
    public static extern void Terminate();

    [DllImport(LIBGLFW, EntryPoint = "glfwDefaultWindowHints", ExactSpelling = true)]
    public static extern void DefaultWindowHints();

    [DllImport(LIBGLFW, EntryPoint = "glfwWindowHint", ExactSpelling = true)]
    public static extern void WindowHint(int target, int hint);

    [DllImport(LIBGLFW, EntryPoint = "glfwDestroyWindow", ExactSpelling = true)]
    public static extern void DestroyWindow(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwIconifyWindow", ExactSpelling = true)]
    public static extern void IconifyWindow(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwRestoreWindow", ExactSpelling = true)]
    public static extern void RestoreWindow(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwShowWindow", ExactSpelling = true)]
    public static extern void ShowWindow(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwHideWindow", ExactSpelling = true)]
    public static extern void HideWindow(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwPollEvents", ExactSpelling = true)]
    public static extern void PollEvents();

    [DllImport(LIBGLFW, EntryPoint = "glfwWaitEvents", ExactSpelling = true)]
    public static extern void WaitEvents();

    [DllImport(LIBGLFW, EntryPoint = "glfwPostEmptyEvent", ExactSpelling = true)]
    public static extern void PostEmptyEvent();

    [DllImport(LIBGLFW, EntryPoint = "glfwDestroyCursor", ExactSpelling = true)]
    public static extern void DestroyCursor(long cursor);

    [DllImport(LIBGLFW, EntryPoint = "glfwMakeContextCurrent", ExactSpelling = true)]
    public static extern void MakeContextCurrent(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwSwapBuffers", ExactSpelling = true)]
    public static extern void SwapBuffers(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwSwapInterval", ExactSpelling = true)]
    public static extern void SwapInterval(int interval);

    [DllImport(LIBGLFW, EntryPoint = "glfwInit", ExactSpelling = true)]
    public static extern int Init();

    [DllImport(LIBGLFW, EntryPoint = "glfwWindowShouldClose", ExactSpelling = true)]
    public static extern int WindowShouldClose(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetWindowAttrib", ExactSpelling = true)]
    public static extern int GetWindowAttrib(IntPtr window, int attrib);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetInputMode", ExactSpelling = true)]
    public static extern int GetInputMode(IntPtr window, int mode);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetKey", ExactSpelling = true)]
    public static extern int GetKey(IntPtr window, int key);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetMouseButton", ExactSpelling = true)]
    public static extern int GetMouseButton(IntPtr window, int button);

    [DllImport(LIBGLFW, EntryPoint = "glfwJoystickPresent", ExactSpelling = true)]
    public static extern int JoystickPresent(int joy);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetJoystickButtons", ExactSpelling = true)]
    public static extern IntPtr GetJoystickButtons(int joy, out int count);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetMonitorName", ExactSpelling = true)]
    public static extern IntPtr GetMonitorName(IntPtr monitor);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetJoystickName", ExactSpelling = true)]
    public static extern IntPtr GetJoystickName(int joy);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetClipboardString", ExactSpelling = true)]
    public static extern IntPtr GetClipboardString(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetVideoModes", ExactSpelling = true)]
    public static extern long[] GetVideoModes(IntPtr monitor, ref int count);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetMonitors", ExactSpelling = true)]
    public static extern long[] GetMonitors(int count);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetVideoMode", ExactSpelling = true)]
    public static extern long GetVideoMode(IntPtr monitor);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetGammaRamp", ExactSpelling = true)]
    public static extern long GetGammaRamp(IntPtr monitor);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetPrimaryMonitor", ExactSpelling = true)]
    public static extern IntPtr GetPrimaryMonitor();

    [DllImport(LIBGLFW, EntryPoint = "glfwGetWindowMonitor", ExactSpelling = true)]
    public static extern IntPtr GetWindowMonitor(IntPtr window);

    [DllImport(LIBGLFW, EntryPoint = "glfwCreateCursor", ExactSpelling = true)]
    public static extern long CreateCursor(long image, int xhot, int yhot);

    [DllImport(LIBGLFW, EntryPoint = "glfwCreateStandardCursor", ExactSpelling = true)]
    public static extern long CreateStandardCursor(int shape);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetCurrentContext", ExactSpelling = true)]
    public static extern long GetCurrentContext();

    [DllImport(LIBGLFW, EntryPoint = "glfwGetJoystickAxes", ExactSpelling = true)]
    public static extern IntPtr GetJoystickAxes(int joy, out int count);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetJoystickHats", ExactSpelling = true)]
    public static extern IntPtr GetJoystickHats(int joy, out int count);

    [DllImport(LIBGLFW, EntryPoint = "glfwGetTime", ExactSpelling = true)]
    public static extern double GetTime();

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowIcon", ExactSpelling = true)]
    public static extern void SetWindowIcon(IntPtr window, int count, GLFWImage[] images);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetErrorCallback", ExactSpelling = true)]
    public static extern ErrorDelegate SetErrorCallback(ErrorDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetMonitorCallback", ExactSpelling = true)]
    public static extern MonitorDelegate SetMonitorCallback(MonitorDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowPosCallback", ExactSpelling = true)]
    public static extern WindowsPosDelegate SetWindowPosCallback(IntPtr window, WindowsPosDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowSizeCallback", ExactSpelling = true)]
    public static extern WindowSizeDelegate SetWindowSizeCallback(IntPtr window, WindowSizeDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowCloseCallback", ExactSpelling = true)]
    public static extern WindowCloseDelegate SetWindowCloseCallback(IntPtr window, WindowCloseDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowRefreshCallback", ExactSpelling = true)]
    public static extern WindowRefreshDelegate SetWindowRefreshCallback(IntPtr window, WindowRefreshDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowFocusCallback", ExactSpelling = true)]
    public static extern WindowFocusDelegate SetWindowFocusCallback(IntPtr window, WindowFocusDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowIconifyCallback", ExactSpelling = true)]
    public static extern WindowIconifyDelegate SetWindowIconifyCallback(IntPtr window, WindowIconifyDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetFramebufferSizeCallback", ExactSpelling = true)]
    public static extern FrameBufferDelegate SetFramebufferSizeCallback(IntPtr window, FrameBufferDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetKeyCallback", ExactSpelling = true)]
    public static extern KeyDelegate SetKeyCallback(IntPtr window, KeyDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetCharCallback", ExactSpelling = true)]
    public static extern CharDelegate SetCharCallback(IntPtr window, CharDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetCharModsCallback", ExactSpelling = true)]
    public static extern CharModDelegate SetCharModsCallback(IntPtr window, CharModDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetMouseButtonCallback", ExactSpelling = true)]
    public static extern MouseButtonDelegate SetMouseButtonCallback(IntPtr window, MouseButtonDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetCursorPosCallback", ExactSpelling = true)]
    public static extern CursorPosDelegate SetCursorPosCallback(IntPtr window, CursorPosDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetCursorEnterCallback", ExactSpelling = true)]
    public static extern CursorEnterDelegate SetCursorEnterCallback(IntPtr window, CursorEnterDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetScrollCallback", ExactSpelling = true)]
    public static extern ScrollDelegate SetScrollCallback(IntPtr window, ScrollDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetDropCallback", ExactSpelling = true)]
    public static extern DropDelegate SetDropCallback(IntPtr window, DropDelegate cbfun);

    [DllImport(LIBGLFW, EntryPoint = "glfwSetJoystickCallback", ExactSpelling = true)]
    public static extern JoystickDelegate SetJoystickCallback(JoystickDelegate cbfun);

    public static int ExtensionSupported(string extension)
    {
        [DllImport(LIBGLFW, EntryPoint = "glfwExtensionSupported", ExactSpelling = true)]
        static extern int glfwExtensionSupported(IntPtr extension);

        IntPtr ptr = Marshal.StringToHGlobalAnsi(extension);

        try
        {
            return glfwExtensionSupported(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static string GetVersionString()
    {
        [DllImport(LIBGLFW, EntryPoint = "glfwGetVersionString", ExactSpelling = true)]
        static extern IntPtr glfwGetVersionString();

        return Marshal.PtrToStringAnsi(glfwGetVersionString()) ?? string.Empty;
    }

    public static IntPtr CreateWindow(int width, int height, string title, IntPtr monitor, IntPtr share)
    {
        [DllImport(LIBGLFW, EntryPoint = "glfwCreateWindow", ExactSpelling = true)]
        static extern IntPtr glfwCreateWindow(int width, int height, IntPtr title, IntPtr monitor, IntPtr share);

        IntPtr ptr = Marshal.StringToHGlobalAnsi(title);

        try
        {
            return glfwCreateWindow(width, height, ptr, monitor, share);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static void SetWindowTitle(IntPtr window, string title)
    {
        [DllImport(LIBGLFW, EntryPoint = "glfwSetWindowTitle", ExactSpelling = true)]
        static extern void glfwSetWindowTitle(IntPtr window, IntPtr title);

        IntPtr ptr = Marshal.StringToHGlobalAnsi(title);

        try
        {
            glfwSetWindowTitle(window, ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static IntPtr GetProcAddress(string procname)
    {
        [DllImport(LIBGLFW, EntryPoint = "glfwGetProcAddress", ExactSpelling = true)]
        static extern IntPtr glfwGetProcAddress(IntPtr procname);

        IntPtr ptr = Marshal.StringToHGlobalAnsi(procname);

        try
        {
            return glfwGetProcAddress(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}

public static class GLFWC
{
    public const int VERSION_MAJOR = 3;
    public const int VERSION_MINOR = 1;
    public const int VERSION_REVISION = 2;
    public const int RELEASE = 0;
    public const int PRESS = 1;
    public const int REPEAT = 2;
    public const int KEY_UNKNOWN = -1;
    public const int KEY_SPACE = 32;
    public const int KEY_APOSTROPHE = 39;
    public const int KEY_COMMA = 44;
    public const int KEY_MINUS = 45;
    public const int KEY_PERIOD = 46;
    public const int KEY_SLASH = 47;
    public const int KEY_0 = 48;
    public const int KEY_1 = 49;
    public const int KEY_2 = 50;
    public const int KEY_3 = 51;
    public const int KEY_4 = 52;
    public const int KEY_5 = 53;
    public const int KEY_6 = 54;
    public const int KEY_7 = 55;
    public const int KEY_8 = 56;
    public const int KEY_9 = 57;
    public const int KEY_SEMICOLON = 59;
    public const int KEY_EQUAL = 61;
    public const int KEY_A = 65;
    public const int KEY_B = 66;
    public const int KEY_C = 67;
    public const int KEY_D = 68;
    public const int KEY_E = 69;
    public const int KEY_F = 70;
    public const int KEY_G = 71;
    public const int KEY_H = 72;
    public const int KEY_I = 73;
    public const int KEY_J = 74;
    public const int KEY_K = 75;
    public const int KEY_L = 76;
    public const int KEY_M = 77;
    public const int KEY_N = 78;
    public const int KEY_O = 79;
    public const int KEY_P = 80;
    public const int KEY_Q = 81;
    public const int KEY_R = 82;
    public const int KEY_S = 83;
    public const int KEY_T = 84;
    public const int KEY_U = 85;
    public const int KEY_V = 86;
    public const int KEY_W = 87;
    public const int KEY_X = 88;
    public const int KEY_Y = 89;
    public const int KEY_Z = 90;
    public const int KEY_LEFT_BRACKET = 91;
    public const int KEY_BACKSLASH = 92;
    public const int KEY_RIGHT_BRACKET = 93;
    public const int KEY_GRAVE_ACCENT = 96;
    public const int KEY_WORLD_1 = 161;
    public const int KEY_WORLD_2 = 162;
    public const int KEY_ESCAPE = 256;
    public const int KEY_ENTER = 257;
    public const int KEY_TAB = 258;
    public const int KEY_BACKSPACE = 259;
    public const int KEY_INSERT = 260;
    public const int KEY_DELETE = 261;
    public const int KEY_RIGHT = 262;
    public const int KEY_LEFT = 263;
    public const int KEY_DOWN = 264;
    public const int KEY_UP = 265;
    public const int KEY_PAGE_UP = 266;
    public const int KEY_PAGE_DOWN = 267;
    public const int KEY_HOME = 268;
    public const int KEY_END = 269;
    public const int KEY_CAPS_LOCK = 280;
    public const int KEY_SCROLL_LOCK = 281;
    public const int KEY_NUM_LOCK = 282;
    public const int KEY_PRINT_SCREEN = 283;
    public const int KEY_PAUSE = 284;
    public const int KEY_F1 = 290;
    public const int KEY_F2 = 291;
    public const int KEY_F3 = 292;
    public const int KEY_F4 = 293;
    public const int KEY_F5 = 294;
    public const int KEY_F6 = 295;
    public const int KEY_F7 = 296;
    public const int KEY_F8 = 297;
    public const int KEY_F9 = 298;
    public const int KEY_F10 = 299;
    public const int KEY_F11 = 300;
    public const int KEY_F12 = 301;
    public const int KEY_F13 = 302;
    public const int KEY_F14 = 303;
    public const int KEY_F15 = 304;
    public const int KEY_F16 = 305;
    public const int KEY_F17 = 306;
    public const int KEY_F18 = 307;
    public const int KEY_F19 = 308;
    public const int KEY_F20 = 309;
    public const int KEY_F21 = 310;
    public const int KEY_F22 = 311;
    public const int KEY_F23 = 312;
    public const int KEY_F24 = 313;
    public const int KEY_F25 = 314;
    public const int KEY_KP_0 = 320;
    public const int KEY_KP_1 = 321;
    public const int KEY_KP_2 = 322;
    public const int KEY_KP_3 = 323;
    public const int KEY_KP_4 = 324;
    public const int KEY_KP_5 = 325;
    public const int KEY_KP_6 = 326;
    public const int KEY_KP_7 = 327;
    public const int KEY_KP_8 = 328;
    public const int KEY_KP_9 = 329;
    public const int KEY_KP_DECIMAL = 330;
    public const int KEY_KP_DIVIDE = 331;
    public const int KEY_KP_MULTIPLY = 332;
    public const int KEY_KP_SUBTRACT = 333;
    public const int KEY_KP_ADD = 334;
    public const int KEY_KP_ENTER = 335;
    public const int KEY_KP_EQUAL = 336;
    public const int KEY_LEFT_SHIFT = 340;
    public const int KEY_LEFT_CONTROL = 341;
    public const int KEY_LEFT_ALT = 342;
    public const int KEY_LEFT_SUPER = 343;
    public const int KEY_RIGHT_SHIFT = 344;
    public const int KEY_RIGHT_CONTROL = 345;
    public const int KEY_RIGHT_ALT = 346;
    public const int KEY_RIGHT_SUPER = 347;
    public const int KEY_MENU = 348;
    public const int KEY_LAST = KEY_MENU;
    public const int MOD_SHIFT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_ALT = 0x0004;
    public const int MOD_SUPER = 0x0008;
    public const int MOUSE_BUTTON_1 = 0;
    public const int MOUSE_BUTTON_2 = 1;
    public const int MOUSE_BUTTON_3 = 2;
    public const int MOUSE_BUTTON_4 = 3;
    public const int MOUSE_BUTTON_5 = 4;
    public const int MOUSE_BUTTON_6 = 5;
    public const int MOUSE_BUTTON_7 = 6;
    public const int MOUSE_BUTTON_8 = 7;
    public const int MOUSE_BUTTON_LAST = MOUSE_BUTTON_8;
    public const int MOUSE_BUTTON_LEFT = MOUSE_BUTTON_1;
    public const int MOUSE_BUTTON_RIGHT = MOUSE_BUTTON_2;
    public const int MOUSE_BUTTON_MIDDLE = MOUSE_BUTTON_3;
    public const int JOYSTICK_1 = 0;
    public const int JOYSTICK_2 = 1;
    public const int JOYSTICK_3 = 2;
    public const int JOYSTICK_4 = 3;
    public const int JOYSTICK_5 = 4;
    public const int JOYSTICK_6 = 5;
    public const int JOYSTICK_7 = 6;
    public const int JOYSTICK_8 = 7;
    public const int JOYSTICK_9 = 8;
    public const int JOYSTICK_10 = 9;
    public const int JOYSTICK_11 = 10;
    public const int JOYSTICK_12 = 11;
    public const int JOYSTICK_13 = 12;
    public const int JOYSTICK_14 = 13;
    public const int JOYSTICK_15 = 14;
    public const int JOYSTICK_16 = 15;
    public const int JOYSTICK_LAST = JOYSTICK_16;
    public const int TRUE = 1;
    public const int FALSE = 0;
    public const int MAXIMIZED = 0x00020008;
    public const int NOT_INITIALIZED = 0x00010001;
    public const int NO_CURRENT_CONTEXT = 0x00010002;
    public const int INVALID_ENUM = 0x00010003;
    public const int INVALID_VALUE = 0x00010004;
    public const int OUT_OF_MEMORY = 0x00010005;
    public const int API_UNAVAILABLE = 0x00010006;
    public const int VERSION_UNAVAILABLE = 0x00010007;
    public const int PLATFORM_ERROR = 0x00010008;
    public const int FORMAT_UNAVAILABLE = 0x00010009;
    public const int FOCUSED = 0x00020001;
    public const int ICONIFIED = 0x00020002;
    public const int RESIZABLE = 0x00020003;
    public const int VISIBLE = 0x00020004;
    public const int DECORATED = 0x00020005;
    public const int AUTO_ICONIFY = 0x00020006;
    public const int FLOATING = 0x00020007;
    public const int RED_BITS = 0x00021001;
    public const int GREEN_BITS = 0x00021002;
    public const int BLUE_BITS = 0x00021003;
    public const int ALPHA_BITS = 0x00021004;
    public const int DEPTH_BITS = 0x00021005;
    public const int STENCIL_BITS = 0x00021006;
    public const int ACCUM_RED_BITS = 0x00021007;
    public const int ACCUM_GREEN_BITS = 0x00021008;
    public const int ACCUM_BLUE_BITS = 0x00021009;
    public const int ACCUM_ALPHA_BITS = 0x0002100A;
    public const int AUX_BUFFERS = 0x0002100B;
    public const int STEREO = 0x0002100C;
    public const int SAMPLES = 0x0002100D;
    public const int SRGB_CAPABLE = 0x0002100E;
    public const int REFRESH_RATE = 0x0002100F;
    public const int DOUBLEBUFFER = 0x00021010;
    public const int CLIENT_API = 0x00022001;
    public const int CONTEXT_VERSION_MAJOR = 0x00022002;
    public const int CONTEXT_VERSION_MINOR = 0x00022003;
    public const int CONTEXT_REVISION = 0x00022004;
    public const int CONTEXT_ROBUSTNESS = 0x00022005;
    public const int OPENGL_FORWARD_COMPAT = 0x00022006;
    public const int OPENGL_DEBUG_CONTEXT = 0x00022007;
    public const int OPENGL_PROFILE = 0x00022008;
    public const int CONTEXT_RELEASE_BEHAVIOR = 0x00022009;
    public const int OPENGL_API = 0x00030001;
    public const int OPENGL_ES_API = 0x00030002;
    public const int NO_ROBUSTNESS = 0;
    public const int NO_RESET_NOTIFICATION = 0x00031001;
    public const int LOSE_CONTEXT_ON_RESET = 0x00031002;
    public const int OPENGL_ANY_PROFILE = 0;
    public const int OPENGL_CORE_PROFILE = 0x00032001;
    public const int OPENGL_COMPAT_PROFILE = 0x00032002;
    public const int CURSOR = 0x00033001;
    public const int STICKY_KEYS = 0x00033002;
    public const int STICKY_MOUSE_BUTTONS = 0x00033003;
    public const int CURSOR_NORMAL = 0x00034001;
    public const int CURSOR_HIDDEN = 0x00034002;
    public const int CURSOR_DISABLED = 0x00034003;
    public const int ANY_RELEASE_BEHAVIOR = 0;
    public const int RELEASE_BEHAVIOR_FLUSH = 0x00035001;
    public const int RELEASE_BEHAVIOR_NONE = 0x00035002;
    public const int ARROW_CURSOR = 0x00036001;
    public const int IBEAM_CURSOR = 0x00036002;
    public const int CROSSHAIR_CURSOR = 0x00036003;
    public const int HAND_CURSOR = 0x00036004;
    public const int HRESIZE_CURSOR = 0x00036005;
    public const int VRESIZE_CURSOR = 0x00036006;
    public const int CONNECTED = 0x00040001;
    public const int DISCONNECTED = 0x00040002;
}