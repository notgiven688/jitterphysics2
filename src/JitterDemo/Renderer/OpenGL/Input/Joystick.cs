using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public class JoystickConnectEventArgs : EventArgs
{
    public int Index { get; private set; }
    public bool Connected { get; private set; }

    public JoystickConnectEventArgs(int index, bool connected)
    {
        Index = index;
        Connected = connected;
    }
}

// TODO: add gamepad states

public static class Joystick
{
    private static readonly float[][] axes = new float[GLFWC.JOYSTICK_LAST][];
    private static readonly byte[][] buttons = new byte[GLFWC.JOYSTICK_LAST][];
    private static readonly byte[][] hats = new byte[GLFWC.JOYSTICK_LAST][];

    private static readonly GLFW.JoystickDelegate jstdelegate;

    public static event EventHandler<JoystickConnectEventArgs>? Connect;

    static Joystick()
    {
        jstdelegate = OnJoystickChange;
        GLFW.SetJoystickCallback(jstdelegate);
    }

    private static void OnJoystickChange(int jid, int evt)
    {
        Connect?.Invoke(null, new JoystickConnectEventArgs(jid, evt == GLFWC.CONNECTED));
    }

    public static bool IsPresent(int index)
    {
        Debug.Assert(index >= 0 && index < GLFWC.JOYSTICK_LAST);
        int present = GLFW.JoystickPresent(index);

        return present == GLFWC.TRUE;
    }

    public static string? GetName(int index)
    {
        Debug.Assert(index >= 0 && index < GLFWC.JOYSTICK_LAST);
        return Marshal.PtrToStringUTF8(GLFW.GetJoystickName(index));
    }

    public static byte[] GetButtons(int index)
    {
        var ptr = GLFW.GetJoystickButtons(index, out int count);
        if (ptr == IntPtr.Zero) return Array.Empty<byte>();

        if (buttons[index] == null || buttons[index].Length < count)
            buttons[index] = new byte[count];

        Marshal.Copy(ptr, buttons[index], 0, count);

        return buttons[index];
    }

    public static float[] GetAxes(int index)
    {
        var ptr = GLFW.GetJoystickAxes(index, out int count);
        if (ptr == IntPtr.Zero) return Array.Empty<float>();

        if (axes[index] == null || axes[index].Length < count)
            axes[index] = new float[count];

        Marshal.Copy(ptr, axes[index], 0, count);

        return axes[index];
    }

    public static byte[] GetHats(int index)
    {
        var ptr = GLFW.GetJoystickHats(index, out int count);
        if (ptr == IntPtr.Zero) return Array.Empty<byte>();

        if (hats[index] == null || hats[index].Length < count)
            hats[index] = new byte[count];

        Marshal.Copy(ptr, hats[index], 0, count);

        return hats[index];
    }
}