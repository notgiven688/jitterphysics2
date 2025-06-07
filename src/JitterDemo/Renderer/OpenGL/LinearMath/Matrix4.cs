using System;
using System.Runtime.InteropServices;

namespace JitterDemo.Renderer.OpenGL;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct Matrix4
{
    public static Matrix4 Identity { get; } = new(
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f
    );

    public Matrix4(float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11;
        M12 = m12;
        M13 = m13;
        M14 = m14;

        M21 = m21;
        M22 = m22;
        M23 = m23;
        M24 = m24;

        M31 = m31;
        M32 = m32;
        M33 = m33;
        M34 = m34;

        M41 = m41;
        M42 = m42;
        M43 = m43;
        M44 = m44;
    }

    // In memory the matrix entries are layed out in column
    // major format. This implies, that the Matrix entries M{row}{column}
    //
    //      M11 M12 M13 M14
    // M =  M21 M22 M23 M24
    //      M31 M32 M33 M34
    //      M41 M42 M43 M44
    //
    // are stored column wise in memory, i.e.
    //
    // M11 M21 M31 M41 M12 M22 M32 M42 M13 M23 M33 M43 M14 M24 M34 M44
    //
    // Vector4 represents a column vector. Multiplication follows the usual convention
    //
    //      M11 M12 M13 M14       a      a*M11 + b*M12 + c*M13 + d*M14
    //      M21 M22 M23 M24   x   b   =  a*M12 + b*M22 + c*M23 + ...
    //      M31 M32 M33 M34       c      ...
    //      M41 M42 M43 M44       d      ...
    //
    // and
    //
    //      A11 A12 A13 A14       B11 B12 B13 B14       A11*B11+A12*B21+A13*B31+A14*B41 ...
    //      A21 A22 A23 A24   x   B21 B22 B23 B24   =   ...
    //      A31 A32 A33 A34       B31 B32 B33 B34       ...
    //      A41 A42 A43 A44       B41 B42 B43 B44       ...
    //

    public static Matrix4 operator *(in Matrix4 value1, in Matrix4 value2)
    {
        return Multiply(value1, value2);
    }

    public static Matrix4 operator +(in Matrix4 value1, in Matrix4 value2)
    {
        return Add(value1, value2);
    }

    public static Matrix4 operator -(in Matrix4 value1, in Matrix4 value2)
    {
        return Subtract(value1, value2);
    }

    public static Matrix4 Transpose(Matrix4 matrix)
    {
        Matrix4 result;

        result.M11 = matrix.M11;
        result.M12 = matrix.M21;
        result.M13 = matrix.M31;
        result.M14 = matrix.M41;
        result.M21 = matrix.M12;
        result.M22 = matrix.M22;
        result.M23 = matrix.M32;
        result.M24 = matrix.M42;
        result.M31 = matrix.M13;
        result.M32 = matrix.M23;
        result.M33 = matrix.M33;
        result.M34 = matrix.M43;
        result.M41 = matrix.M14;
        result.M42 = matrix.M24;
        result.M43 = matrix.M34;
        result.M44 = matrix.M44;

        return result;
    }

    public static Matrix4 Subtract(in Matrix4 value1, in Matrix4 value2)
    {
        Matrix4 result;

        result.M11 = value1.M11 - value2.M11;
        result.M12 = value1.M12 - value2.M12;
        result.M13 = value1.M13 - value2.M13;
        result.M14 = value1.M14 - value2.M14;
        result.M21 = value1.M21 - value2.M21;
        result.M22 = value1.M22 - value2.M22;
        result.M23 = value1.M23 - value2.M23;
        result.M24 = value1.M24 - value2.M24;
        result.M31 = value1.M31 - value2.M31;
        result.M32 = value1.M32 - value2.M32;
        result.M33 = value1.M33 - value2.M33;
        result.M34 = value1.M34 - value2.M34;
        result.M41 = value1.M41 - value2.M41;
        result.M42 = value1.M42 - value2.M42;
        result.M43 = value1.M43 - value2.M43;
        result.M44 = value1.M44 - value2.M44;

        return result;
    }

    public static Matrix4 Add(in Matrix4 value1, in Matrix4 value2)
    {
        Matrix4 result;

        result.M11 = value1.M11 + value2.M11;
        result.M12 = value1.M12 + value2.M12;
        result.M13 = value1.M13 + value2.M13;
        result.M14 = value1.M14 + value2.M14;
        result.M21 = value1.M21 + value2.M21;
        result.M22 = value1.M22 + value2.M22;
        result.M23 = value1.M23 + value2.M23;
        result.M24 = value1.M24 + value2.M24;
        result.M31 = value1.M31 + value2.M31;
        result.M32 = value1.M32 + value2.M32;
        result.M33 = value1.M33 + value2.M33;
        result.M34 = value1.M34 + value2.M34;
        result.M41 = value1.M41 + value2.M41;
        result.M42 = value1.M42 + value2.M42;
        result.M43 = value1.M43 + value2.M43;
        result.M44 = value1.M44 + value2.M44;

        return result;
    }

    public static Matrix4 Multiply(in Matrix4 value1, in Matrix4 value2)
    {
        Matrix4 result;

        // First row
        result.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 +
                     value1.M14 * value2.M41;
        result.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 +
                     value1.M14 * value2.M42;
        result.M13 = value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 +
                     value1.M14 * value2.M43;
        result.M14 = value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34 +
                     value1.M14 * value2.M44;

        // Second row
        result.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 +
                     value1.M24 * value2.M41;
        result.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 +
                     value1.M24 * value2.M42;
        result.M23 = value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 +
                     value1.M24 * value2.M43;
        result.M24 = value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34 +
                     value1.M24 * value2.M44;

        // Third row
        result.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 +
                     value1.M34 * value2.M41;
        result.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 +
                     value1.M34 * value2.M42;
        result.M33 = value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 +
                     value1.M34 * value2.M43;
        result.M34 = value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34 +
                     value1.M34 * value2.M44;

        // Fourth row
        result.M41 = value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 +
                     value1.M44 * value2.M41;
        result.M42 = value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 +
                     value1.M44 * value2.M42;
        result.M43 = value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 +
                     value1.M44 * value2.M43;
        result.M44 = value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 +
                     value1.M44 * value2.M44;

        return result;
    }

    public static bool Invert(Matrix4 matrix, out Matrix4 result)
    {
        float a = matrix.M11, b = matrix.M12, c = matrix.M13, d = matrix.M14;
        float e = matrix.M21, f = matrix.M22, g = matrix.M23, h = matrix.M24;
        float i = matrix.M31, j = matrix.M32, k = matrix.M33, l = matrix.M34;
        float m = matrix.M41, n = matrix.M42, o = matrix.M43, p = matrix.M44;

        float kp_lo = k * p - l * o;
        float jp_ln = j * p - l * n;
        float jo_kn = j * o - k * n;
        float ip_lm = i * p - l * m;
        float io_km = i * o - k * m;
        float in_jm = i * n - j * m;

        float a11 = +(f * kp_lo - g * jp_ln + h * jo_kn);
        float a12 = -(e * kp_lo - g * ip_lm + h * io_km);
        float a13 = +(e * jp_ln - f * ip_lm + h * in_jm);
        float a14 = -(e * jo_kn - f * io_km + g * in_jm);

        float det = a * a11 + b * a12 + c * a13 + d * a14;

        if (Math.Abs(det) < float.Epsilon)
        {
            result = new Matrix4(float.NaN, float.NaN, float.NaN, float.NaN,
                float.NaN, float.NaN, float.NaN, float.NaN,
                float.NaN, float.NaN, float.NaN, float.NaN,
                float.NaN, float.NaN, float.NaN, float.NaN);
            return false;
        }

        float invDet = 1.0f / det;

        result.M11 = a11 * invDet;
        result.M21 = a12 * invDet;
        result.M31 = a13 * invDet;
        result.M41 = a14 * invDet;

        result.M12 = -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet;
        result.M22 = +(a * kp_lo - c * ip_lm + d * io_km) * invDet;
        result.M32 = -(a * jp_ln - b * ip_lm + d * in_jm) * invDet;
        result.M42 = +(a * jo_kn - b * io_km + c * in_jm) * invDet;

        float gp_ho = g * p - h * o;
        float fp_hn = f * p - h * n;
        float fo_gn = f * o - g * n;
        float ep_hm = e * p - h * m;
        float eo_gm = e * o - g * m;
        float en_fm = e * n - f * m;

        result.M13 = +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet;
        result.M23 = -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet;
        result.M33 = +(a * fp_hn - b * ep_hm + d * en_fm) * invDet;
        result.M43 = -(a * fo_gn - b * eo_gm + c * en_fm) * invDet;

        float gl_hk = g * l - h * k;
        float fl_hj = f * l - h * j;
        float fk_gj = f * k - g * j;
        float el_hi = e * l - h * i;
        float ek_gi = e * k - g * i;
        float ej_fi = e * j - f * i;

        result.M14 = -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet;
        result.M24 = +(a * gl_hk - c * el_hi + d * ek_gi) * invDet;
        result.M34 = -(a * fl_hj - b * el_hi + d * ej_fi) * invDet;
        result.M44 = +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet;

        return true;
    }

    [FieldOffset(0)] public float M11;
    [FieldOffset(4)] public float M21;
    [FieldOffset(8)] public float M31;
    [FieldOffset(12)] public float M41;

    [FieldOffset(16)] public float M12;
    [FieldOffset(20)] public float M22;
    [FieldOffset(24)] public float M32;
    [FieldOffset(28)] public float M42;

    [FieldOffset(32)] public float M13;
    [FieldOffset(36)] public float M23;
    [FieldOffset(40)] public float M33;
    [FieldOffset(44)] public float M43;

    [FieldOffset(48)] public float M14;
    [FieldOffset(52)] public float M24;
    [FieldOffset(56)] public float M34;
    [FieldOffset(60)] public float M44;
}