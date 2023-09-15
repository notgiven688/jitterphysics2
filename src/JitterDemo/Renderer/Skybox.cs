using JitterDemo.Renderer.OpenGL;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer;

public class SkyboxShader : BasicShader
{
    public UniformMatrix4 Projection { get; }
    public UniformMatrix4 View { get; }

    public SkyboxShader() : base(vshader, fshader)
    {
        Projection = GetUniform<UniformMatrix4>("projection");
        View = GetUniform<UniformMatrix4>("view");
    }

    private static readonly string vshader = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;

        out vec3 TexCoords;

        uniform mat4 projection;
        uniform mat4 view;

        void main()
        {
            TexCoords = aPos;
            gl_Position = projection * mat4(mat3(view)) * vec4(aPos, 1.0);
        }  
        ";

    private static readonly string fshader = @"
        #version 330 core
        out vec4 FragColor;

        in vec3 TexCoords;

        uniform samplerCube skybox;

        void main()
        {   
            vec3 blue = vec3(66.0f / 255.0f, 135.0f / 255.0f, 245.0f / 255.0f);
            float ddot = max(dot(TexCoords/length(TexCoords),vec3(0,1,1))+0.4f,0);
            FragColor = vec4(blue*0.9+vec3(1,1,1)*ddot*0.1f,1);
        }
        ";
}

public class Skybox
{
    private VertexArrayObject vao = null!;
    private SkyboxShader shader = null!;
    private CubemapTexture cmTexture = null!;

    private float[] VertexBuffer()
    {
        return new[]
        {
            // positions          
            -1.0f, 1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, 1.0f, -1.0f,
            -1.0f, 1.0f, -1.0f,

            -1.0f, -1.0f, 1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f, 1.0f, -1.0f,
            -1.0f, 1.0f, -1.0f,
            -1.0f, 1.0f, 1.0f,
            -1.0f, -1.0f, 1.0f,

            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f, 1.0f,
            -1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, -1.0f, 1.0f,
            -1.0f, -1.0f, 1.0f,

            -1.0f, 1.0f, -1.0f,
            1.0f, 1.0f, -1.0f,
            1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, 1.0f,
            -1.0f, 1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, 1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f, 1.0f,
            1.0f, -1.0f, 1.0f
        };
    }

    public void Load()
    {
        shader = new SkyboxShader();

        vao = new VertexArrayObject();

        ArrayBuffer ab0 = new();
        ab0.SetData(VertexBuffer());

        int sof = sizeof(float);
        vao.VertexAttributes[0].Set(ab0, 3, VertexAttributeType.Float, false, 3 * sof, 0);

        /*
        Bitmap[] bitmaps = new Bitmap[6] { // r,l, t, b, f, b
        new Bitmap(System.IO.Path.Combine("assets", "skybox", "right.jpg")),
        new Bitmap(System.IO.Path.Combine("assets", "skybox", "left.jpg")),
        new Bitmap(System.IO.Path.Combine("assets", "skybox", "top.jpg")),
        new Bitmap(System.IO.Path.Combine("assets", "skybox", "bottom.jpg")),
        new Bitmap(System.IO.Path.Combine("assets", "skybox", "front.jpg")),
        new Bitmap(System.IO.Path.Combine("assets", "skybox", "back.jpg"))};
        */

        cmTexture = new CubemapTexture();
        //cmTexture.LoadBitmaps(bitmaps);
    }

    public void Draw()
    {
        Camera camera = RenderWindow.Instance.Camera;

        GL.DepthMask(false);
        GLDevice.SetCullFaceMode(CullMode.Back);
        shader.Use();
        shader.View.Set(camera.ViewMatrix);
        shader.Projection.Set(camera.ProjectionMatrix);
        // proj?
        vao.Bind();
        cmTexture.Bind();
        GL.DrawArrays(GLC.TRIANGLES, 0, 36);
        GL.DepthMask(true);
    }
}