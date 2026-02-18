using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class TexturedQuad
{
    private readonly VertexArrayObject vao;
    private readonly QuadShader shader;

    public Texture2D Texture { get; set; } = null!;

    public TexturedQuad(int width = 200, int height = 200)
    {
        vao = new VertexArrayObject();

        ArrayBuffer ab0 = new();
        vao.ElementArrayBuffer = new ElementArrayBuffer();

        Vector2[] vertices = new Vector2[4];
        TriangleVertexIndex[] indices = new TriangleVertexIndex[2];

        vertices[0] = new Vector2(0, 0);
        vertices[1] = new Vector2(1, 0);
        vertices[2] = new Vector2(1, 1);
        vertices[3] = new Vector2(0, 1);

        indices[0] = new TriangleVertexIndex(1, 0, 2);
        indices[1] = new TriangleVertexIndex(2, 0, 3);

        ab0.SetData(vertices);

        vao.ElementArrayBuffer.SetData(indices);
        vao.VertexAttributes[0].Set(ab0, 2, VertexAttributeType.Float, false, 2 * sizeof(float), 0);

        shader = new QuadShader();
        shader.Use();
        shader.Size.Set(width, height);
    }

    public Vector2 Position { get; set; }

    public void Draw()
    {
        Texture.Bind(0);

        vao.Bind();
        shader.Use();

        (int w, int h) = RenderWindow.Instance.FramebufferSize;

        Matrix4 m = MatrixHelper.CreateOrthographicOffCenter(0.0f, w, h, 0, +1f, -1f);

        shader.Projection.Set(m);
        shader.Offset.Set(Position);

        GLDevice.Enable(Capability.Blend);
        GLDevice.Disable(Capability.DepthTest);
        GLDevice.DrawElements(DrawMode.Triangles, 6, IndexType.UnsignedInt, 0);
        GLDevice.Disable(Capability.Blend);
        GLDevice.Enable(Capability.DepthTest);
    }
}

public class QuadShader : BasicShader
{
    public UniformVector2 Offset { get; }
    public UniformMatrix4 Projection { get; }
    public UniformTexture FontTexture { private set; get; }
    public UniformVector2 Size { get; }

    public QuadShader() : base(vshader, fshader)
    {
        Offset = GetUniform<UniformVector2>("offset");
        Projection = GetUniform<UniformMatrix4>("projection");
        Size = GetUniform<UniformVector2>("size");
        FontTexture = GetUniform<UniformTexture>("fontTexture");
    }

    private static readonly string vshader = @"
        #version 330 core
        layout (location = 0) in vec2 aPos;
        uniform vec2 offset;
        uniform vec2 size;
        uniform mat4 projection;
        out vec2 TexCoord;
        void main()
        {
            gl_Position = projection * vec4(vec3(aPos * size + offset, 1 ), 1);
            TexCoord=vec2(aPos.x , aPos.y) ;
        }
        ";

    private static readonly string fshader = @"
        #version 330 core
        uniform sampler2D fontTexture;
        in vec2 TexCoord;
        out vec4 FragColor;
        void main()
        {
            vec2 tc = TexCoord;
            FragColor = texture(fontTexture, tc);
        }
        ";
}