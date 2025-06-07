using System;
using System.Runtime.CompilerServices;

namespace Jitter2.LinearMath;

using System.Numerics;

// Enables implicit conversion between JVector/JQuaternion and .NET's own numeric types (Vector3 and Quaternion).
// This allows seamless interoperability with .NET libraries that use these types.

public partial struct JVector
{
    public static implicit operator JVector((Real x, Real y, Real z) tuple) => new JVector(tuple.x, tuple.y, tuple.z);

    public static implicit operator Vector3(in JVector v) => new((float)v.X, (float)v.Y, (float)v.Z); // Unsafe.As<JVector, Vector3>(ref Unsafe.AsRef(v));

    public static implicit operator JVector(in Vector3 v) => new(v.X, v.Y, v.Z); // Unsafe.As<Vector3, JVector>(ref Unsafe.AsRef(v));
}

public partial struct JQuaternion
{
    public static implicit operator JQuaternion((Real x, Real y, Real z, Real w) tuple) => new JQuaternion(tuple.x, tuple.y, tuple.z, tuple.w);

    public static implicit operator Quaternion(in JQuaternion q) => new((float)q.X, (float)q.Y, (float)q.Z, (float)q.W); // Unsafe.As<JQuaternion, Quaternion>(ref Unsafe.AsRef(q));

    public static implicit operator JQuaternion(in Quaternion q) => new(q.X, q.Y, q.Z, q.W); // Unsafe.As<Quaternion, JQuaternion>(ref Unsafe.AsRef(q));
}

#if DEBUG

public static class UnsafeBase64Serializer<T> where T : unmanaged
{
    public static string Serialize(in T value)
    {
        int size = Unsafe.SizeOf<T>();
        byte[] buffer = new byte[size];

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                *(T*)ptr = value;
            }
        }

        return Convert.ToBase64String(buffer);
    }

    public static T Deserialize(string base64)
    {
        byte[] buffer = Convert.FromBase64String(base64);
        if (buffer.Length != Unsafe.SizeOf<T>())
            throw new InvalidOperationException("Data size does not match type size.");

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                return *(T*)ptr;
            }
        }
    }
}

#endif