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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public enum ShaderType : uint
{
    Vertex = GLC.VERTEX_SHADER,
    Fragment = GLC.FRAGMENT_SHADER,
    Geometry = GLC.GEOMETRY_SHADER
}

public class BasicShader : ShaderProgram
{
    public BasicShader(string vertex, string fragment)
    {
        Shaders.Add(new Shader(ShaderType.Vertex, vertex));
        Shaders.Add(new Shader(ShaderType.Fragment, fragment));
        Link();
    }
}

public class ShaderCompileException : Exception
{
    private static string Embed(string msg)
    {
        return $"An error occured while compiling a shader:{Environment.NewLine}{msg}";
    }

    public ShaderCompileException(string msg) : base(Embed(msg))
    {
    }
}

public class ShaderLinkException : Exception
{
    private static string Embed(string msg)
    {
        return $"An error occured while linking a shader:{Environment.NewLine}{msg}";
    }

    public ShaderLinkException(string msg) : base(Embed(msg))
    {
    }
}

public class ShaderException : Exception
{
    public ShaderException(string msg) : base(msg)
    {
    }
}

[GLObject(GLObjectType.Shader)]
public class Shader : GLObject
{
    public string Code { get; }
    public ShaderType Type { private set; get; }

    public Shader(ShaderType type, string code) : base(GL.CreateShader((uint)type))
    {
        Code = code;
        Type = type;

        CompileShader();
    }

    private void CompileShader()
    {
        GL.ShaderSource(Handle, 1, new[] { Code }, null);
        GL.CompileShader(Handle);
        GL.GetShaderiv(Handle, GLC.COMPILE_STATUS, out int success);

        if (success == GLC.FALSE)
        {
            GL.GetShaderInfoLog(Handle, 1024, out int length, out string infoLog);
#if DEBUG
            System.Diagnostics.Debug.Fail(infoLog);
#endif
            throw new ShaderCompileException(infoLog);
        }
    }

    // TODO: delete shader
}

[GLObject(GLObjectType.ShaderProgram)]
public class ShaderProgram : GLObject
{
    public ReadOnlyDictionary<string, Uniform> Uniforms { get; private set; } = null!;

    public List<Shader> Shaders { get; }

    public ShaderProgram() : base(GL.CreateProgram())
    {
        Shaders = new List<Shader>();
    }

    // TODO: better error handling
    public T GetUniform<T>(string uniform) where T : Uniform
    {
        if (!Uniforms.TryGetValue(uniform, out Uniform? val))
        {
            throw new ShaderException("Could not find uniform");
        }

        if (val is not T result)
        {
            throw new ShaderException("Uniform is not of type ..");
        }

        return result;
    }

    public void Link()
    {
        for (int i = 0; i < Shaders.Count; i++)
        {
            GL.AttachShader(Handle, Shaders[i].Handle);
        }

        GL.LinkProgram(Handle);

        GL.GetProgramiv(Handle, GLC.LINK_STATUS, out int success);

        if (success == GLC.FALSE)
        {
            GL.GetProgramInfoLog(Handle, 1024, out int _, out string infoLog);
#if DEBUG
            Debug.Fail(infoLog);
#endif
            throw new ShaderLinkException(infoLog);
        }

        // TODO: delete shader
        PrepareUniforms();
    }

    public void Use()
    {
        // TODO: add bind() to object class?
        GL.UseProgram(Handle);
    }

    protected void PrepareUniforms()
    {
        const int MaxNameLen = 256;

        Dictionary<string, Uniform> uniforms = new();
        Uniforms = new ReadOnlyDictionary<string, Uniform>(uniforms);

        GL.GetProgramiv(Handle, GLC.ACTIVE_UNIFORMS, out int count);

        for (uint i = 0; i < count; i++)
        {
            GL.GetActiveUniform(Handle, i, MaxNameLen, out int length, out int _, out uint type, out string name);

            if (length + 1 == MaxNameLen)
            {
                throw new InvalidOperationException($"Uniform name longer than {MaxNameLen} characters!");
            }

            int location = GL.GetUniformLocation(Handle, name);

            if (location == -1)
            {
                throw new InvalidOperationException($"Could not get uniform location of {name}.");
            }

            switch (type)
            {
                case GLC.FLOAT:
                    uniforms.Add(name, new UniformFloat(this, location));
                    break;
                case GLC.FLOAT_VEC2:
                    uniforms.Add(name, new UniformVector2(this, location));
                    break;
                case GLC.FLOAT_VEC3:
                    uniforms.Add(name, new UniformVector3(this, location));
                    break;
                case GLC.FLOAT_VEC4:
                    uniforms.Add(name, new UniformVector4(this, location));
                    break;
                case GLC.FLOAT_MAT4:
                    uniforms.Add(name, new UniformMatrix4(this, location));
                    break;
                case GLC.SAMPLER_2D:
                    uniforms.Add(name, new UniformTexture(this, location));
                    break;
                case GLC.BOOL:
                    uniforms.Add(name, new UniformBool(this, location));
                    break;
                case GLC.UNSIGNED_INT:
                    uniforms.Add(name, new UniformUint(this, location));
                    break;
                default:
                    throw new NotImplementedException($"Type '{type}' not supported!");
            }
        }
    }
}