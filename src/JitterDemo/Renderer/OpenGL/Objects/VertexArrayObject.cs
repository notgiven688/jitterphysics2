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

    public ElementArrayBuffer? ElementArrayBuffer
    {
        get
        {
            Bind();
            uint buffer = (uint)GL.GetIntegerv(GLC.ELEMENT_ARRAY_BUFFER_BINDING);
            return buffer == 0 ? null : new ElementArrayBuffer(buffer);
        }
        set
        {
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