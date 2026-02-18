using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public enum VertexAttributeType : uint
{
    Byte = GLC.BYTE,
    UnsignedByte = GLC.UNSIGNED_BYTE,
    Short = GLC.SHORT,
    Int = GLC.INT,
    UnsignedInt = GLC.UNSIGNED_INT,
    HalfFloat = GLC.HALF_FLOAT,
    Float = GLC.FLOAT,
    Double = GLC.DOUBLE,
    Fixed = GLC.FIXED
}

public class VertexAttribute
{
    public VertexArrayObject vao;
    public uint index;

    public bool Enable
    {
        set
        {
            vao.Bind();
            if (value) GL.EnableVertexAttribArray(index);
            else GL.DisableVertexAttribArray(index);
        }
    }

    public uint Divisor
    {
        set
        {
            vao.Bind();
            GL.VertexAttribDivisor(index, value);
        }
    }

    public VertexAttribute(VertexArrayObject vao, uint index)
    {
        this.vao = vao;
        this.index = index;
    }

    public void Set(ArrayBuffer arrayBuffer, int size, VertexAttributeType type, bool normalized, int stride,
        int pointer)
    {
        vao.Bind();
        arrayBuffer.Bind();
        GL.EnableVertexAttribArray(index);
        GL.VertexAttribPointer(index, size, (uint)type, normalized, stride, pointer);
    }
}

public class IndexedVertexAttribute
{
    private readonly VertexAttribute[] vertexAttributes;

    public IndexedVertexAttribute(VertexArrayObject vao, int count)
    {
        vertexAttributes = new VertexAttribute[count];
        for (int i = 0; i < count; i++) vertexAttributes[i] = new VertexAttribute(vao, (uint)i);
    }

    public VertexAttribute this[int i] => vertexAttributes[i];
}

[GLObject(GLObjectType.VertexArrayObject)]
public sealed class VertexArrayObject : GLObject
{
    public static VertexArrayObject Default = new(0);

    public uint VAO;

    public IndexedVertexAttribute VertexAttributes { get; private set; }

    private ElementArrayBuffer? elementArrayBuffer;

    public ElementArrayBuffer? ElementArrayBuffer
    {
        get => elementArrayBuffer;
        set
        {
            elementArrayBuffer = value;
            Bind();
            value?.Bind();
        }
    }

    public VertexArrayObject(uint vao) : base(vao)
    {
        VertexAttributes = new IndexedVertexAttribute(this, 16);
    }

    public VertexArrayObject() : base(GL.GenVertexArray())
    {
        VertexAttributes = new IndexedVertexAttribute(this, 16);
    }

    public void Bind()
    {
        GL.BindVertexArray(Handle);
    }

    public bool IsActive()
    {
        int handle = GL.GetIntegerv(GLC.VERTEX_ARRAY_BINDING);
        return handle == Handle;
    }
}