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

        if (GLObjects.TryGetValue(ot, out var dict) && dict.TryGetValue(handle, out var obj))
        {
            return (T)obj;
        }

        throw new Exception($"Could not find {typeof(T).Name} with handle {handle}.");
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

    protected void Remove()
    {
        GLObjectType ot = GetType().GetCustomAttribute<GLObjectAttribute>(true)!.ObjectType;
        if (GLObjects.TryGetValue(ot, out var dict))
        {
            dict.Remove(Handle);
        }
    }

    public uint Handle { get; }

    public bool IsNull => Handle == 0;
}