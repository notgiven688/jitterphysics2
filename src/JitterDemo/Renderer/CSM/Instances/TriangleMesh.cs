using System.IO;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class TriangleMesh : CSMInstance
{
    public readonly Mesh Mesh;

    public TriangleMesh(string objFile, float scale = 1.0f)
    {
        string filename = Path.Combine("assets", objFile);
        Mesh = Mesh.LoadMesh(filename);
        Mesh.Transform(MatrixHelper.CreateScale(scale));
    }

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        return (Mesh.Vertices, Mesh.Indices);
    }

}