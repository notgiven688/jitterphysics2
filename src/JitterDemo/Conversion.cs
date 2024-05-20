using System;
using System.Runtime.CompilerServices;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Conversion
{
    public static JVector ToJitterVector(Vector3 im)
    {
        return new JVector(im.X, im.Y, im.Z);
    }

    public static Vector3 FromJitter(JVector vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    public static Matrix4 FromJitter(JMatrix jmat)
    {
        return new Matrix4
        {
            M11 = jmat.M11,
            M12 = jmat.M12,
            M13 = jmat.M13,
            M14 = 0,

            M21 = jmat.M21,
            M22 = jmat.M22,
            M23 = jmat.M23,
            M24 = 0,

            M31 = jmat.M31,
            M32 = jmat.M32,
            M33 = jmat.M33,
            M34 = 0,

            M41 = 0,
            M42 = 0,
            M43 = 0,
            M44 = 1
        };
    }

    public static Matrix4 FromJitter(RigidBody body)
    {
        Unsafe.SkipInit(out Matrix4 mat);

        JMatrix ori = JMatrix.CreateFromQuaternion(body.Data.Orientation);
        ref JVector pos = ref body.Data.Position;

        mat.M11 = ori.M11;
        mat.M12 = ori.M12;
        mat.M13 = ori.M13;
        mat.M14 = pos.X;

        mat.M21 = ori.M21;
        mat.M22 = ori.M22;
        mat.M23 = ori.M23;
        mat.M24 = pos.Y;

        mat.M31 = ori.M31;
        mat.M32 = ori.M32;
        mat.M33 = ori.M33;
        mat.M34 = pos.Z;

        mat.M41 = 0;
        mat.M42 = 0;
        mat.M43 = 0;
        mat.M44 = 1;

        return mat;
    }
}