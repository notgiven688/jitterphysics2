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

using System;
using JitterDemo.Renderer.OpenGL.Native;

namespace JitterDemo.Renderer.OpenGL;

[GLObject(GLObjectType.Texture)]
public class Texture : GLObject
{
    public enum Format : uint
    {
        Depth = GLC.DEPTH_COMPONENT,
        DepthStencil = GLC.DEPTH_STENCIL,
        Red = GLC.RED,
        RedGreen = GLC.RG,
        RedGreenBlue = GLC.RGB,
        RedGreenBlueAlpha = GLC.RGBA
    }

    public enum Type : uint
    {
        Float = GLC.FLOAT,
        UnsignedByte = GLC.UNSIGNED_BYTE
    }

    public enum Wrap : uint
    {
        Repeat = GLC.REPEAT,
        MirroredRepeat = GLC.MIRRORED_REPEAT,
        ClampToEdge = GLC.CLAMP_TO_EDGE,
        ClampToBorder = GLC.CLAMP_TO_BORDER
    }

    public enum Filter : uint
    {
        Nearest = GLC.NEAREST,
        Linear = GLC.LINEAR,
        NearestMipmapNearest = GLC.NEAREST_MIPMAP_NEAREST,
        LinearMipmapNearest = GLC.LINEAR_MIPMAP_NEAREST,
        NearestMipmapLinear = GLC.NEAREST_MIPMAP_LINEAR,
        LinearMipmapLinear = GLC.LINEAR_MIPMAP_LINEAR
    }

    public enum Anisotropy
    {
        Filter_1x = 1,
        Filter_2x = 2,
        Filter_4x = 4,
        Filter_8x = 8,
        Filter_16x = 16
    }

    public Texture() : base(GL.GenTexture())
    {
        // https://stackoverflow.com/questions/30554008/when-binding-an-fbo-do-you-need-to-call-glframebuffertexture2d-every-frame
        Bind();
    }

    public virtual void Bind()
    {
        GL.BindTexture(GLC.TEXTURE_2D, Handle);
    }

    public virtual void Bind(uint textureUnit)
    {
        GL.ActiveTexture(GLC.TEXTURE0 + textureUnit);
        GL.BindTexture(GLC.TEXTURE_2D, Handle);
    }
}

public class CubemapTexture : Texture
{
    public override void Bind()
    {
        GL.BindTexture(GLC.TEXTURE_CUBE_MAP, Handle);
    }

    public override void Bind(uint textureUnit)
    {
        GL.ActiveTexture(GLC.TEXTURE0 + textureUnit);
        GL.BindTexture(GLC.TEXTURE_CUBE_MAP, Handle);
    }

    public void LoadBitmaps(IntPtr[] bitmaps, int width, int height)
    {
        if (bitmaps.Length != 6) throw new ArgumentException("Array length has to be 6.", nameof(bitmaps));

        GL.BindTexture(GLC.TEXTURE_CUBE_MAP, Handle);

        for (int i = 0; i < 6; i++)
        {
            GL.TexImage2D(GLC.TEXTURE_CUBE_MAP_POSITIVE_X + (uint)i,
                0, (int)GLC.RGBA, width, height, 0,
                GLC.BGRA, GLC.UNSIGNED_BYTE, bitmaps[i]);
        }

        GL.TexParameteri(GLC.TEXTURE_CUBE_MAP, GLC.TEXTURE_MIN_FILTER, (int)GLC.LINEAR);
        GL.TexParameteri(GLC.TEXTURE_CUBE_MAP, GLC.TEXTURE_MIN_FILTER, (int)GLC.LINEAR);
        GL.TexParameteri(GLC.TEXTURE_CUBE_MAP, GLC.TEXTURE_WRAP_S, (int)GLC.CLAMP_TO_EDGE);
        GL.TexParameteri(GLC.TEXTURE_CUBE_MAP, GLC.TEXTURE_WRAP_T, (int)GLC.CLAMP_TO_EDGE);
        GL.TexParameteri(GLC.TEXTURE_CUBE_MAP, GLC.TEXTURE_WRAP_R, (int)GLC.CLAMP_TO_EDGE);
    }
}

public class Texture2D : Texture
{
    public unsafe static Texture2D EmptyTexture()
    {
        var result = new Texture2D();

        int black = 0;
        
        result.LoadImage((IntPtr)(&black), 1, 1, false);
        return result;
    }
    public void LoadImage(IntPtr data, int width, int height, bool generateMipmap = true)
    {
        GL.BindTexture(GLC.TEXTURE_2D, Handle);
        GL.TexImage2D(GLC.TEXTURE_2D, 0, (int)GLC.RGBA, width, height, 0, GLC.BGRA, GLC.UNSIGNED_BYTE, data);

        if (generateMipmap)
        {
            SetMinMagFilter(Filter.LinearMipmapLinear, Filter.Linear);
            GL.GenerateMipmap(GLC.TEXTURE_2D);
        }
        else
        {
            SetMinMagFilter(Filter.Linear, Filter.Linear);
        }
    }

    public void SetAnisotropicFiltering(Anisotropy anisotropy)
    {
        Bind();

        const uint GL_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FE;
        const uint GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FF;

        unsafe
        {
            float largest;
            GL.GetFloatv(GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, &largest);

            if ((int)anisotropy < largest) largest = (float)anisotropy;
            GL.TexParameterfv(GLC.TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, &largest);
        }
    }

    public void Specify(Format format, int width, int height, Type type)
    {
        Bind();
        GL.TexImage2D(GLC.TEXTURE_2D, 0, (int)format, width, height, 0, (uint)format, (uint)type, IntPtr.Zero);
    }

    public void SetBorderColor(Vector4 color)
    {
        Bind();
        unsafe
        {
            GL.TexParameterfv(GLC.TEXTURE_2D, GLC.TEXTURE_BORDER_COLOR, &color.X);
        }
    }

    public void SetWrap(Wrap wrap)
    {
        Bind();
        GL.TexParameteri(GLC.TEXTURE_2D, GLC.TEXTURE_WRAP_S, (int)wrap);
        GL.TexParameteri(GLC.TEXTURE_2D, GLC.TEXTURE_WRAP_T, (int)wrap);
    }

    public void SetMinMagFilter(Filter minFilter, Filter maxFilter)
    {
        Bind();
        GL.TexParameteri(GLC.TEXTURE_2D, GLC.TEXTURE_MIN_FILTER, (int)minFilter);
        GL.TexParameteri(GLC.TEXTURE_2D, GLC.TEXTURE_MAG_FILTER, (int)maxFilter);
    }
}