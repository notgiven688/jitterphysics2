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
using System.Runtime.InteropServices;
using GLenum = System.UInt32;
using GLuint = System.UInt32;
using GLint = System.Int32;
using GLsizei = System.Int32;
using GLchar = System.Byte;
using GLubyte = System.Byte;
using GLfloat = System.Single;
using GLboolean = System.Boolean;
using GLclampf = System.Single;
using GLbitfield = System.UInt32;

#pragma warning disable CS8618

namespace JitterDemo.Renderer.OpenGL.Native;

// The opengl-functions can not be directly imported using DllImport.
//
// https://registry.khronos.org/OpenGL/ABI/
// https://www.khronos.org/opengl/wiki/Load_OpenGL_Functions
// https://stackoverflow.com/questions/36661449/glfw-load-functions
//
// We rely on GLFW's GetProcAddress to return the correct function pointer
// and use Marshal.GetDelegateForFunctionPointer to make the function
// available in the managed world.

public static class GL
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DebugMessageDelegate(uint source, uint type, uint id, uint severity, int length, IntPtr buf);

    private delegate void glDepthMaskDelegate(bool flag);

    private static glDepthMaskDelegate glDepthMask;

    private delegate void glGenerateMipmapDelegate(uint target);

    private static glGenerateMipmapDelegate glGenerateMipmap;

    private delegate void glBindVertexArrayDelegate(uint array);

    private static glBindVertexArrayDelegate glBindVertexArray;

    private delegate void glCompileShaderDelegate(uint shader);

    private static glCompileShaderDelegate glCompileShader;

    private delegate void glScissorDelegate(int x, int y, int width, int height);

    private static glScissorDelegate glScissor;

    private unsafe delegate void glGenBuffersDelegate(int n, uint* ids);

    private static glGenBuffersDelegate glGenBuffers;

    private unsafe delegate void glGenFramebuffersDelegate(int n, uint* ids);

    private static glGenFramebuffersDelegate glGenFramebuffers;

    private unsafe delegate void glGenVertexArraysDelegate(int n, uint* ids);

    private static glGenVertexArraysDelegate glGenVertexArrays;

    private delegate void glBindFramebufferDelegate(uint target, uint framebuffer);

    private static glBindFramebufferDelegate glBindFramebuffer;

    private delegate void glFramebufferTexture2DDelegate(uint target, uint attachment, uint textarget, uint texture,
        int level);

    private static glFramebufferTexture2DDelegate glFramebufferTexture2D;

    private delegate void glNamedFramebufferDrawBufferDelegate(uint framebuffer, uint buf);

    private static glNamedFramebufferDrawBufferDelegate glNamedFramebufferDrawBuffer;

    private delegate void glBindBufferDelegate(uint target, uint buffer);

    private static glBindBufferDelegate glBindBuffer;

    private delegate void glBufferDataDelegate(uint target, int size, IntPtr data, uint usage);

    private static glBufferDataDelegate glBufferData;

    private delegate void glEnableVertexAttribArrayDelegate(uint index);

    private static glEnableVertexAttribArrayDelegate glEnableVertexAttribArray;

    private delegate void glVertexAttribPointerDelegate(uint index, int size, uint type, bool normalized, int stride,
        IntPtr pointer);

    private static glVertexAttribPointerDelegate glVertexAttribPointer;

    private delegate void glVertexAttribDivisorDelegate(uint index, uint divisor);

    private static glVertexAttribDivisorDelegate glVertexAttribDivisor;

    private delegate uint glCreateProgramDelegate();

    private static glCreateProgramDelegate glCreateProgram;

    private delegate uint glCreateShaderDelegate(uint shaderType);

    private static glCreateShaderDelegate glCreateShader;

    private delegate void glShaderSourceDelegate(uint shader, int count, string[] @string, int[]? length);

    private static glShaderSourceDelegate glShaderSource;

    private delegate void glGetActiveUniformDelegate(uint program, uint index, int bufSize, [Out] out int length,
        [Out] out int size, [Out] out uint type, [Out] IntPtr name);

    private static glGetActiveUniformDelegate glGetActiveUniform;

    private delegate int glGetUniformLocationDelegate(uint program, string name);

    private static glGetUniformLocationDelegate glGetUniformLocation;

    private delegate void glGetShaderivDelegate(uint shader, uint pname, [Out] out int @params);

    private static glGetShaderivDelegate glGetShaderiv;

    private delegate void glAttachShaderDelegate(uint program, uint shader);

    private static glAttachShaderDelegate glAttachShader;

    private delegate void glDeleteShaderDelegate(uint shader);

    private static glDeleteShaderDelegate glDeleteShader;

    private delegate void glLinkProgramDelegate(uint program);

    private static glLinkProgramDelegate glLinkProgram;

    private delegate void glGetProgramivDelegate(uint program, uint pname, [Out] out int @params);

    private static glGetProgramivDelegate glGetProgramiv;

    private delegate void glUseProgramDelegate(uint program);

    private static glUseProgramDelegate glUseProgram;

    private delegate void glDrawArraysInstancedDelegate(uint mode, int first, int count, int instancecount);

    private static glDrawArraysInstancedDelegate glDrawArraysInstanced;

    private delegate void glDrawArraysDelegate(uint mode, int first, int count);

    private static glDrawArraysDelegate glDrawArrays;

    private delegate void glDrawElementsInstancedDelegate(uint mode, int count, uint type, IntPtr indices,
        int instancecount);

    private static glDrawElementsInstancedDelegate glDrawElementsInstanced;

    private delegate void glUniform1fDelegate(int location, float v0);

    private static glUniform1fDelegate glUniform1f;

    private delegate void glUniform1uiDelegate(int location, uint v0);

    private static glUniform1uiDelegate glUniform1ui;

    private delegate void glUniform1iDelegate(int location, int v0);

    private static glUniform1iDelegate glUniform1i;

    private unsafe delegate void glUniform1uivDelegate(int location, int count, uint* value);

    private static glUniform1uivDelegate glUniform1uiv;

    private unsafe delegate void glUniform1fvDelegate(int location, int count, float* value);

    private static glUniform1fvDelegate glUniform1fv;

    private delegate void glUniform2fDelegate(int location, float v0, float v1);

    private static glUniform2fDelegate glUniform2f;

    private delegate void glUniform3fDelegate(int location, float v0, float v1, float v2);

    private static glUniform3fDelegate glUniform3f;

    private delegate void glUniform4fDelegate(int location, float v0, float v1, float v2, float v3);

    private static glUniform4fDelegate glUniform4f;

    private delegate void glActiveTextureDelegate(uint texture);

    private static glActiveTextureDelegate glActiveTexture;

    private delegate void glTexParameterfDelegate(uint target, uint pname, float param);

    private static glTexParameterfDelegate glTexParameterf;

    private unsafe delegate void glUniform2fvDelegate(int location, int count, float* value);

    private static glUniform2fvDelegate glUniform2fv;

    private unsafe delegate void glUniform3fvDelegate(int location, int count, float* value);

    private static glUniform3fvDelegate glUniform3fv;

    private unsafe delegate void glUniform4fvDelegate(int location, int count, float* value);

    private static glUniform4fvDelegate glUniform4fv;

    private unsafe delegate void glUniformMatrix4fvDelegate(int location, int count, bool transpose, float* value);

    private static glUniformMatrix4fvDelegate glUniformMatrix4fv;

    private delegate void glDebugMessageCallbackDelegate(DebugMessageDelegate callback, IntPtr userParam);

    private static glDebugMessageCallbackDelegate glDebugMessageCallback;

    private unsafe delegate void glTexParameterfvDelegate(uint target, uint pname, float* @params);

    private static glTexParameterfvDelegate glTexParameterfv;

    private delegate void glPolygonModeDelegate(uint face, uint mode);

    private static glPolygonModeDelegate glPolygonMode;

    private unsafe delegate void glGetFloatvDelegate(uint pname, float* @params);

    private static glGetFloatvDelegate glGetFloatv;

    private delegate void glDrawElementsDelegate(uint mode, int count, uint type, IntPtr indices);

    private static glDrawElementsDelegate glDrawElements;

    private delegate void glBlendFuncDelegate(uint sfactor, uint dfactor);

    private static glBlendFuncDelegate glBlendFunc;

    private delegate void glClearColorDelegate(float red, float green, float blue, float alpha);

    private static glClearColorDelegate glClearColor;

    private delegate void glTexParameteriDelegate(uint target, uint pname, int param);

    private static glTexParameteriDelegate glTexParameteri;

    private delegate void glDisableDelegate(uint cap);

    private static glDisableDelegate glDisable;

    private delegate void glEnableDelegate(uint cap);

    private static glEnableDelegate glEnable;

    private delegate void glCullFaceDelegate(uint mode);

    private static glCullFaceDelegate glCullFace;

    private delegate void glClearDelegate(uint mask);

    private static glClearDelegate glClear;

    private delegate void glViewportDelegate(int x, int y, int width, int height);

    private static glViewportDelegate glViewport;

    private delegate void glTexImage2DDelegate(uint target, int level, int internalformat, int width, int height,
        int border, uint format, uint type, IntPtr data);

    private static glTexImage2DDelegate glTexImage2D;

    private delegate void glBindTextureDelegate(uint target, uint texture);

    private static glBindTextureDelegate glBindTexture;

    private delegate void glDeleteProgramDelegate(uint program);

    private static glDeleteProgramDelegate glDeleteProgram;

    private delegate void glDisableVertexAttribArrayDelegate(uint index);

    private static glDisableVertexAttribArrayDelegate glDisableVertexAttribArray;

    private unsafe delegate void glGenTexturesDelegate(int n, uint* textures);

    private static glGenTexturesDelegate glGenTextures;

    private delegate uint glGetErrorDelegate();

    private static glGetErrorDelegate glGetError;

    private unsafe delegate void glGetIntegervDelegate(uint target, int* data);

    private static glGetIntegervDelegate glGetIntegerv;

    private delegate void glBufferSubDataDelegate(uint target, int offset, int size, IntPtr data);

    private static glBufferSubDataDelegate glBufferSubData;

    private delegate void glGetProgramInfoLogDelegate(uint program, [In] int maxLength, [Out] out int length,
        [Out] IntPtr infoLog);

    private static glGetProgramInfoLogDelegate glGetProgramInfoLog;

    private delegate void glGetShaderInfoLogDelegate(uint shader, [In] int maxLength, [Out] out int length,
        [Out] IntPtr infoLog);

    private static glGetShaderInfoLogDelegate glGetShaderInfoLog;

    private static T GetDelegate<T>()
    {
        string name = typeof(T).Name.Replace("Delegate", string.Empty);

        IntPtr ptr = GLFW.GetProcAddress(name);
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    public static void Load()
    {
        glActiveTexture = GetDelegate<glActiveTextureDelegate>();
        glCreateProgram = GetDelegate<glCreateProgramDelegate>();
        glUseProgram = GetDelegate<glUseProgramDelegate>();
        glDrawArraysInstanced = GetDelegate<glDrawArraysInstancedDelegate>();
        glDepthMask = GetDelegate<glDepthMaskDelegate>();
        glDrawArrays = GetDelegate<glDrawArraysDelegate>();
        glDrawElementsInstanced = GetDelegate<glDrawElementsInstancedDelegate>();
        glGenVertexArrays = GetDelegate<glGenVertexArraysDelegate>();
        glDebugMessageCallback = GetDelegate<glDebugMessageCallbackDelegate>();
        glGenerateMipmap = GetDelegate<glGenerateMipmapDelegate>();
        glBindBuffer = GetDelegate<glBindBufferDelegate>();
        glNamedFramebufferDrawBuffer = GetDelegate<glNamedFramebufferDrawBufferDelegate>();
        glFramebufferTexture2D = GetDelegate<glFramebufferTexture2DDelegate>();
        glBindFramebuffer = GetDelegate<glBindFramebufferDelegate>();
        glGenBuffers = GetDelegate<glGenBuffersDelegate>();
        glBindVertexArray = GetDelegate<glBindVertexArrayDelegate>();
        glVertexAttribDivisor = GetDelegate<glVertexAttribDivisorDelegate>();
        glEnableVertexAttribArray = GetDelegate<glEnableVertexAttribArrayDelegate>();
        glGenFramebuffers = GetDelegate<glGenFramebuffersDelegate>();
        glGetUniformLocation = GetDelegate<glGetUniformLocationDelegate>();
        glCompileShader = GetDelegate<glCompileShaderDelegate>();
        glScissor = GetDelegate<glScissorDelegate>();
        glCreateShader = GetDelegate<glCreateShaderDelegate>();
        glAttachShader = GetDelegate<glAttachShaderDelegate>();
        glDeleteShader = GetDelegate<glDeleteShaderDelegate>();
        glLinkProgram = GetDelegate<glLinkProgramDelegate>();
        glGetProgramiv = GetDelegate<glGetProgramivDelegate>();
        glGetShaderiv = GetDelegate<glGetShaderivDelegate>();
        glShaderSource = GetDelegate<glShaderSourceDelegate>();
        glGetActiveUniform = GetDelegate<glGetActiveUniformDelegate>();
        glUniform1f = GetDelegate<glUniform1fDelegate>();
        glUniform1ui = GetDelegate<glUniform1uiDelegate>();
        glUniform1i = GetDelegate<glUniform1iDelegate>();
        glUniform1uiv = GetDelegate<glUniform1uivDelegate>();
        glUniform1fv = GetDelegate<glUniform1fvDelegate>();
        glUniform2f = GetDelegate<glUniform2fDelegate>();
        glUniform3f = GetDelegate<glUniform3fDelegate>();
        glUniform4f = GetDelegate<glUniform4fDelegate>();
        glTexParameterf = GetDelegate<glTexParameterfDelegate>();
        glUniform2fv = GetDelegate<glUniform2fvDelegate>();
        glUniform3fv = GetDelegate<glUniform3fvDelegate>();
        glUniform4fv = GetDelegate<glUniform4fvDelegate>();
        glUniformMatrix4fv = GetDelegate<glUniformMatrix4fvDelegate>();
        glBufferData = GetDelegate<glBufferDataDelegate>();
        glVertexAttribPointer = GetDelegate<glVertexAttribPointerDelegate>();
        glTexParameterfv = GetDelegate<glTexParameterfvDelegate>();
        glPolygonMode = GetDelegate<glPolygonModeDelegate>();
        glGetFloatv = GetDelegate<glGetFloatvDelegate>();
        glDrawElements = GetDelegate<glDrawElementsDelegate>();
        glBlendFunc = GetDelegate<glBlendFuncDelegate>();
        glClearColor = GetDelegate<glClearColorDelegate>();
        glTexParameteri = GetDelegate<glTexParameteriDelegate>();
        glDisable = GetDelegate<glDisableDelegate>();
        glEnable = GetDelegate<glEnableDelegate>();
        glCullFace = GetDelegate<glCullFaceDelegate>();
        glClear = GetDelegate<glClearDelegate>();
        glViewport = GetDelegate<glViewportDelegate>();
        glTexImage2D = GetDelegate<glTexImage2DDelegate>();
        glBindTexture = GetDelegate<glBindTextureDelegate>();
        glDeleteProgram = GetDelegate<glDeleteProgramDelegate>();
        glDisableVertexAttribArray = GetDelegate<glDisableVertexAttribArrayDelegate>();
        glGetError = GetDelegate<glGetErrorDelegate>();
        glGetIntegerv = GetDelegate<glGetIntegervDelegate>();
        glBufferSubData = GetDelegate<glBufferSubDataDelegate>();
        glGenTextures = GetDelegate<glGenTexturesDelegate>();
        glGetShaderInfoLog = GetDelegate<glGetShaderInfoLogDelegate>();
        glGetProgramInfoLog = GetDelegate<glGetProgramInfoLogDelegate>();
    }

    public static void DepthMask(bool flag)
    {
        glDepthMask(flag);
    }

    public static unsafe void TexParameterfv(uint target, uint pname, float* @params)
    {
        glTexParameterfv(target, pname, @params);
    }

    public static unsafe void GetFloatv(uint pname, float* @params)
    {
        glGetFloatv(pname, @params);
    }

    public static void PolygonMode(uint face, uint mode)
    {
        glPolygonMode(face, mode);
    }

    public static void DrawElements(uint mode, int count, uint type, IntPtr indices)
    {
        glDrawElements(mode, count, type, indices);
    }

    public static void DrawElementsBaseVertex(uint mode, int count, uint type, IntPtr indices, int basevertex)
    {
        glDrawElements(mode, count, type, indices);
    }

    public static void BlendFunc(uint sfactor, uint dfactor)
    {
        glBlendFunc(sfactor, dfactor);
    }

    public static void ClearColor(float red, float green, float blue, float alpha)
    {
        glClearColor(red, green, blue, alpha);
    }

    public static void TexParameteri(uint target, uint pname, int param)
    {
        glTexParameteri(target, pname, param);
    }

    public static void Disable(uint cap)
    {
        glDisable(cap);
    }

    public static void Enable(uint cap)
    {
        glEnable(cap);
    }

    public static void CullFace(uint mode)
    {
        glCullFace(mode);
    }

    public static void Clear(uint mask)
    {
        glClear(mask);
    }

    public static void Viewport(int x, int y, int width, int height)
    {
        glViewport(x, y, width, height);
    }

    public static void TexImage2D(uint target, int level, int internalformat, int width, int height, int border,
        uint format, uint type, IntPtr data)
    {
        glTexImage2D(target, level, internalformat, width, height, border, format, type, data);
    }

    public static void BindTexture(uint target, uint texture)
    {
        glBindTexture(target, texture);
    }

    public static void DeleteProgram(uint program)
    {
        glDeleteProgram(program);
    }

    public static void DisableVertexAttribArray(uint index)
    {
        glDisableVertexAttribArray(index);
    }

    public static uint GetError()
    {
        return glGetError();
    }

    public static unsafe void GetIntegerv(uint target, int* data)
    {
        glGetIntegerv(target, data);
    }

    public static void BufferSubData(uint target, int offset, int size, IntPtr data)
    {
        glBufferSubData(target, offset, size, data);
    }

    public static void ActiveTexture(uint texture)
    {
        glActiveTexture(texture);
    }

    public static uint CreateProgram()
    {
        return glCreateProgram();
    }

    public static void UseProgram(uint program)
    {
        glUseProgram(program);
    }

    public static void DrawArraysInstanced(uint mode, int first, int count, int instancecount)
    {
        glDrawArraysInstanced(mode, first, count, instancecount);
    }

    public static void DrawArrays(uint mode, int first, int count)
    {
        glDrawArrays(mode, first, count);
    }

    public static void DrawElementsInstanced(uint mode, int count, uint type, IntPtr indices, int instancecount)
    {
        glDrawElementsInstanced(mode, count, type, indices, instancecount);
    }

    public static void DebugMessageCallback(DebugMessageDelegate callback, IntPtr userParam)
    {
        glDebugMessageCallback(callback, userParam);
    }

    public static void GenerateMipmap(uint target)
    {
        glGenerateMipmap(target);
    }

    public static void BindBuffer(uint target, uint buffer)
    {
        glBindBuffer(target, buffer);
    }

    public static void NamedFramebufferDrawBuffer(uint framebuffer, uint buf)
    {
        glNamedFramebufferDrawBuffer(framebuffer, buf);
    }

    public static void FramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level)
    {
        glFramebufferTexture2D(target, attachment, textarget, texture, level);
    }

    public static void BindFramebuffer(uint target, uint framebuffer)
    {
        glBindFramebuffer(target, framebuffer);
    }

    public static void BindVertexArray(uint array)
    {
        glBindVertexArray(array);
    }

    public static void VertexAttribDivisor(uint index, uint divisor)
    {
        glVertexAttribDivisor(index, divisor);
    }

    public static void EnableVertexAttribArray(uint index)
    {
        glEnableVertexAttribArray(index);
    }

    public static int GetUniformLocation(uint program, string name)
    {
        return glGetUniformLocation(program, name);
    }

    public static void CompileShader(uint shader)
    {
        glCompileShader(shader);
    }

    public static void Scissor(int x, int y, int width, int height)
    {
        glScissor(x, y, width, height);
    }

    public static uint CreateShader(uint shaderType)
    {
        return glCreateShader(shaderType);
    }

    public static void AttachShader(uint program, uint shader)
    {
        glAttachShader(program, shader);
    }

    public static void DeleteShader(uint shader)
    {
        glDeleteShader(shader);
    }

    public static void LinkProgram(uint program)
    {
        glLinkProgram(program);
    }

    public static void GetProgramiv(uint program, uint pname, [Out] out int @params)
    {
        glGetProgramiv(program, pname, out @params);
    }

    public static void GetShaderiv(uint shader, uint pname, [Out] out int @params)
    {
        glGetShaderiv(shader, pname, out @params);
    }

    public static void ShaderSource(uint shader, int count, string[] @string, int[]? length)
    {
        glShaderSource(shader, count, @string, length);
    }

    public static void Uniform1f(int location, float v0)
    {
        glUniform1f(location, v0);
    }

    public static void Uniform1ui(int location, uint v0)
    {
        glUniform1ui(location, v0);
    }

    public static void Uniform1i(int location, int v0)
    {
        glUniform1i(location, v0);
    }

    public static unsafe void Uniform1uiv(int location, int count, uint* value)
    {
        glUniform1uiv(location, count, value);
    }

    public static unsafe void Uniform1fv(int location, int count, float* value)
    {
        glUniform1fv(location, count, value);
    }

    public static void Uniform2f(int location, float v0, float v1)
    {
        glUniform2f(location, v0, v1);
    }

    public static void Uniform3f(int location, float v0, float v1, float v2)
    {
        glUniform3f(location, v0, v1, v2);
    }

    public static void Uniform4f(int location, float v0, float v1, float v2, float v3)
    {
        glUniform4f(location, v0, v1, v2, v3);
    }

    public static void TexParameterf(uint target, uint pname, float param)
    {
        glTexParameterf(target, pname, param);
    }

    public static unsafe void Uniform2fv(int location, int count, float* value)
    {
        glUniform2fv(location, count, value);
    }

    public static unsafe void Uniform3fv(int location, int count, float* value)
    {
        glUniform3fv(location, count, value);
    }

    public static unsafe void Uniform4fv(int location, int count, float* value)
    {
        glUniform4fv(location, count, value);
    }

    public static unsafe void UniformMatrix4fv(int location, int count, bool transpose, float* value)
    {
        glUniformMatrix4fv(location, count, transpose, value);
    }

    public static void BufferData(uint target, int size, IntPtr data, uint usage)
    {
        glBufferData(target, size, data, usage);
    }

    public static void VertexAttribPointer(uint index, int size, uint type, bool normalized, int stride, IntPtr pointer)
    {
        glVertexAttribPointer(index, size, type, normalized, stride, pointer);
    }

    public static void GetShaderInfoLog(uint shader, int maxLength, out int length, out string infoLog)
    {
        IntPtr buffer = Marshal.AllocHGlobal(maxLength);

        try
        {
            glGetShaderInfoLog(shader, maxLength, out length, buffer);
            infoLog = Marshal.PtrToStringUTF8(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static void GetProgramInfoLog(uint program, int maxLength, out int length, out string infoLog)
    {
        IntPtr buffer = Marshal.AllocHGlobal(maxLength);

        try
        {
            glGetProgramInfoLog(program, maxLength, out length, buffer);
            infoLog = Marshal.PtrToStringUTF8(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static void GetActiveUniform(uint program, uint index, int bufSize, out int length, out int size,
        out uint type, out string name)
    {
        IntPtr buffer = Marshal.AllocHGlobal(bufSize);

        try
        {
            glGetActiveUniform(program, index, bufSize, out length, out size, out type, buffer);
            name = Marshal.PtrToStringUTF8(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static uint GenBuffer()
    {
        uint id;
        unsafe
        {
            glGenBuffers(1, &id);
        }

        return id;
    }

    public static uint GenVertexArray()
    {
        uint id;
        unsafe
        {
            glGenVertexArrays(1, &id);
        }

        return id;
    }

    public static int GetIntegerv(uint target)
    {
        int result;
        unsafe
        {
            glGetIntegerv(target, &result);
        }

        return result;
    }

    public static void GenTextures(int n, out uint[] ids)
    {
        ids = new uint[n];
        unsafe
        {
            fixed (uint* first = ids)
            {
                glGenTextures(n, first);
            }
        }
    }

    public static uint GenTexture()
    {
        uint id;
        unsafe
        {
            glGenTextures(1, &id);
        }

        return id;
    }

    public static uint GenFramebuffer()
    {
        uint id;
        unsafe
        {
            glGenFramebuffers(1, &id);
        }

        return id;
    }
}

public static class GLC
{
    public const uint DEPTH_BUFFER_BIT = 0x00000100;
    public const uint STENCIL_BUFFER_BIT = 0x00000400;
    public const uint COLOR_BUFFER_BIT = 0x00004000;
    public const uint FALSE = 0;
    public const uint TRUE = 1;
    public const uint POINTS = 0x0000;
    public const uint LINES = 0x0001;
    public const uint LINE_LOOP = 0x0002;
    public const uint LINE_STRIP = 0x0003;
    public const uint TRIANGLES = 0x0004;
    public const uint TRIANGLE_STRIP = 0x0005;
    public const uint TRIANGLE_FAN = 0x0006;
    public const uint QUADS = 0x0007;
    public const uint NEVER = 0x0200;
    public const uint LESS = 0x0201;
    public const uint EQUAL = 0x0202;
    public const uint LEQUAL = 0x0203;
    public const uint GREATER = 0x0204;
    public const uint NOTEQUAL = 0x0205;
    public const uint GEQUAL = 0x0206;
    public const uint ALWAYS = 0x0207;
    public const uint ZERO = 0;
    public const uint ONE = 1;
    public const uint SRC_COLOR = 0x0300;
    public const uint ONE_MINUS_SRC_COLOR = 0x0301;
    public const uint SRC_ALPHA = 0x0302;
    public const uint ONE_MINUS_SRC_ALPHA = 0x0303;
    public const uint DST_ALPHA = 0x0304;
    public const uint ONE_MINUS_DST_ALPHA = 0x0305;
    public const uint DST_COLOR = 0x0306;
    public const uint ONE_MINUS_DST_COLOR = 0x0307;
    public const uint SRC_ALPHA_SATURATE = 0x0308;
    public const uint NONE = 0;
    public const uint FRONT_LEFT = 0x0400;
    public const uint FRONT_RIGHT = 0x0401;
    public const uint BACK_LEFT = 0x0402;
    public const uint BACK_RIGHT = 0x0403;
    public const uint FRONT = 0x0404;
    public const uint BACK = 0x0405;
    public const uint LEFT = 0x0406;
    public const uint RIGHT = 0x0407;
    public const uint FRONT_AND_BACK = 0x0408;
    public const uint NO_ERROR = 0;
    public const uint INVALID_ENUM = 0x0500;
    public const uint INVALID_VALUE = 0x0501;
    public const uint INVALID_OPERATION = 0x0502;
    public const uint OUT_OF_MEMORY = 0x0505;
    public const uint CW = 0x0900;
    public const uint CCW = 0x0901;
    public const uint POINT_SIZE = 0x0B11;
    public const uint POINT_SIZE_RANGE = 0x0B12;
    public const uint POINT_SIZE_GRANULARITY = 0x0B13;
    public const uint LINE_SMOOTH = 0x0B20;
    public const uint LINE_WIDTH = 0x0B21;
    public const uint LINE_WIDTH_RANGE = 0x0B22;
    public const uint LINE_WIDTH_GRANULARITY = 0x0B23;
    public const uint POLYGON_MODE = 0x0B40;
    public const uint POLYGON_SMOOTH = 0x0B41;
    public const uint CULL_FACE = 0x0B44;
    public const uint CULL_FACE_MODE = 0x0B45;
    public const uint FRONT_FACE = 0x0B46;
    public const uint DEPTH_RANGE = 0x0B70;
    public const uint DEPTH_TEST = 0x0B71;
    public const uint DEPTH_WRITEMASK = 0x0B72;
    public const uint DEPTH_CLEAR_VALUE = 0x0B73;
    public const uint DEPTH_FUNC = 0x0B74;
    public const uint STENCIL_TEST = 0x0B90;
    public const uint STENCIL_CLEAR_VALUE = 0x0B91;
    public const uint STENCIL_FUNC = 0x0B92;
    public const uint STENCIL_VALUE_MASK = 0x0B93;
    public const uint STENCIL_FAIL = 0x0B94;
    public const uint STENCIL_PASS_DEPTH_FAIL = 0x0B95;
    public const uint STENCIL_PASS_DEPTH_PASS = 0x0B96;
    public const uint STENCIL_REF = 0x0B97;
    public const uint STENCIL_WRITEMASK = 0x0B98;
    public const uint VIEWPORT = 0x0BA2;
    public const uint DITHER = 0x0BD0;
    public const uint BLEND_DST = 0x0BE0;
    public const uint BLEND_SRC = 0x0BE1;
    public const uint BLEND = 0x0BE2;
    public const uint LOGIC_OP_MODE = 0x0BF0;
    public const uint COLOR_LOGIC_OP = 0x0BF2;
    public const uint DRAW_BUFFER = 0x0C01;
    public const uint READ_BUFFER = 0x0C02;
    public const uint SCISSOR_BOX = 0x0C10;
    public const uint SCISSOR_TEST = 0x0C11;
    public const uint COLOR_CLEAR_VALUE = 0x0C22;
    public const uint COLOR_WRITEMASK = 0x0C23;
    public const uint DOUBLEBUFFER = 0x0C32;
    public const uint STEREO = 0x0C33;
    public const uint LINE_SMOOTH_HINT = 0x0C52;
    public const uint POLYGON_SMOOTH_HINT = 0x0C53;
    public const uint UNPACK_SWAP_BYTES = 0x0CF0;
    public const uint UNPACK_LSB_FIRST = 0x0CF1;
    public const uint UNPACK_ROW_LENGTH = 0x0CF2;
    public const uint UNPACK_SKIP_ROWS = 0x0CF3;
    public const uint UNPACK_SKIP_PIXELS = 0x0CF4;
    public const uint UNPACK_ALIGNMENT = 0x0CF5;
    public const uint PACK_SWAP_BYTES = 0x0D00;
    public const uint PACK_LSB_FIRST = 0x0D01;
    public const uint PACK_ROW_LENGTH = 0x0D02;
    public const uint PACK_SKIP_ROWS = 0x0D03;
    public const uint PACK_SKIP_PIXELS = 0x0D04;
    public const uint PACK_ALIGNMENT = 0x0D05;
    public const uint MAX_TEXTURE_SIZE = 0x0D33;
    public const uint MAX_VIEWPORT_DIMS = 0x0D3A;
    public const uint SUBPIXEL_BITS = 0x0D50;
    public const uint TEXTURE_1D = 0x0DE0;
    public const uint TEXTURE_2D = 0x0DE1;
    public const uint POLYGON_OFFSET_UNITS = 0x2A00;
    public const uint POLYGON_OFFSET_POINT = 0x2A01;
    public const uint POLYGON_OFFSET_LINE = 0x2A02;
    public const uint POLYGON_OFFSET_FILL = 0x8037;
    public const uint POLYGON_OFFSET_FACTOR = 0x8038;
    public const uint TEXTURE_BINDING_1D = 0x8068;
    public const uint TEXTURE_BINDING_2D = 0x8069;
    public const uint TEXTURE_WIDTH = 0x1000;
    public const uint TEXTURE_HEIGHT = 0x1001;
    public const uint TEXTURE_INTERNAL_FORMAT = 0x1003;
    public const uint TEXTURE_BORDER_COLOR = 0x1004;
    public const uint TEXTURE_RED_SIZE = 0x805C;
    public const uint TEXTURE_GREEN_SIZE = 0x805D;
    public const uint TEXTURE_BLUE_SIZE = 0x805E;
    public const uint TEXTURE_ALPHA_SIZE = 0x805F;
    public const uint DONT_CARE = 0x1100;
    public const uint FASTEST = 0x1101;
    public const uint NICEST = 0x1102;
    public const uint BYTE = 0x1400;
    public const uint UNSIGNED_BYTE = 0x1401;
    public const uint SHORT = 0x1402;
    public const uint UNSIGNED_SHORT = 0x1403;
    public const uint INT = 0x1404;
    public const uint UNSIGNED_INT = 0x1405;
    public const uint FLOAT = 0x1406;
    public const uint DOUBLE = 0x140A;
    public const uint STACK_OVERFLOW = 0x0503;
    public const uint STACK_UNDERFLOW = 0x0504;
    public const uint CLEAR = 0x1500;
    public const uint AND = 0x1501;
    public const uint AND_REVERSE = 0x1502;
    public const uint COPY = 0x1503;
    public const uint AND_INVERTED = 0x1504;
    public const uint NOOP = 0x1505;
    public const uint XOR = 0x1506;
    public const uint OR = 0x1507;
    public const uint NOR = 0x1508;
    public const uint EQUIV = 0x1509;
    public const uint INVERT = 0x150A;
    public const uint OR_REVERSE = 0x150B;
    public const uint COPY_INVERTED = 0x150C;
    public const uint OR_INVERTED = 0x150D;
    public const uint NAND = 0x150E;
    public const uint SET = 0x150F;
    public const uint TEXTURE = 0x1702;
    public const uint COLOR = 0x1800;
    public const uint DEPTH = 0x1801;
    public const uint STENCIL = 0x1802;
    public const uint STENCIL_INDEX = 0x1901;
    public const uint DEPTH_COMPONENT = 0x1902;
    public const uint RED = 0x1903;
    public const uint GREEN = 0x1904;
    public const uint BLUE = 0x1905;
    public const uint ALPHA = 0x1906;
    public const uint RGB = 0x1907;
    public const uint RGBA = 0x1908;
    public const uint POINT = 0x1B00;
    public const uint LINE = 0x1B01;
    public const uint FILL = 0x1B02;
    public const uint KEEP = 0x1E00;
    public const uint REPLACE = 0x1E01;
    public const uint INCR = 0x1E02;
    public const uint DECR = 0x1E03;
    public const uint VENDOR = 0x1F00;
    public const uint RENDERER = 0x1F01;
    public const uint VERSION = 0x1F02;
    public const uint EXTENSIONS = 0x1F03;
    public const uint NEAREST = 0x2600;
    public const uint LINEAR = 0x2601;
    public const uint NEAREST_MIPMAP_NEAREST = 0x2700;
    public const uint LINEAR_MIPMAP_NEAREST = 0x2701;
    public const uint NEAREST_MIPMAP_LINEAR = 0x2702;
    public const uint LINEAR_MIPMAP_LINEAR = 0x2703;
    public const uint TEXTURE_MAG_FILTER = 0x2800;
    public const uint TEXTURE_MIN_FILTER = 0x2801;
    public const uint TEXTURE_WRAP_S = 0x2802;
    public const uint TEXTURE_WRAP_T = 0x2803;
    public const uint PROXY_TEXTURE_1D = 0x8063;
    public const uint PROXY_TEXTURE_2D = 0x8064;
    public const uint REPEAT = 0x2901;
    public const uint R3_G3_B2 = 0x2A10;
    public const uint RGB4 = 0x804F;
    public const uint RGB5 = 0x8050;
    public const uint RGB8 = 0x8051;
    public const uint RGB10 = 0x8052;
    public const uint RGB12 = 0x8053;
    public const uint RGB16 = 0x8054;
    public const uint RGBA2 = 0x8055;
    public const uint RGBA4 = 0x8056;
    public const uint RGB5_A1 = 0x8057;
    public const uint RGBA8 = 0x8058;
    public const uint RGB10_A2 = 0x8059;
    public const uint RGBA12 = 0x805A;
    public const uint RGBA16 = 0x805B;
    public const uint CURRENT_BIT = 0x00000001;
    public const uint POINT_BIT = 0x00000002;
    public const uint LINE_BIT = 0x00000004;
    public const uint POLYGON_BIT = 0x00000008;
    public const uint POLYGON_STIPPLE_BIT = 0x00000010;
    public const uint PIXEL_MODE_BIT = 0x00000020;
    public const uint LIGHTING_BIT = 0x00000040;
    public const uint FOG_BIT = 0x00000080;
    public const uint ACCUM_BUFFER_BIT = 0x00000200;
    public const uint VIEWPORT_BIT = 0x00000800;
    public const uint TRANSFORM_BIT = 0x00001000;
    public const uint ENABLE_BIT = 0x00002000;
    public const uint HINT_BIT = 0x00008000;
    public const uint EVAL_BIT = 0x00010000;
    public const uint LIST_BIT = 0x00020000;
    public const uint TEXTURE_BIT = 0x00040000;
    public const uint SCISSOR_BIT = 0x00080000;
    public const uint ALL_ATTRIB_BITS = 0xFFFFFFFF;
    public const uint CLIENT_PIXEL_STORE_BIT = 0x00000001;
    public const uint CLIENT_VERTEX_ARRAY_BIT = 0x00000002;
    public const uint CLIENT_ALL_ATTRIB_BITS = 0xFFFFFFFF;
    public const uint QUAD_STRIP = 0x0008;
    public const uint POLYGON = 0x0009;
    public const uint ACCUM = 0x0100;
    public const uint LOAD = 0x0101;
    public const uint RETURN = 0x0102;
    public const uint MULT = 0x0103;
    public const uint ADD = 0x0104;
    public const uint AUX0 = 0x0409;
    public const uint AUX1 = 0x040A;
    public const uint AUX2 = 0x040B;
    public const uint AUX3 = 0x040C;
    public const uint _2D = 0x0600;
    public const uint _3D = 0x0601;
    public const uint _3D_COLOR = 0x0602;
    public const uint _3D_COLOR_TEXTURE = 0x0603;
    public const uint _4D_COLOR_TEXTURE = 0x0604;
    public const uint PASS_THROUGH_TOKEN = 0x0700;
    public const uint POINT_TOKEN = 0x0701;
    public const uint LINE_TOKEN = 0x0702;
    public const uint POLYGON_TOKEN = 0x0703;
    public const uint BITMAP_TOKEN = 0x0704;
    public const uint DRAW_PIXEL_TOKEN = 0x0705;
    public const uint COPY_PIXEL_TOKEN = 0x0706;
    public const uint LINE_RESET_TOKEN = 0x0707;
    public const uint EXP = 0x0800;
    public const uint EXP2 = 0x0801;
    public const uint COEFF = 0x0A00;
    public const uint ORDER = 0x0A01;
    public const uint DOMAIN = 0x0A02;
    public const uint PIXEL_MAP_I_TO_I = 0x0C70;
    public const uint PIXEL_MAP_S_TO_S = 0x0C71;
    public const uint PIXEL_MAP_I_TO_R = 0x0C72;
    public const uint PIXEL_MAP_I_TO_G = 0x0C73;
    public const uint PIXEL_MAP_I_TO_B = 0x0C74;
    public const uint PIXEL_MAP_I_TO_A = 0x0C75;
    public const uint PIXEL_MAP_R_TO_R = 0x0C76;
    public const uint PIXEL_MAP_G_TO_G = 0x0C77;
    public const uint PIXEL_MAP_B_TO_B = 0x0C78;
    public const uint PIXEL_MAP_A_TO_A = 0x0C79;
    public const uint VERTEX_ARRAY_POINTER = 0x808E;
    public const uint NORMAL_ARRAY_POINTER = 0x808F;
    public const uint COLOR_ARRAY_POINTER = 0x8090;
    public const uint INDEX_ARRAY_POINTER = 0x8091;
    public const uint TEXTURE_COORD_ARRAY_POINTER = 0x8092;
    public const uint EDGE_FLAG_ARRAY_POINTER = 0x8093;
    public const uint FEEDBACK_BUFFER_POINTER = 0x0DF0;
    public const uint SELECTION_BUFFER_POINTER = 0x0DF3;
    public const uint CURRENT_COLOR = 0x0B00;
    public const uint CURRENT_INDEX = 0x0B01;
    public const uint CURRENT_NORMAL = 0x0B02;
    public const uint CURRENT_TEXTURE_COORDS = 0x0B03;
    public const uint CURRENT_RASTER_COLOR = 0x0B04;
    public const uint CURRENT_RASTER_INDEX = 0x0B05;
    public const uint CURRENT_RASTER_TEXTURE_COORDS = 0x0B06;
    public const uint CURRENT_RASTER_POSITION = 0x0B07;
    public const uint CURRENT_RASTER_POSITION_VALID = 0x0B08;
    public const uint CURRENT_RASTER_DISTANCE = 0x0B09;
    public const uint POINT_SMOOTH = 0x0B10;
    public const uint LINE_STIPPLE = 0x0B24;
    public const uint LINE_STIPPLE_PATTERN = 0x0B25;
    public const uint LINE_STIPPLE_REPEAT = 0x0B26;
    public const uint LIST_MODE = 0x0B30;
    public const uint MAX_LIST_NESTING = 0x0B31;
    public const uint LIST_BASE = 0x0B32;
    public const uint LIST_INDEX = 0x0B33;
    public const uint POLYGON_STIPPLE = 0x0B42;
    public const uint EDGE_FLAG = 0x0B43;
    public const uint LIGHTING = 0x0B50;
    public const uint LIGHT_MODEL_LOCAL_VIEWER = 0x0B51;
    public const uint LIGHT_MODEL_TWO_SIDE = 0x0B52;
    public const uint LIGHT_MODEL_AMBIENT = 0x0B53;
    public const uint SHADE_MODEL = 0x0B54;
    public const uint COLOR_MATERIAL_FACE = 0x0B55;
    public const uint COLOR_MATERIAL_PARAMETER = 0x0B56;
    public const uint COLOR_MATERIAL = 0x0B57;
    public const uint FOG = 0x0B60;
    public const uint FOG_INDEX = 0x0B61;
    public const uint FOG_DENSITY = 0x0B62;
    public const uint FOG_START = 0x0B63;
    public const uint FOG_END = 0x0B64;
    public const uint FOG_MODE = 0x0B65;
    public const uint FOG_COLOR = 0x0B66;
    public const uint ACCUM_CLEAR_VALUE = 0x0B80;
    public const uint MATRIX_MODE = 0x0BA0;
    public const uint NORMALIZE = 0x0BA1;
    public const uint MODELVIEW_STACK_DEPTH = 0x0BA3;
    public const uint PROJECTION_STACK_DEPTH = 0x0BA4;
    public const uint TEXTURE_STACK_DEPTH = 0x0BA5;
    public const uint MODELVIEW_MATRIX = 0x0BA6;
    public const uint PROJECTION_MATRIX = 0x0BA7;
    public const uint TEXTURE_MATRIX = 0x0BA8;
    public const uint ATTRIB_STACK_DEPTH = 0x0BB0;
    public const uint CLIENT_ATTRIB_STACK_DEPTH = 0x0BB1;
    public const uint ALPHA_TEST = 0x0BC0;
    public const uint ALPHA_TEST_FUNC = 0x0BC1;
    public const uint ALPHA_TEST_REF = 0x0BC2;
    public const uint INDEX_LOGIC_OP = 0x0BF1;
    public const uint LOGIC_OP = 0x0BF1;
    public const uint AUX_BUFFERS = 0x0C00;
    public const uint INDEX_CLEAR_VALUE = 0x0C20;
    public const uint INDEX_WRITEMASK = 0x0C21;
    public const uint INDEX_MODE = 0x0C30;
    public const uint RGBA_MODE = 0x0C31;
    public const uint RENDER_MODE = 0x0C40;
    public const uint PERSPECTIVE_CORRECTION_HINT = 0x0C50;
    public const uint POINT_SMOOTH_HINT = 0x0C51;
    public const uint FOG_HINT = 0x0C54;
    public const uint TEXTURE_GEN_S = 0x0C60;
    public const uint TEXTURE_GEN_T = 0x0C61;
    public const uint TEXTURE_GEN_R = 0x0C62;
    public const uint TEXTURE_GEN_Q = 0x0C63;
    public const uint PIXEL_MAP_I_TO_I_SIZE = 0x0CB0;
    public const uint PIXEL_MAP_S_TO_S_SIZE = 0x0CB1;
    public const uint PIXEL_MAP_I_TO_R_SIZE = 0x0CB2;
    public const uint PIXEL_MAP_I_TO_G_SIZE = 0x0CB3;
    public const uint PIXEL_MAP_I_TO_B_SIZE = 0x0CB4;
    public const uint PIXEL_MAP_I_TO_A_SIZE = 0x0CB5;
    public const uint PIXEL_MAP_R_TO_R_SIZE = 0x0CB6;
    public const uint PIXEL_MAP_G_TO_G_SIZE = 0x0CB7;
    public const uint PIXEL_MAP_B_TO_B_SIZE = 0x0CB8;
    public const uint PIXEL_MAP_A_TO_A_SIZE = 0x0CB9;
    public const uint MAP_COLOR = 0x0D10;
    public const uint MAP_STENCIL = 0x0D11;
    public const uint INDEX_SHIFT = 0x0D12;
    public const uint INDEX_OFFSET = 0x0D13;
    public const uint RED_SCALE = 0x0D14;
    public const uint RED_BIAS = 0x0D15;
    public const uint ZOOM_X = 0x0D16;
    public const uint ZOOM_Y = 0x0D17;
    public const uint GREEN_SCALE = 0x0D18;
    public const uint GREEN_BIAS = 0x0D19;
    public const uint BLUE_SCALE = 0x0D1A;
    public const uint BLUE_BIAS = 0x0D1B;
    public const uint ALPHA_SCALE = 0x0D1C;
    public const uint ALPHA_BIAS = 0x0D1D;
    public const uint DEPTH_SCALE = 0x0D1E;
    public const uint DEPTH_BIAS = 0x0D1F;
    public const uint MAX_EVAL_ORDER = 0x0D30;
    public const uint MAX_LIGHTS = 0x0D31;
    public const uint MAX_CLIP_PLANES = 0x0D32;
    public const uint MAX_PIXEL_MAP_TABLE = 0x0D34;
    public const uint MAX_ATTRIB_STACK_DEPTH = 0x0D35;
    public const uint MAX_MODELVIEW_STACK_DEPTH = 0x0D36;
    public const uint MAX_NAME_STACK_DEPTH = 0x0D37;
    public const uint MAX_PROJECTION_STACK_DEPTH = 0x0D38;
    public const uint MAX_TEXTURE_STACK_DEPTH = 0x0D39;
    public const uint MAX_CLIENT_ATTRIB_STACK_DEPTH = 0x0D3B;
    public const uint INDEX_BITS = 0x0D51;
    public const uint RED_BITS = 0x0D52;
    public const uint GREEN_BITS = 0x0D53;
    public const uint BLUE_BITS = 0x0D54;
    public const uint ALPHA_BITS = 0x0D55;
    public const uint DEPTH_BITS = 0x0D56;
    public const uint STENCIL_BITS = 0x0D57;
    public const uint ACCUM_RED_BITS = 0x0D58;
    public const uint ACCUM_GREEN_BITS = 0x0D59;
    public const uint ACCUM_BLUE_BITS = 0x0D5A;
    public const uint ACCUM_ALPHA_BITS = 0x0D5B;
    public const uint NAME_STACK_DEPTH = 0x0D70;
    public const uint AUTO_NORMAL = 0x0D80;
    public const uint MAP1_COLOR_4 = 0x0D90;
    public const uint MAP1_INDEX = 0x0D91;
    public const uint MAP1_NORMAL = 0x0D92;
    public const uint MAP1_TEXTURE_COORD_1 = 0x0D93;
    public const uint MAP1_TEXTURE_COORD_2 = 0x0D94;
    public const uint MAP1_TEXTURE_COORD_3 = 0x0D95;
    public const uint MAP1_TEXTURE_COORD_4 = 0x0D96;
    public const uint MAP1_VERTEX_3 = 0x0D97;
    public const uint MAP1_VERTEX_4 = 0x0D98;
    public const uint MAP2_COLOR_4 = 0x0DB0;
    public const uint MAP2_INDEX = 0x0DB1;
    public const uint MAP2_NORMAL = 0x0DB2;
    public const uint MAP2_TEXTURE_COORD_1 = 0x0DB3;
    public const uint MAP2_TEXTURE_COORD_2 = 0x0DB4;
    public const uint MAP2_TEXTURE_COORD_3 = 0x0DB5;
    public const uint MAP2_TEXTURE_COORD_4 = 0x0DB6;
    public const uint MAP2_VERTEX_3 = 0x0DB7;
    public const uint MAP2_VERTEX_4 = 0x0DB8;
    public const uint MAP1_GRID_DOMAIN = 0x0DD0;
    public const uint MAP1_GRID_SEGMENTS = 0x0DD1;
    public const uint MAP2_GRID_DOMAIN = 0x0DD2;
    public const uint MAP2_GRID_SEGMENTS = 0x0DD3;
    public const uint FEEDBACK_BUFFER_SIZE = 0x0DF1;
    public const uint FEEDBACK_BUFFER_TYPE = 0x0DF2;
    public const uint SELECTION_BUFFER_SIZE = 0x0DF4;
    public const uint VERTEX_ARRAY = 0x8074;
    public const uint NORMAL_ARRAY = 0x8075;
    public const uint COLOR_ARRAY = 0x8076;
    public const uint INDEX_ARRAY = 0x8077;
    public const uint TEXTURE_COORD_ARRAY = 0x8078;
    public const uint EDGE_FLAG_ARRAY = 0x8079;
    public const uint VERTEX_ARRAY_SIZE = 0x807A;
    public const uint VERTEX_ARRAY_TYPE = 0x807B;
    public const uint VERTEX_ARRAY_STRIDE = 0x807C;
    public const uint NORMAL_ARRAY_TYPE = 0x807E;
    public const uint NORMAL_ARRAY_STRIDE = 0x807F;
    public const uint COLOR_ARRAY_SIZE = 0x8081;
    public const uint COLOR_ARRAY_TYPE = 0x8082;
    public const uint COLOR_ARRAY_STRIDE = 0x8083;
    public const uint INDEX_ARRAY_TYPE = 0x8085;
    public const uint INDEX_ARRAY_STRIDE = 0x8086;
    public const uint TEXTURE_COORD_ARRAY_SIZE = 0x8088;
    public const uint TEXTURE_COORD_ARRAY_TYPE = 0x8089;
    public const uint TEXTURE_COORD_ARRAY_STRIDE = 0x808A;
    public const uint EDGE_FLAG_ARRAY_STRIDE = 0x808C;
    public const uint TEXTURE_COMPONENTS = 0x1003;
    public const uint TEXTURE_BORDER = 0x1005;
    public const uint TEXTURE_LUMINANCE_SIZE = 0x8060;
    public const uint TEXTURE_INTENSITY_SIZE = 0x8061;
    public const uint TEXTURE_PRIORITY = 0x8066;
    public const uint TEXTURE_RESIDENT = 0x8067;
    public const uint AMBIENT = 0x1200;
    public const uint DIFFUSE = 0x1201;
    public const uint SPECULAR = 0x1202;
    public const uint POSITION = 0x1203;
    public const uint SPOT_DIRECTION = 0x1204;
    public const uint SPOT_EXPONENT = 0x1205;
    public const uint SPOT_CUTOFF = 0x1206;
    public const uint CONSTANT_ATTENUATION = 0x1207;
    public const uint LINEAR_ATTENUATION = 0x1208;
    public const uint QUADRATIC_ATTENUATION = 0x1209;
    public const uint COMPILE = 0x1300;
    public const uint COMPILE_AND_EXECUTE = 0x1301;
    public const uint _2_BYTES = 0x1407;
    public const uint _3_BYTES = 0x1408;
    public const uint _4_BYTES = 0x1409;
    public const uint EMISSION = 0x1600;
    public const uint SHININESS = 0x1601;
    public const uint AMBIENT_AND_DIFFUSE = 0x1602;
    public const uint COLOR_INDEXES = 0x1603;
    public const uint MODELVIEW = 0x1700;
    public const uint PROJECTION = 0x1701;
    public const uint COLOR_INDEX = 0x1900;
    public const uint LUMINANCE = 0x1909;
    public const uint LUMINANCE_ALPHA = 0x190A;
    public const uint BITMAP = 0x1A00;
    public const uint RENDER = 0x1C00;
    public const uint FEEDBACK = 0x1C01;
    public const uint SELECT = 0x1C02;
    public const uint FLAT = 0x1D00;
    public const uint SMOOTH = 0x1D01;
    public const uint S = 0x2000;
    public const uint T = 0x2001;
    public const uint R = 0x2002;
    public const uint Q = 0x2003;
    public const uint MODULATE = 0x2100;
    public const uint DECAL = 0x2101;
    public const uint TEXTURE_ENV_MODE = 0x2200;
    public const uint TEXTURE_ENV_COLOR = 0x2201;
    public const uint TEXTURE_ENV = 0x2300;
    public const uint EYE_LINEAR = 0x2400;
    public const uint OBJECT_LINEAR = 0x2401;
    public const uint SPHERE_MAP = 0x2402;
    public const uint TEXTURE_GEN_MODE = 0x2500;
    public const uint OBJECT_PLANE = 0x2501;
    public const uint EYE_PLANE = 0x2502;
    public const uint CLAMP = 0x2900;
    public const uint ALPHA4 = 0x803B;
    public const uint ALPHA8 = 0x803C;
    public const uint ALPHA12 = 0x803D;
    public const uint ALPHA16 = 0x803E;
    public const uint LUMINANCE4 = 0x803F;
    public const uint LUMINANCE8 = 0x8040;
    public const uint LUMINANCE12 = 0x8041;
    public const uint LUMINANCE16 = 0x8042;
    public const uint LUMINANCE4_ALPHA4 = 0x8043;
    public const uint LUMINANCE6_ALPHA2 = 0x8044;
    public const uint LUMINANCE8_ALPHA8 = 0x8045;
    public const uint LUMINANCE12_ALPHA4 = 0x8046;
    public const uint LUMINANCE12_ALPHA12 = 0x8047;
    public const uint LUMINANCE16_ALPHA16 = 0x8048;
    public const uint INTENSITY = 0x8049;
    public const uint INTENSITY4 = 0x804A;
    public const uint INTENSITY8 = 0x804B;
    public const uint INTENSITY12 = 0x804C;
    public const uint INTENSITY16 = 0x804D;
    public const uint V2F = 0x2A20;
    public const uint V3F = 0x2A21;
    public const uint C4UB_V2F = 0x2A22;
    public const uint C4UB_V3F = 0x2A23;
    public const uint C3F_V3F = 0x2A24;
    public const uint N3F_V3F = 0x2A25;
    public const uint C4F_N3F_V3F = 0x2A26;
    public const uint T2F_V3F = 0x2A27;
    public const uint T4F_V4F = 0x2A28;
    public const uint T2F_C4UB_V3F = 0x2A29;
    public const uint T2F_C3F_V3F = 0x2A2A;
    public const uint T2F_N3F_V3F = 0x2A2B;
    public const uint T2F_C4F_N3F_V3F = 0x2A2C;
    public const uint T4F_C4F_N3F_V4F = 0x2A2D;
    public const uint CLIP_PLANE0 = 0x3000;
    public const uint CLIP_PLANE1 = 0x3001;
    public const uint CLIP_PLANE2 = 0x3002;
    public const uint CLIP_PLANE3 = 0x3003;
    public const uint CLIP_PLANE4 = 0x3004;
    public const uint CLIP_PLANE5 = 0x3005;
    public const uint LIGHT0 = 0x4000;
    public const uint LIGHT1 = 0x4001;
    public const uint LIGHT2 = 0x4002;
    public const uint LIGHT3 = 0x4003;
    public const uint LIGHT4 = 0x4004;
    public const uint LIGHT5 = 0x4005;
    public const uint LIGHT6 = 0x4006;
    public const uint LIGHT7 = 0x4007;
    public const uint UNSIGNED_BYTE_3_3_2 = 0x8032;
    public const uint UNSIGNED_SHORT_4_4_4_4 = 0x8033;
    public const uint UNSIGNED_SHORT_5_5_5_1 = 0x8034;
    public const uint UNSIGNED_INT_8_8_8_8 = 0x8035;
    public const uint UNSIGNED_INT_10_10_10_2 = 0x8036;
    public const uint TEXTURE_BINDING_3D = 0x806A;
    public const uint PACK_SKIP_IMAGES = 0x806B;
    public const uint PACK_IMAGE_HEIGHT = 0x806C;
    public const uint UNPACK_SKIP_IMAGES = 0x806D;
    public const uint UNPACK_IMAGE_HEIGHT = 0x806E;
    public const uint TEXTURE_3D = 0x806F;
    public const uint PROXY_TEXTURE_3D = 0x8070;
    public const uint TEXTURE_DEPTH = 0x8071;
    public const uint TEXTURE_WRAP_R = 0x8072;
    public const uint MAX_3D_TEXTURE_SIZE = 0x8073;
    public const uint UNSIGNED_BYTE_2_3_3_REV = 0x8362;
    public const uint UNSIGNED_SHORT_5_6_5 = 0x8363;
    public const uint UNSIGNED_SHORT_5_6_5_REV = 0x8364;
    public const uint UNSIGNED_SHORT_4_4_4_4_REV = 0x8365;
    public const uint UNSIGNED_SHORT_1_5_5_5_REV = 0x8366;
    public const uint UNSIGNED_INT_8_8_8_8_REV = 0x8367;
    public const uint UNSIGNED_INT_2_10_10_10_REV = 0x8368;
    public const uint BGR = 0x80E0;
    public const uint BGRA = 0x80E1;
    public const uint MAX_ELEMENTS_VERTICES = 0x80E8;
    public const uint MAX_ELEMENTS_INDICES = 0x80E9;
    public const uint CLAMP_TO_EDGE = 0x812F;
    public const uint TEXTURE_MIN_LOD = 0x813A;
    public const uint TEXTURE_MAX_LOD = 0x813B;
    public const uint TEXTURE_BASE_LEVEL = 0x813C;
    public const uint TEXTURE_MAX_LEVEL = 0x813D;
    public const uint SMOOTH_POINT_SIZE_RANGE = 0x0B12;
    public const uint SMOOTH_POINT_SIZE_GRANULARITY = 0x0B13;
    public const uint SMOOTH_LINE_WIDTH_RANGE = 0x0B22;
    public const uint SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23;
    public const uint ALIASED_LINE_WIDTH_RANGE = 0x846E;
    public const uint RESCALE_NORMAL = 0x803A;
    public const uint LIGHT_MODEL_COLOR_CONTROL = 0x81F8;
    public const uint SINGLE_COLOR = 0x81F9;
    public const uint SEPARATE_SPECULAR_COLOR = 0x81FA;
    public const uint ALIASED_POINT_SIZE_RANGE = 0x846D;
    public const uint TEXTURE0 = 0x84C0;
    public const uint TEXTURE1 = 0x84C1;
    public const uint TEXTURE2 = 0x84C2;
    public const uint TEXTURE3 = 0x84C3;
    public const uint TEXTURE4 = 0x84C4;
    public const uint TEXTURE5 = 0x84C5;
    public const uint TEXTURE6 = 0x84C6;
    public const uint TEXTURE7 = 0x84C7;
    public const uint TEXTURE8 = 0x84C8;
    public const uint TEXTURE9 = 0x84C9;
    public const uint TEXTURE10 = 0x84CA;
    public const uint TEXTURE11 = 0x84CB;
    public const uint TEXTURE12 = 0x84CC;
    public const uint TEXTURE13 = 0x84CD;
    public const uint TEXTURE14 = 0x84CE;
    public const uint TEXTURE15 = 0x84CF;
    public const uint TEXTURE16 = 0x84D0;
    public const uint TEXTURE17 = 0x84D1;
    public const uint TEXTURE18 = 0x84D2;
    public const uint TEXTURE19 = 0x84D3;
    public const uint TEXTURE20 = 0x84D4;
    public const uint TEXTURE21 = 0x84D5;
    public const uint TEXTURE22 = 0x84D6;
    public const uint TEXTURE23 = 0x84D7;
    public const uint TEXTURE24 = 0x84D8;
    public const uint TEXTURE25 = 0x84D9;
    public const uint TEXTURE26 = 0x84DA;
    public const uint TEXTURE27 = 0x84DB;
    public const uint TEXTURE28 = 0x84DC;
    public const uint TEXTURE29 = 0x84DD;
    public const uint TEXTURE30 = 0x84DE;
    public const uint TEXTURE31 = 0x84DF;
    public const uint ACTIVE_TEXTURE = 0x84E0;
    public const uint MULTISAMPLE = 0x809D;
    public const uint SAMPLE_ALPHA_TO_COVERAGE = 0x809E;
    public const uint SAMPLE_ALPHA_TO_ONE = 0x809F;
    public const uint SAMPLE_COVERAGE = 0x80A0;
    public const uint SAMPLE_BUFFERS = 0x80A8;
    public const uint SAMPLES = 0x80A9;
    public const uint SAMPLE_COVERAGE_VALUE = 0x80AA;
    public const uint SAMPLE_COVERAGE_INVERT = 0x80AB;
    public const uint TEXTURE_CUBE_MAP = 0x8513;
    public const uint TEXTURE_BINDING_CUBE_MAP = 0x8514;
    public const uint TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
    public const uint TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
    public const uint TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
    public const uint TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
    public const uint TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
    public const uint TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;
    public const uint PROXY_TEXTURE_CUBE_MAP = 0x851B;
    public const uint MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C;
    public const uint COMPRESSED_RGB = 0x84ED;
    public const uint COMPRESSED_RGBA = 0x84EE;
    public const uint TEXTURE_COMPRESSION_HINT = 0x84EF;
    public const uint TEXTURE_COMPRESSED_IMAGE_SIZE = 0x86A0;
    public const uint TEXTURE_COMPRESSED = 0x86A1;
    public const uint NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2;
    public const uint COMPRESSED_TEXTURE_FORMATS = 0x86A3;
    public const uint CLAMP_TO_BORDER = 0x812D;
    public const uint CLIENT_ACTIVE_TEXTURE = 0x84E1;
    public const uint MAX_TEXTURE_UNITS = 0x84E2;
    public const uint TRANSPOSE_MODELVIEW_MATRIX = 0x84E3;
    public const uint TRANSPOSE_PROJECTION_MATRIX = 0x84E4;
    public const uint TRANSPOSE_TEXTURE_MATRIX = 0x84E5;
    public const uint TRANSPOSE_COLOR_MATRIX = 0x84E6;
    public const uint MULTISAMPLE_BIT = 0x20000000;
    public const uint NORMAL_MAP = 0x8511;
    public const uint REFLECTION_MAP = 0x8512;
    public const uint COMPRESSED_ALPHA = 0x84E9;
    public const uint COMPRESSED_LUMINANCE = 0x84EA;
    public const uint COMPRESSED_LUMINANCE_ALPHA = 0x84EB;
    public const uint COMPRESSED_INTENSITY = 0x84EC;
    public const uint COMBINE = 0x8570;
    public const uint COMBINE_RGB = 0x8571;
    public const uint COMBINE_ALPHA = 0x8572;
    public const uint SOURCE0_RGB = 0x8580;
    public const uint SOURCE1_RGB = 0x8581;
    public const uint SOURCE2_RGB = 0x8582;
    public const uint SOURCE0_ALPHA = 0x8588;
    public const uint SOURCE1_ALPHA = 0x8589;
    public const uint SOURCE2_ALPHA = 0x858A;
    public const uint OPERAND0_RGB = 0x8590;
    public const uint OPERAND1_RGB = 0x8591;
    public const uint OPERAND2_RGB = 0x8592;
    public const uint OPERAND0_ALPHA = 0x8598;
    public const uint OPERAND1_ALPHA = 0x8599;
    public const uint OPERAND2_ALPHA = 0x859A;
    public const uint RGB_SCALE = 0x8573;
    public const uint ADD_SIGNED = 0x8574;
    public const uint INTERPOLATE = 0x8575;
    public const uint SUBTRACT = 0x84E7;
    public const uint CONSTANT = 0x8576;
    public const uint PRIMARY_COLOR = 0x8577;
    public const uint PREVIOUS = 0x8578;
    public const uint DOT3_RGB = 0x86AE;
    public const uint DOT3_RGBA = 0x86AF;
    public const uint BLEND_DST_RGB = 0x80C8;
    public const uint BLEND_SRC_RGB = 0x80C9;
    public const uint BLEND_DST_ALPHA = 0x80CA;
    public const uint BLEND_SRC_ALPHA = 0x80CB;
    public const uint POINT_FADE_THRESHOLD_SIZE = 0x8128;
    public const uint DEPTH_COMPONENT16 = 0x81A5;
    public const uint DEPTH_COMPONENT24 = 0x81A6;
    public const uint DEPTH_COMPONENT32 = 0x81A7;
    public const uint MIRRORED_REPEAT = 0x8370;
    public const uint MAX_TEXTURE_LOD_BIAS = 0x84FD;
    public const uint TEXTURE_LOD_BIAS = 0x8501;
    public const uint INCR_WRAP = 0x8507;
    public const uint DECR_WRAP = 0x8508;
    public const uint TEXTURE_DEPTH_SIZE = 0x884A;
    public const uint TEXTURE_COMPARE_MODE = 0x884C;
    public const uint TEXTURE_COMPARE_FUNC = 0x884D;
    public const uint POINT_SIZE_MIN = 0x8126;
    public const uint POINT_SIZE_MAX = 0x8127;
    public const uint POINT_DISTANCE_ATTENUATION = 0x8129;
    public const uint GENERATE_MIPMAP = 0x8191;
    public const uint GENERATE_MIPMAP_HINT = 0x8192;
    public const uint FOG_COORDINATE_SOURCE = 0x8450;
    public const uint FOG_COORDINATE = 0x8451;
    public const uint FRAGMENT_DEPTH = 0x8452;
    public const uint CURRENT_FOG_COORDINATE = 0x8453;
    public const uint FOG_COORDINATE_ARRAY_TYPE = 0x8454;
    public const uint FOG_COORDINATE_ARRAY_STRIDE = 0x8455;
    public const uint FOG_COORDINATE_ARRAY_POINTER = 0x8456;
    public const uint FOG_COORDINATE_ARRAY = 0x8457;
    public const uint COLOR_SUM = 0x8458;
    public const uint CURRENT_SECONDARY_COLOR = 0x8459;
    public const uint SECONDARY_COLOR_ARRAY_SIZE = 0x845A;
    public const uint SECONDARY_COLOR_ARRAY_TYPE = 0x845B;
    public const uint SECONDARY_COLOR_ARRAY_STRIDE = 0x845C;
    public const uint SECONDARY_COLOR_ARRAY_POINTER = 0x845D;
    public const uint SECONDARY_COLOR_ARRAY = 0x845E;
    public const uint TEXTURE_FILTER_CONTROL = 0x8500;
    public const uint DEPTH_TEXTURE_MODE = 0x884B;
    public const uint COMPARE_R_TO_TEXTURE = 0x884E;
    public const uint FUNC_ADD = 0x8006;
    public const uint FUNC_SUBTRACT = 0x800A;
    public const uint FUNC_REVERSE_SUBTRACT = 0x800B;
    public const uint MIN = 0x8007;
    public const uint MAX = 0x8008;
    public const uint CONSTANT_COLOR = 0x8001;
    public const uint ONE_MINUS_CONSTANT_COLOR = 0x8002;
    public const uint CONSTANT_ALPHA = 0x8003;
    public const uint ONE_MINUS_CONSTANT_ALPHA = 0x8004;
    public const uint BUFFER_SIZE = 0x8764;
    public const uint BUFFER_USAGE = 0x8765;
    public const uint QUERY_COUNTER_BITS = 0x8864;
    public const uint CURRENT_QUERY = 0x8865;
    public const uint QUERY_RESULT = 0x8866;
    public const uint QUERY_RESULT_AVAILABLE = 0x8867;
    public const uint ARRAY_BUFFER = 0x8892;
    public const uint ELEMENT_ARRAY_BUFFER = 0x8893;
    public const uint ARRAY_BUFFER_BINDING = 0x8894;
    public const uint ELEMENT_ARRAY_BUFFER_BINDING = 0x8895;
    public const uint VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F;
    public const uint READ_ONLY = 0x88B8;
    public const uint WRITE_ONLY = 0x88B9;
    public const uint READ_WRITE = 0x88BA;
    public const uint BUFFER_ACCESS = 0x88BB;
    public const uint BUFFER_MAPPED = 0x88BC;
    public const uint BUFFER_MAP_POINTER = 0x88BD;
    public const uint STREAM_DRAW = 0x88E0;
    public const uint STREAM_READ = 0x88E1;
    public const uint STREAM_COPY = 0x88E2;
    public const uint STATIC_DRAW = 0x88E4;
    public const uint STATIC_READ = 0x88E5;
    public const uint STATIC_COPY = 0x88E6;
    public const uint DYNAMIC_DRAW = 0x88E8;
    public const uint DYNAMIC_READ = 0x88E9;
    public const uint DYNAMIC_COPY = 0x88EA;
    public const uint SAMPLES_PASSED = 0x8914;
    public const uint SRC1_ALPHA = 0x8589;
    public const uint VERTEX_ARRAY_BUFFER_BINDING = 0x8896;
    public const uint NORMAL_ARRAY_BUFFER_BINDING = 0x8897;
    public const uint COLOR_ARRAY_BUFFER_BINDING = 0x8898;
    public const uint INDEX_ARRAY_BUFFER_BINDING = 0x8899;
    public const uint TEXTURE_COORD_ARRAY_BUFFER_BINDING = 0x889A;
    public const uint EDGE_FLAG_ARRAY_BUFFER_BINDING = 0x889B;
    public const uint SECONDARY_COLOR_ARRAY_BUFFER_BINDING = 0x889C;
    public const uint FOG_COORDINATE_ARRAY_BUFFER_BINDING = 0x889D;
    public const uint WEIGHT_ARRAY_BUFFER_BINDING = 0x889E;
    public const uint FOG_COORD_SRC = 0x8450;
    public const uint FOG_COORD = 0x8451;
    public const uint CURRENT_FOG_COORD = 0x8453;
    public const uint FOG_COORD_ARRAY_TYPE = 0x8454;
    public const uint FOG_COORD_ARRAY_STRIDE = 0x8455;
    public const uint FOG_COORD_ARRAY_POINTER = 0x8456;
    public const uint FOG_COORD_ARRAY = 0x8457;
    public const uint FOG_COORD_ARRAY_BUFFER_BINDING = 0x889D;
    public const uint SRC0_RGB = 0x8580;
    public const uint SRC1_RGB = 0x8581;
    public const uint SRC2_RGB = 0x8582;
    public const uint SRC0_ALPHA = 0x8588;
    public const uint SRC2_ALPHA = 0x858A;
    public const uint BLEND_EQUATION_RGB = 0x8009;
    public const uint VERTEX_ATTRIB_ARRAY_ENABLED = 0x8622;
    public const uint VERTEX_ATTRIB_ARRAY_SIZE = 0x8623;
    public const uint VERTEX_ATTRIB_ARRAY_STRIDE = 0x8624;
    public const uint VERTEX_ATTRIB_ARRAY_TYPE = 0x8625;
    public const uint CURRENT_VERTEX_ATTRIB = 0x8626;
    public const uint VERTEX_PROGRAM_POINT_SIZE = 0x8642;
    public const uint VERTEX_ATTRIB_ARRAY_POINTER = 0x8645;
    public const uint STENCIL_BACK_FUNC = 0x8800;
    public const uint STENCIL_BACK_FAIL = 0x8801;
    public const uint STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802;
    public const uint STENCIL_BACK_PASS_DEPTH_PASS = 0x8803;
    public const uint MAX_DRAW_BUFFERS = 0x8824;
    public const uint DRAW_BUFFER0 = 0x8825;
    public const uint DRAW_BUFFER1 = 0x8826;
    public const uint DRAW_BUFFER2 = 0x8827;
    public const uint DRAW_BUFFER3 = 0x8828;
    public const uint DRAW_BUFFER4 = 0x8829;
    public const uint DRAW_BUFFER5 = 0x882A;
    public const uint DRAW_BUFFER6 = 0x882B;
    public const uint DRAW_BUFFER7 = 0x882C;
    public const uint DRAW_BUFFER8 = 0x882D;
    public const uint DRAW_BUFFER9 = 0x882E;
    public const uint DRAW_BUFFER10 = 0x882F;
    public const uint DRAW_BUFFER11 = 0x8830;
    public const uint DRAW_BUFFER12 = 0x8831;
    public const uint DRAW_BUFFER13 = 0x8832;
    public const uint DRAW_BUFFER14 = 0x8833;
    public const uint DRAW_BUFFER15 = 0x8834;
    public const uint BLEND_EQUATION_ALPHA = 0x883D;
    public const uint MAX_VERTEX_ATTRIBS = 0x8869;
    public const uint VERTEX_ATTRIB_ARRAY_NORMALIZED = 0x886A;
    public const uint MAX_TEXTURE_IMAGE_UNITS = 0x8872;
    public const uint FRAGMENT_SHADER = 0x8B30;
    public const uint VERTEX_SHADER = 0x8B31;
    public const uint MAX_FRAGMENT_UNIFORM_COMPONENTS = 0x8B49;
    public const uint MAX_VERTEX_UNIFORM_COMPONENTS = 0x8B4A;
    public const uint MAX_VARYING_FLOATS = 0x8B4B;
    public const uint MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C;
    public const uint MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D;
    public const uint SHADER_TYPE = 0x8B4F;
    public const uint FLOAT_VEC2 = 0x8B50;
    public const uint FLOAT_VEC3 = 0x8B51;
    public const uint FLOAT_VEC4 = 0x8B52;
    public const uint INT_VEC2 = 0x8B53;
    public const uint INT_VEC3 = 0x8B54;
    public const uint INT_VEC4 = 0x8B55;
    public const uint BOOL = 0x8B56;
    public const uint BOOL_VEC2 = 0x8B57;
    public const uint BOOL_VEC3 = 0x8B58;
    public const uint BOOL_VEC4 = 0x8B59;
    public const uint FLOAT_MAT2 = 0x8B5A;
    public const uint FLOAT_MAT3 = 0x8B5B;
    public const uint FLOAT_MAT4 = 0x8B5C;
    public const uint SAMPLER_1D = 0x8B5D;
    public const uint SAMPLER_2D = 0x8B5E;
    public const uint SAMPLER_3D = 0x8B5F;
    public const uint SAMPLER_CUBE = 0x8B60;
    public const uint SAMPLER_1D_SHADOW = 0x8B61;
    public const uint SAMPLER_2D_SHADOW = 0x8B62;
    public const uint DELETE_STATUS = 0x8B80;
    public const uint COMPILE_STATUS = 0x8B81;
    public const uint LINK_STATUS = 0x8B82;
    public const uint VALIDATE_STATUS = 0x8B83;
    public const uint INFO_LOG_LENGTH = 0x8B84;
    public const uint ATTACHED_SHADERS = 0x8B85;
    public const uint ACTIVE_UNIFORMS = 0x8B86;
    public const uint ACTIVE_UNIFORM_MAX_LENGTH = 0x8B87;
    public const uint SHADER_SOURCE_LENGTH = 0x8B88;
    public const uint ACTIVE_ATTRIBUTES = 0x8B89;
    public const uint ACTIVE_ATTRIBUTE_MAX_LENGTH = 0x8B8A;
    public const uint FRAGMENT_SHADER_DERIVATIVE_HINT = 0x8B8B;
    public const uint SHADING_LANGUAGE_VERSION = 0x8B8C;
    public const uint CURRENT_PROGRAM = 0x8B8D;
    public const uint POINT_SPRITE_COORD_ORIGIN = 0x8CA0;
    public const uint LOWER_LEFT = 0x8CA1;
    public const uint UPPER_LEFT = 0x8CA2;
    public const uint STENCIL_BACK_REF = 0x8CA3;
    public const uint STENCIL_BACK_VALUE_MASK = 0x8CA4;
    public const uint STENCIL_BACK_WRITEMASK = 0x8CA5;
    public const uint VERTEX_PROGRAM_TWO_SIDE = 0x8643;
    public const uint POINT_SPRITE = 0x8861;
    public const uint COORD_REPLACE = 0x8862;
    public const uint MAX_TEXTURE_COORDS = 0x8871;
    public const uint PIXEL_PACK_BUFFER = 0x88EB;
    public const uint PIXEL_UNPACK_BUFFER = 0x88EC;
    public const uint PIXEL_PACK_BUFFER_BINDING = 0x88ED;
    public const uint PIXEL_UNPACK_BUFFER_BINDING = 0x88EF;
    public const uint FLOAT_MAT2x3 = 0x8B65;
    public const uint FLOAT_MAT2x4 = 0x8B66;
    public const uint FLOAT_MAT3x2 = 0x8B67;
    public const uint FLOAT_MAT3x4 = 0x8B68;
    public const uint FLOAT_MAT4x2 = 0x8B69;
    public const uint FLOAT_MAT4x3 = 0x8B6A;
    public const uint SRGB = 0x8C40;
    public const uint SRGB8 = 0x8C41;
    public const uint SRGB_ALPHA = 0x8C42;
    public const uint SRGB8_ALPHA8 = 0x8C43;
    public const uint COMPRESSED_SRGB = 0x8C48;
    public const uint COMPRESSED_SRGB_ALPHA = 0x8C49;
    public const uint CURRENT_RASTER_SECONDARY_COLOR = 0x845F;
    public const uint SLUMINANCE_ALPHA = 0x8C44;
    public const uint SLUMINANCE8_ALPHA8 = 0x8C45;
    public const uint SLUMINANCE = 0x8C46;
    public const uint SLUMINANCE8 = 0x8C47;
    public const uint COMPRESSED_SLUMINANCE = 0x8C4A;
    public const uint COMPRESSED_SLUMINANCE_ALPHA = 0x8C4B;
    public const uint COMPARE_REF_TO_TEXTURE = 0x884E;
    public const uint CLIP_DISTANCE0 = 0x3000;
    public const uint CLIP_DISTANCE1 = 0x3001;
    public const uint CLIP_DISTANCE2 = 0x3002;
    public const uint CLIP_DISTANCE3 = 0x3003;
    public const uint CLIP_DISTANCE4 = 0x3004;
    public const uint CLIP_DISTANCE5 = 0x3005;
    public const uint CLIP_DISTANCE6 = 0x3006;
    public const uint CLIP_DISTANCE7 = 0x3007;
    public const uint MAX_CLIP_DISTANCES = 0x0D32;
    public const uint MAJOR_VERSION = 0x821B;
    public const uint MINOR_VERSION = 0x821C;
    public const uint NUM_EXTENSIONS = 0x821D;
    public const uint CONTEXT_FLAGS = 0x821E;
    public const uint COMPRESSED_RED = 0x8225;
    public const uint COMPRESSED_RG = 0x8226;
    public const uint CONTEXT_FLAG_FORWARD_COMPATIBLE_BIT = 0x00000001;
    public const uint RGBA32F = 0x8814;
    public const uint RGB32F = 0x8815;
    public const uint RGBA16F = 0x881A;
    public const uint RGB16F = 0x881B;
    public const uint VERTEX_ATTRIB_ARRAY_INTEGER = 0x88FD;
    public const uint MAX_ARRAY_TEXTURE_LAYERS = 0x88FF;
    public const uint MIN_PROGRAM_TEXEL_OFFSET = 0x8904;
    public const uint MAX_PROGRAM_TEXEL_OFFSET = 0x8905;
    public const uint CLAMP_READ_COLOR = 0x891C;
    public const uint FIXED_ONLY = 0x891D;
    public const uint MAX_VARYING_COMPONENTS = 0x8B4B;
    public const uint TEXTURE_1D_ARRAY = 0x8C18;
    public const uint PROXY_TEXTURE_1D_ARRAY = 0x8C19;
    public const uint TEXTURE_2D_ARRAY = 0x8C1A;
    public const uint PROXY_TEXTURE_2D_ARRAY = 0x8C1B;
    public const uint TEXTURE_BINDING_1D_ARRAY = 0x8C1C;
    public const uint TEXTURE_BINDING_2D_ARRAY = 0x8C1D;
    public const uint R11F_G11F_B10F = 0x8C3A;
    public const uint UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B;
    public const uint RGB9_E5 = 0x8C3D;
    public const uint UNSIGNED_INT_5_9_9_9_REV = 0x8C3E;
    public const uint TEXTURE_SHARED_SIZE = 0x8C3F;
    public const uint TRANSFORM_FEEDBACK_VARYING_MAX_LENGTH = 0x8C76;
    public const uint TRANSFORM_FEEDBACK_BUFFER_MODE = 0x8C7F;
    public const uint MAX_TRANSFORM_FEEDBACK_SEPARATE_COMPONENTS = 0x8C80;
    public const uint TRANSFORM_FEEDBACK_VARYINGS = 0x8C83;
    public const uint TRANSFORM_FEEDBACK_BUFFER_START = 0x8C84;
    public const uint TRANSFORM_FEEDBACK_BUFFER_SIZE = 0x8C85;
    public const uint PRIMITIVES_GENERATED = 0x8C87;
    public const uint TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN = 0x8C88;
    public const uint RASTERIZER_DISCARD = 0x8C89;
    public const uint MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS = 0x8C8A;
    public const uint MAX_TRANSFORM_FEEDBACK_SEPARATE_ATTRIBS = 0x8C8B;
    public const uint INTERLEAVED_ATTRIBS = 0x8C8C;
    public const uint SEPARATE_ATTRIBS = 0x8C8D;
    public const uint TRANSFORM_FEEDBACK_BUFFER = 0x8C8E;
    public const uint TRANSFORM_FEEDBACK_BUFFER_BINDING = 0x8C8F;
    public const uint RGBA32UI = 0x8D70;
    public const uint RGB32UI = 0x8D71;
    public const uint RGBA16UI = 0x8D76;
    public const uint RGB16UI = 0x8D77;
    public const uint RGBA8UI = 0x8D7C;
    public const uint RGB8UI = 0x8D7D;
    public const uint RGBA32I = 0x8D82;
    public const uint RGB32I = 0x8D83;
    public const uint RGBA16I = 0x8D88;
    public const uint RGB16I = 0x8D89;
    public const uint RGBA8I = 0x8D8E;
    public const uint RGB8I = 0x8D8F;
    public const uint RED_INTEGER = 0x8D94;
    public const uint GREEN_INTEGER = 0x8D95;
    public const uint BLUE_INTEGER = 0x8D96;
    public const uint RGB_INTEGER = 0x8D98;
    public const uint RGBA_INTEGER = 0x8D99;
    public const uint BGR_INTEGER = 0x8D9A;
    public const uint BGRA_INTEGER = 0x8D9B;
    public const uint SAMPLER_1D_ARRAY = 0x8DC0;
    public const uint SAMPLER_2D_ARRAY = 0x8DC1;
    public const uint SAMPLER_1D_ARRAY_SHADOW = 0x8DC3;
    public const uint SAMPLER_2D_ARRAY_SHADOW = 0x8DC4;
    public const uint SAMPLER_CUBE_SHADOW = 0x8DC5;
    public const uint UNSIGNED_INT_VEC2 = 0x8DC6;
    public const uint UNSIGNED_INT_VEC3 = 0x8DC7;
    public const uint UNSIGNED_INT_VEC4 = 0x8DC8;
    public const uint INT_SAMPLER_1D = 0x8DC9;
    public const uint INT_SAMPLER_2D = 0x8DCA;
    public const uint INT_SAMPLER_3D = 0x8DCB;
    public const uint INT_SAMPLER_CUBE = 0x8DCC;
    public const uint INT_SAMPLER_1D_ARRAY = 0x8DCE;
    public const uint INT_SAMPLER_2D_ARRAY = 0x8DCF;
    public const uint UNSIGNED_INT_SAMPLER_1D = 0x8DD1;
    public const uint UNSIGNED_INT_SAMPLER_2D = 0x8DD2;
    public const uint UNSIGNED_INT_SAMPLER_3D = 0x8DD3;
    public const uint UNSIGNED_INT_SAMPLER_CUBE = 0x8DD4;
    public const uint UNSIGNED_INT_SAMPLER_1D_ARRAY = 0x8DD6;
    public const uint UNSIGNED_INT_SAMPLER_2D_ARRAY = 0x8DD7;
    public const uint QUERY_WAIT = 0x8E13;
    public const uint QUERY_NO_WAIT = 0x8E14;
    public const uint QUERY_BY_REGION_WAIT = 0x8E15;
    public const uint QUERY_BY_REGION_NO_WAIT = 0x8E16;
    public const uint BUFFER_ACCESS_FLAGS = 0x911F;
    public const uint BUFFER_MAP_LENGTH = 0x9120;
    public const uint BUFFER_MAP_OFFSET = 0x9121;
    public const uint DEPTH_COMPONENT32F = 0x8CAC;
    public const uint DEPTH32F_STENCIL8 = 0x8CAD;
    public const uint FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD;
    public const uint INVALID_FRAMEBUFFER_OPERATION = 0x0506;
    public const uint FRAMEBUFFER_ATTACHMENT_COLOR_ENCODING = 0x8210;
    public const uint FRAMEBUFFER_ATTACHMENT_COMPONENT_TYPE = 0x8211;
    public const uint FRAMEBUFFER_ATTACHMENT_RED_SIZE = 0x8212;
    public const uint FRAMEBUFFER_ATTACHMENT_GREEN_SIZE = 0x8213;
    public const uint FRAMEBUFFER_ATTACHMENT_BLUE_SIZE = 0x8214;
    public const uint FRAMEBUFFER_ATTACHMENT_ALPHA_SIZE = 0x8215;
    public const uint FRAMEBUFFER_ATTACHMENT_DEPTH_SIZE = 0x8216;
    public const uint FRAMEBUFFER_ATTACHMENT_STENCIL_SIZE = 0x8217;
    public const uint FRAMEBUFFER_DEFAULT = 0x8218;
    public const uint FRAMEBUFFER_UNDEFINED = 0x8219;
    public const uint DEPTH_STENCIL_ATTACHMENT = 0x821A;
    public const uint MAX_RENDERBUFFER_SIZE = 0x84E8;
    public const uint DEPTH_STENCIL = 0x84F9;
    public const uint UNSIGNED_INT_24_8 = 0x84FA;
    public const uint DEPTH24_STENCIL8 = 0x88F0;
    public const uint TEXTURE_STENCIL_SIZE = 0x88F1;
    public const uint TEXTURE_RED_TYPE = 0x8C10;
    public const uint TEXTURE_GREEN_TYPE = 0x8C11;
    public const uint TEXTURE_BLUE_TYPE = 0x8C12;
    public const uint TEXTURE_ALPHA_TYPE = 0x8C13;
    public const uint TEXTURE_DEPTH_TYPE = 0x8C16;
    public const uint UNSIGNED_NORMALIZED = 0x8C17;
    public const uint FRAMEBUFFER_BINDING = 0x8CA6;
    public const uint DRAW_FRAMEBUFFER_BINDING = 0x8CA6;
    public const uint RENDERBUFFER_BINDING = 0x8CA7;
    public const uint READ_FRAMEBUFFER = 0x8CA8;
    public const uint DRAW_FRAMEBUFFER = 0x8CA9;
    public const uint READ_FRAMEBUFFER_BINDING = 0x8CAA;
    public const uint RENDERBUFFER_SAMPLES = 0x8CAB;
    public const uint FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE = 0x8CD0;
    public const uint FRAMEBUFFER_ATTACHMENT_OBJECT_NAME = 0x8CD1;
    public const uint FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL = 0x8CD2;
    public const uint FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE = 0x8CD3;
    public const uint FRAMEBUFFER_ATTACHMENT_TEXTURE_LAYER = 0x8CD4;
    public const uint FRAMEBUFFER_COMPLETE = 0x8CD5;
    public const uint FRAMEBUFFER_INCOMPLETE_ATTACHMENT = 0x8CD6;
    public const uint FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT = 0x8CD7;
    public const uint FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER = 0x8CDB;
    public const uint FRAMEBUFFER_INCOMPLETE_READ_BUFFER = 0x8CDC;
    public const uint FRAMEBUFFER_UNSUPPORTED = 0x8CDD;
    public const uint MAX_COLOR_ATTACHMENTS = 0x8CDF;
    public const uint COLOR_ATTACHMENT0 = 0x8CE0;
    public const uint COLOR_ATTACHMENT1 = 0x8CE1;
    public const uint COLOR_ATTACHMENT2 = 0x8CE2;
    public const uint COLOR_ATTACHMENT3 = 0x8CE3;
    public const uint COLOR_ATTACHMENT4 = 0x8CE4;
    public const uint COLOR_ATTACHMENT5 = 0x8CE5;
    public const uint COLOR_ATTACHMENT6 = 0x8CE6;
    public const uint COLOR_ATTACHMENT7 = 0x8CE7;
    public const uint COLOR_ATTACHMENT8 = 0x8CE8;
    public const uint COLOR_ATTACHMENT9 = 0x8CE9;
    public const uint COLOR_ATTACHMENT10 = 0x8CEA;
    public const uint COLOR_ATTACHMENT11 = 0x8CEB;
    public const uint COLOR_ATTACHMENT12 = 0x8CEC;
    public const uint COLOR_ATTACHMENT13 = 0x8CED;
    public const uint COLOR_ATTACHMENT14 = 0x8CEE;
    public const uint COLOR_ATTACHMENT15 = 0x8CEF;
    public const uint COLOR_ATTACHMENT16 = 0x8CF0;
    public const uint COLOR_ATTACHMENT17 = 0x8CF1;
    public const uint COLOR_ATTACHMENT18 = 0x8CF2;
    public const uint COLOR_ATTACHMENT19 = 0x8CF3;
    public const uint COLOR_ATTACHMENT20 = 0x8CF4;
    public const uint COLOR_ATTACHMENT21 = 0x8CF5;
    public const uint COLOR_ATTACHMENT22 = 0x8CF6;
    public const uint COLOR_ATTACHMENT23 = 0x8CF7;
    public const uint COLOR_ATTACHMENT24 = 0x8CF8;
    public const uint COLOR_ATTACHMENT25 = 0x8CF9;
    public const uint COLOR_ATTACHMENT26 = 0x8CFA;
    public const uint COLOR_ATTACHMENT27 = 0x8CFB;
    public const uint COLOR_ATTACHMENT28 = 0x8CFC;
    public const uint COLOR_ATTACHMENT29 = 0x8CFD;
    public const uint COLOR_ATTACHMENT30 = 0x8CFE;
    public const uint COLOR_ATTACHMENT31 = 0x8CFF;
    public const uint DEPTH_ATTACHMENT = 0x8D00;
    public const uint STENCIL_ATTACHMENT = 0x8D20;
    public const uint FRAMEBUFFER = 0x8D40;
    public const uint RENDERBUFFER = 0x8D41;
    public const uint RENDERBUFFER_WIDTH = 0x8D42;
    public const uint RENDERBUFFER_HEIGHT = 0x8D43;
    public const uint RENDERBUFFER_INTERNAL_FORMAT = 0x8D44;
    public const uint STENCIL_INDEX1 = 0x8D46;
    public const uint STENCIL_INDEX4 = 0x8D47;
    public const uint STENCIL_INDEX8 = 0x8D48;
    public const uint STENCIL_INDEX16 = 0x8D49;
    public const uint RENDERBUFFER_RED_SIZE = 0x8D50;
    public const uint RENDERBUFFER_GREEN_SIZE = 0x8D51;
    public const uint RENDERBUFFER_BLUE_SIZE = 0x8D52;
    public const uint RENDERBUFFER_ALPHA_SIZE = 0x8D53;
    public const uint RENDERBUFFER_DEPTH_SIZE = 0x8D54;
    public const uint RENDERBUFFER_STENCIL_SIZE = 0x8D55;
    public const uint FRAMEBUFFER_INCOMPLETE_MULTISAMPLE = 0x8D56;
    public const uint MAX_SAMPLES = 0x8D57;
    public const uint INDEX = 0x8222;
    public const uint TEXTURE_LUMINANCE_TYPE = 0x8C14;
    public const uint TEXTURE_INTENSITY_TYPE = 0x8C15;
    public const uint FRAMEBUFFER_SRGB = 0x8DB9;
    public const uint HALF_FLOAT = 0x140B;
    public const uint MAP_READ_BIT = 0x0001;
    public const uint MAP_WRITE_BIT = 0x0002;
    public const uint MAP_INVALIDATE_RANGE_BIT = 0x0004;
    public const uint MAP_INVALIDATE_BUFFER_BIT = 0x0008;
    public const uint MAP_FLUSH_EXPLICIT_BIT = 0x0010;
    public const uint MAP_UNSYNCHRONIZED_BIT = 0x0020;
    public const uint COMPRESSED_RED_RGTC1 = 0x8DBB;
    public const uint COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC;
    public const uint COMPRESSED_RG_RGTC2 = 0x8DBD;
    public const uint COMPRESSED_SIGNED_RG_RGTC2 = 0x8DBE;
    public const uint RG = 0x8227;
    public const uint RG_INTEGER = 0x8228;
    public const uint R8 = 0x8229;
    public const uint R16 = 0x822A;
    public const uint RG8 = 0x822B;
    public const uint RG16 = 0x822C;
    public const uint R16F = 0x822D;
    public const uint R32F = 0x822E;
    public const uint RG16F = 0x822F;
    public const uint RG32F = 0x8230;
    public const uint R8I = 0x8231;
    public const uint R8UI = 0x8232;
    public const uint R16I = 0x8233;
    public const uint R16UI = 0x8234;
    public const uint R32I = 0x8235;
    public const uint R32UI = 0x8236;
    public const uint RG8I = 0x8237;
    public const uint RG8UI = 0x8238;
    public const uint RG16I = 0x8239;
    public const uint RG16UI = 0x823A;
    public const uint RG32I = 0x823B;
    public const uint RG32UI = 0x823C;
    public const uint VERTEX_ARRAY_BINDING = 0x85B5;
    public const uint CLAMP_VERTEX_COLOR = 0x891A;
    public const uint CLAMP_FRAGMENT_COLOR = 0x891B;
    public const uint ALPHA_INTEGER = 0x8D97;
    public const uint CONTEXT_CORE_PROFILE_BIT = 0x00000001;
    public const uint CONTEXT_COMPATIBILITY_PROFILE_BIT = 0x00000002;
    public const uint LINES_ADJACENCY = 0x000A;
    public const uint LINE_STRIP_ADJACENCY = 0x000B;
    public const uint TRIANGLES_ADJACENCY = 0x000C;
    public const uint TRIANGLE_STRIP_ADJACENCY = 0x000D;
    public const uint PROGRAM_POINT_SIZE = 0x8642;
    public const uint MAX_GEOMETRY_TEXTURE_IMAGE_UNITS = 0x8C29;
    public const uint FRAMEBUFFER_ATTACHMENT_LAYERED = 0x8DA7;
    public const uint FRAMEBUFFER_INCOMPLETE_LAYER_TARGETS = 0x8DA8;
    public const uint GEOMETRY_SHADER = 0x8DD9;
    public const uint GEOMETRY_VERTICES_OUT = 0x8916;
    public const uint GEOMETRY_INPUT_TYPE = 0x8917;
    public const uint GEOMETRY_OUTPUT_TYPE = 0x8918;
    public const uint MAX_GEOMETRY_UNIFORM_COMPONENTS = 0x8DDF;
    public const uint MAX_GEOMETRY_OUTPUT_VERTICES = 0x8DE0;
    public const uint MAX_GEOMETRY_TOTAL_OUTPUT_COMPONENTS = 0x8DE1;
    public const uint MAX_VERTEX_OUTPUT_COMPONENTS = 0x9122;
    public const uint MAX_GEOMETRY_INPUT_COMPONENTS = 0x9123;
    public const uint MAX_GEOMETRY_OUTPUT_COMPONENTS = 0x9124;
    public const uint MAX_FRAGMENT_INPUT_COMPONENTS = 0x9125;
    public const uint CONTEXT_PROFILE_MASK = 0x9126;
    public const uint DEPTH_CLAMP = 0x864F;
    public const uint QUADS_FOLLOW_PROVOKING_VERTEX_CONVENTION = 0x8E4C;
    public const uint FIRST_VERTEX_CONVENTION = 0x8E4D;
    public const uint LAST_VERTEX_CONVENTION = 0x8E4E;
    public const uint PROVOKING_VERTEX = 0x8E4F;
    public const uint TEXTURE_CUBE_MAP_SEAMLESS = 0x884F;
    public const uint MAX_SERVER_WAIT_TIMEOUT = 0x9111;
    public const uint OBJECT_TYPE = 0x9112;
    public const uint SYNC_CONDITION = 0x9113;
    public const uint SYNC_STATUS = 0x9114;
    public const uint SYNC_FLAGS = 0x9115;
    public const uint SYNC_FENCE = 0x9116;
    public const uint SYNC_GPU_COMMANDS_COMPLETE = 0x9117;
    public const uint UNSIGNALED = 0x9118;
    public const uint SIGNALED = 0x9119;
    public const uint ALREADY_SIGNALED = 0x911A;
    public const uint TIMEOUT_EXPIRED = 0x911B;
    public const uint CONDITION_SATISFIED = 0x911C;
    public const uint WAIT_FAILED = 0x911D;
    public const uint TIMEOUT_IGNORED = 0xFFFFFFFF;
    public const uint SYNC_FLUSH_COMMANDS_BIT = 0x00000001;
    public const uint SAMPLE_POSITION = 0x8E50;
    public const uint SAMPLE_MASK = 0x8E51;
    public const uint SAMPLE_MASK_VALUE = 0x8E52;
    public const uint MAX_SAMPLE_MASK_WORDS = 0x8E59;
    public const uint TEXTURE_2D_MULTISAMPLE = 0x9100;
    public const uint PROXY_TEXTURE_2D_MULTISAMPLE = 0x9101;
    public const uint TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9102;
    public const uint PROXY_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9103;
    public const uint TEXTURE_BINDING_2D_MULTISAMPLE = 0x9104;
    public const uint TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY = 0x9105;
    public const uint TEXTURE_SAMPLES = 0x9106;
    public const uint TEXTURE_FIXED_SAMPLE_LOCATIONS = 0x9107;
    public const uint SAMPLER_2D_MULTISAMPLE = 0x9108;
    public const uint INT_SAMPLER_2D_MULTISAMPLE = 0x9109;
    public const uint UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE = 0x910A;
    public const uint SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910B;
    public const uint INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910C;
    public const uint UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910D;
    public const uint MAX_COLOR_TEXTURE_SAMPLES = 0x910E;
    public const uint MAX_DEPTH_TEXTURE_SAMPLES = 0x910F;
    public const uint MAX_INTEGER_SAMPLES = 0x9110;
    public const uint SAMPLER_2D_RECT = 0x8B63;
    public const uint SAMPLER_2D_RECT_SHADOW = 0x8B64;
    public const uint SAMPLER_BUFFER = 0x8DC2;
    public const uint INT_SAMPLER_2D_RECT = 0x8DCD;
    public const uint INT_SAMPLER_BUFFER = 0x8DD0;
    public const uint UNSIGNED_INT_SAMPLER_2D_RECT = 0x8DD5;
    public const uint UNSIGNED_INT_SAMPLER_BUFFER = 0x8DD8;
    public const uint TEXTURE_BUFFER = 0x8C2A;
    public const uint MAX_TEXTURE_BUFFER_SIZE = 0x8C2B;
    public const uint TEXTURE_BINDING_BUFFER = 0x8C2C;
    public const uint TEXTURE_BUFFER_DATA_STORE_BINDING = 0x8C2D;
    public const uint TEXTURE_RECTANGLE = 0x84F5;
    public const uint TEXTURE_BINDING_RECTANGLE = 0x84F6;
    public const uint PROXY_TEXTURE_RECTANGLE = 0x84F7;
    public const uint MAX_RECTANGLE_TEXTURE_SIZE = 0x84F8;
    public const uint R8_SNORM = 0x8F94;
    public const uint RG8_SNORM = 0x8F95;
    public const uint RGB8_SNORM = 0x8F96;
    public const uint RGBA8_SNORM = 0x8F97;
    public const uint R16_SNORM = 0x8F98;
    public const uint RG16_SNORM = 0x8F99;
    public const uint RGB16_SNORM = 0x8F9A;
    public const uint RGBA16_SNORM = 0x8F9B;
    public const uint SIGNED_NORMALIZED = 0x8F9C;
    public const uint PRIMITIVE_RESTART = 0x8F9D;
    public const uint PRIMITIVE_RESTART_INDEX = 0x8F9E;
    public const uint COPY_READ_BUFFER = 0x8F36;
    public const uint COPY_WRITE_BUFFER = 0x8F37;
    public const uint UNIFORM_BUFFER = 0x8A11;
    public const uint UNIFORM_BUFFER_BINDING = 0x8A28;
    public const uint UNIFORM_BUFFER_START = 0x8A29;
    public const uint UNIFORM_BUFFER_SIZE = 0x8A2A;
    public const uint MAX_VERTEX_UNIFORM_BLOCKS = 0x8A2B;
    public const uint MAX_GEOMETRY_UNIFORM_BLOCKS = 0x8A2C;
    public const uint MAX_FRAGMENT_UNIFORM_BLOCKS = 0x8A2D;
    public const uint MAX_COMBINED_UNIFORM_BLOCKS = 0x8A2E;
    public const uint MAX_UNIFORM_BUFFER_BINDINGS = 0x8A2F;
    public const uint MAX_UNIFORM_BLOCK_SIZE = 0x8A30;
    public const uint MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS = 0x8A31;
    public const uint MAX_COMBINED_GEOMETRY_UNIFORM_COMPONENTS = 0x8A32;
    public const uint MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS = 0x8A33;
    public const uint UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8A34;
    public const uint ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH = 0x8A35;
    public const uint ACTIVE_UNIFORM_BLOCKS = 0x8A36;
    public const uint UNIFORM_TYPE = 0x8A37;
    public const uint UNIFORM_SIZE = 0x8A38;
    public const uint UNIFORM_NAME_LENGTH = 0x8A39;
    public const uint UNIFORM_BLOCK_INDEX = 0x8A3A;
    public const uint UNIFORM_OFFSET = 0x8A3B;
    public const uint UNIFORM_ARRAY_STRIDE = 0x8A3C;
    public const uint UNIFORM_MATRIX_STRIDE = 0x8A3D;
    public const uint UNIFORM_IS_ROW_MAJOR = 0x8A3E;
    public const uint UNIFORM_BLOCK_BINDING = 0x8A3F;
    public const uint UNIFORM_BLOCK_DATA_SIZE = 0x8A40;
    public const uint UNIFORM_BLOCK_NAME_LENGTH = 0x8A41;
    public const uint UNIFORM_BLOCK_ACTIVE_UNIFORMS = 0x8A42;
    public const uint UNIFORM_BLOCK_ACTIVE_UNIFORM_INDICES = 0x8A43;
    public const uint UNIFORM_BLOCK_REFERENCED_BY_VERTEX_SHADER = 0x8A44;
    public const uint UNIFORM_BLOCK_REFERENCED_BY_GEOMETRY_SHADER = 0x8A45;
    public const uint UNIFORM_BLOCK_REFERENCED_BY_FRAGMENT_SHADER = 0x8A46;
    public const uint INVALID_INDEX = 0xFFFFFFFF;
    public const uint VERTEX_ATTRIB_ARRAY_DIVISOR = 0x88FE;
    public const uint SRC1_COLOR = 0x88F9;
    public const uint ONE_MINUS_SRC1_COLOR = 0x88FA;
    public const uint ONE_MINUS_SRC1_ALPHA = 0x88FB;
    public const uint MAX_DUAL_SOURCE_DRAW_BUFFERS = 0x88FC;
    public const uint ANY_SAMPLES_PASSED = 0x8C2F;
    public const uint SAMPLER_BINDING = 0x8919;
    public const uint RGB10_A2UI = 0x906F;
    public const uint TEXTURE_SWIZZLE_R = 0x8E42;
    public const uint TEXTURE_SWIZZLE_G = 0x8E43;
    public const uint TEXTURE_SWIZZLE_B = 0x8E44;
    public const uint TEXTURE_SWIZZLE_A = 0x8E45;
    public const uint TEXTURE_SWIZZLE_RGBA = 0x8E46;
    public const uint TIME_ELAPSED = 0x88BF;
    public const uint TIMESTAMP = 0x8E28;
    public const uint INT_2_10_10_10_REV = 0x8D9F;
    public const uint SAMPLE_SHADING = 0x8C36;
    public const uint MIN_SAMPLE_SHADING_VALUE = 0x8C37;
    public const uint MIN_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5E;
    public const uint MAX_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5F;
    public const uint TEXTURE_CUBE_MAP_ARRAY = 0x9009;
    public const uint TEXTURE_BINDING_CUBE_MAP_ARRAY = 0x900A;
    public const uint PROXY_TEXTURE_CUBE_MAP_ARRAY = 0x900B;
    public const uint SAMPLER_CUBE_MAP_ARRAY = 0x900C;
    public const uint SAMPLER_CUBE_MAP_ARRAY_SHADOW = 0x900D;
    public const uint INT_SAMPLER_CUBE_MAP_ARRAY = 0x900E;
    public const uint UNSIGNED_INT_SAMPLER_CUBE_MAP_ARRAY = 0x900F;
    public const uint DRAW_INDIRECT_BUFFER = 0x8F3F;
    public const uint DRAW_INDIRECT_BUFFER_BINDING = 0x8F43;
    public const uint GEOMETRY_SHADER_INVOCATIONS = 0x887F;
    public const uint MAX_GEOMETRY_SHADER_INVOCATIONS = 0x8E5A;
    public const uint MIN_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5B;
    public const uint MAX_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5C;
    public const uint FRAGMENT_INTERPOLATION_OFFSET_BITS = 0x8E5D;
    public const uint MAX_VERTEX_STREAMS = 0x8E71;
    public const uint DOUBLE_VEC2 = 0x8FFC;
    public const uint DOUBLE_VEC3 = 0x8FFD;
    public const uint DOUBLE_VEC4 = 0x8FFE;
    public const uint DOUBLE_MAT2 = 0x8F46;
    public const uint DOUBLE_MAT3 = 0x8F47;
    public const uint DOUBLE_MAT4 = 0x8F48;
    public const uint DOUBLE_MAT2x3 = 0x8F49;
    public const uint DOUBLE_MAT2x4 = 0x8F4A;
    public const uint DOUBLE_MAT3x2 = 0x8F4B;
    public const uint DOUBLE_MAT3x4 = 0x8F4C;
    public const uint DOUBLE_MAT4x2 = 0x8F4D;
    public const uint DOUBLE_MAT4x3 = 0x8F4E;
    public const uint ACTIVE_SUBROUTINES = 0x8DE5;
    public const uint ACTIVE_SUBROUTINE_UNIFORMS = 0x8DE6;
    public const uint ACTIVE_SUBROUTINE_UNIFORM_LOCATIONS = 0x8E47;
    public const uint ACTIVE_SUBROUTINE_MAX_LENGTH = 0x8E48;
    public const uint ACTIVE_SUBROUTINE_UNIFORM_MAX_LENGTH = 0x8E49;
    public const uint MAX_SUBROUTINES = 0x8DE7;
    public const uint MAX_SUBROUTINE_UNIFORM_LOCATIONS = 0x8DE8;
    public const uint NUM_COMPATIBLE_SUBROUTINES = 0x8E4A;
    public const uint COMPATIBLE_SUBROUTINES = 0x8E4B;
    public const uint PATCHES = 0x000E;
    public const uint PATCH_VERTICES = 0x8E72;
    public const uint PATCH_DEFAULT_INNER_LEVEL = 0x8E73;
    public const uint PATCH_DEFAULT_OUTER_LEVEL = 0x8E74;
    public const uint TESS_CONTROL_OUTPUT_VERTICES = 0x8E75;
    public const uint TESS_GEN_MODE = 0x8E76;
    public const uint TESS_GEN_SPACING = 0x8E77;
    public const uint TESS_GEN_VERTEX_ORDER = 0x8E78;
    public const uint TESS_GEN_POINT_MODE = 0x8E79;
    public const uint ISOLINES = 0x8E7A;
    public const uint FRACTIONAL_ODD = 0x8E7B;
    public const uint FRACTIONAL_EVEN = 0x8E7C;
    public const uint MAX_PATCH_VERTICES = 0x8E7D;
    public const uint MAX_TESS_GEN_LEVEL = 0x8E7E;
    public const uint MAX_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E7F;
    public const uint MAX_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E80;
    public const uint MAX_TESS_CONTROL_TEXTURE_IMAGE_UNITS = 0x8E81;
    public const uint MAX_TESS_EVALUATION_TEXTURE_IMAGE_UNITS = 0x8E82;
    public const uint MAX_TESS_CONTROL_OUTPUT_COMPONENTS = 0x8E83;
    public const uint MAX_TESS_PATCH_COMPONENTS = 0x8E84;
    public const uint MAX_TESS_CONTROL_TOTAL_OUTPUT_COMPONENTS = 0x8E85;
    public const uint MAX_TESS_EVALUATION_OUTPUT_COMPONENTS = 0x8E86;
    public const uint MAX_TESS_CONTROL_UNIFORM_BLOCKS = 0x8E89;
    public const uint MAX_TESS_EVALUATION_UNIFORM_BLOCKS = 0x8E8A;
    public const uint MAX_TESS_CONTROL_INPUT_COMPONENTS = 0x886C;
    public const uint MAX_TESS_EVALUATION_INPUT_COMPONENTS = 0x886D;
    public const uint MAX_COMBINED_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E1E;
    public const uint MAX_COMBINED_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E1F;
    public const uint UNIFORM_BLOCK_REFERENCED_BY_TESS_CONTROL_SHADER = 0x84F0;
    public const uint UNIFORM_BLOCK_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x84F1;
    public const uint TESS_EVALUATION_SHADER = 0x8E87;
    public const uint TESS_CONTROL_SHADER = 0x8E88;
    public const uint TRANSFORM_FEEDBACK = 0x8E22;
    public const uint TRANSFORM_FEEDBACK_BUFFER_PAUSED = 0x8E23;
    public const uint TRANSFORM_FEEDBACK_BUFFER_ACTIVE = 0x8E24;
    public const uint TRANSFORM_FEEDBACK_BINDING = 0x8E25;
    public const uint MAX_TRANSFORM_FEEDBACK_BUFFERS = 0x8E70;
    public const uint FIXED = 0x140C;
    public const uint IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A;
    public const uint IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B;
    public const uint LOW_FLOAT = 0x8DF0;
    public const uint MEDIUM_FLOAT = 0x8DF1;
    public const uint HIGH_FLOAT = 0x8DF2;
    public const uint LOW_INT = 0x8DF3;
    public const uint MEDIUM_INT = 0x8DF4;
    public const uint HIGH_INT = 0x8DF5;
    public const uint SHADER_COMPILER = 0x8DFA;
    public const uint SHADER_BINARY_FORMATS = 0x8DF8;
    public const uint NUM_SHADER_BINARY_FORMATS = 0x8DF9;
    public const uint MAX_VERTEX_UNIFORM_VECTORS = 0x8DFB;
    public const uint MAX_VARYING_VECTORS = 0x8DFC;
    public const uint MAX_FRAGMENT_UNIFORM_VECTORS = 0x8DFD;
    public const uint RGB565 = 0x8D62;
    public const uint PROGRAM_BINARY_RETRIEVABLE_HINT = 0x8257;
    public const uint PROGRAM_BINARY_LENGTH = 0x8741;
    public const uint NUM_PROGRAM_BINARY_FORMATS = 0x87FE;
    public const uint PROGRAM_BINARY_FORMATS = 0x87FF;
    public const uint VERTEX_SHADER_BIT = 0x00000001;
    public const uint FRAGMENT_SHADER_BIT = 0x00000002;
    public const uint GEOMETRY_SHADER_BIT = 0x00000004;
    public const uint TESS_CONTROL_SHADER_BIT = 0x00000008;
    public const uint TESS_EVALUATION_SHADER_BIT = 0x00000010;
    public const uint ALL_SHADER_BITS = 0xFFFFFFFF;
    public const uint PROGRAM_SEPARABLE = 0x8258;
    public const uint ACTIVE_PROGRAM = 0x8259;
    public const uint PROGRAM_PIPELINE_BINDING = 0x825A;
    public const uint MAX_VIEWPORTS = 0x825B;
    public const uint VIEWPORT_SUBPIXEL_BITS = 0x825C;
    public const uint VIEWPORT_BOUNDS_RANGE = 0x825D;
    public const uint LAYER_PROVOKING_VERTEX = 0x825E;
    public const uint VIEWPORT_INDEX_PROVOKING_VERTEX = 0x825F;
    public const uint UNDEFINED_VERTEX = 0x8260;
    public const uint COPY_READ_BUFFER_BINDING = 0x8F36;
    public const uint COPY_WRITE_BUFFER_BINDING = 0x8F37;
    public const uint TRANSFORM_FEEDBACK_ACTIVE = 0x8E24;
    public const uint TRANSFORM_FEEDBACK_PAUSED = 0x8E23;
    public const uint UNPACK_COMPRESSED_BLOCK_WIDTH = 0x9127;
    public const uint UNPACK_COMPRESSED_BLOCK_HEIGHT = 0x9128;
    public const uint UNPACK_COMPRESSED_BLOCK_DEPTH = 0x9129;
    public const uint UNPACK_COMPRESSED_BLOCK_SIZE = 0x912A;
    public const uint PACK_COMPRESSED_BLOCK_WIDTH = 0x912B;
    public const uint PACK_COMPRESSED_BLOCK_HEIGHT = 0x912C;
    public const uint PACK_COMPRESSED_BLOCK_DEPTH = 0x912D;
    public const uint PACK_COMPRESSED_BLOCK_SIZE = 0x912E;
    public const uint NUM_SAMPLE_COUNTS = 0x9380;
    public const uint MIN_MAP_BUFFER_ALIGNMENT = 0x90BC;
    public const uint ATOMIC_COUNTER_BUFFER = 0x92C0;
    public const uint ATOMIC_COUNTER_BUFFER_BINDING = 0x92C1;
    public const uint ATOMIC_COUNTER_BUFFER_START = 0x92C2;
    public const uint ATOMIC_COUNTER_BUFFER_SIZE = 0x92C3;
    public const uint ATOMIC_COUNTER_BUFFER_DATA_SIZE = 0x92C4;
    public const uint ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTERS = 0x92C5;
    public const uint ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTER_INDICES = 0x92C6;
    public const uint ATOMIC_COUNTER_BUFFER_REFERENCED_BY_VERTEX_SHADER = 0x92C7;
    public const uint ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_CONTROL_SHADER = 0x92C8;
    public const uint ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x92C9;
    public const uint ATOMIC_COUNTER_BUFFER_REFERENCED_BY_GEOMETRY_SHADER = 0x92CA;
    public const uint ATOMIC_COUNTER_BUFFER_REFERENCED_BY_FRAGMENT_SHADER = 0x92CB;
    public const uint MAX_VERTEX_ATOMIC_COUNTER_BUFFERS = 0x92CC;
    public const uint MAX_TESS_CONTROL_ATOMIC_COUNTER_BUFFERS = 0x92CD;
    public const uint MAX_TESS_EVALUATION_ATOMIC_COUNTER_BUFFERS = 0x92CE;
    public const uint MAX_GEOMETRY_ATOMIC_COUNTER_BUFFERS = 0x92CF;
    public const uint MAX_FRAGMENT_ATOMIC_COUNTER_BUFFERS = 0x92D0;
    public const uint MAX_COMBINED_ATOMIC_COUNTER_BUFFERS = 0x92D1;
    public const uint MAX_VERTEX_ATOMIC_COUNTERS = 0x92D2;
    public const uint MAX_TESS_CONTROL_ATOMIC_COUNTERS = 0x92D3;
    public const uint MAX_TESS_EVALUATION_ATOMIC_COUNTERS = 0x92D4;
    public const uint MAX_GEOMETRY_ATOMIC_COUNTERS = 0x92D5;
    public const uint MAX_FRAGMENT_ATOMIC_COUNTERS = 0x92D6;
    public const uint MAX_COMBINED_ATOMIC_COUNTERS = 0x92D7;
    public const uint MAX_ATOMIC_COUNTER_BUFFER_SIZE = 0x92D8;
    public const uint MAX_ATOMIC_COUNTER_BUFFER_BINDINGS = 0x92DC;
    public const uint ACTIVE_ATOMIC_COUNTER_BUFFERS = 0x92D9;
    public const uint UNIFORM_ATOMIC_COUNTER_BUFFER_INDEX = 0x92DA;
    public const uint UNSIGNED_INT_ATOMIC_COUNTER = 0x92DB;
    public const uint VERTEX_ATTRIB_ARRAY_BARRIER_BIT = 0x00000001;
    public const uint ELEMENT_ARRAY_BARRIER_BIT = 0x00000002;
    public const uint UNIFORM_BARRIER_BIT = 0x00000004;
    public const uint TEXTURE_FETCH_BARRIER_BIT = 0x00000008;
    public const uint SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020;
    public const uint COMMAND_BARRIER_BIT = 0x00000040;
    public const uint PIXEL_BUFFER_BARRIER_BIT = 0x00000080;
    public const uint TEXTURE_UPDATE_BARRIER_BIT = 0x00000100;
    public const uint BUFFER_UPDATE_BARRIER_BIT = 0x00000200;
    public const uint FRAMEBUFFER_BARRIER_BIT = 0x00000400;
    public const uint TRANSFORM_FEEDBACK_BARRIER_BIT = 0x00000800;
    public const uint ATOMIC_COUNTER_BARRIER_BIT = 0x00001000;
    public const uint ALL_BARRIER_BITS = 0xFFFFFFFF;
    public const uint MAX_IMAGE_UNITS = 0x8F38;
    public const uint MAX_COMBINED_IMAGE_UNITS_AND_FRAGMENT_OUTPUTS = 0x8F39;
    public const uint IMAGE_BINDING_NAME = 0x8F3A;
    public const uint IMAGE_BINDING_LEVEL = 0x8F3B;
    public const uint IMAGE_BINDING_LAYERED = 0x8F3C;
    public const uint IMAGE_BINDING_LAYER = 0x8F3D;
    public const uint IMAGE_BINDING_ACCESS = 0x8F3E;
    public const uint IMAGE_1D = 0x904C;
    public const uint IMAGE_2D = 0x904D;
    public const uint IMAGE_3D = 0x904E;
    public const uint IMAGE_2D_RECT = 0x904F;
    public const uint IMAGE_CUBE = 0x9050;
    public const uint IMAGE_BUFFER = 0x9051;
    public const uint IMAGE_1D_ARRAY = 0x9052;
    public const uint IMAGE_2D_ARRAY = 0x9053;
    public const uint IMAGE_CUBE_MAP_ARRAY = 0x9054;
    public const uint IMAGE_2D_MULTISAMPLE = 0x9055;
    public const uint IMAGE_2D_MULTISAMPLE_ARRAY = 0x9056;
    public const uint INT_IMAGE_1D = 0x9057;
    public const uint INT_IMAGE_2D = 0x9058;
    public const uint INT_IMAGE_3D = 0x9059;
    public const uint INT_IMAGE_2D_RECT = 0x905A;
    public const uint INT_IMAGE_CUBE = 0x905B;
    public const uint INT_IMAGE_BUFFER = 0x905C;
    public const uint INT_IMAGE_1D_ARRAY = 0x905D;
    public const uint INT_IMAGE_2D_ARRAY = 0x905E;
    public const uint INT_IMAGE_CUBE_MAP_ARRAY = 0x905F;
    public const uint INT_IMAGE_2D_MULTISAMPLE = 0x9060;
    public const uint INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9061;
    public const uint UNSIGNED_INT_IMAGE_1D = 0x9062;
    public const uint UNSIGNED_INT_IMAGE_2D = 0x9063;
    public const uint UNSIGNED_INT_IMAGE_3D = 0x9064;
    public const uint UNSIGNED_INT_IMAGE_2D_RECT = 0x9065;
    public const uint UNSIGNED_INT_IMAGE_CUBE = 0x9066;
    public const uint UNSIGNED_INT_IMAGE_BUFFER = 0x9067;
    public const uint UNSIGNED_INT_IMAGE_1D_ARRAY = 0x9068;
    public const uint UNSIGNED_INT_IMAGE_2D_ARRAY = 0x9069;
    public const uint UNSIGNED_INT_IMAGE_CUBE_MAP_ARRAY = 0x906A;
    public const uint UNSIGNED_INT_IMAGE_2D_MULTISAMPLE = 0x906B;
    public const uint UNSIGNED_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x906C;
    public const uint MAX_IMAGE_SAMPLES = 0x906D;
    public const uint IMAGE_BINDING_FORMAT = 0x906E;
    public const uint IMAGE_FORMAT_COMPATIBILITY_TYPE = 0x90C7;
    public const uint IMAGE_FORMAT_COMPATIBILITY_BY_SIZE = 0x90C8;
    public const uint IMAGE_FORMAT_COMPATIBILITY_BY_CLASS = 0x90C9;
    public const uint MAX_VERTEX_IMAGE_UNIFORMS = 0x90CA;
    public const uint MAX_TESS_CONTROL_IMAGE_UNIFORMS = 0x90CB;
    public const uint MAX_TESS_EVALUATION_IMAGE_UNIFORMS = 0x90CC;
    public const uint MAX_GEOMETRY_IMAGE_UNIFORMS = 0x90CD;
    public const uint MAX_FRAGMENT_IMAGE_UNIFORMS = 0x90CE;
    public const uint MAX_COMBINED_IMAGE_UNIFORMS = 0x90CF;
    public const uint COMPRESSED_RGBA_BPTC_UNORM = 0x8E8C;
    public const uint COMPRESSED_SRGB_ALPHA_BPTC_UNORM = 0x8E8D;
    public const uint COMPRESSED_RGB_BPTC_SIGNED_FLOAT = 0x8E8E;
    public const uint COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT = 0x8E8F;
    public const uint TEXTURE_IMMUTABLE_FORMAT = 0x912F;
    public const uint NUM_SHADING_LANGUAGE_VERSIONS = 0x82E9;
    public const uint VERTEX_ATTRIB_ARRAY_LONG = 0x874E;
    public const uint COMPRESSED_RGB8_ETC2 = 0x9274;
    public const uint COMPRESSED_SRGB8_ETC2 = 0x9275;
    public const uint COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276;
    public const uint COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277;
    public const uint COMPRESSED_RGBA8_ETC2_EAC = 0x9278;
    public const uint COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279;
    public const uint COMPRESSED_R11_EAC = 0x9270;
    public const uint COMPRESSED_SIGNED_R11_EAC = 0x9271;
    public const uint COMPRESSED_RG11_EAC = 0x9272;
    public const uint COMPRESSED_SIGNED_RG11_EAC = 0x9273;
    public const uint PRIMITIVE_RESTART_FIXED_INDEX = 0x8D69;
    public const uint ANY_SAMPLES_PASSED_CONSERVATIVE = 0x8D6A;
    public const uint MAX_ELEMENT_INDEX = 0x8D6B;
    public const uint COMPUTE_SHADER = 0x91B9;
    public const uint MAX_COMPUTE_UNIFORM_BLOCKS = 0x91BB;
    public const uint MAX_COMPUTE_TEXTURE_IMAGE_UNITS = 0x91BC;
    public const uint MAX_COMPUTE_IMAGE_UNIFORMS = 0x91BD;
    public const uint MAX_COMPUTE_SHARED_MEMORY_SIZE = 0x8262;
    public const uint MAX_COMPUTE_UNIFORM_COMPONENTS = 0x8263;
    public const uint MAX_COMPUTE_ATOMIC_COUNTER_BUFFERS = 0x8264;
    public const uint MAX_COMPUTE_ATOMIC_COUNTERS = 0x8265;
    public const uint MAX_COMBINED_COMPUTE_UNIFORM_COMPONENTS = 0x8266;
    public const uint MAX_COMPUTE_WORK_GROUP_INVOCATIONS = 0x90EB;
    public const uint MAX_COMPUTE_WORK_GROUP_COUNT = 0x91BE;
    public const uint MAX_COMPUTE_WORK_GROUP_SIZE = 0x91BF;
    public const uint COMPUTE_WORK_GROUP_SIZE = 0x8267;
    public const uint UNIFORM_BLOCK_REFERENCED_BY_COMPUTE_SHADER = 0x90EC;
    public const uint ATOMIC_COUNTER_BUFFER_REFERENCED_BY_COMPUTE_SHADER = 0x90ED;
    public const uint DISPATCH_INDIRECT_BUFFER = 0x90EE;
    public const uint DISPATCH_INDIRECT_BUFFER_BINDING = 0x90EF;
    public const uint COMPUTE_SHADER_BIT = 0x00000020;
    public const uint DEBUG_OUTPUT_SYNCHRONOUS = 0x8242;
    public const uint DEBUG_NEXT_LOGGED_MESSAGE_LENGTH = 0x8243;
    public const uint DEBUG_CALLBACK_FUNCTION = 0x8244;
    public const uint DEBUG_CALLBACK_USER_PARAM = 0x8245;
    public const uint DEBUG_SOURCE_API = 0x8246;
    public const uint DEBUG_SOURCE_WINDOW_SYSTEM = 0x8247;
    public const uint DEBUG_SOURCE_SHADER_COMPILER = 0x8248;
    public const uint DEBUG_SOURCE_THIRD_PARTY = 0x8249;
    public const uint DEBUG_SOURCE_APPLICATION = 0x824A;
    public const uint DEBUG_SOURCE_OTHER = 0x824B;
    public const uint DEBUG_TYPE_ERROR = 0x824C;
    public const uint DEBUG_TYPE_DEPRECATED_BEHAVIOR = 0x824D;
    public const uint DEBUG_TYPE_UNDEFINED_BEHAVIOR = 0x824E;
    public const uint DEBUG_TYPE_PORTABILITY = 0x824F;
    public const uint DEBUG_TYPE_PERFORMANCE = 0x8250;
    public const uint DEBUG_TYPE_OTHER = 0x8251;
    public const uint MAX_DEBUG_MESSAGE_LENGTH = 0x9143;
    public const uint MAX_DEBUG_LOGGED_MESSAGES = 0x9144;
    public const uint DEBUG_LOGGED_MESSAGES = 0x9145;
    public const uint DEBUG_SEVERITY_HIGH = 0x9146;
    public const uint DEBUG_SEVERITY_MEDIUM = 0x9147;
    public const uint DEBUG_SEVERITY_LOW = 0x9148;
    public const uint DEBUG_TYPE_MARKER = 0x8268;
    public const uint DEBUG_TYPE_PUSH_GROUP = 0x8269;
    public const uint DEBUG_TYPE_POP_GROUP = 0x826A;
    public const uint DEBUG_SEVERITY_NOTIFICATION = 0x826B;
    public const uint MAX_DEBUG_GROUP_STACK_DEPTH = 0x826C;
    public const uint DEBUG_GROUP_STACK_DEPTH = 0x826D;
    public const uint BUFFER = 0x82E0;
    public const uint SHADER = 0x82E1;
    public const uint PROGRAM = 0x82E2;
    public const uint QUERY = 0x82E3;
    public const uint PROGRAM_PIPELINE = 0x82E4;
    public const uint SAMPLER = 0x82E6;
    public const uint MAX_LABEL_LENGTH = 0x82E8;
    public const uint DEBUG_OUTPUT = 0x92E0;
    public const uint CONTEXT_FLAG_DEBUG_BIT = 0x00000002;
    public const uint MAX_UNIFORM_LOCATIONS = 0x826E;
    public const uint FRAMEBUFFER_DEFAULT_WIDTH = 0x9310;
    public const uint FRAMEBUFFER_DEFAULT_HEIGHT = 0x9311;
    public const uint FRAMEBUFFER_DEFAULT_LAYERS = 0x9312;
    public const uint FRAMEBUFFER_DEFAULT_SAMPLES = 0x9313;
    public const uint FRAMEBUFFER_DEFAULT_FIXED_SAMPLE_LOCATIONS = 0x9314;
    public const uint MAX_FRAMEBUFFER_WIDTH = 0x9315;
    public const uint MAX_FRAMEBUFFER_HEIGHT = 0x9316;
    public const uint MAX_FRAMEBUFFER_LAYERS = 0x9317;
    public const uint MAX_FRAMEBUFFER_SAMPLES = 0x9318;
    public const uint INTERNALFORMAT_SUPPORTED = 0x826F;
    public const uint INTERNALFORMAT_PREFERRED = 0x8270;
    public const uint INTERNALFORMAT_RED_SIZE = 0x8271;
    public const uint INTERNALFORMAT_GREEN_SIZE = 0x8272;
    public const uint INTERNALFORMAT_BLUE_SIZE = 0x8273;
    public const uint INTERNALFORMAT_ALPHA_SIZE = 0x8274;
    public const uint INTERNALFORMAT_DEPTH_SIZE = 0x8275;
    public const uint INTERNALFORMAT_STENCIL_SIZE = 0x8276;
    public const uint INTERNALFORMAT_SHARED_SIZE = 0x8277;
    public const uint INTERNALFORMAT_RED_TYPE = 0x8278;
    public const uint INTERNALFORMAT_GREEN_TYPE = 0x8279;
    public const uint INTERNALFORMAT_BLUE_TYPE = 0x827A;
    public const uint INTERNALFORMAT_ALPHA_TYPE = 0x827B;
    public const uint INTERNALFORMAT_DEPTH_TYPE = 0x827C;
    public const uint INTERNALFORMAT_STENCIL_TYPE = 0x827D;
    public const uint MAX_WIDTH = 0x827E;
    public const uint MAX_HEIGHT = 0x827F;
    public const uint MAX_DEPTH = 0x8280;
    public const uint MAX_LAYERS = 0x8281;
    public const uint MAX_COMBINED_DIMENSIONS = 0x8282;
    public const uint COLOR_COMPONENTS = 0x8283;
    public const uint DEPTH_COMPONENTS = 0x8284;
    public const uint STENCIL_COMPONENTS = 0x8285;
    public const uint COLOR_RENDERABLE = 0x8286;
    public const uint DEPTH_RENDERABLE = 0x8287;
    public const uint STENCIL_RENDERABLE = 0x8288;
    public const uint FRAMEBUFFER_RENDERABLE = 0x8289;
    public const uint FRAMEBUFFER_RENDERABLE_LAYERED = 0x828A;
    public const uint FRAMEBUFFER_BLEND = 0x828B;
    public const uint READ_PIXELS = 0x828C;
    public const uint READ_PIXELS_FORMAT = 0x828D;
    public const uint READ_PIXELS_TYPE = 0x828E;
    public const uint TEXTURE_IMAGE_FORMAT = 0x828F;
    public const uint TEXTURE_IMAGE_TYPE = 0x8290;
    public const uint GET_TEXTURE_IMAGE_FORMAT = 0x8291;
    public const uint GET_TEXTURE_IMAGE_TYPE = 0x8292;
    public const uint MIPMAP = 0x8293;
    public const uint MANUAL_GENERATE_MIPMAP = 0x8294;
    public const uint AUTO_GENERATE_MIPMAP = 0x8295;
    public const uint COLOR_ENCODING = 0x8296;
    public const uint SRGB_READ = 0x8297;
    public const uint SRGB_WRITE = 0x8298;
    public const uint FILTER = 0x829A;
    public const uint VERTEX_TEXTURE = 0x829B;
    public const uint TESS_CONTROL_TEXTURE = 0x829C;
    public const uint TESS_EVALUATION_TEXTURE = 0x829D;
    public const uint GEOMETRY_TEXTURE = 0x829E;
    public const uint FRAGMENT_TEXTURE = 0x829F;
    public const uint COMPUTE_TEXTURE = 0x82A0;
    public const uint TEXTURE_SHADOW = 0x82A1;
    public const uint TEXTURE_GATHER = 0x82A2;
    public const uint TEXTURE_GATHER_SHADOW = 0x82A3;
    public const uint SHADER_IMAGE_LOAD = 0x82A4;
    public const uint SHADER_IMAGE_STORE = 0x82A5;
    public const uint SHADER_IMAGE_ATOMIC = 0x82A6;
    public const uint IMAGE_TEXEL_SIZE = 0x82A7;
    public const uint IMAGE_COMPATIBILITY_CLASS = 0x82A8;
    public const uint IMAGE_PIXEL_FORMAT = 0x82A9;
    public const uint IMAGE_PIXEL_TYPE = 0x82AA;
    public const uint SIMULTANEOUS_TEXTURE_AND_DEPTH_TEST = 0x82AC;
    public const uint SIMULTANEOUS_TEXTURE_AND_STENCIL_TEST = 0x82AD;
    public const uint SIMULTANEOUS_TEXTURE_AND_DEPTH_WRITE = 0x82AE;
    public const uint SIMULTANEOUS_TEXTURE_AND_STENCIL_WRITE = 0x82AF;
    public const uint TEXTURE_COMPRESSED_BLOCK_WIDTH = 0x82B1;
    public const uint TEXTURE_COMPRESSED_BLOCK_HEIGHT = 0x82B2;
    public const uint TEXTURE_COMPRESSED_BLOCK_SIZE = 0x82B3;
    public const uint CLEAR_BUFFER = 0x82B4;
    public const uint TEXTURE_VIEW = 0x82B5;
    public const uint VIEW_COMPATIBILITY_CLASS = 0x82B6;
    public const uint FULL_SUPPORT = 0x82B7;
    public const uint CAVEAT_SUPPORT = 0x82B8;
    public const uint IMAGE_CLASS_4_X_32 = 0x82B9;
    public const uint IMAGE_CLASS_2_X_32 = 0x82BA;
    public const uint IMAGE_CLASS_1_X_32 = 0x82BB;
    public const uint IMAGE_CLASS_4_X_16 = 0x82BC;
    public const uint IMAGE_CLASS_2_X_16 = 0x82BD;
    public const uint IMAGE_CLASS_1_X_16 = 0x82BE;
    public const uint IMAGE_CLASS_4_X_8 = 0x82BF;
    public const uint IMAGE_CLASS_2_X_8 = 0x82C0;
    public const uint IMAGE_CLASS_1_X_8 = 0x82C1;
    public const uint IMAGE_CLASS_11_11_10 = 0x82C2;
    public const uint IMAGE_CLASS_10_10_10_2 = 0x82C3;
    public const uint VIEW_CLASS_128_BITS = 0x82C4;
    public const uint VIEW_CLASS_96_BITS = 0x82C5;
    public const uint VIEW_CLASS_64_BITS = 0x82C6;
    public const uint VIEW_CLASS_48_BITS = 0x82C7;
    public const uint VIEW_CLASS_32_BITS = 0x82C8;
    public const uint VIEW_CLASS_24_BITS = 0x82C9;
    public const uint VIEW_CLASS_16_BITS = 0x82CA;
    public const uint VIEW_CLASS_8_BITS = 0x82CB;
    public const uint VIEW_CLASS_S3TC_DXT1_RGB = 0x82CC;
    public const uint VIEW_CLASS_S3TC_DXT1_RGBA = 0x82CD;
    public const uint VIEW_CLASS_S3TC_DXT3_RGBA = 0x82CE;
    public const uint VIEW_CLASS_S3TC_DXT5_RGBA = 0x82CF;
    public const uint VIEW_CLASS_RGTC1_RED = 0x82D0;
    public const uint VIEW_CLASS_RGTC2_RG = 0x82D1;
    public const uint VIEW_CLASS_BPTC_UNORM = 0x82D2;
    public const uint VIEW_CLASS_BPTC_FLOAT = 0x82D3;
    public const uint UNIFORM = 0x92E1;
    public const uint UNIFORM_BLOCK = 0x92E2;
    public const uint PROGRAM_INPUT = 0x92E3;
    public const uint PROGRAM_OUTPUT = 0x92E4;
    public const uint BUFFER_VARIABLE = 0x92E5;
    public const uint SHADER_STORAGE_BLOCK = 0x92E6;
    public const uint VERTEX_SUBROUTINE = 0x92E8;
    public const uint TESS_CONTROL_SUBROUTINE = 0x92E9;
    public const uint TESS_EVALUATION_SUBROUTINE = 0x92EA;
    public const uint GEOMETRY_SUBROUTINE = 0x92EB;
    public const uint FRAGMENT_SUBROUTINE = 0x92EC;
    public const uint COMPUTE_SUBROUTINE = 0x92ED;
    public const uint VERTEX_SUBROUTINE_UNIFORM = 0x92EE;
    public const uint TESS_CONTROL_SUBROUTINE_UNIFORM = 0x92EF;
    public const uint TESS_EVALUATION_SUBROUTINE_UNIFORM = 0x92F0;
    public const uint GEOMETRY_SUBROUTINE_UNIFORM = 0x92F1;
    public const uint FRAGMENT_SUBROUTINE_UNIFORM = 0x92F2;
    public const uint COMPUTE_SUBROUTINE_UNIFORM = 0x92F3;
    public const uint TRANSFORM_FEEDBACK_VARYING = 0x92F4;
    public const uint ACTIVE_RESOURCES = 0x92F5;
    public const uint MAX_NAME_LENGTH = 0x92F6;
    public const uint MAX_NUM_ACTIVE_VARIABLES = 0x92F7;
    public const uint MAX_NUM_COMPATIBLE_SUBROUTINES = 0x92F8;
    public const uint NAME_LENGTH = 0x92F9;
    public const uint TYPE = 0x92FA;
    public const uint ARRAY_SIZE = 0x92FB;
    public const uint OFFSET = 0x92FC;
    public const uint BLOCK_INDEX = 0x92FD;
    public const uint ARRAY_STRIDE = 0x92FE;
    public const uint MATRIX_STRIDE = 0x92FF;
    public const uint IS_ROW_MAJOR = 0x9300;
    public const uint ATOMIC_COUNTER_BUFFER_INDEX = 0x9301;
    public const uint BUFFER_BINDING = 0x9302;
    public const uint BUFFER_DATA_SIZE = 0x9303;
    public const uint NUM_ACTIVE_VARIABLES = 0x9304;
    public const uint ACTIVE_VARIABLES = 0x9305;
    public const uint REFERENCED_BY_VERTEX_SHADER = 0x9306;
    public const uint REFERENCED_BY_TESS_CONTROL_SHADER = 0x9307;
    public const uint REFERENCED_BY_TESS_EVALUATION_SHADER = 0x9308;
    public const uint REFERENCED_BY_GEOMETRY_SHADER = 0x9309;
    public const uint REFERENCED_BY_FRAGMENT_SHADER = 0x930A;
    public const uint REFERENCED_BY_COMPUTE_SHADER = 0x930B;
    public const uint TOP_LEVEL_ARRAY_SIZE = 0x930C;
    public const uint TOP_LEVEL_ARRAY_STRIDE = 0x930D;
    public const uint LOCATION = 0x930E;
    public const uint LOCATION_INDEX = 0x930F;
    public const uint IS_PER_PATCH = 0x92E7;
    public const uint SHADER_STORAGE_BUFFER = 0x90D2;
    public const uint SHADER_STORAGE_BUFFER_BINDING = 0x90D3;
    public const uint SHADER_STORAGE_BUFFER_START = 0x90D4;
    public const uint SHADER_STORAGE_BUFFER_SIZE = 0x90D5;
    public const uint MAX_VERTEX_SHADER_STORAGE_BLOCKS = 0x90D6;
    public const uint MAX_GEOMETRY_SHADER_STORAGE_BLOCKS = 0x90D7;
    public const uint MAX_TESS_CONTROL_SHADER_STORAGE_BLOCKS = 0x90D8;
    public const uint MAX_TESS_EVALUATION_SHADER_STORAGE_BLOCKS = 0x90D9;
    public const uint MAX_FRAGMENT_SHADER_STORAGE_BLOCKS = 0x90DA;
    public const uint MAX_COMPUTE_SHADER_STORAGE_BLOCKS = 0x90DB;
    public const uint MAX_COMBINED_SHADER_STORAGE_BLOCKS = 0x90DC;
    public const uint MAX_SHADER_STORAGE_BUFFER_BINDINGS = 0x90DD;
    public const uint MAX_SHADER_STORAGE_BLOCK_SIZE = 0x90DE;
    public const uint SHADER_STORAGE_BUFFER_OFFSET_ALIGNMENT = 0x90DF;
    public const uint SHADER_STORAGE_BARRIER_BIT = 0x00002000;
    public const uint MAX_COMBINED_SHADER_OUTPUT_RESOURCES = 0x8F39;
    public const uint DEPTH_STENCIL_TEXTURE_MODE = 0x90EA;
    public const uint TEXTURE_BUFFER_OFFSET = 0x919D;
    public const uint TEXTURE_BUFFER_SIZE = 0x919E;
    public const uint TEXTURE_BUFFER_OFFSET_ALIGNMENT = 0x919F;
    public const uint TEXTURE_VIEW_MIN_LEVEL = 0x82DB;
    public const uint TEXTURE_VIEW_NUM_LEVELS = 0x82DC;
    public const uint TEXTURE_VIEW_MIN_LAYER = 0x82DD;
    public const uint TEXTURE_VIEW_NUM_LAYERS = 0x82DE;
    public const uint TEXTURE_IMMUTABLE_LEVELS = 0x82DF;
    public const uint VERTEX_ATTRIB_BINDING = 0x82D4;
    public const uint VERTEX_ATTRIB_RELATIVE_OFFSET = 0x82D5;
    public const uint VERTEX_BINDING_DIVISOR = 0x82D6;
    public const uint VERTEX_BINDING_OFFSET = 0x82D7;
    public const uint VERTEX_BINDING_STRIDE = 0x82D8;
    public const uint MAX_VERTEX_ATTRIB_RELATIVE_OFFSET = 0x82D9;
    public const uint MAX_VERTEX_ATTRIB_BINDINGS = 0x82DA;
    public const uint VERTEX_BINDING_BUFFER = 0x8F4F;
    public const uint DISPLAY_LIST = 0x82E7;
    public const uint MAX_VERTEX_ATTRIB_STRIDE = 0x82E5;
    public const uint PRIMITIVE_RESTART_FOR_PATCHES_SUPPORTED = 0x8221;
    public const uint TEXTURE_BUFFER_BINDING = 0x8C2A;
    public const uint MAP_PERSISTENT_BIT = 0x0040;
    public const uint MAP_COHERENT_BIT = 0x0080;
    public const uint DYNAMIC_STORAGE_BIT = 0x0100;
    public const uint CLIENT_STORAGE_BIT = 0x0200;
    public const uint CLIENT_MAPPED_BUFFER_BARRIER_BIT = 0x00004000;
    public const uint BUFFER_IMMUTABLE_STORAGE = 0x821F;
    public const uint BUFFER_STORAGE_FLAGS = 0x8220;
    public const uint CLEAR_TEXTURE = 0x9365;
    public const uint LOCATION_COMPONENT = 0x934A;
    public const uint TRANSFORM_FEEDBACK_BUFFER_INDEX = 0x934B;
    public const uint TRANSFORM_FEEDBACK_BUFFER_STRIDE = 0x934C;
    public const uint QUERY_BUFFER = 0x9192;
    public const uint QUERY_BUFFER_BARRIER_BIT = 0x00008000;
    public const uint QUERY_BUFFER_BINDING = 0x9193;
    public const uint QUERY_RESULT_NO_WAIT = 0x9194;
    public const uint MIRROR_CLAMP_TO_EDGE = 0x8743;
    public const uint CONTEXT_LOST = 0x0507;
    public const uint NEGATIVE_ONE_TO_ONE = 0x935E;
    public const uint ZERO_TO_ONE = 0x935F;
    public const uint CLIP_ORIGIN = 0x935C;
    public const uint CLIP_DEPTH_MODE = 0x935D;
    public const uint QUERY_WAIT_INVERTED = 0x8E17;
    public const uint QUERY_NO_WAIT_INVERTED = 0x8E18;
    public const uint QUERY_BY_REGION_WAIT_INVERTED = 0x8E19;
    public const uint QUERY_BY_REGION_NO_WAIT_INVERTED = 0x8E1A;
    public const uint MAX_CULL_DISTANCES = 0x82F9;
    public const uint MAX_COMBINED_CLIP_AND_CULL_DISTANCES = 0x82FA;
    public const uint TEXTURE_TARGET = 0x1006;
    public const uint QUERY_TARGET = 0x82EA;
    public const uint GUILTY_CONTEXT_RESET = 0x8253;
    public const uint INNOCENT_CONTEXT_RESET = 0x8254;
    public const uint UNKNOWN_CONTEXT_RESET = 0x8255;
    public const uint RESET_NOTIFICATION_STRATEGY = 0x8256;
    public const uint LOSE_CONTEXT_ON_RESET = 0x8252;
    public const uint NO_RESET_NOTIFICATION = 0x8261;
    public const uint CONTEXT_FLAG_ROBUST_ACCESS_BIT = 0x00000004;
    public const uint CONTEXT_RELEASE_BEHAVIOR = 0x82FB;
    public const uint CONTEXT_RELEASE_BEHAVIOR_FLUSH = 0x82FC;
}