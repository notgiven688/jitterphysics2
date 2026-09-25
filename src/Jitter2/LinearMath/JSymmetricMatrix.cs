/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitter2.LinearMath;

/// <summary>A symmetric 3-by-3 matrix stored in six scalars.</summary>
[StructLayout(LayoutKind.Explicit, Size = 6 * sizeof(Real))]
public struct JSymmetricMatrix : IEquatable<JSymmetricMatrix>
{
    [FieldOffset(0 * sizeof(Real))] public Real M11;
    [FieldOffset(1 * sizeof(Real))] public Real M22;
    [FieldOffset(2 * sizeof(Real))] public Real M33;
    [FieldOffset(3 * sizeof(Real))] public Real M12;
    [FieldOffset(3 * sizeof(Real))] public Real M21;
    [FieldOffset(4 * sizeof(Real))] public Real M13;
    [FieldOffset(4 * sizeof(Real))] public Real M31;
    [FieldOffset(5 * sizeof(Real))] public Real M23;
    [FieldOffset(5 * sizeof(Real))] public Real M32;

    public static readonly JSymmetricMatrix Zero = default;
    public static readonly JSymmetricMatrix Identity = new() { M11 = 1, M22 = 1, M33 = 1 };

    /// <summary>Copies the six independent entries of a symmetric matrix.</summary>
    public JSymmetricMatrix(in JMatrix matrix)
    {
        this = default;
        M11 = matrix.M11;
        M22 = matrix.M22;
        M33 = matrix.M33;
        // Match the entries historically used by the contact solver.
        M12 = matrix.M21;
        M13 = matrix.M31;
        M23 = matrix.M23;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator JMatrix(JSymmetricMatrix matrix) => new(
        matrix.M11, matrix.M12, matrix.M13,
        matrix.M12, matrix.M22, matrix.M23,
        matrix.M13, matrix.M23, matrix.M33);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JSymmetricMatrix operator +(in JSymmetricMatrix a, in JSymmetricMatrix b) => new()
    {
        M11 = a.M11 + b.M11, M22 = a.M22 + b.M22, M33 = a.M33 + b.M33,
        M12 = a.M12 + b.M12, M13 = a.M13 + b.M13, M23 = a.M23 + b.M23
    };

    // Eliminate forbidden angular velocities from an inverse inertia tensor using
    // Schur complements. Merely zeroing rows/columns would give the wrong response
    // when the original inertia couples a permitted axis to a forbidden one.
    internal readonly JSymmetricMatrix Restrict(int axes)
    {
        if (axes == 7) return this;
        if (axes == 0) return Zero;
        JSymmetricMatrix result = this;
        if ((axes & 1) == 0)
        {
            if (result.M11 > 0)
            {
                Real xy = result.M12 / result.M11;
                Real xz = result.M13 / result.M11;
                result.M22 -= xy * result.M12;
                result.M23 -= xy * result.M13;
                result.M33 -= xz * result.M13;
            }
            result.M11 = result.M12 = result.M13 = 0;
        }
        if ((axes & 2) == 0)
        {
            if (result.M22 > 0)
            {
                Real xy = result.M12 / result.M22;
                Real yz = result.M23 / result.M22;
                result.M11 -= xy * result.M12;
                result.M13 -= xy * result.M23;
                result.M33 -= yz * result.M23;
            }
            result.M22 = result.M12 = result.M23 = 0;
        }
        if ((axes & 4) == 0)
        {
            if (result.M33 > 0)
            {
                Real xz = result.M13 / result.M33;
                Real yz = result.M23 / result.M33;
                result.M11 -= xz * result.M13;
                result.M12 -= xz * result.M23;
                result.M22 -= yz * result.M23;
            }
            result.M33 = result.M13 = result.M23 = 0;
        }
        // Subtraction can produce tiny negative diagonal entries at zero rank.
        result.M11 = MathR.Max(0, result.M11);
        result.M22 = MathR.Max(0, result.M22);
        result.M33 = MathR.Max(0, result.M33);
        return result;
    }

    public readonly bool Equals(JSymmetricMatrix other) =>
        M11 == other.M11 && M22 == other.M22 && M33 == other.M33 &&
        M12 == other.M12 && M13 == other.M13 && M23 == other.M23;

    public override readonly bool Equals(object? obj) => obj is JSymmetricMatrix other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(M11, M22, M33, M12, M13, M23);
    public static bool operator ==(JSymmetricMatrix a, JSymmetricMatrix b) => a.Equals(b);
    public static bool operator !=(JSymmetricMatrix a, JSymmetricMatrix b) => !a.Equals(b);
}
