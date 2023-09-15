using System;

namespace JitterDemo.Renderer.OpenGL;

public static class MatrixHelper
{
    public static Matrix4 CreateTranslation(float xPosition, float yPosition, float zPosition)
    {
        Matrix4 result = Matrix4.Identity;

        result.M14 = xPosition;
        result.M24 = yPosition;
        result.M34 = zPosition;

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

    public static Matrix4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance,
        float farPlaneDistance)
    {
        if (fieldOfView <= 0.0f || fieldOfView >= Math.PI)
            throw new ArgumentOutOfRangeException(nameof(fieldOfView));

        if (nearPlaneDistance <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

        if (farPlaneDistance <= 0.0f)
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

        if (nearPlaneDistance >= farPlaneDistance)
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

        float yScale = 1.0f / (float)Math.Tan(fieldOfView * 0.5f);
        float xScale = yScale / aspectRatio;

        Matrix4 result;

        result.M11 = xScale;
        result.M12 = result.M13 = result.M14 = 0.0f;

        result.M22 = yScale;
        result.M21 = result.M23 = result.M24 = 0.0f;

        result.M31 = result.M32 = 0.0f;
        result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
        result.M34 = -1.0f;

        result.M41 = result.M42 = result.M44 = 0.0f;
        result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

        return Matrix4.Transpose(result);
    }

    public static Matrix4 CreateRotationX(float radians)
    {
        Matrix4 result = Matrix4.Identity;

        float c = (float)Math.Cos(radians);
        float s = (float)Math.Sin(radians);

        // [  1  0  0  0 ]
        // [  0  c -s  0 ]
        // [  0  s  c  0 ]
        // [  0  0  0  1 ]
        result.M22 = c;
        result.M23 = -s;
        result.M32 = s;
        result.M33 = c;

        return result;
    }

    public static Matrix4 CreateRotationY(float radians)
    {
        Matrix4 result = Matrix4.Identity;

        float c = (float)Math.Cos(radians);
        float s = (float)Math.Sin(radians);

        // [  c  0  s  0 ]
        // [  0  1  0  0 ]
        // [ -s  0  c  0 ]
        // [  0  0  0  1 ]
        result.M11 = c;
        result.M13 = s;
        result.M31 = -s;
        result.M33 = c;

        return result;
    }

    public static Matrix4 CreateRotationZ(float radians)
    {
        Matrix4 result = Matrix4.Identity;

        float c = (float)Math.Cos(radians);
        float s = (float)Math.Sin(radians);

        // [  c -s  0  0 ]
        // [  s  c  0  0 ]
        // [  0  0  1  0 ]
        // [  0  0  0  1 ]
        result.M11 = c;
        result.M12 = -s;
        result.M21 = s;
        result.M22 = c;

        return result;
    }

    public static Matrix4 CreateScale(float scale)
    {
        Matrix4 result = Matrix4.Identity;

        result.M11 = scale;
        result.M22 = scale;
        result.M33 = scale;

        return result;
    }

    public static Matrix4 CreateScale(float xScale, float yScale, float zScale)
    {
        Matrix4 result = Matrix4.Identity;

        result.M11 = xScale;
        result.M22 = yScale;
        result.M33 = zScale;

        return result;
    }

    public static Matrix4 CreateOrthographicOffCenter(float left, float right, float bottom, float top,
        float zNearPlane, float zFarPlane)
    {
        Matrix4 result;

        result.M11 = 2.0f / (right - left);
        result.M12 = result.M13 = result.M14 = 0.0f;

        result.M22 = 2.0f / (top - bottom);
        result.M21 = result.M23 = result.M24 = 0.0f;

        result.M33 = 1.0f / (zNearPlane - zFarPlane);
        result.M31 = result.M32 = result.M34 = 0.0f;

        result.M41 = (left + right) / (left - right);
        result.M42 = (top + bottom) / (bottom - top);
        result.M43 = zNearPlane / (zNearPlane - zFarPlane);
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