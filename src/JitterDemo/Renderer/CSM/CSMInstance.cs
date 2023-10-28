using System;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public struct TransformColor
{
    public Matrix4 Transform;
    public Vector3 Color;

    public static TransformColor Default = Default2();

    private static TransformColor Default2()
    {
        TransformColor tc;
        tc.Transform = Matrix4.Identity;
        tc.Color = new Vector3(0, 1, 0);
        return tc;
    }
}

public class CSMInstance
{
    public Texture2D Texture { set; get; } = Texture2D.EmptyTexture();

    public VertexArrayObject Vao = null!;
    public PhongShader shader = null!;
    public ArrayBuffer ab = null!;

    protected ArrayBuffer worldMatrices = null!;
    protected int IndexLen;

    public TransformColor[] WorldMatrices = { TransformColor.Default };
    public int Count { set; get; } = 1;

    public virtual (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        throw new NotImplementedException();
    }

    public void PushMatrix(in Matrix4 matrix)
    {
        PushMatrix(matrix, new Vector3(1, 1, 1));
    }

    public void PushMatrix(in Matrix4 matrix, in Vector3 color)
    {
        Count++;

        TransformColor tc;
        tc.Transform = matrix;
        tc.Color = color;

        if (Count >= WorldMatrices.Length)
        {
            Array.Resize(ref WorldMatrices, WorldMatrices.Length * 2);
        }

        WorldMatrices[Count - 1] = tc;
    }

    public virtual void LightPass(PhongShader shader)
    {
        if (Count == 0) return;

        Texture?.Bind(3);

        Vao.Bind();
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, IndexLen, IndexType.UnsignedInt, 0, Count);
    }

    public virtual void UpdateWorldMatrices()
    {
        if (Count == 0) return;
        worldMatrices.SetData(WorldMatrices, Count);
    }

    public virtual void ShadowPass(ShadowShader shader)
    {
        if (Count == 0) return;

        Vao.Bind();
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, IndexLen, IndexType.UnsignedInt, 0, Count);
    }

    public virtual void Load()
    {
        Vao = new VertexArrayObject();

        (var vertices, var indices) = ProvideVertices();
        IndexLen = indices.Length * 3;

        ab = new ArrayBuffer();
        ab.SetData(vertices);

        int sof = sizeof(float);

        Vao.VertexAttributes[5].Set(ab, 3, VertexAttributeType.Float, false, 8 * sof, 0 * sof); // position
        Vao.VertexAttributes[6].Set(ab, 3, VertexAttributeType.Float, false, 8 * sof, 3 * sof); // normal
        Vao.VertexAttributes[7].Set(ab, 2, VertexAttributeType.Float, false, 8 * sof, 6 * sof); // texture

        worldMatrices = new ArrayBuffer();

        Vao.VertexAttributes[0].Set(worldMatrices, 4, VertexAttributeType.Float, false, 19 * sof, 0 * sof);
        Vao.VertexAttributes[1].Set(worldMatrices, 4, VertexAttributeType.Float, false, 19 * sof, 4 * sof);
        Vao.VertexAttributes[2].Set(worldMatrices, 4, VertexAttributeType.Float, false, 19 * sof, 8 * sof);
        Vao.VertexAttributes[3].Set(worldMatrices, 4, VertexAttributeType.Float, false, 19 * sof, 12 * sof);
        Vao.VertexAttributes[4].Set(worldMatrices, 3, VertexAttributeType.Float, false, 19 * sof, 16 * sof);

        Vao.VertexAttributes[0].Divisor = 1;
        Vao.VertexAttributes[1].Divisor = 1;
        Vao.VertexAttributes[2].Divisor = 1;
        Vao.VertexAttributes[3].Divisor = 1;
        Vao.VertexAttributes[4].Divisor = 1;

        Vao.ElementArrayBuffer = new ElementArrayBuffer();
        Vao.ElementArrayBuffer.SetData(indices);
    }
}