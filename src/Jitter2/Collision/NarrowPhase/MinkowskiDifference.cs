/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.CompilerServices;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Provides methods for computing points on the Minkowski difference of two convex shapes.
/// </summary>
public static class MinkowskiDifference
{
    /// <summary>
    /// Represents a vertex utilized in algorithms that operate on the Minkowski sum of two shapes.
    /// </summary>
    public struct Vertex
    {
        public JVector V;
        public JVector A;
        public JVector B;

        public Vertex(JVector v)
        {
            V = v;
        }
    }

    /// <summary>
    /// Calculates the support function S_{A-B}(d) = S_{A}(d) - S_{B}(-d), where "d" represents the direction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Support<TA,TB>(in TA supportA, in TB supportB, in JQuaternion orientationB,
        in JVector positionB, in JVector direction, out Vertex v) where TA : ISupportMappable where TB : ISupportMappable
    {
        JVector.Negate(direction, out JVector tmp);
        supportA.SupportMap(direction, out v.A);

        JVector.ConjugatedTransform(tmp, orientationB, out JVector tmp2);
        supportB.SupportMap(tmp2, out v.B);
        JVector.Transform(v.B, orientationB, out v.B);
        JVector.Add(v.B, positionB, out v.B);

        JVector.Subtract(v.A, v.B, out v.V);
    }

    /// <summary>
    /// Retrieves a point within the Minkowski Difference.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCenter<TA,TB>(in TA supportA, in TB supportB, in JQuaternion orientationB, in JVector positionB,
        out Vertex center) where TA : ISupportMappable where TB : ISupportMappable
    {
        supportA.GetCenter(out center.A);
        supportB.GetCenter(out center.B);
        JVector.Transform(center.B, orientationB, out center.B);
        JVector.Add(positionB, center.B, out center.B);
        JVector.Subtract(center.A, center.B, out center.V);
    }
}