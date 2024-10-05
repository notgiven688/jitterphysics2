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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;

using Vertex = Jitter2.Collision.MinkowskiDifference.Vertex;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Jitter2.Collision;

// This is a GJK-Implementation "by the book".

public unsafe struct SimplexSolverAB
{
    const float Epsilon = 1e-8f;

    private struct Barycentric
    {
        public float Lambda0;
        public float Lambda1;
        public float Lambda2;
        public float Lambda3;

        public float this[int i]
        {
            get => ((float*)Unsafe.AsPointer(ref this.Lambda0))[i];
            set => ((float*)Unsafe.AsPointer(ref this.Lambda0))[i] = value;
        }
    }

    private Vertex v0;
    private Vertex v1;
    private Vertex v2;
    private Vertex v3;

    private Barycentric barycentric;
    private uint usageMask;

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
        float vsq = v.LengthSquared();

        bool degenerate = vsq < Epsilon;

        float t = -JVector.Dot(a, v) / vsq;
        float lambda0 = 1 - t;
        float lambda1 = t;

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

        float t = normal.LengthSquared();
        float it = 1.0f / t;

        bool degenerate = t < Epsilon;

        JVector.Cross(u, a, out var c1);
        JVector.Cross(a, v, out var c2);

        float lambda2 = JVector.Dot(c1, normal) * it;
        float lambda1 = JVector.Dot(c2, normal) * it;
        float lambda0 = 1.0f - lambda2 - lambda1;

        float bestDistance = float.MaxValue;
        Unsafe.SkipInit(out JVector closestPt);
        Unsafe.SkipInit(out Barycentric b0);

        if (lambda0 < 0.0f || degenerate)
        {
            var closest = ClosestSegment(i1, i2, ref b0, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < 0.0f || degenerate)
        {
            var closest = ClosestSegment(i0, i2, ref b0, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < 0.0f || degenerate)
        {
            var closest = ClosestSegment(i0, i1, ref b0, out uint m);
            float dist = closest.LengthSquared();
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
        float Determinant(in JVector a, in JVector b, in JVector c, in JVector d)
        {
            return JVector.Dot(b - a, JVector.Cross(c - a, d - a));
        }

        float detT = Determinant(v0.V, v1.V, v2.V, v3.V);
        float idetT = 1.0f / detT;

        bool degenerate = detT * detT < Epsilon;

        float lambda0 = Determinant(JVector.Zero, v1.V, v2.V, v3.V) * idetT;
        float lambda1 = Determinant(v0.V, JVector.Zero, v2.V, v3.V) * idetT;
        float lambda2 = Determinant(v0.V, v1.V, JVector.Zero, v3.V) * idetT;
        float lambda3 = 1.0f - lambda0 - lambda1 - lambda2;

        float bestDistance = float.MaxValue;

        Unsafe.SkipInit(out JVector closestPt);
        Unsafe.SkipInit(out Barycentric b0);

        mask = 0;

        if (lambda0 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(1, 2, 3, ref b0, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(0, 2, 3, ref b0, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 3, ref b0, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda3 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 2, ref b0, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                bc = b0;
                mask = m;
                bestDistance = dist;
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

    public bool AddVertex(in JVector vertex, out JVector closest)
    {
        Unsafe.SkipInit(out Vertex fullVertex);
        fullVertex.V = vertex;
        return AddVertex(fullVertex, out closest);
    }

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
                barycentric[i0] = 1.0f;
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