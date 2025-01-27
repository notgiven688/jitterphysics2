/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>
/// A 32-bit floating point variable representing an angle. This structure exists to eliminate
/// ambiguity between radians and degrees in the Jitter API.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 1*sizeof(Real))]
public struct JAngle : IEquatable<JAngle>
{
    [field: FieldOffset(0*sizeof(Real))]
    public Real Radian { get; set; }

    /// <summary>
    /// Returns a string representation of the <see cref="JAngle"/>.
    /// </summary>
    public readonly override string ToString()
    {
        return $"Radian={Radian}, Degree={Degree}";
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is JAngle other && Equals(other);
    }

    public readonly bool Equals(JAngle p)
    {
        return p.Radian == Radian;
    }

    public readonly override int GetHashCode()
    {
        return Radian.GetHashCode();
    }

    public Real Degree
    {
        readonly get => Radian / MathR.PI * (Real)180.0;
        set => Radian = value / (Real)180.0 * MathR.PI;
    }

    public static JAngle FromRadian(Real rad)
    {
        return new JAngle { Radian = rad };
    }

    public static JAngle FromDegree(Real deg)
    {
        return new JAngle { Degree = deg };
    }

    public static explicit operator JAngle(Real angle)
    {
        return FromRadian(angle);
    }

    public static JAngle operator -(JAngle a)
    {
        return FromRadian(-a.Radian);
    }

    public static JAngle operator +(JAngle a, JAngle b)
    {
        return FromRadian(a.Radian + b.Radian);
    }

    public static JAngle operator -(JAngle a, JAngle b)
    {
        return FromRadian(a.Radian - b.Radian);
    }

    public static bool operator ==(JAngle l, JAngle r)
    {
        return (Real)l == (Real)r;
    }

    public static bool operator !=(JAngle l, JAngle r)
    {
        return (Real)l != (Real)r;
    }

    public static bool operator <(JAngle l, JAngle r)
    {
        return (Real)l < (Real)r;
    }

    public static bool operator >(JAngle l, JAngle r)
    {
        return (Real)l > (Real)r;
    }

    public static bool operator >=(JAngle l, JAngle r)
    {
        return (Real)l >= (Real)r;
    }

    public static bool operator <=(JAngle l, JAngle r)
    {
        return (Real)l <= (Real)r;
    }

    public static explicit operator Real(JAngle angle)
    {
        return angle.Radian;
    }
}