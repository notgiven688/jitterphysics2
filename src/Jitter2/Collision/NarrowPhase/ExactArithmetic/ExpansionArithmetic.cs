/*
 * Error-free sum/difference and expansion accumulation adapted from
 * Jonathan Richard Shewchuk's public-domain predicates.c.
 * https://www.cs.cmu.edu/~quake/robust.html
 * Products use a fused multiply-add to recover the exact residual.
 */

using System;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;

namespace Jitter2.Collision.ExactArithmetic;

// Exact sums of nonoverlapping doubles, stored from least to most significant.
// Callers must keep coordinates inside PlaneFilter's safe range: error-free
// transforms require that both products and their residuals avoid underflow.
// Scratch storage is read only through initialized prefix lengths.
internal static class ExpansionArithmetic
{
    [SkipLocalsInit]
    public static int Side(in JVector a, in JVector b, in JVector c, in JVector point)
    {
        Span<double> differences = stackalloc double[18];
        var ux = differences[..2]; var uy = differences[2..4]; var uz = differences[4..6];
        var vx = differences[6..8]; var vy = differences[8..10]; var vz = differences[10..12];
        var dx = differences[12..14]; var dy = differences[14..16]; var dz = differences[16..18];
        int nx = Difference(a.X, b.X, ux), ny = Difference(a.Y, b.Y, uy), nz = Difference(a.Z, b.Z, uz);
        int mx = Difference(a.X, c.X, vx), my = Difference(a.Y, c.Y, vy), mz = Difference(a.Z, c.Z, vz);
        int px = Difference(point.X, a.X, dx), py = Difference(point.Y, a.Y, dy), pz = Difference(point.Z, a.Z, dz);
        // Each cross component has at most 16 terms. Multiplying it by a
        // two-term displacement adds at most 64 terms; three components fit 192.
        Span<double> cross = stackalloc double[16];
        Span<double> determinant = stackalloc double[192];
        int length = Cross(uy[..ny], vz[..mz], uz[..nz], vy[..my], cross);
        int count = AddProduct(determinant, 0, cross[..length], dx[..px], false);
        length = Cross(uz[..nz], vx[..mx], ux[..nx], vz[..mz], cross);
        count = AddProduct(determinant, count, cross[..length], dy[..py], false);
        length = Cross(ux[..nx], vy[..my], uy[..ny], vx[..mx], cross);
        count = AddProduct(determinant, count, cross[..length], dz[..pz], false);
        return count == 0 ? 0 : Math.Sign(determinant[count - 1]);
    }

    [SkipLocalsInit]
    public static void Normal(in JVector a, in JVector b, in JVector c, out double x, out double y, out double z)
    {
        Span<double> differences = stackalloc double[12];
        var ux = differences[..2]; var uy = differences[2..4]; var uz = differences[4..6];
        var vx = differences[6..8]; var vy = differences[8..10]; var vz = differences[10..12];
        int nx = Difference(a.X, b.X, ux), ny = Difference(a.Y, b.Y, uy), nz = Difference(a.Z, b.Z, uz);
        int mx = Difference(a.X, c.X, vx), my = Difference(a.Y, c.Y, vy), mz = Difference(a.Z, c.Z, vz);
        Span<double> cross = stackalloc double[16];
        x = Estimate(cross[..Cross(uy[..ny], vz[..mz], uz[..nz], vy[..my], cross)]);
        y = Estimate(cross[..Cross(uz[..nz], vx[..mx], ux[..nx], vz[..mz], cross)]);
        z = Estimate(cross[..Cross(ux[..nx], vy[..my], uy[..ny], vx[..mx], cross)]);
    }

    private static double Estimate(ReadOnlySpan<double> expansion)
    {
        double value = 0;
        foreach (double part in expansion) value += part;
        return value;
    }

    private static int Cross(ReadOnlySpan<double> a, ReadOnlySpan<double> b,
        ReadOnlySpan<double> c, ReadOnlySpan<double> d, Span<double> result)
    {
        int length = AddProduct(result, 0, a, b, false);
        return AddProduct(result, length, c, d, true);
    }

    private static int AddProduct(Span<double> result, int length,
        ReadOnlySpan<double> a, ReadOnlySpan<double> b, bool negate)
    {
        foreach (double x in a)
        foreach (double y in b)
        {
            double product = x * y;
            double residual = Math.FusedMultiplyAdd(x, y, -product);
            if (negate) { product = -product; residual = -residual; }
            if (residual != 0) length = Grow(result, length, residual);
            if (product != 0) length = Grow(result, length, product);
        }

        return length;
    }

    // grow_expansion_zeroelim can operate in place: it never overwrites an
    // input component that has not yet been read.
    private static int Grow(Span<double> expansion, int length, double value)
    {
        int count = 0;
        for (int i = 0; i < length; i++)
        {
            double part = expansion[i];
            double sum = value + part;
            double virtualPart = sum - value;
            double residual = (value - (sum - virtualPart)) + (part - virtualPart);
            if (residual != 0) expansion[count++] = residual;
            value = sum;
        }

        if (value != 0) expansion[count++] = value;
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Difference(double a, double b, Span<double> result)
    {
        double value = a - b;
        double virtualB = a - value;
        double residual = (a - (value + virtualB)) + (virtualB - b);
        int count = 0;
        if (residual != 0) result[count++] = residual;
        if (value != 0) result[count++] = value;
        return count;
    }
}
