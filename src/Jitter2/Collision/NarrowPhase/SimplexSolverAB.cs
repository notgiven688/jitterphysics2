/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.LinearMath;

using Vertex = Jitter2.Collision.MinkowskiDifference.Vertex;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Jitter2.Collision;

/// <summary>
/// Implements the Gilbert-Johnson-Keerthi (GJK) simplex solver with support for retrieving
/// the closest points on the original shapes (A and B spaces).
/// </summary>
/// <remarks>
/// Unlike <see cref="SimplexSolver"/>, this solver tracks barycentric coordinates and
/// the original support points, enabling extraction of the closest points on each shape
/// via <see cref="GetClosest"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct SimplexSolverAB
{
    const Real Epsilon = (Real)1e-8;

    private struct Barycentric
    {
        public Real Lambda0;
        public Real Lambda1;
        public Real Lambda2;
        public Real Lambda3;

        public Real this[int i]
        {
            get => ((Real*)Unsafe.AsPointer(ref this.Lambda0))[i];
            set => ((Real*)Unsafe.AsPointer(ref this.Lambda0))[i] = value;
        }
    }

    private Vertex v0;
    private Vertex v1;
    private Vertex v2;
    private Vertex v3;

    private Barycentric barycentric;
    private uint usageMask;

    /// <summary>
    /// Resets the solver to an empty simplex.
    /// </summary>
    public void Reset()
    {
        usageMask = 0;
    }

    private JVector ClosestSegment(int i0, int i1, ref Barycentric bc, out uint mask)
    {
        var ptr = (Vertex*)Unsafe.AsPointer(ref this.v0);
        JVector a = ptr[i0].V;
        JVector b = ptr[i1].V;

        JVector v = b - a;
        Real vsq = v.LengthSquared();

        bool degenerate = vsq < Epsilon;

        Real t = -JVector.Dot(a, v) / vsq;
        Real lambda0 = 1 - t;
        Real lambda1 = t;

        mask = (1u << i0 | 1u << i1);

        if (lambda0 < 0 || degenerate)
        {
            mask = 1u << i1;
            lambda0 = 0;
            lambda1 = 1;
        }
        else if (lambda1 < 0)
        {
            mask = 1u << i0;
            lambda0 = 1;
            lambda1 = 0;
        }

        bc[i0] = lambda0;
        bc[i1] = lambda1;

        return lambda0 * a + lambda1 * b;
    }

    private JVector ClosestTriangle(int i0, int i1, int i2, ref Barycentric bc, out uint mask)
    {
        mask = 0;

        var ptr = (Vertex*)Unsafe.AsPointer(ref this.v0);
        JVector a = ptr[i0].V;
        JVector b = ptr[i1].V;
        JVector c = ptr[i2].V;

        JVector.Subtract(a, b, out var u);
        JVector.Subtract(a, c, out var v);

        JVector normal = u % v;

        Real t = normal.LengthSquared();
        Real it = (Real)1.0 / t;

        bool degenerate = t < Epsilon;

        JVector.Cross(u, a, out var c1);
        JVector.Cross(a, v, out var c2);

        Real lambda2 = JVector.Dot(c1, normal) * it;
        Real lambda1 = JVector.Dot(c2, normal) * it;
        Real lambda0 = (Real)1.0 - lambda2 - lambda1;

        Real bestDistance = Real.MaxValue;
        Unsafe.SkipInit(out JVector closestPt);
        Unsafe.SkipInit(out Barycentric b0);

        if (lambda0 < (Real)0.0 || degenerate)
        {
            var closest = ClosestSegment(i1, i2, ref b0, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < (Real)0.0 || degenerate)
        {
            var closest = ClosestSegment(i0, i2, ref b0, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < (Real)0.0 || degenerate)
        {
            var closest = ClosestSegment(i0, i1, ref b0, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                closestPt = closest;
            }
        }

        if (mask != 0) return closestPt;

        bc[i0] = lambda0;
        bc[i1] = lambda1;
        bc[i2] = lambda2;

        mask = (1u << i0) | (1u << i1) | (1u << i2);
        return lambda0 * a + lambda1 * b + lambda2 * c;
    }

    private JVector ClosestTetrahedron(ref Barycentric bc, out uint mask)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Real Determinant(in JVector a, in JVector b, in JVector c, in JVector d)
        {
            return JVector.Dot(b - a, JVector.Cross(c - a, d - a));
        }

        Real detT = Determinant(v0.V, v1.V, v2.V, v3.V);
        Real inverseDetT = (Real)1.0 / detT;

        bool degenerate = detT * detT < Epsilon;

        Real lambda0 = Determinant(JVector.Zero, v1.V, v2.V, v3.V) * inverseDetT;
        Real lambda1 = Determinant(v0.V, JVector.Zero, v2.V, v3.V) * inverseDetT;
        Real lambda2 = Determinant(v0.V, v1.V, JVector.Zero, v3.V) * inverseDetT;
        Real lambda3 = (Real)1.0 - lambda0 - lambda1 - lambda2;

        Real bestDistance = Real.MaxValue;

        Unsafe.SkipInit(out JVector closestPt);
        Unsafe.SkipInit(out Barycentric b0);

        mask = 0;

        if (lambda0 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(1, 2, 3, ref b0, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(0, 2, 3, ref b0, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 3, ref b0, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda3 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 2, ref b0, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                // bestDistance = dist;
                closestPt = closest;
            }
        }

        if (mask != 0) return closestPt;

        barycentric[0] = lambda0;
        barycentric[1] = lambda1;
        barycentric[2] = lambda2;
        barycentric[3] = lambda3;

        mask = 0b1111;
        return JVector.Zero;
    }

    /// <summary>
    /// Computes the closest points on the original shapes A and B using the current simplex
    /// and barycentric coordinates.
    /// </summary>
    /// <param name="pointA">The closest point on shape A.</param>
    /// <param name="pointB">The closest point on shape B.</param>
    public void GetClosest(out JVector pointA, out JVector pointB)
    {
        pointA = JVector.Zero;
        pointB = JVector.Zero;

        var ptr = (Vertex*)Unsafe.AsPointer(ref this.v0);

        for (int i = 0; i < 4; i++)
        {
            if ((usageMask & (1u << i)) == 0) continue;
            pointA += barycentric[i] * ptr[i].A;
            pointB += barycentric[i] * ptr[i].B;
        }
    }

    /// <summary>
    /// Adds a vertex to the simplex and computes the new closest point to the origin.
    /// </summary>
    /// <param name="vertex">The vertex position on the Minkowski difference.</param>
    /// <param name="closest">The point on the reduced simplex closest to the origin.</param>
    /// <returns>
    /// <c>true</c> if the origin is not contained within the simplex;
    /// <c>false</c> if the origin is enclosed by the tetrahedron.
    /// </returns>
    public bool AddVertex(in JVector vertex, out JVector closest)
    {
        Unsafe.SkipInit(out Vertex fullVertex);
        fullVertex.V = vertex;
        return AddVertex(fullVertex, out closest);
    }

    /// <summary>
    /// Adds a vertex (with full A/B support point data) to the simplex and computes
    /// the new closest point to the origin.
    /// </summary>
    /// <param name="vertex">The Minkowski difference vertex including support points from both shapes.</param>
    /// <param name="closest">The point on the reduced simplex closest to the origin.</param>
    /// <returns>
    /// <c>true</c> if the origin is not contained within the simplex;
    /// <c>false</c> if the origin is enclosed by the tetrahedron.
    /// </returns>
    public bool AddVertex(in Vertex vertex, out JVector closest)
    {
        Unsafe.SkipInit(out closest);

        var ptr = (Vertex*)Unsafe.AsPointer(ref this.v0);
        int* ix = stackalloc int[4];

        int useCount = 0;
        int freeSlot = 0;

        for (int i = 0; i < 4; i++)
        {
            if ((usageMask & (1u << i)) != 0) ix[useCount++] = i;
            else freeSlot = i;
        }

        ix[useCount++] = freeSlot;
        ptr[freeSlot] = vertex;

        switch (useCount)
        {
            case 1:
            {
                int i0 = ix[0];
                closest = ptr[i0].V;
                usageMask = 1u << i0;
                barycentric[i0] = (Real)1.0;
                return true;
            }
            case 2:
            {
                int i0 = ix[0], i1 = ix[1];
                closest = ClosestSegment(i0, i1, ref barycentric, out usageMask);
                return true;
            }
            case 3:
            {
                int i0 = ix[0], i1 = ix[1], i2 = ix[2];
                closest = ClosestTriangle(i0, i1, i2, ref barycentric, out usageMask);
                return true;
            }
            case 4:
            {
                closest = ClosestTetrahedron(ref barycentric, out usageMask);
                return usageMask != 0b1111;
            }
        }

        Debug.Assert(false, "Unreachable.");
        return false;
    }
}