using System;

#if USE_DOUBLE_PRECISION
using Real = System.Double;
using MathR = System.Math;
#else
using Real = System.Single;
using MathR = System.MathF;
#endif

namespace JitterDemo.Renderer.OpenGL;

public static class MatrixHelper
{
    public static Matrix4 CreateTranslation(Real xPosition, Real yPosition, Real zPosition)
    {
        Matrix4 result = Matrix4.Identity;

        result.M14 = (float)xPosition;
        result.M24 = (float)yPosition;
        result.M34 = (float)zPosition;

        return result;
    }

    public static Matrix4 CreateTranslation(in Vector3 position)
    {
        Matrix4 result = Matrix4.Identity;
        result.M14 = position.X;
        result.M24 = position.Y;
        result.M34 = position.Z;
        return result;
    }

    public static Matrix4 CreatePerspectiveFieldOfView(Real fieldOfView, Real aspectRatio, Real nearPlaneDistance,
        Real farPlaneDistance)
    {
        if (fieldOfView <= 0.0f || fieldOfView >= Math.PI)
            throw new ArgumentOutOfRangeException(nameof(fieldOfView));

        if (nearPlaneDistance <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

        if (farPlaneDistance <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

        Real yScale = 1.0f / (Real)Math.Tan(fieldOfView * 0.5f);
        Real xScale = yScale / aspectRatio;

        Matrix4 result;

        result.M11 = (float)xScale;
        result.M12 = result.M13 = result.M14 = 0.0f;

        result.M22 = (float)yScale;
        result.M21 = result.M23 = result.M24 = 0.0f;

        result.M31 = result.M32 = 0.0f;
        result.M33 = (float)(farPlaneDistance / (nearPlaneDistance - farPlaneDistance));
        result.M34 = -1.0f;

        result.M41 = result.M42 = result.M44 = 0.0f;
        result.M43 = (float)(nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance));

        return Matrix4.Transpose(result);
    }

    public static Matrix4 CreateRotationX(Real radians)
    {
        Matrix4 result = Matrix4.Identity;

        Real c = (Real)Math.Cos(radians);
        Real s = (Real)Math.Sin(radians);

        // [  1  0  0  0 ]
        // [  0  c -s  0 ]
        // [  0  s  c  0 ]
        // [  0  0  0  1 ]
        result.M22 = (float)c;
        result.M23 = (float)-s;
        result.M32 = (float)s;
        result.M33 = (float)c;

        return result;
    }

    public static Matrix4 CreateRotationY(Real radians)
    {
        Matrix4 result = Matrix4.Identity;

        Real c = (Real)Math.Cos(radians);
        Real s = (Real)Math.Sin(radians);

        // [  c  0  s  0 ]
        // [  0  1  0  0 ]
        // [ -s  0  c  0 ]
        // [  0  0  0  1 ]
        result.M11 = (float)c;
        result.M13 = (float)s;
        result.M31 = (float)-s;
        result.M33 = (float)c;

        return result;
    }

    public static Matrix4 CreateRotationZ(Real radians)
    {
        Matrix4 result = Matrix4.Identity;

        Real c = (Real)Math.Cos(radians);
        Real s = (Real)Math.Sin(radians);

        // [  c -s  0  0 ]
        // [  s  c  0  0 ]
        // [  0  0  1  0 ]
        // [  0  0  0  1 ]
        result.M11 = (float)c;
        result.M12 = (float)-s;
        result.M21 = (float)s;
        result.M22 = (float)c;

        return result;
    }

    public static Matrix4 CreateScale(Real scale)
    {
        Matrix4 result = Matrix4.Identity;

        result.M11 = (float)scale;
        result.M22 = (float)scale;
        result.M33 = (float)scale;

        return result;
    }

    public static Matrix4 CreateScale(Real xScale, Real yScale, Real zScale)
    {
        Matrix4 result = Matrix4.Identity;

        result.M11 = (float)xScale;
        result.M22 = (float)yScale;
        result.M33 = (float)zScale;

        return result;
    }

    public static Matrix4 CreateOrthographicOffCenter(Real left, Real right, Real bottom, Real top,
        Real zNearPlane, Real zFarPlane)
    {
        Matrix4 result;

        result.M11 = (float)(2.0f / (right - left));
        result.M12 = result.M13 = result.M14 = 0.0f;

        result.M22 = (float)(2.0f / (top - bottom));
        result.M21 = result.M23 = result.M24 = 0.0f;

        result.M33 = (float)(1.0f / (zNearPlane - zFarPlane));
        result.M31 = result.M32 = result.M34 = 0.0f;

        result.M41 = (float)((left + right) / (left - right));
        result.M42 = (float)((top + bottom) / (bottom - top));
        result.M43 = (float)(zNearPlane / (zNearPlane - zFarPlane));
        result.M44 = 1.0f;

        return Matrix4.Transpose(result);
    }

    public static Matrix4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
    {
        Vector3 zaxis = Vector3.Normalize(cameraPosition - cameraTarget);
        Vector3 xaxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zaxis));
        Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

        Matrix4 result;

        result.M11 = xaxis.X;
        result.M12 = yaxis.X;
        result.M13 = zaxis.X;
        result.M14 = 0.0f;
        result.M21 = xaxis.Y;
        result.M22 = yaxis.Y;
        result.M23 = zaxis.Y;
        result.M24 = 0.0f;
        result.M31 = xaxis.Z;
        result.M32 = yaxis.Z;
        result.M33 = zaxis.Z;
        result.M34 = 0.0f;
        result.M41 = -Vector3.Dot(xaxis, cameraPosition);
        result.M42 = -Vector3.Dot(yaxis, cameraPosition);
        result.M43 = -Vector3.Dot(zaxis, cameraPosition);
        result.M44 = 1.0f;

        return Matrix4.Transpose(result);
    }
}