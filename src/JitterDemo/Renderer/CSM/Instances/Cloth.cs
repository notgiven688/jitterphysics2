using System.IO;
using Jitter2.Collision;
using Jitter2.LinearMath;
using JitterDemo.Renderer.OpenGL;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer;

public class Cloth : CSMInstance
{
    private Vertex[] vertices;
    private TriangleVertexIndex[] indices;

    public Cloth()
    {
        // dummy data
        vertices = new Vertex[4];
        vertices[0] = new Vertex(new Vector3(-1, 0, -1), Vector3.UnitY, new Vector2(0, 0));
        vertices[1] = new Vertex(new Vector3(-1, 0, +1), Vector3.UnitY, new Vector2(0, 1));
        vertices[2] = new Vertex(new Vector3(+1, 0, -1), Vector3.UnitY, new Vector2(1, 0));
        vertices[3] = new Vertex(new Vector3(+1, 0, +1), Vector3.UnitY, new Vector2(1, 1));

        indices = new TriangleVertexIndex[2];
        indices[0] = new TriangleVertexIndex(1, 0, 2);
        indices[1] = new TriangleVertexIndex(1, 2, 3);
    }

    public Vertex[] Vertices => vertices;

    public void SetIndices(TriangleVertexIndex[] indices)
    {
        IndexLen = indices.Length * 3;
        Vao.ElementArrayBuffer!.SetData(indices);

        uint largest = 0;
        for (int i = 0; i < indices.Length; i++)
        {
            if (indices[i].T1 > largest) largest = indices[i].T1;
            if (indices[i].T2 > largest) largest = indices[i].T2;
            if (indices[i].T3 > largest) largest = indices[i].T3;
        }

        this.vertices = new Vertex[largest + 1];
        this.indices = indices;
    }

    public void VerticesChanged()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Normal = Vector3.Zero;
        }

        for (int i = 0; i < indices.Length; i++)
        {
            ref var v1 = ref vertices[indices[i].T1];
            ref var v2 = ref vertices[indices[i].T2];
            ref var v3 = ref vertices[indices[i].T3];
            
            var p0 = v1.Position;
            var p1 = v2.Position;
            var p2 = v3.Position;
            var n = Vector3.Cross(p2 - p1, p2 - p0);

            v1.Normal += n;
            v2.Normal += n;
            v3.Normal += n;
        }
        
        this.ab.SetData(vertices);
    }

    public override void Load()
    {
        string filename = Path.Combine("assets", "texture_10.tga");
        Image.LoadImage(filename).FixedData((img, ptr) => { Texture.LoadImage(ptr, img.Width, img.Height); });

        Texture.SetWrap(OpenGL.Texture.Wrap.Repeat);
        Texture.SetAnisotropicFiltering(OpenGL.Texture.Anisotropy.Filter_8x);
        
        base.Load();
    }

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        return (vertices, indices);
    }

    public override void ShadowPass(ShadowShader shader)
    {
        GLDevice.Disable(Capability.CullFace);
        base.ShadowPass(shader);
        GLDevice.Enable(Capability.CullFace);
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();
        shader.MaterialProperties.ColorMixing.Set(0.1f,0.0f,0.9f);
        shader.MaterialProperties.FlipNormal = true;
        GLDevice.SetCullFaceMode(CullMode.Back);
        base.LightPass(shader);
        shader.MaterialProperties.FlipNormal = false;
        GLDevice.SetCullFaceMode(CullMode.Front);
        base.LightPass(shader);
    }
}