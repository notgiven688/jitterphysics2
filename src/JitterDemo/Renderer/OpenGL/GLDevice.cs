using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

public enum DrawMode : uint
{
    Lines = GLC.LINES,
    Triangles = GLC.TRIANGLES
}

public enum CullMode : uint
{
    Front = GLC.FRONT,
    Back = GLC.BACK,
    FrontAndBack = GLC.FRONT_AND_BACK
}

public enum IndexType : uint
{
    UnsignedInt = GLC.UNSIGNED_INT,
    UnsignedShort = GLC.UNSIGNED_SHORT
}

public enum ClearFlags : uint
{
    Color = GLC.COLOR_BUFFER_BIT,
    Depth = GLC.DEPTH_BUFFER_BIT,
    Stencil = GLC.STENCIL_BUFFER_BIT,
    ColorAndDepth = Color | Depth
}

public enum BlendFunc : uint
{
    Zero = GLC.ZERO,
    One = GLC.ONE,
    SrcAlpha = GLC.SRC_ALPHA,
    OneMinusSrcAlpha = GLC.ONE_MINUS_SRC_ALPHA,
    SrcColor = GLC.SRC_COLOR,
    DstAlpha = GLC.DST_ALPHA,
    DstColor = GLC.DST_COLOR,
    OneMinusSrcColor = GLC.ONE_MINUS_SRC_COLOR
}

public enum Capability : uint
{
    Blend = GLC.BLEND,
    DepthTest = GLC.DEPTH_TEST,
    CullFace = GLC.CULL_FACE,
    ScissorTest = GLC.SCISSOR_TEST
}

public static class GLDevice
{
    public static void Clear(ClearFlags flags) => GL.Clear((uint)flags);
    public static void ClearColor(float r, float g, float b, float a) => GL.ClearColor(r, g, b, a);

    public static void Enable(Capability cap) => GL.Enable((uint)cap);
    public static void Disable(Capability cap) => GL.Disable((uint)cap);

    public static void CullFace(CullMode mode) => GL.CullFace((uint)mode);
    public static void Viewport(int x, int y, int w, int h) => GL.Viewport(x, y, w, h);

    /// Sends all queued commands to the GPU without waiting for them.
    public static void Flush() => GL.Flush();
    public static void Blend(BlendFunc src, BlendFunc dst) => GL.BlendFunc((uint)src, (uint)dst);

    public static void DepthMask(bool write) => GL.DepthMask(write);

    public static void DrawArrays(DrawMode mode, int first, int count)
        => GL.DrawArrays((uint)mode, first, count);

    public static void DrawElements(DrawMode mode, int count, IndexType type, int offsetBytes)
        => GL.DrawElements((uint)mode, count, (uint)type, offsetBytes);

    public static void DrawElementsInstanced(DrawMode mode, int count, IndexType type, int offsetBytes, int instances)
        => GL.DrawElementsInstanced((uint)mode, count, (uint)type, offsetBytes, instances);

    public static void DrawElementsBaseVertex(DrawMode mode, int count, IndexType type, int offsetBytes, int baseVertex)
        => GL.DrawElementsBaseVertex((uint)mode, count, (uint)type, offsetBytes, baseVertex);

    public static void Scissor(int x, int y, int w, int h) => GL.Scissor(x, y, w, h);
}
