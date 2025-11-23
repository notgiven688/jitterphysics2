using System.IO;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class RenderWindow : GLFWWindow
{
    private TexturedQuad shadowDebug;

    public CSMRenderer CSMRenderer { get; }
    public Camera Camera { get; set; }
    public ImGuiRenderer GuiRenderer { get; }
    public DebugRenderer DebugRenderer { get; }

    private readonly Skybox skybox;

    public bool ShowShadowDebug { set; get; } = false;

    public static RenderWindow Instance { get; private set; } = null!;

    private double lastTime;

    public RenderWindow()
    {
        Instance = this;

        Camera = new FreeCamera();

        CSMRenderer = new CSMRenderer();
        GuiRenderer = new ImGuiRenderer();
        DebugRenderer = new DebugRenderer();

        skybox = new Skybox();

        shadowDebug = null!;

        lastTime = Time;
    }

    public override void Draw()
    {
        float timeDelta = (float)(Time - lastTime);
        lastTime = Time;

        GLDevice.Enable(Capability.DepthTest);
        GLDevice.Enable(Capability.Blend);
        GLDevice.SetBlendFunction(BlendFunction.SourceAlpha, BlendFunction.OneMinusSourceAlpha);

        GLDevice.SetClearColor(73.0f / 255.0f, 76.0f / 255.0f, 92.0f / 255.0f, 1);

        GLDevice.Clear(ClearFlags.ColorBuffer | ClearFlags.DepthBuffer);

        skybox.Draw();

        CSMRenderer.Draw();

        DebugRenderer.Draw();

        if (ShowShadowDebug)
        {
            shadowDebug.Position = new Vector2(10, 10);
            shadowDebug.Draw();
        }

        if (Keyboard.IsKeyDown(Keyboard.Key.Escape))
        {
            Close();
        }

        GuiRenderer.Draw(timeDelta);

        Camera.IgnoreKeyboardInput = GuiRenderer.WantsCaptureKeyboard;
        Camera.IgnoreMouseInput = GuiRenderer.WantsCaptureMouse;

        Camera.Update();
    }

    public override void Load()
    {
        skybox.Load();

        CSMRenderer.Load();

        VerticalSync = true;

        GuiRenderer.Load();

        shadowDebug = new TexturedQuad();
        shadowDebug.Texture = CSMRenderer.depthMap[0];

        DebugRenderer.Load();

        string filename = Path.Combine("assets", "logo.tga");

        /*
        Image.LoadImage(filename).FixedData((img, data) =>
        {
            logo = new TexturedQuad(img.Width, img.Height);
            logo.Texture = new Texture2D();
            logo.Texture.LoadImage(data, img.Width, img.Height, false);
        });
        */

        Camera.Position = new Vector3(0, 4, 8);

        Camera.Update();
    }
}