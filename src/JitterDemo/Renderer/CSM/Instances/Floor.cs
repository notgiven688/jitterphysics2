using System.IO;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class JitterFloor : CSMInstance
{
    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        const int size = 100;

        Vertex[] vertices = new Vertex[4];
        vertices[0] = new Vertex(new Vector3(-size, 0, -size), Vector3.UnitY, new Vector2(0, 0));
        vertices[1] = new Vertex(new Vector3(-size, 0, +size), Vector3.UnitY, new Vector2(0, size));
        vertices[2] = new Vertex(new Vector3(+size, 0, -size), Vector3.UnitY, new Vector2(size, 0));
        vertices[3] = new Vertex(new Vector3(+size, 0, +size), Vector3.UnitY, new Vector2(size, size));

        TriangleVertexIndex[] indices = new TriangleVertexIndex[2];
        indices[0] = new TriangleVertexIndex(1, 0, 2);
        indices[1] = new TriangleVertexIndex(1, 2, 3);

        return (vertices, indices);
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();

        shader.MaterialProperties.Shininess.Set(10.0f);

        shader.MaterialProperties.ColorMixing.Set(0.0f, 0, 1f);

        base.LightPass(shader);
    }

    public override void Load()
    {
        string filename = Path.Combine("assets", "unit.tga");

        Image.LoadImage(filename).FixedData((img, ptr) => { Texture.LoadImage(ptr, img.Width, img.Height); });

        Texture.SetWrap(OpenGL.Texture.Wrap.Repeat);
        Texture.SetAnisotropicFiltering(OpenGL.Texture.Anisotropy.Filter_8x);

        base.Load();
    }
}