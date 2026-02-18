using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

[GLObject(GLObjectType.ElementArrayBuffer)]
public sealed class ElementArrayBuffer : GLBuffer
{
    public ElementArrayBuffer() : base(GLC.ELEMENT_ARRAY_BUFFER)
    {
    }

    public ElementArrayBuffer(uint buffer) : base(buffer, GLC.ELEMENT_ARRAY_BUFFER)
    {
    }
}