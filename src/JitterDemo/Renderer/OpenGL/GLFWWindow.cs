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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public struct CreationSettings
{
    public int Width;
    public int Height;
    public string Title;

    public CreationSettings(int width, int height, string title)
    {
        Title = title;
        Width = width;
        Height = height;
    }
}

public class OpenGLVersionNotSupportedException : Exception
{
    public OpenGLVersionNotSupportedException(string msg) : base(msg)
    {
    }
}

public class GLFWWindow
{
    private string title = string.Empty;

    private GLFW.ErrorDelegate errorFunction = null!;

    public virtual void Draw()
    {
    }

    public virtual void Load()
    {
    }

    public IntPtr Handle { get; private set; }

    private int width;
    private int height;

    public (int Width, int Height) FramebufferSize
    {
        get
        {
            int fbw = 0, fbh = 0;
            GLFW.GetFramebufferSize(Handle, ref fbw, ref fbh);
            return (fbw, fbh);
        }
    }

    public int Width
    {
        get => width;
        set
        {
            width = value;
            GLFW.SetWindowSize(Handle, width, height);
        }
    }

    public int Height
    {
        get => height;
        set
        {
            height = value;
            GLFW.SetWindowSize(Handle, width, height);
        }
    }

    public string Title
    {
        get => title;
        set
        {
            title = value;
            GLFW.SetWindowTitle(Handle, value);
        }
    }

    public double Time { get; private set; }

    public bool VerticalSync
    {
        set
        {
            GLFW.MakeContextCurrent(Handle);
            GLFW.SwapInterval(value ? 1 : 0);
        }
    }

    public Keyboard Keyboard { private set; get; } = null!;

    public Mouse Mouse { private set; get; } = null!;

    private static bool Created;

    public GLFWWindow()
    {
        callback = DebugMessageCallback;

        if (Created) throw new NotSupportedException("There is only support for one GLFW-Window.");
        Created = true;
    }

    public (int, int) OpenGLVersion { get; private set; }

    public (int, int, int) GLFWVersion { get; private set; }

    public void Open(CreationSettings settings)
    {
        if (GLFW.Init() == 0)
        {
            throw new Exception("Unable to initialize GLFW.");
        }

        GLFW.GetVersion(out int glfwmajor, out int glfwminor, out int glfwrev);
        GLFWVersion = (glfwmajor, glfwminor, glfwrev);

        string version = GLFW.GetVersionString();
        Debug.WriteLine($"DEBUG: GLFW-Version string: {version}");

        InitWindow(settings);

        int glmajor = GL.GetIntegerv(GLC.MAJOR_VERSION);
        int glminor = GL.GetIntegerv(GLC.MINOR_VERSION);
        OpenGLVersion = (glmajor, glminor);

        Debug.WriteLine($"DEBUG: Using OpenGL {glmajor}.{glminor}");

        if (glmajor < 3 || (glmajor == 3 && glminor < 3))
        {
            throw new OpenGLVersionNotSupportedException(
                $"OpenGL version {glmajor}.{glminor} detected. At least OpenGL 3.3 is required to run this program.");
        }

        // https://www.khronos.org/opengl/wiki/Debug_Output
        GL.DebugMessageCallback(callback, IntPtr.Zero);
        GL.Enable(GLC.DEBUG_OUTPUT_SYNCHRONOUS);

        int flags = GL.GetIntegerv(GLC.CONTEXT_FLAGS);
        if ((flags & GLC.CONTEXT_FLAG_DEBUG_BIT) != 0)
        {
            Debug.WriteLine("DEBUG: OpenGL Debug context enabled.");
        }

        InitEvents();

        Keyboard = new Keyboard(Handle);
        Mouse = new Mouse(Handle);

        GLFW.GetWindowSize(Handle, out width, out height);

        Time = GLFW.GetTime();

        Load();

        Sync();
    }

    private readonly GL.DebugMessageDelegate callback;

    private readonly StringBuilder messageBuilder = new();

    public virtual void GLDebugMessage(GLDebugMessage message)
    {
        messageBuilder.AppendLine($"[{DateTimeOffset.Now:MM/dd/yyyy hh:mm:ss.fff tt}]: GLDebug Message");
        messageBuilder.AppendLine(new string('-', messageBuilder.Length));
        messageBuilder.AppendLine($"Source: {message.Source}");
        messageBuilder.AppendLine($"Severity: {message.Severity}");
        messageBuilder.AppendLine($"Type: {message.Type}");
        messageBuilder.AppendLine($"Id: {message.Id}");
        messageBuilder.AppendLine($"{message.Message}");

        Debug.WriteLine(messageBuilder.ToString());

        if (message.Severity == GLDebugMessageSeverity.High)
        {
            throw new Exception(messageBuilder.ToString());
        }

        messageBuilder.Clear();
    }

    public virtual void GLFWErrorMessage(int code, string message)
    {
        messageBuilder.AppendLine($"[{DateTimeOffset.Now:MM/dd/yyyy hh:mm:ss.fff tt}]: GLFW Error");
        messageBuilder.AppendLine(new string('-', messageBuilder.Length));
        messageBuilder.AppendLine($"Id: {code}");
        messageBuilder.AppendLine($"{message}");

        Debug.WriteLine(messageBuilder.ToString());
        messageBuilder.Clear();
    }

    private void DebugMessageCallback(uint source, uint type, uint id, uint severity, int length, IntPtr buf)
    {
        // https://learnopengl.com/In-Practice/Debugging
        // ignore non-significant error/warning codes

        if (id == 131169 || id == 131185 || id == 131218 || id == 131204) return;

        string msg = Marshal.PtrToStringUTF8(buf) ?? string.Empty;

        GLDebugMessage message;
        message.Id = id;
        message.Message = msg;
        message.Source = (GLDebugMessageSource)source;
        message.Severity = (GLDebugMessageSeverity)severity;
        message.Type = (GLDebugMessageType)type;

        GLDebugMessage(message);
    }

    public void Close()
    {
        GLFW.SetWindowShouldClose(Handle, 1);
    }

    private void InitWindow(CreationSettings settings)
    {
        GLFW.WindowHint(GLFWC.SAMPLES, 4);

#if DEBUG
        GLFW.WindowHint(GLFWC.OPENGL_DEBUG_CONTEXT, GLFWC.TRUE);
#endif

        Handle = GLFW.CreateWindow(settings.Width, settings.Height,
            settings.Title, IntPtr.Zero, IntPtr.Zero);

        if (Handle == IntPtr.Zero)
        {
            GLFW.Terminate();
            throw new Exception("Unable to create window.");
        }

        GLFW.MakeContextCurrent(Handle);

        GL.Load();
        GL.Enable(GLC.MULTISAMPLE);
    }

    private void InitEvents()
    {
        errorFunction = OnErrorFunction;
        GLFW.SetErrorCallback(errorFunction);
    }

    private int targetFPS = 100;
    private double targetTicks = Stopwatch.Frequency / 100.0d;

    public int TargetFPS
    {
        get => targetFPS;
        set
        {
            targetFPS = value;
            targetTicks = Stopwatch.Frequency / (double)targetFPS;
        }
    }

    private void Sync()
    {
        while (GLFW.WindowShouldClose(Handle) == 0)
        {
            long time = Stopwatch.GetTimestamp();
            GLFW.GetWindowSize(Handle, out width, out height);

            Time = GLFW.GetTime();
            Draw();
            GLFW.SwapBuffers(Handle);
            Keyboard.SwapStates();
            Mouse.SwapStates();
            GLFW.PollEvents();

            while (targetTicks - (Stopwatch.GetTimestamp() - time) > 0)
            {
                Thread.Sleep(0);
            }
        }

        GLFW.DestroyWindow(Handle);
    }

    private void OnErrorFunction(int errorCode, string description)
    {
        GLFWErrorMessage(errorCode, description);
    }

    public virtual void WindowResize()
    {
    }
}