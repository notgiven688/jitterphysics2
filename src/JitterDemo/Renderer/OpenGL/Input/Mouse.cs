using System;
using System.Collections;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public class Mouse
{
    private readonly BitArray currentMouseState = new(8);
    private readonly BitArray lastMouseState = new(8);

    private Coordinate currentMousePos;
    private Coordinate lastMousePos;

    private Coordinate scrollWheel;

    public enum Button
    {
        B1 = GLFWC.MOUSE_BUTTON_1,
        B2 = GLFWC.MOUSE_BUTTON_2,
        B3 = GLFWC.MOUSE_BUTTON_3,
        B4 = GLFWC.MOUSE_BUTTON_4,
        B5 = GLFWC.MOUSE_BUTTON_5,
        B6 = GLFWC.MOUSE_BUTTON_6,
        B7 = GLFWC.MOUSE_BUTTON_7,
        B8 = GLFWC.MOUSE_BUTTON_8,
#pragma warning disable CA1069
        Last = GLFWC.MOUSE_BUTTON_LAST,
        Left = GLFWC.MOUSE_BUTTON_LEFT,
        Right = GLFWC.MOUSE_BUTTON_RIGHT,
        Middle = GLFWC.MOUSE_BUTTON_MIDDLE
#pragma warning restore CA1069
    }

    public struct Coordinate
    {
        public double X;
        public double Y;

        public Coordinate(double x, double y)
        {
            X = x;
            Y = y;
        }

        public void SetZero()
        {
            X = Y = 0;
        }
    }

    public Coordinate Position => currentMousePos;

    public Coordinate ScrollWheel => scrollWheel;

    public static Mouse Instance { get; private set; } = null!;

    private readonly GLFW.MouseButtonDelegate mousefun;
    private readonly GLFW.CursorPosDelegate cursorposfun;
    private readonly GLFW.ScrollDelegate scrollfun;

    public Mouse(IntPtr window)
    {
        mousefun = OnMouseButton;
        cursorposfun = OnMousePos;
        scrollfun = OnMouseScroll;

        GLFW.SetMouseButtonCallback(window, mousefun);
        GLFW.SetCursorPosCallback(window, cursorposfun);
        GLFW.SetScrollCallback(window, scrollfun);
        Instance = this;
    }

    private void OnMouseScroll(IntPtr windowHandle, double xoffset, double yoffset)
    {
        scrollWheel.X = xoffset;
        scrollWheel.Y = yoffset;
    }

    private void OnMousePos(IntPtr windowHandle, double mousex, double mousey)
    {
        currentMousePos = new Coordinate(mousex, mousey);
    }

    private void OnMouseButton(IntPtr windowHandle, int button, int action, int mods)
    {
        currentMouseState.Set(button, action != GLFWC.RELEASE);
    }

    public bool ButtonPressBegin(Button k)
    {
        return currentMouseState[(int)k] && !lastMouseState[(int)k];
    }
    
    public bool ButtonPressEnd(Button k)
    {
        return !currentMouseState[(int)k] && lastMouseState[(int)k];
    }

    public bool IsButtonDown(Button k)
    {
        return currentMouseState[(int)k];
    }

    public Coordinate DeltaPosition => new(currentMousePos.X - lastMousePos.X, currentMousePos.Y - lastMousePos.Y);

    public void SwapStates()
    {
        lastMouseState.SetAll(false);
        lastMouseState.Xor(currentMouseState);

        lastMousePos = currentMousePos;
        scrollWheel.SetZero();
    }
}