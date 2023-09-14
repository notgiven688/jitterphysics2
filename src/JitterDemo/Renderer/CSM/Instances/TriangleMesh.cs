using System.IO;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class TriangleMesh : CSMInstance
{
    public readonly Mesh mesh;

    public Vector3 Color { get; set; }

    public TriangleMesh(string objFile, float scale = 1.0f)
    {
        string filename = Path.Combine("assets", objFile);
        mesh = Mesh.LoadMesh(filename, true);
        mesh.Transform(MatrixHelper.CreateScale(scale));
    }

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        return (mesh.Vertices, mesh.Indices);
    }

    public override void LightPass(PhongShader shader)
    {
        //Renderer.OpenGL.Native.GL.PolygonMode(Renderer.OpenGL.Native.GLC.FRONT_AND_BACK, Renderer.OpenGL.Native.GLC.LINE);
        base.LightPass(shader);
        //Renderer.OpenGL.Native.GL.PolygonMode(Renderer.OpenGL.Native.GLC.FRONT_AND_BACK, Renderer.OpenGL.Native.GLC.FILL);
    }
}