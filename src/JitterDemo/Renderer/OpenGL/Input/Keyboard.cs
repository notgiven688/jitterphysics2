using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public class Keyboard
{
    public enum Key
    {
        Space = GLFWC.KEY_SPACE,
        Apostrophe = GLFWC.KEY_APOSTROPHE,
        Comma = GLFWC.KEY_COMMA,
        Minus = GLFWC.KEY_MINUS,
        Period = GLFWC.KEY_PERIOD,
        Slash = GLFWC.KEY_SLASH,
        D0 = GLFWC.KEY_0,
        D1 = GLFWC.KEY_1,
        D2 = GLFWC.KEY_2,
        D3 = GLFWC.KEY_3,
        D4 = GLFWC.KEY_4,
        D5 = GLFWC.KEY_5,
        D6 = GLFWC.KEY_6,
        D7 = GLFWC.KEY_7,
        D8 = GLFWC.KEY_8,
        D9 = GLFWC.KEY_9,
        Semicolon = GLFWC.KEY_SEMICOLON,
        Equal = GLFWC.KEY_EQUAL,
        A = GLFWC.KEY_A,
        B = GLFWC.KEY_B,
        C = GLFWC.KEY_C,
        D = GLFWC.KEY_D,
        E = GLFWC.KEY_E,
        F = GLFWC.KEY_F,
        G = GLFWC.KEY_G,
        H = GLFWC.KEY_H,
        I = GLFWC.KEY_I,
        J = GLFWC.KEY_J,
        K = GLFWC.KEY_K,
        L = GLFWC.KEY_L,
        M = GLFWC.KEY_M,
        N = GLFWC.KEY_N,
        O = GLFWC.KEY_O,
        P = GLFWC.KEY_P,
        Q = GLFWC.KEY_Q,
        R = GLFWC.KEY_R,
        S = GLFWC.KEY_S,
        T = GLFWC.KEY_T,
        U = GLFWC.KEY_U,
        V = GLFWC.KEY_V,
        W = GLFWC.KEY_W,
        X = GLFWC.KEY_X,
        Y = GLFWC.KEY_Y,
        Z = GLFWC.KEY_Z,
        LeftBracket = GLFWC.KEY_LEFT_BRACKET,
        Backslash = GLFWC.KEY_BACKSLASH,
        RightBracket = GLFWC.KEY_RIGHT_BRACKET,
        GraveAccent = GLFWC.KEY_GRAVE_ACCENT,
        World1 = GLFWC.KEY_WORLD_1,
        World2 = GLFWC.KEY_WORLD_2,
        Escape = GLFWC.KEY_ESCAPE,
        Enter = GLFWC.KEY_ENTER,
        Tab = GLFWC.KEY_TAB,
        Backspace = GLFWC.KEY_BACKSPACE,
        Insert = GLFWC.KEY_INSERT,
        Delete = GLFWC.KEY_DELETE,
        Right = GLFWC.KEY_RIGHT,
        Left = GLFWC.KEY_LEFT,
        Down = GLFWC.KEY_DOWN,
        Up = GLFWC.KEY_UP,
        PageUp = GLFWC.KEY_PAGE_UP,
        PageDown = GLFWC.KEY_PAGE_DOWN,
        Home = GLFWC.KEY_HOME,
        End = GLFWC.KEY_END,
        CapsLock = GLFWC.KEY_CAPS_LOCK,
        ScrollLock = GLFWC.KEY_SCROLL_LOCK,
        NumLock = GLFWC.KEY_NUM_LOCK,
        PrintScreen = GLFWC.KEY_PRINT_SCREEN,
        Pause = GLFWC.KEY_PAUSE,
        F1 = GLFWC.KEY_F1,
        F2 = GLFWC.KEY_F2,
        F3 = GLFWC.KEY_F3,
        F4 = GLFWC.KEY_F4,
        F5 = GLFWC.KEY_F5,
        F6 = GLFWC.KEY_F6,
        F7 = GLFWC.KEY_F7,
        F8 = GLFWC.KEY_F8,
        F9 = GLFWC.KEY_F9,
        F10 = GLFWC.KEY_F10,
        F11 = GLFWC.KEY_F11,
        F12 = GLFWC.KEY_F12,
        F13 = GLFWC.KEY_F13,
        F14 = GLFWC.KEY_F14,
        F15 = GLFWC.KEY_F15,
        F16 = GLFWC.KEY_F16,
        F17 = GLFWC.KEY_F17,
        F18 = GLFWC.KEY_F18,
        F19 = GLFWC.KEY_F19,
        F20 = GLFWC.KEY_F20,
        F21 = GLFWC.KEY_F21,
        F22 = GLFWC.KEY_F22,
        F23 = GLFWC.KEY_F23,
        F24 = GLFWC.KEY_F24,
        F25 = GLFWC.KEY_F25,
        Kp0 = GLFWC.KEY_KP_0,
        Kp1 = GLFWC.KEY_KP_1,
        Kp2 = GLFWC.KEY_KP_2,
        Kp3 = GLFWC.KEY_KP_3,
        Kp4 = GLFWC.KEY_KP_4,
        Kp5 = GLFWC.KEY_KP_5,
        Kp6 = GLFWC.KEY_KP_6,
        Kp7 = GLFWC.KEY_KP_7,
        Kp8 = GLFWC.KEY_KP_8,
        Kp9 = GLFWC.KEY_KP_9,
        KpDecimal = GLFWC.KEY_KP_DECIMAL,
        KpDivide = GLFWC.KEY_KP_DIVIDE,
        KpMultiply = GLFWC.KEY_KP_MULTIPLY,
        KpSubtract = GLFWC.KEY_KP_SUBTRACT,
        KpAdd = GLFWC.KEY_KP_ADD,
        KpEnter = GLFWC.KEY_KP_ENTER,
        KpEqual = GLFWC.KEY_KP_EQUAL,
        LeftShift = GLFWC.KEY_LEFT_SHIFT,
        LeftControl = GLFWC.KEY_LEFT_CONTROL,
        LeftAlt = GLFWC.KEY_LEFT_ALT,
        LeftSuper = GLFWC.KEY_LEFT_SUPER,
        RightShift = GLFWC.KEY_RIGHT_SHIFT,
        RightControl = GLFWC.KEY_RIGHT_CONTROL,
        RightAlt = GLFWC.KEY_RIGHT_ALT,
        RightSuper = GLFWC.KEY_RIGHT_SUPER,
        Menu = GLFWC.KEY_MENU,
#pragma warning disable CA1069
        Last = GLFWC.KEY_LAST
#pragma warning restore CA1069
    }

    private readonly BitArray currentKeyState = new(512);
    private readonly BitArray lastKeyState = new(512);

    public static Keyboard Instance { private set; get; } = null!;

    private readonly List<uint> charInput = new();

    private readonly GLFW.KeyDelegate keyfun;
    private readonly GLFW.CharDelegate charfun;

    public Keyboard(IntPtr window)
    {
        keyfun = OnKeyFunction;
        GLFW.SetKeyCallback(window, keyfun);

        charfun = OnCharDelegate;
        GLFW.SetCharCallback(window, charfun);

        Instance = this;
    }

    public IEnumerable<uint> CharInput => charInput;

    private void OnCharDelegate(IntPtr windowHandle, uint codepoint)
    {
        charInput.Add(codepoint);
    }

    private void OnKeyFunction(IntPtr windowHandle, int key, int scanCode, int action, int mods)
    {
        if (key == GLFWC.KEY_UNKNOWN)
        {
            Debug.WriteLine($"Key {key} is unknown");
            return;
        }

        currentKeyState.Set(key, action != GLFWC.RELEASE);
    }

    public void SwapStates()
    {
        lastKeyState.SetAll(false);
        lastKeyState.Xor(currentKeyState);
        charInput.Clear();
    }

    public bool KeyPressBegin(Key k)
    {
        return currentKeyState[(int)k] && !lastKeyState[(int)k];
    }

    public bool KeyPressEnded(Key k)
    {
        return !currentKeyState[(int)k] && lastKeyState[(int)k];
    }

    public bool IsKeyDown(Key k)
    {
        return currentKeyState[(int)k];
    }
}