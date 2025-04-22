using System.IO;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class TriangleMesh : CSMInstance
{
    public readonly Mesh Mesh;

    public Vector3 Color { get; set; }

    public TriangleMesh(string objFile, float scale = 1.0f)
    {
        string filename = Path.Combine("assets", objFile);
        Mesh = Mesh.LoadMesh(filename, true);
        Mesh.Transform(MatrixHelper.CreateScale(scale));
    }

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        return (Mesh.Vertices, Mesh.Indices);
    }

    public override void LightPass(PhongShader shader)
    {
        //Renderer.OpenGL.Native.GL.PolygonMode(Renderer.OpenGL.Native.GLC.FRONT_AND_BACK, Renderer.OpenGL.Native.GLC.LINE);
        base.LightPass(shader);
        //Renderer.OpenGL.Native.GL.PolygonMode(Renderer.OpenGL.Native.GLC.FRONT_AND_BACK, Renderer.OpenGL.Native.GLC.FILL);
    }
}