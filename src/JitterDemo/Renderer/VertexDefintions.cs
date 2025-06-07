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

using System.Runtime.InteropServices;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 Texture;

    public Vertex(Vector3 position)
    {
        Position = position;
        Normal = Vector3.UnitX;
        Texture = Vector2.Zero;
    }

    public Vertex(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
        Texture = Vector2.Zero;
    }

    public Vertex(Vector3 position, Vector3 normal, Vector2 texture)
    {
        Position = position;
        Normal = normal;
        Texture = texture;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct TriangleVertexIndex
{
    public uint T1;
    public uint T2;
    public uint T3;

    public TriangleVertexIndex(uint t1, uint t2, uint t3)
    {
        T1 = t1;
        T2 = t2;
        T3 = t3;
    }

    public TriangleVertexIndex(int t1, int t2, int t3)
    {
        T1 = (uint)t1;
        T2 = (uint)t2;
        T3 = (uint)t3;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct LineVertexIndex
{
    public uint T1;
    public uint T2;

    public LineVertexIndex(uint t1, uint t2)
    {
        T1 = t1;
        T2 = t2;
    }

    public LineVertexIndex(int t1, int t2)
    {
        T1 = (uint)t1;
        T2 = (uint)t2;
    }
}