using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JitterDemo.Renderer.DearImGui;
using JitterDemo.Renderer.OpenGL;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer;

public class ImGuiShader : BasicShader
{
    public UniformMatrix4 Projection { get; }

    public ImGuiShader() : base(vshader, fshader)
    {
        Projection = GetUniform<UniformMatrix4>("projection_matrix");
    }

    private static readonly string vshader = @"
            #version 330 core

            uniform mat4 projection_matrix;

            layout (location = 0) in vec2 in_position;
            layout (location = 1) in vec2 in_texCoord;
            layout (location = 2) in vec4 in_color;

            out vec4 color;
            out vec2 texCoord;

            void main()
            {
                gl_Position = projection_matrix * vec4(in_position, 0, 1);
                color = in_color;
                texCoord = in_texCoord;
            }
        ";

    private static readonly string fshader = @"
            #version 330 core

            uniform sampler2D FontTexture;

            in vec4 color;
            in vec2 texCoord;

            out vec4 outputColor;

            void main()
            {
                outputColor = color * texture(FontTexture, texCoord);
            }
        ";
}

public class ImGuiRenderer
{
    private ArrayBuffer ab = null!;
    private VertexArrayObject vao = null!;
    private ElementArrayBuffer eab = null!;
    private ImGuiShader shader = null!;

    private readonly List<Texture2D> textures = new();
    
    public bool WantsCaptureMouse { private set; get; }
    public bool WantsCaptureKeyboard { private set; get; }

    private unsafe void RebuildFontAtlas()
    {
        ImGuiIO* io = ImGuiNative.igGetIO();
        byte* pixel;
        int width, height, bpp;

        ImGuiNative.ImFontAtlas_GetTexDataAsRGBA32(io->Fonts, &pixel, &width, &height, &bpp);

        var texture = new Texture2D();
        texture.LoadImage((IntPtr)pixel, width, height, false);
        textures.Add(texture);

        ImGuiNative.ImFontAtlas_SetTexID(io->Fonts, textures.Count - 1);
        ImGuiNative.ImFontAtlas_ClearTexData(io->Fonts);
    }

    public unsafe void Load()
    {
        shader = new ImGuiShader();
        vao = new VertexArrayObject();

        ImFontAtlas* shared_font_atlas = null;
        IntPtr ctx = ImGuiNative.igCreateContext(shared_font_atlas);
        ImGuiNative.igSetCurrentContext(ctx);

        int sof = sizeof(float);

        ab = new ArrayBuffer();
        eab = new ElementArrayBuffer();
        vao.ElementArrayBuffer = eab;

        int stride = Unsafe.SizeOf<ImDrawVert>();
        vao.VertexAttributes[0].Set(ab, 2, VertexAttributeType.Float, false, stride, 0);
        vao.VertexAttributes[1].Set(ab, 2, VertexAttributeType.Float, false, stride, 2 * sof);
        vao.VertexAttributes[2].Set(ab, 4, VertexAttributeType.UnsignedByte, true, stride, 4 * sof);

        RebuildFontAtlas();

        ImGuiIO* io = ImGuiNative.igGetIO();
        io->DeltaTime = 0;
        var r = RenderWindow.Instance;
        io->DisplaySize = new Vector2(r.Width, r.Height);

        ImGui.DisableIni();
    }

    public unsafe void Draw(float deltaTime)
    {
        ImGuiIO* pio = ImGuiNative.igGetIO();

        pio->DeltaTime = deltaTime;

        Mouse m = Mouse.Instance;
        ImGuiNative.ImGuiIO_AddMousePosEvent(pio, (float)m.Position.X, (float)m.Position.Y);
        ImGuiNative.ImGuiIO_AddMouseButtonEvent(pio, 0, Convert.ToByte(m.IsButtonDown(Mouse.Button.Left)));
        ImGuiNative.ImGuiIO_AddMouseButtonEvent(pio, 1, Convert.ToByte(m.IsButtonDown(Mouse.Button.Right)));
        ImGuiNative.ImGuiIO_AddMouseButtonEvent(pio, 2, Convert.ToByte(m.IsButtonDown(Mouse.Button.Middle)));
        ImGuiNative.ImGuiIO_AddMouseWheelEvent(pio, (float)m.ScrollWheel.X, (float)m.ScrollWheel.Y);
        
        Keyboard k = Keyboard.Instance;
        foreach(uint inputc in k.CharInput) { ImGuiNative.ImGuiIO_AddInputCharacter(pio, inputc); }
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.LeftArrow, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Left)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.RightArrow, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Right)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.UpArrow, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Up)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.DownArrow, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Down)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.PageDown, Convert.ToByte(k.IsKeyDown(Keyboard.Key.PageDown)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.PageUp, Convert.ToByte(k.IsKeyDown(Keyboard.Key.PageUp)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.LeftCtrl,Convert.ToByte(k.IsKeyDown(Keyboard.Key.LeftControl)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.LeftAlt, Convert.ToByte(k.IsKeyDown(Keyboard.Key.LeftAlt)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.ModAlt, Convert.ToByte(k.IsKeyDown(Keyboard.Key.RightAlt)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.Backspace, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Backspace)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.Tab, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Tab)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.LeftShift, Convert.ToByte(k.IsKeyDown(Keyboard.Key.LeftShift)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.RightShift, Convert.ToByte(k.IsKeyDown(Keyboard.Key.RightShift)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.Delete, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Delete)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.End, Convert.ToByte(k.IsKeyDown(Keyboard.Key.End)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.Home, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Home)));
        ImGuiNative.ImGuiIO_AddKeyEvent(pio, ImGuiKey.Escape, Convert.ToByte(k.IsKeyDown(Keyboard.Key.Escape)));

        this.WantsCaptureKeyboard = Convert.ToBoolean(pio->WantCaptureKeyboard);
        this.WantsCaptureMouse = Convert.ToBoolean(pio->WantCaptureMouse);
        
        var r = RenderWindow.Instance;

        pio->DisplaySize = new Vector2(r.Width, r.Height);
        pio->DisplayFramebufferScale = new Vector2(1, 1);

        vao.Bind();
        shader.Use();

        Matrix4 pm = MatrixHelper.CreateOrthographicOffCenter(0.0f, r.Width, r.Height, 0, -1f, +1f);
        shader.Projection.Set(pm);

        GLDevice.Enable(Capability.Blend);
        GLDevice.Enable(Capability.ScissorTest);
        GLDevice.Disable(Capability.DepthTest);
        GLDevice.Disable(Capability.CullFace);

        ImDrawData* pdraw_data = ImGuiNative.igGetDrawData();

        ImGuiNative.ImDrawData_ScaleClipRects(pdraw_data, pio->DisplayFramebufferScale);

        for (int i = 0; i < pdraw_data->CmdListsCount; i++)
        {
            ImDrawList* pcmdList = pdraw_data->CmdLists[i];

            ab.SetData(pcmdList->VtxBuffer.Data, pcmdList->VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(),
                GLC.DYNAMIC_DRAW);
            eab.SetData(pcmdList->IdxBuffer.Data, pcmdList->IdxBuffer.Size * sizeof(ushort), GLC.DYNAMIC_DRAW);

            for (int pcmdi = 0; pcmdi < pcmdList->CmdBuffer.Size; pcmdi++)
            {
                ImDrawCmd* cmdBuffer = (ImDrawCmd*)pcmdList->CmdBuffer.Data;
                ImDrawCmd cmdBufferData = cmdBuffer[pcmdi];

                textures[(int)cmdBufferData.TextureId].Bind(0);
                var clip = cmdBufferData.ClipRect;
                GL.Scissor((int)clip.X, r.Height - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                GLDevice.DrawElementsBaseVertex(DrawMode.Triangles, (int)cmdBufferData.ElemCount,
                    IndexType.UnsignedShort, (int)cmdBufferData.IdxOffset * sizeof(ushort),
                    (int)cmdBufferData.VtxOffset);
            }
        }

        GLDevice.Disable(Capability.Blend);
        GLDevice.Enable(Capability.DepthTest);
        GLDevice.Enable(Capability.CullFace);
        GLDevice.Disable(Capability.ScissorTest);
    }
}