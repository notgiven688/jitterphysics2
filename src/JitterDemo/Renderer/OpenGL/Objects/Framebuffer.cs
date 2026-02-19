using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

[GLObject(GLObjectType.FrameBuffer)]
public class FrameBuffer : GLObject
{
    public static readonly FrameBuffer Default = new(0);

    public FrameBuffer() : base(GL.GenFramebuffer())
    {
    }

    private FrameBuffer(uint fbo) : base(fbo)
    {
    }

    public void Bind()
    {
        GL.BindFramebuffer(GLC.FRAMEBUFFER, Handle);
    }

    public void AttachDepthTexture(Texture2D texture)
    {
        Bind();
        GL.FramebufferTexture2D(GLC.FRAMEBUFFER, GLC.DEPTH_ATTACHMENT, GLC.TEXTURE_2D, texture.Handle, 0);
        GL.DrawBuffer(GLC.NONE);
    }
}