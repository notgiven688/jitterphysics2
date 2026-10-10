/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;

namespace Jitter2.Collision.ExactArithmetic;

// A cached floating-point filter for dot(cross(a-b, a-c), p-a).
// This accepts only proven nonzero signs. Uncertain signs, including exact zero,
// must be evaluated by the caller's exact arithmetic.
// Callers validate all coordinates with IsSafe before constructing or querying it.
internal struct PlaneFilter
{
    private const double UnitRoundoff = 1.1102230246251565e-16; // 2^-53, not double.Epsilon.
    private const double ErrorFactor = 16 * UnitRoundoff;
    private readonly double errorX, errorY, errorZ;
    public double NormalX, NormalY, NormalZ;

    // Geometry still uses approximate normals. Re-evaluate poorly conditioned
    // cross products through exact expansions before converting to doubles.
    public readonly bool ReliableNormal => Math.Max(Math.Abs(NormalX), Math.Max(Math.Abs(NormalY), Math.Abs(NormalZ))) >
        1048576 * Math.Max(errorX, Math.Max(errorY, errorZ));

    public PlaneFilter(in JVector a, in JVector b, in JVector c)
    {
        double ux = (double)a.X - b.X, uy = (double)a.Y - b.Y, uz = (double)a.Z - b.Z;
        double vx = (double)a.X - c.X, vy = (double)a.Y - c.Y, vz = (double)a.Z - c.Z;
        double yz = uy * vz, zy = uz * vy;
        double zx = uz * vx, xz = ux * vz;
        double xy = ux * vy, yx = uy * vx;
        NormalX = yz - zy;
        NormalY = zx - xz;
        NormalZ = xy - yx;

        // Two rounded coordinate differences, a product, and a subtraction
        // give < 5u times the sum of absolute rounded products. 16u leaves
        // room for rounding while computing the bound itself.
        errorX = ErrorFactor * (Math.Abs(yz) + Math.Abs(zy));
        errorY = ErrorFactor * (Math.Abs(zx) + Math.Abs(xz));
        errorZ = ErrorFactor * (Math.Abs(xy) + Math.Abs(yx));
    }

    public void Negate()
    {
        NormalX = -NormalX;
        NormalY = -NormalY;
        NormalZ = -NormalZ;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TrySide(in JVector anchor, in JVector point, out int sign)
    {
        sign = 0;
        double dx = (double)point.X - anchor.X, dy = (double)point.Y - anchor.Y, dz = (double)point.Z - anchor.Z;
        double x = NormalX * dx, y = NormalY * dy, z = NormalZ * dz;
        double value = (x + y) + z;
        double roundoff = ErrorFactor * ((Math.Abs(x) + Math.Abs(y)) + Math.Abs(z));
        double coefficientError = (errorX * Math.Abs(dx) + errorY * Math.Abs(dy)) + errorZ * Math.Abs(dz);
        // 16u covers the rounded displacement and dot product (< 8u).
        // The factor two also covers rounding of the coefficient-error sum.
        double bound = roundoff + 2 * coefficientError;
        if (Math.Abs(value) <= bound) return false;
        sign = value > 0 ? 1 : -1;
        return true;
    }

    // Nonzero coordinates in [2^-200, 2^200] ensure that coordinate differences,
    // all products, and the error bounds stay normal and finite in double.
    // Values outside this window are unsupported, including double subnormals.
    public static bool IsSafe(in JVector point) => SafeCoordinate(point.X) && SafeCoordinate(point.Y) && SafeCoordinate(point.Z);

    private static bool SafeCoordinate(double value) => value == 0 ||
        (Math.Abs(value) >= 6.223015277861142e-61 && Math.Abs(value) <= 1.6069380442589903e60);
}
