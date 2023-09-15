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