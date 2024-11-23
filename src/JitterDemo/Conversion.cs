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
        return new Vector3((float)vector.X, (float)vector.Y, (float)vector.Z);
    }

    public static Matrix4 FromJitter(JMatrix jmat)
    {
        return new Matrix4
        {
            M11 = (float)jmat.M11,
            M12 = (float)jmat.M12,
            M13 = (float)jmat.M13,
            M14 = 0,

            M21 = (float)jmat.M21,
            M22 = (float)jmat.M22,
            M23 = (float)jmat.M23,
            M24 = 0,

            M31 = (float)jmat.M31,
            M32 = (float)jmat.M32,
            M33 = (float)jmat.M33,
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

        mat.M11 = (float)ori.M11;
        mat.M12 = (float)ori.M12;
        mat.M13 = (float)ori.M13;
        mat.M14 = (float)pos.X;

        mat.M21 = (float)ori.M21;
        mat.M22 = (float)ori.M22;
        mat.M23 = (float)ori.M23;
        mat.M24 = (float)pos.Y;

        mat.M31 = (float)ori.M31;
        mat.M32 = (float)ori.M32;
        mat.M33 = (float)ori.M33;
        mat.M34 = (float)pos.Z;

        mat.M41 = 0;
        mat.M42 = 0;
        mat.M43 = 0;
        mat.M44 = 1;

        return mat;
    }
}