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
using System.Collections.Generic;
using System.Reflection;

namespace JitterDemo.Renderer.OpenGL;

public enum GLObjectType
{
    Unspecified,
    ArrayBuffer,
    ElementArrayBuffer,
    VertexArrayObject,
    FrameBuffer,
    Buffer,
    Shader,
    ShaderProgram,
    Texture
}

[AttributeUsage(AttributeTargets.Class)]
public class GLObjectAttribute : Attribute
{
    public GLObjectType ObjectType { get; }

    public GLObjectAttribute(GLObjectType type)
    {
        ObjectType = type;
    }
}

[GLObjectAttribute(GLObjectType.Unspecified)]
public class GLObject
{
    internal static Dictionary<GLObjectType, Dictionary<uint, GLObject>> GLObjects = new();

    public static T Retrieve<T>(uint handle) where T : GLObject
    {
        GLObjectType ot = typeof(T).GetCustomAttribute<GLObjectAttribute>(true)!.ObjectType;
        T? result = ot as T;

        if (result == null) throw new Exception("Could not find handle.");
        return result;
    }

    public GLObject(uint handle)
    {
        Handle = handle;

        // We keep track of the managed objects which wrap functions around
        // a particular native handle, c.f. Retrieve<T>(uint handle).
        GLObjectType ot = GetType().GetCustomAttribute<GLObjectAttribute>(true)!.ObjectType;

        if (!GLObjects.TryGetValue(ot, out Dictionary<uint, GLObject>? dict))
        {
            dict = new Dictionary<uint, GLObject>();
            GLObjects.Add(ot, dict);
        }

        dict.TryAdd(handle, this);
    }

    public uint Handle { get; }

    public bool IsNull => Handle == 0;
}