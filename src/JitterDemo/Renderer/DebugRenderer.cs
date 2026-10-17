using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

/// Immediate-mode line renderer for debug overlays (contact points, AABBs, rays, ...).
/// One array per color; callers push primitives each frame, the renderer streams them
/// to the GPU and flushes before drawing the scene.
public sealed class DebugRenderer : IDebugDrawer
{
    public enum Color
    {
        White = 0,
        Red = 1,
        Green = 2,
        Count = 3
    }

    private static readonly uint[] ColorValues =
    {
        DebugDrawColors.White,
        DebugDrawColors.Red,
        DebugDrawColors.Green
    };

    private sealed class Bucket
    {
        public Vector3[] Vertices = new Vector3[128];
        public LineVertexIndex[] Indices = new LineVertexIndex[128];
        public int VertexCount;
        public int IndexCount;

        public uint PushVertex(float x, float y, float z)
        {
            if (VertexCount == Vertices.Length) Array.Resize(ref Vertices, Vertices.Length * 2);
            Vertices[VertexCount] = new Vector3(x, y, z);
            return (uint)VertexCount++;
        }

        public void PushIndex(uint i1, uint i2)
        {
            if (IndexCount == Indices.Length) Array.Resize(ref Indices, Indices.Length * 2);
            Indices[IndexCount++] = new LineVertexIndex(i1, i2);
        }

        public void Clear() { VertexCount = 0; IndexCount = 0; }
    }

    private readonly Dictionary<uint, Bucket> buckets = new();

    private Bucket GetBucket(uint color)
    {
        if (!buckets.TryGetValue(color, out Bucket? bucket)) buckets.Add(color, bucket = new Bucket());
        return bucket;
    }

    private Vao vao = null!;
    private GpuBuffer vbo = null!;
    private GpuBuffer ebo = null!;
    private Shader shader = null!;

    public void Load()
    {
        shader = new Shader(Vs, Fs);

        vao = new Vao();
        vbo = GpuBuffer.Vertex();
        ebo = GpuBuffer.Index();

        vao.Attrib(0, vbo, 3, AttribType.Float, 3 * sizeof(float), 0);
        vao.AttachIndexBuffer(ebo);
    }

    public void Draw(Camera camera)
    {
        shader.Use();
        shader.Set("uProjection", camera.ProjectionMatrix);
        shader.Set("uView", camera.ViewMatrix);

        vao.Bind();

        foreach (var (color, bucket) in buckets)
        {
            if (bucket.IndexCount == 0) continue;

            vbo.Stream<Vector3>(bucket.Vertices, bucket.VertexCount);
            ebo.Stream<LineVertexIndex>(bucket.Indices, bucket.IndexCount);

            shader.Set("uColor", new Vector4((color >> 24) / 255.0f, ((color >> 16) & 255) / 255.0f,
                ((color >> 8) & 255) / 255.0f, (color & 255) / 255.0f));
            GLDevice.DrawElements(DrawMode.Lines, bucket.IndexCount * 2, IndexType.UnsignedInt, 0);

            bucket.Clear();
        }
    }

    public void PushLine(Color color, in Vector3 a, in Vector3 b)
        => PushLine(ColorValues[(int)color], a, b);

    public void PushLine(uint color, in Vector3 a, in Vector3 b)
    {
        var bucket = GetBucket(color);
        uint i0 = bucket.PushVertex(a.X, a.Y, a.Z);
        uint i1 = bucket.PushVertex(b.X, b.Y, b.Z);
        bucket.PushIndex(i0, i1);
    }

    public void PushBox(Color color, in Vector3 min, in Vector3 max)
    {
        var bucket = GetBucket(ColorValues[(int)color]);

        uint o0 = bucket.PushVertex(min.X, min.Y, min.Z);
        uint o1 = bucket.PushVertex(max.X, min.Y, min.Z);
        uint o2 = bucket.PushVertex(min.X, max.Y, min.Z);
        uint o3 = bucket.PushVertex(min.X, min.Y, max.Z);
        uint o4 = bucket.PushVertex(max.X, max.Y, min.Z);
        uint o5 = bucket.PushVertex(min.X, max.Y, max.Z);
        uint o6 = bucket.PushVertex(max.X, min.Y, max.Z);
        uint o7 = bucket.PushVertex(max.X, max.Y, max.Z);

        bucket.PushIndex(o0, o1); bucket.PushIndex(o0, o2); bucket.PushIndex(o0, o3);
        bucket.PushIndex(o1, o4); bucket.PushIndex(o1, o6);
        bucket.PushIndex(o2, o4); bucket.PushIndex(o2, o5);
        bucket.PushIndex(o3, o5); bucket.PushIndex(o3, o6);
        bucket.PushIndex(o4, o7); bucket.PushIndex(o5, o7); bucket.PushIndex(o6, o7);
    }

    public void PushPoint(Color color, in Vector3 pos, float halfSize = 1.0f)
        => PushPoint(ColorValues[(int)color], pos, halfSize);

    public void PushPoint(uint color, in Vector3 pos, float halfSize = 1.0f)
    {
        var bucket = GetBucket(color);
        uint i0 = bucket.PushVertex(pos.X - halfSize, pos.Y, pos.Z);
        uint i1 = bucket.PushVertex(pos.X + halfSize, pos.Y, pos.Z);
        uint i2 = bucket.PushVertex(pos.X, pos.Y - halfSize, pos.Z);
        uint i3 = bucket.PushVertex(pos.X, pos.Y + halfSize, pos.Z);
        uint i4 = bucket.PushVertex(pos.X, pos.Y, pos.Z - halfSize);
        uint i5 = bucket.PushVertex(pos.X, pos.Y, pos.Z + halfSize);

        bucket.PushIndex(i0, i1);
        bucket.PushIndex(i2, i3);
        bucket.PushIndex(i4, i5);
    }

    public void DrawSegment(in JVector a, in JVector b, uint color) =>
        PushLine(color, Conversion.FromJitter(a), Conversion.FromJitter(b));

    private const string Vs = @"
#version 330 core
layout(location = 0) in vec3 aPos;

uniform mat4 uView;
uniform mat4 uProjection;

void main() { gl_Position = uProjection * uView * vec4(aPos, 1.0); }
";

    private const string Fs = @"
#version 330 core
uniform vec4 uColor;
out vec4 FragColor;
void main() { FragColor = uColor; }
";
}
