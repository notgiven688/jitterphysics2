using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public abstract class Uniform
{
    protected readonly ShaderProgram Shader;
    protected readonly int Location;

    protected Uniform(ShaderProgram shader, int location)
    {
        Shader = shader;
        Location = location;
    }
}

public class UniformUint : Uniform
{
    public UniformUint(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(uint value)
    {
        GL.Uniform1ui(Location, value);
    }

    public void Set(uint[] values)
    {
        unsafe
        {
            fixed (uint* first = values)
            {
                GL.Uniform1uiv(Location, values.Length, first);
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
        GL.Uniform1i(Location, value ? 1 : 0);
    }
}

public class UniformFloat : Uniform
{
    public UniformFloat(ShaderProgram shader, int location) : base(shader, location)
    {
    }

    public void Set(float value)
    {
        GL.Uniform1f(Location, value);
    }

    public void Set(float[] values)
    {
        unsafe
        {
            fixed (float* first = values)
            {
                GL.Uniform1fv(Location, values.Length, first);
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
        GL.Uniform1i(Location, value);
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
                GL.UniformMatrix4fv(Location, value.Length, transpose, first);
            }
        }
    }

    public void Set(in Matrix4 value, bool transpose)
    {
        unsafe
        {
            fixed (float* first = &value.M11)
            {
                GL.UniformMatrix4fv(Location, 1, transpose, first);
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
                GL.Uniform2fv(Location, 1, first);
            }
        }
    }

    public void Set(Vector2[] value)
    {
        unsafe
        {
            fixed (float* first = &value[0].X)
            {
                GL.Uniform2fv(Location, value.Length, first);
            }
        }
    }

    public void Set(float x, float y)
    {
        GL.Uniform2f(Location, x, y);
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
                GL.Uniform3fv(Location, 1, first);
            }
        }
    }

    public void Set(float x, float y, float z)
    {
        GL.Uniform3f(Location, x, y, z);
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
                GL.Uniform4fv(Location, 1, first);
            }
        }
    }

    public void Set(float x, float y, float z, float w)
    {
        GL.Uniform4f(Location, x, y, z, w);
    }
}