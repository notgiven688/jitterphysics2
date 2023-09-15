/* Copyright <2022> <Thorben Linneweber>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 */

using System.Runtime.CompilerServices;
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
    ColorBuffer = GLC.COLOR_BUFFER_BIT,
    DepthBuffer = GLC.DEPTH_BUFFER_BIT,
    StencilBuffer = GLC.STENCIL_BUFFER_BIT
}

public enum BlendFunction : uint
{
    Zero = GLC.ZERO,
    One = GLC.ONE,
    SourceColor = GLC.SRC_COLOR,
    OneMinusSourceColor = GLC.ONE_MINUS_SRC_COLOR,
    DestinationColor = GLC.DST_COLOR,
    OneMinusDestinationColor = GLC.ONE_MINUS_DST_COLOR,
    SourceAlpha = GLC.SRC_ALPHA,
    OneMinusSourceAlpha = GLC.ONE_MINUS_SRC_ALPHA,
    DestiantionAlpha = GLC.DST_ALPHA,
    OneMinusDestinationAlpha = GLC.ONE_MINUS_DST_ALPHA,
    ConstantColor = GLC.CONSTANT_COLOR,
    OneMinusConstantColor = GLC.ONE_MINUS_CONSTANT_COLOR,
    ConstantAlpha = GLC.CONSTANT_ALPHA,
    OneMinusConstantAlpha = GLC.ONE_MINUS_CONSTANT_ALPHA,
    SourceAlphaSaturate = GLC.SRC_ALPHA_SATURATE,
    Source1Color = GLC.SRC1_COLOR,
    OneMinusSource1Color = GLC.ONE_MINUS_SRC1_COLOR,
    Source1Alpha = GLC.SRC1_ALPHA,
    OneMinusSource1 = GLC.ONE_MINUS_SRC1_ALPHA
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
    static GLDevice()
    {
        // make sure the "default" instances are registered
        RuntimeHelpers.RunClassConstructor(typeof(FrameBuffer).TypeHandle);
    }

    public static ShaderProgram ActiveShaderProgram
    {
        get
        {
            uint active = (uint)GL.GetIntegerv(GLC.CURRENT_PROGRAM);
            return GLObject.Retrieve<ShaderProgram>(active);
        }
    }

    public static FrameBuffer ActiveFrameBuffer
    {
        get
        {
            uint active = (uint)GL.GetIntegerv(GLC.DRAW_FRAMEBUFFER_BINDING);
            return GLObject.Retrieve<FrameBuffer>(active);
        }
    }

    public static Texture2D ActiveTexture2D
    {
        get
        {
            uint active = (uint)GL.GetIntegerv(GLC.TEXTURE_BINDING_2D);
            return GLObject.Retrieve<Texture2D>(active);
        }
    }

    public static void DrawElementsInstanced(DrawMode mode, int count, IndexType type, int start, int num)
    {
        GL.DrawElementsInstanced((uint)mode, count, (uint)type, start, num);
    }

    public static void DrawElementsBaseVertex(DrawMode mode, int count, IndexType type, int start, int baseVertex)
    {
        GL.DrawElementsBaseVertex((uint)mode, count, (uint)type, start, baseVertex);
    }

    public static void DrawElements(DrawMode mode, int count, IndexType type, int start)
    {
        GL.DrawElements((uint)mode, count, (uint)type, start);
    }

    public static void DrawArrays(DrawMode mode, int first, int count)
    {
        GL.DrawArrays((uint)mode, first, count);
    }

    public static void Clear(ClearFlags flags)
    {
        GL.Clear((uint)flags);
    }

    public static void SetCullFaceMode(CullMode cullMode)
    {
        GL.CullFace((uint)cullMode);
    }

    public static void SetViewport(int x, int y, int width, int height)
    {
        GL.Viewport(x, y, width, height);
    }

    public static void SetBlendFunction(BlendFunction sfactor, BlendFunction dfactor)
    {
        GL.BlendFunc((uint)sfactor, (uint)dfactor);
    }

    public static void Enable(Capability capability)
    {
        GL.Enable((uint)capability);
    }

    public static void Disable(Capability capability)
    {
        GL.Disable((uint)capability);
    }

    public static void SetClearColor(float r, float g, float b, float a)
    {
        GL.ClearColor(r, g, b, a);
    }
}