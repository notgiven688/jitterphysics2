using System.Runtime.CompilerServices;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Conversion
{
    public static JMatrix ToJitterMatrix(Matrix4 im)
    {
        JMatrix mat;
        mat.M11 = im.M11;
        mat.M12 = im.M12;
        mat.M13 = im.M13;

        mat.M21 = im.M21;
        mat.M22 = im.M22;
        mat.M23 = im.M23;

        mat.M31 = im.M31;
        mat.M32 = im.M32;
        mat.M33 = im.M33;

        return mat;
    }

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
        Matrix4 mat = new Matrix4();
        mat.M11 = jmat.M11;
        mat.M12 = jmat.M12;
        mat.M13 = jmat.M13;
        mat.M14 = 0;

        mat.M21 = jmat.M21;
        mat.M22 = jmat.M22;
        mat.M23 = jmat.M23;
        mat.M24 = 0;

        mat.M31 = jmat.M31;
        mat.M32 = jmat.M32;
        mat.M33 = jmat.M33;
        mat.M34 = 0;

        mat.M41 = 0;
        mat.M42 = 0;
        mat.M43 = 0;
        mat.M44 = 1;

        return mat;
    }

    public static unsafe void FromJitterOpt(RigidBody body, ref Matrix4 mat)
    {
        Unsafe.CopyBlock(Unsafe.AsPointer(ref mat.M11), Unsafe.AsPointer(ref body.Data.Orientation.M11), 12);
        Unsafe.CopyBlock(Unsafe.AsPointer(ref mat.M12), Unsafe.AsPointer(ref body.Data.Orientation.M12), 12);
        Unsafe.CopyBlock(Unsafe.AsPointer(ref mat.M13), Unsafe.AsPointer(ref body.Data.Orientation.M13), 12);
        Unsafe.CopyBlock(Unsafe.AsPointer(ref mat.M14), Unsafe.AsPointer(ref body.Data.Position.X), 12);
    }

    public static Matrix4 FromJitter(RigidBody body)
    {
        Unsafe.SkipInit(out Matrix4 mat);

        ref JMatrix ori = ref body.Data.Orientation;
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