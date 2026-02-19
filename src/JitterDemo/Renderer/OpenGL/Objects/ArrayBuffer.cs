using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

[GLObject(GLObjectType.ArrayBuffer)]
public sealed class ArrayBuffer : GLBuffer
{
    public ArrayBuffer() : base(GLC.ARRAY_BUFFER)
    {
    }

    public ArrayBuffer(uint buffer) : base(buffer, GLC.ARRAY_BUFFER)
    {
    }
}