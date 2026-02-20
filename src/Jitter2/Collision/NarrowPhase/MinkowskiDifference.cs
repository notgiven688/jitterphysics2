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
/// <remarks>
/// The Minkowski difference A - B is defined as the set of all points (a - b) where a is in A and b is in B.
/// This is the fundamental construct used by GJK and EPA algorithms to detect collisions.
/// </remarks>
public static class MinkowskiDifference
{
    /// <summary>
    /// Represents a vertex on the Minkowski difference of two shapes.
    /// </summary>
    public struct Vertex
    {
        /// <summary>The point on the Minkowski difference: <c>V = A - B</c>.</summary>
        public JVector V;

        /// <summary>The support point on shape A in world space.</summary>
        public JVector A;

        /// <summary>The support point on shape B in world space.</summary>
        public JVector B;

        /// <summary>
        /// Creates a vertex with only the difference point set.
        /// </summary>
        /// <param name="v">The Minkowski difference point.</param>
        public Vertex(JVector v)
        {
            V = v;
        }
    }

    /// <summary>
    /// Computes the support function S_{A-B}(d) = S_A(d) - S_B(-d) for the Minkowski difference.
    /// </summary>
    /// <typeparam name="Ta">The type of support shape A.</typeparam>
    /// <typeparam name="Tb">The type of support shape B.</typeparam>
    /// <param name="supportA">The support function of shape A (at origin, not rotated).</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B.</param>
    /// <param name="positionB">The position of shape B.</param>
    /// <param name="direction">The search direction.</param>
    /// <param name="v">The resulting vertex containing support points from both shapes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Support<Ta,Tb>(in Ta supportA, in Tb supportB, in JQuaternion orientationB,
        in JVector positionB, in JVector direction, out Vertex v) where Ta : ISupportMappable where Tb : ISupportMappable
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
    /// Computes a point guaranteed to be inside the Minkowski difference.
    /// </summary>
    /// <typeparam name="Ta">The type of support shape A.</typeparam>
    /// <typeparam name="Tb">The type of support shape B.</typeparam>
    /// <param name="supportA">The support function of shape A (at origin, not rotated).</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B.</param>
    /// <param name="positionB">The position of shape B.</param>
    /// <param name="center">The resulting vertex representing the center of the Minkowski difference.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCenter<Ta,Tb>(in Ta supportA, in Tb supportB, in JQuaternion orientationB, in JVector positionB,
        out Vertex center) where Ta : ISupportMappable where Tb : ISupportMappable
    {
        supportA.GetCenter(out center.A);
        supportB.GetCenter(out center.B);
        JVector.Transform(center.B, orientationB, out center.B);
        JVector.Add(positionB, center.B, out center.B);
        JVector.Subtract(center.A, center.B, out center.V);
    }
}