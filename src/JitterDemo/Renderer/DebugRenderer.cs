using System;
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
        #version 330 core

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
        public Vector3[] Vertices = new Vector3[64];
        public LineVertexIndex[] Indices = new LineVertexIndex[64];

        public int VertexCount;
        public int IndexCount;

        public void Add(float x, float y, float z)
        {
            if (VertexCount == Vertices.Length)
            {
                Array.Resize(ref Vertices, VertexCount * 2);
            }

            Vertices[VertexCount++] = new Vector3(x, y, z);
        }

        public void Add(uint i1, uint i2)
        {
            if (IndexCount == Indices.Length)
            {
                Array.Resize(ref Indices, IndexCount * 2);
            }

            Indices[IndexCount++] = new LineVertexIndex(i1, i2);
        }

        public void Clear()
        {
            VertexCount = IndexCount = 0;
        }
    }

    private readonly LineBuffer[] buffers = CreateBuffers();

    private static LineBuffer[] CreateBuffers()
    {
        var result = new LineBuffer[(int)Color.NumColor];
        for (int i = 0; i < result.Length; i++)
            result[i] = new LineBuffer();
        return result;
    }

    private VertexArrayObject vao = null!;
    private LineShader shader = null!;
    private ArrayBuffer ab = null!;
    private ElementArrayBuffer eab = null!;

    public void Draw()
    {
        Camera camera = RenderWindow.Instance.Camera;

        shader.Use();

        shader.Color.Set(new Vector4(0, 1, 0, 1));

        shader.Projection.Set(camera.ProjectionMatrix);
        shader.View.Set(camera.ViewMatrix);

        vao.Bind();

        for (int i = 0; i < (int)Color.NumColor; i++)
        {
            var lines = buffers[i];

            if (lines.IndexCount == 0) continue;

            eab.SetData(lines.Indices, lines.IndexCount);
            ab.SetData(lines.Vertices, lines.VertexCount);

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
        var list = buffers[(int)color];
        uint offset = (uint)list.VertexCount;

        list.Add(pointA.X, pointA.Y, pointA.Z);
        list.Add(pointB.X, pointB.Y, pointB.Z);
        list.Add(offset + 0, offset + 1);
    }

    public void PushBox(Color color, in Vector3 min, in Vector3 max)
    {
        var list = buffers[(int)color];

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
        var list = buffers[(int)color];
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
        shader = new LineShader();

        vao = new VertexArrayObject();

        ab = new ArrayBuffer();
        eab = new ElementArrayBuffer();

        int sof = sizeof(float);

        vao.VertexAttributes[0].Set(ab, 3, VertexAttributeType.Float, false, 3 * sof, 0 * sof); // position
        vao.ElementArrayBuffer = eab;
    }
}