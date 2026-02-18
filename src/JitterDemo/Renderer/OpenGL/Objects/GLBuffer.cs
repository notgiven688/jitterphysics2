using System;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

[GLObjectAttribute(GLObjectType.Buffer)]
public class GLBuffer : GLObject
{
    private readonly uint type;

    private static uint GenBuffer()
    {
        VertexArrayObject.Default.Bind();
        return GL.GenBuffer();
    }

    public GLBuffer(uint type) : base(GenBuffer())
    {
        this.type = type;
    }

    public GLBuffer(uint buffer, uint type) : base(buffer)
    {
        this.type = type;
    }

    public void Bind()
    {
        GL.BindBuffer(type, Handle);
    }

    public void SetData<T>(T[] vertices, int size, uint usage = GLC.STATIC_DRAW) where T : unmanaged
    {
        if (size > vertices.Length) throw new Exception();

        Bind();

        unsafe
        {
            fixed (T* first = vertices)
            {
                GL.BufferData(type, sizeof(T) * size, (IntPtr)first, usage);
            }
        }
    }

    public void SetData<T>(T[] vertices, uint usage = GLC.STATIC_DRAW) where T : unmanaged
    {
        SetData(vertices, vertices.Length, usage);
    }

    public void SetData(IntPtr data, int size, uint usage = GLC.STATIC_DRAW)
    {
        Bind();
        GL.BufferData(type, size, data, usage);
    }

    public void SetData(IntPtr data, int offset, int size)
    {
        Bind();
        GL.BufferSubData(type, offset, size, data);
    }
}