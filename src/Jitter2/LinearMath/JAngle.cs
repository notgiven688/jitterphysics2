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
[StructLayout(LayoutKind.Explicit, Size = 1 * sizeof(Real))]
public struct JAngle : IEquatable<JAngle>, IFormattable
{
    /// <summary>
    /// Gets or sets the angle value in radians.
    /// </summary>
    [field: FieldOffset(0 * sizeof(Real))]
    public Real Radian { get; set; }

    /// <summary>
    /// Returns a string representation of the <see cref="JAngle"/>.
    /// </summary>
    public readonly override string ToString() => ToString(null, null);

    /// <summary>Formats the angle in radians and degrees using the specified numeric format and the current culture.</summary>
    public readonly string ToString(string? format) => ToString(format, null);

    /// <summary>Formats the angle in radians and degrees using the specified numeric format and culture.</summary>
    /// <param name="format">A numeric format string, or null or empty to use the general format.</param>
    /// <param name="formatProvider">The format provider, or null to use the current culture.</param>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"Radian={Radian.ToString(format, formatProvider)}, Degree={Degree.ToString(format, formatProvider)}";
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
