/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>
/// A floating point variable of type <see cref="Real"/> representing an angle. This structure exists to eliminate
/// ambiguity between radians and degrees in the Jitter API.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 1*sizeof(Real))]
public struct JAngle : IEquatable<JAngle>
{
    /// <summary>
    /// Gets or sets the angle value in radians.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the angle value in degrees.
    /// </summary>
    public Real Degree
    {
        readonly get => Radian / MathR.PI * (Real)180.0;
        set => Radian = value / (Real)180.0 * MathR.PI;
    }

    /// <summary>
    /// Creates a <see cref="JAngle"/> from a value in radians.
    /// </summary>
    public static JAngle FromRadian(Real rad)
    {
        return new JAngle { Radian = rad };
    }

    /// <summary>
    /// Creates a <see cref="JAngle"/> from a value in degrees.
    /// </summary>
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