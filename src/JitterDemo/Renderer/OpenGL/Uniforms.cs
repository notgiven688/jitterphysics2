/* Copyright <2021> <Thorben Linneweber>
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

using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public abstract class Uniform
{
    public ShaderProgram shader;
    public int location;

    public Uniform(ShaderProgram shader, int location)
    {
        this.shader = shader;
        this.location = location;
    }
}

public class UniformUint : Uniform
{
    public UniformUint(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(uint value)
    {
        GL.Uniform1ui(location, value);
    }

    public void Set(uint[] values)
    {
        unsafe
        {
            fixed (uint* first = values)
            {
                GL.Uniform1uiv(location, values.Length, first);
            }
        }
    }
}

public class UniformBool : Uniform
{
    public UniformBool(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(bool value)
    {
        GL.Uniform1i(location, value ? 1 : 0);
    }
}

public class UniformFloat : Uniform
{
    public UniformFloat(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(float value)
    {
        GL.Uniform1f(location, value);
    }

    public void Set(float[] values)
    {
        unsafe
        {
            fixed (float* first = values)
            {
                GL.Uniform1fv(location, values.Length, first);
            }
        }
    }
}

public class UniformTexture : Uniform
{
    public UniformTexture(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(int value)
    {
        GL.Uniform1i(location, value);
    }
}

public class UniformMatrix4 : Uniform
{
    public UniformMatrix4(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(Matrix4[] value, bool transpose)
    {
        unsafe
        {
            fixed (float* first = &value[0].M11)
            {
                GL.UniformMatrix4fv(location, value.Length, transpose, first);
            }
        }
    }

    public void Set(in Matrix4 value, bool transpose)
    {
        unsafe
        {
            fixed (float* first = &value.M11)
            {
                GL.UniformMatrix4fv(location, 1, transpose, first);
            }
        }
    }

    public void Set(in Matrix4 value)
    {
        Set(value, false);
    }
}

public class UniformVector2 : Uniform
{
    public UniformVector2(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(in Vector2 value)
    {
        unsafe
        {
            fixed (float* first = &value.X)
            {
                GL.Uniform2fv(location, 1, first);
            }
        }
    }

    public void Set(Vector2[] value)
    {
        unsafe
        {
            fixed (float* first = &value[0].X)
            {
                GL.Uniform2fv(location, value.Length, first);
            }
        }
    }

    public void Set(float x, float y)
    {
        GL.Uniform2f(location, x, y);
    }
}

public class UniformVector3 : Uniform
{
    public UniformVector3(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(in Vector3 value)
    {
        unsafe
        {
            fixed (float* first = &value.X)
            {
                GL.Uniform3fv(location, 1, first);
            }
        }
    }

    public void Set(float x, float y, float z)
    {
        GL.Uniform3f(location, x, y, z);
    }
}

public class UniformVector4 : Uniform
{
    public UniformVector4(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(in Vector4 value)
    {
        unsafe
        {
            fixed (float* first = &value.X)
            {
                GL.Uniform4fv(location, 1, first);
            }
        }
    }

    public void Set(float x, float y, float z, float w)
    {
        GL.Uniform4f(location, x, y, z, w);
    }
}