/*
 * Adapted from ExactHull's Exact and Exact3 types.
 * https://github.com/notgiven688/ExactHull
 * (c) Thorben Linneweber
 * SPDX-License-Identifier: MIT
 *
 * MIT License
 *
 * Copyright (c) Thorben Linneweber
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
using System.Numerics;

namespace JitterTests.Behavior;

// Test-only reference arithmetic on the supplied IEEE coordinates. No division is needed by
// the orientation predicates: a dyadic is closed under addition and multiplication.
internal readonly struct Dyadic
{
    private readonly BigInteger mantissa;
    private readonly int exponent;

    public int Sign => mantissa.Sign;
    public bool IsZero => mantissa.IsZero;

    public Dyadic(BigInteger mantissa, int exponent)
    {
        if (mantissa.IsZero)
        {
            this.mantissa = BigInteger.Zero;
            this.exponent = 0;
            return;
        }

        while (mantissa.IsEven)
        {
            mantissa >>= 1;
            exponent++;
        }

        this.mantissa = mantissa;
        this.exponent = exponent;
    }

    public static Dyadic FromDouble(double value)
    {
        if (!double.IsFinite(value)) throw new ArgumentException("Coordinate must be finite.", nameof(value));

        long bits = BitConverter.DoubleToInt64Bits(value);
        int rawExponent = (int)((bits >> 52) & 0x7ff);
        long fraction = bits & 0x000f_ffff_ffff_ffff;
        BigInteger significand = rawExponent == 0 ? fraction : (1L << 52) | fraction;
        if (bits < 0) significand = -significand;
        return new Dyadic(significand, rawExponent == 0 ? -1074 : rawExponent - 1075);
    }

    public static Dyadic operator +(Dyadic a, Dyadic b)
    {
        if (a.IsZero) return b;
        if (b.IsZero) return a;
        int commonExponent = Math.Min(a.exponent, b.exponent);
        return new Dyadic((a.mantissa << (a.exponent - commonExponent)) +
                          (b.mantissa << (b.exponent - commonExponent)), commonExponent);
    }

    public static Dyadic operator -(Dyadic a, Dyadic b) => a + -b;
    public static Dyadic operator -(Dyadic a) => new(-a.mantissa, a.exponent);
    public static Dyadic operator *(Dyadic a, Dyadic b) => new(a.mantissa * b.mantissa, a.exponent + b.exponent);
}

internal readonly struct Dyadic3(Dyadic x, Dyadic y, Dyadic z)
{
    public readonly Dyadic X = x, Y = y, Z = z;
    public bool IsZero => X.IsZero && Y.IsZero && Z.IsZero;

    public static Dyadic3 FromVector(in JVector v) =>
        new(Dyadic.FromDouble(v.X), Dyadic.FromDouble(v.Y), Dyadic.FromDouble(v.Z));

    public static Dyadic Dot(in Dyadic3 a, in Dyadic3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Dyadic3 Cross(in Dyadic3 a, in Dyadic3 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    public static Dyadic3 operator +(Dyadic3 a, Dyadic3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Dyadic3 operator -(Dyadic3 a, Dyadic3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Dyadic3 operator -(Dyadic3 a) => new(-a.X, -a.Y, -a.Z);
    public static Dyadic3 operator *(Dyadic3 a, Dyadic b) => new(a.X * b, a.Y * b, a.Z * b);
}
