using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class TestCube : CSMInstance
{
    private float timea;

    public override void LightPass(PhongShader shader)
    {
        if (Count == 0) return;

        Texture?.Bind(3);
        Vao.Bind();

        timea = (float)RenderWindow.Instance.Time;

        shader.Model.Set(MatrixHelper.CreateRotationX(timea));
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, 36, IndexType.UnsignedInt, 0, Count);
        shader.Model.Set(MatrixHelper.CreateRotationY(timea));
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, 36, IndexType.UnsignedInt, 36, Count);
        shader.Model.Set(Matrix4.Identity);
    }

    public override void ShadowPass(ShadowShader shader)
    {
        if (Count == 0) return;

        Vao.Bind();

        shader.Model.Set(MatrixHelper.CreateRotationX(timea));
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, 36, IndexType.UnsignedInt, 0, Count);
        shader.Model.Set(MatrixHelper.CreateRotationY(timea));
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, 36, IndexType.UnsignedInt, 36, Count);
        shader.Model.Set(Matrix4.Identity);
    }

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        // a 1x1x1 unit cube centered around the origin.
        Vertex[] vertices = new Vertex[48]; // 6 faces times 4 vertices
        TriangleVertexIndex[] indices = new TriangleVertexIndex[24]; // 2 triangles per face

        Vector3[] normals =
        {
            new(0, 0, 1),
            new(0, 0, -1),
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0)
        };

        for (int i = 0; i < 6; i++)
        {
            Vector3 n = normals[i];
            Vector3 s1 = new(n.Y, n.Z, n.X);
            Vector3 s2 = Vector3.Cross(n, s1);

            vertices[4 * i + 0] = new Vertex((n - s1 - s2) * 0.5f, n);
            vertices[4 * i + 1] = new Vertex((n - s1 + s2) * 0.5f, n);
            vertices[4 * i + 2] = new Vertex((n + s1 + s2) * 0.5f, n);
            vertices[4 * i + 3] = new Vertex((n + s1 - s2) * 0.5f, n);

            indices[2 * i + 0] = new TriangleVertexIndex(4 * i + 1, 4 * i + 0, 4 * i + 2);
            indices[2 * i + 1] = new TriangleVertexIndex(4 * i + 2, 4 * i + 0, 4 * i + 3);
        }

        for (int i = 0; i < 6; i++)
        {
            Vector3 n = normals[i];
            Vector3 s1 = new(n.Y, n.Z, n.X);
            Vector3 s2 = Vector3.Cross(n, s1);

            vertices[24 + 4 * i + 0] = new Vertex((n - s1 - s2) * 0.5f, n);
            vertices[24 + 4 * i + 1] = new Vertex((n - s1 + s2) * 0.5f, n);
            vertices[24 + 4 * i + 2] = new Vertex((n + s1 + s2) * 0.5f, n);
            vertices[24 + 4 * i + 3] = new Vertex((n + s1 - s2) * 0.5f, n);

            indices[12 + 2 * i + 0] = new TriangleVertexIndex(24 + 4 * i + 1, 24 + 4 * i + 0, 24 + 4 * i + 2);
            indices[12 + 2 * i + 1] = new TriangleVertexIndex(24 + 4 * i + 2, 24 + 4 * i + 0, 24 + 4 * i + 3);
        }

        return (vertices, indices);
    }
}