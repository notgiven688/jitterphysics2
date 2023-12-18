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

using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class LineShader : BasicShader
{
    public UniformVector4 Color { get; }

    public UniformMatrix4 View { get; }
    public UniformMatrix4 Projection { get; }

    public LineShader() : base(vshader, fshader)
    {
        View = GetUniform<UniformMatrix4>("view");
        Projection = GetUniform<UniformMatrix4>("projection");
        Color = GetUniform<UniformVector4>("color");
    }

    private static readonly string vshader = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        
        uniform mat4 view;
        uniform mat4 projection;

        void main()
        {
            gl_Position = projection * view * vec4(aPos, 1.0);
        }
        ";

    private static readonly string fshader = @"
        #version 420 core

        uniform vec4 color;
    
        out vec4 FragColor;
     
        void main()
        {
            FragColor = color;
        }

        ";
}

public class DebugRenderer
{
    public enum Color
    {
        White = 0,
        Red = 1,
        Green = 2,
        NumColor = 3
    }

    private readonly Vector4[] colors =
    {
        new(1, 1, 1, 1),
        new(1, 0, 0, 1),
        new(0, 1, 0, 1)
    };

    private struct Line
    {
        public Vector3 From;
        public Vector3 To;

        public Line(in Vector3 from, in Vector3 to)
        {
            From = from;
            To = to;
        }

        public Line(float minx, float miny, float minz, float maxx, float maxy, float maxz)
        {
            From = new Vector3(minx, miny, minz);
            To = new Vector3(maxx, maxy, maxz);
        }
    }

    private class LineBuffer
    {
        public Vector3[] vertices = new Vector3[64];
        public LineVertexIndex[] indices = new LineVertexIndex[64];

        public int VertexCount;
        public int IndexCount;

        public void Add(float x, float y, float z)
        {
            if (VertexCount == vertices.Length)
            {
                Array.Resize(ref vertices, VertexCount * 2);
            }

            vertices[VertexCount++] = new Vector3(x, y, z);
        }

        public void Add(uint i1, uint i2)
        {
            if (IndexCount == indices.Length)
            {
                Array.Resize(ref indices, IndexCount * 2);
            }

            indices[IndexCount++] = new LineVertexIndex(i1, i2);
        }

        public void Clear()
        {
            VertexCount = IndexCount = 0;
        }
    }

    private List<LineBuffer> buffer = null!;

    private VertexArrayObject Vao = null!;
    private LineShader shader = null!;
    private ArrayBuffer ab = null!;
    private ElementArrayBuffer eab = null!;

    protected ArrayBuffer worldMatrices = null!;

    public void Draw()
    {
        Camera camera = RenderWindow.Instance.Camera;

        shader.Use();

        shader.Color.Set(new Vector4(0, 1, 0, 1));

        shader.Projection.Set(camera.ProjectionMatrix);
        shader.View.Set(camera.ViewMatrix);

        Vao.Bind();

        for (int i = 0; i < (int)Color.NumColor; i++)
        {
            var lines = buffer[i];

            if (lines.IndexCount == 0) continue;

            eab.SetData(lines.indices, lines.IndexCount);
            ab.SetData(lines.vertices, lines.VertexCount);

            shader.Color.Set(colors[i]);
            GLDevice.DrawElements(DrawMode.Lines, lines.IndexCount * 2, IndexType.UnsignedInt, 0);

            lines.Clear();
        }

        GLDevice.Disable(Capability.Blend);
        GLDevice.Enable(Capability.DepthTest);
        GLDevice.Enable(Capability.CullFace);
    }

    public void PushLine(Color color, in Vector3 pointA, in Vector3 pointB)
    {
        var list = buffer[(int)color];
        uint offset = (uint)list.VertexCount;

        list.Add(pointA.X, pointA.Y, pointA.Z);
        list.Add(pointB.X, pointB.Y, pointB.Z);
        list.Add(offset + 0, offset + 1);
    }

    public void PushBox(Color color, in Vector3 min, in Vector3 max)
    {
        var list = buffer[(int)color];

        uint offset = (uint)list.VertexCount;

        list.Add(min.X, min.Y, min.Z);
        list.Add(max.X, min.Y, min.Z);
        list.Add(min.X, max.Y, min.Z);
        list.Add(min.X, min.Y, max.Z);
        list.Add(max.X, max.Y, min.Z);
        list.Add(min.X, max.Y, max.Z);
        list.Add(max.X, min.Y, max.Z);
        list.Add(max.X, max.Y, max.Z);

        list.Add(offset + 0, offset + 1);
        list.Add(offset + 0, offset + 2);
        list.Add(offset + 0, offset + 3);
        list.Add(offset + 1, offset + 4);
        list.Add(offset + 1, offset + 6);
        list.Add(offset + 2, offset + 4);
        list.Add(offset + 2, offset + 5);
        list.Add(offset + 3, offset + 5);
        list.Add(offset + 3, offset + 6);
        list.Add(offset + 4, offset + 7);
        list.Add(offset + 5, offset + 7);
        list.Add(offset + 6, offset + 7);
    }

    public void PushPoint(Color color, in Vector3 pos, float halfSize = 1.0f)
    {
        var list = buffer[(int)color];
        uint offset = (uint)list.VertexCount;

        list.Add(pos.X - halfSize, pos.Y, pos.Z);
        list.Add(pos.X + halfSize, pos.Y, pos.Z);
        list.Add(pos.X, pos.Y - halfSize, pos.Z);
        list.Add(pos.X, pos.Y + halfSize, pos.Z);
        list.Add(pos.X, pos.Y, pos.Z - halfSize);
        list.Add(pos.X, pos.Y, pos.Z + halfSize);

        list.Add(offset + 0, offset + 1);
        list.Add(offset + 2, offset + 3);
        list.Add(offset + 4, offset + 5);
    }

    public void Load()
    {
        buffer = new List<LineBuffer>();

        for (int i = 0; i < (int)Color.NumColor; i++)
        {
            buffer.Add(new LineBuffer());
        }

        shader = new LineShader();

        Vao = new VertexArrayObject();

        ab = new ArrayBuffer();
        eab = new ElementArrayBuffer();

        int sof = sizeof(float);

        Vao.VertexAttributes[0].Set(ab, 3, VertexAttributeType.Float, false, 3 * sof, 0 * sof); // position
        Vao.ElementArrayBuffer = eab;
    }
}