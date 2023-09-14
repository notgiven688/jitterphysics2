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
using System.IO;

namespace JitterDemo.Renderer;

public class Image
{
    public int Width { private set; get; }
    public int Height { private set; get; }

    private readonly byte[] argbData;

    public Image(byte[] argbData, int width, int height)
    {
        this.argbData = argbData;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Minimimal  *.tga (Truevision TGA) loader for true color images with runtime
    /// length encoding support.
    /// </summary>
    public static Image LoadImage(string filename)
    {
        const int DataOffset = 18;

        var data = File.ReadAllBytes(filename).AsSpan();

        int imageType = data[2];
        int imageWidth = (data[13] << 8) | data[12];
        int imageHeight = (data[15] << 8) | data[14];
        int bitPerPixel = data[16];
        int descriptor = data[17];

        // Image type 2 and 10: The image data is a direct representation of the pixel color.
        // For a Pixel Depth of 15 and 16 bit, each pixel is stored with 5 bits per color. 
        // If the pixel depth is 16 bits, the topmost bit is reserved for transparency. For
        // a pixel depth of 24 bits, each pixel is stored with 8 bits per color. A 32-bit pixel
        // depth defines an additional 8-bit alpha channel.
        // [https://en.wikipedia.org/wiki/Truevision_TGA]

        if (!((imageType == 2 || imageType == 10) && (bitPerPixel == 24 || bitPerPixel == 32)))
        {
            throw new Exception("Only 24bit and 32bit encoded *.tga-files supported!");
        }

        var colorData = data[DataOffset..data.Length];

        int bytesPerPixel = bitPerPixel / 8;

        bool hl = (descriptor & 0x20) == 0;
        bool vl = (descriptor & 0x10) != 0;

        int FlipImage(int index)
        {
            int row = index / imageWidth;
            int col = index - row * imageWidth;
            if (hl) row = imageHeight - 1 - row;
            if (vl) col = imageWidth - 1 - col;
            return row * imageWidth + col;
        }

        int pixelCount = imageWidth * imageHeight;

        byte[] decoded = new byte[4 * pixelCount];
        int pixelIndex = 0;

        int pos = 0;
        while (pixelIndex < pixelCount)
        {
            int skip = 0;
            int count = pixelCount;

            if (imageType == 10)
            {
                skip = (colorData[pos] & 0x80) != 0 ? 1 : 0;
                count = (colorData[pos] & 0x7F) + 1;
                pos += 1;
            }

            for (int i = 0; i < count; i++)
            {
                int idx = 4 * FlipImage(pixelIndex++);
                decoded[idx + 0] = colorData[pos + 0];
                decoded[idx + 1] = colorData[pos + 1];
                decoded[idx + 2] = colorData[pos + 2];
                decoded[idx + 3] = bytesPerPixel == 4 ? colorData[pos + 3] : (byte)0;
                pos += bytesPerPixel * (1 - skip);
            }

            pos += bytesPerPixel * skip;
        }

        return new Image(decoded, imageWidth, imageHeight);
    }

    public unsafe void FixedData(Action<Image, IntPtr> action)
    {
        fixed (void* ptr = argbData)
        {
            action(this, (IntPtr)ptr);
        }
    }
}