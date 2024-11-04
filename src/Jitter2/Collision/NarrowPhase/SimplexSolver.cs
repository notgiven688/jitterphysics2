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

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Jitter2.Collision;

// This is a GJK-Implementation "by the book".

public unsafe struct SimplexSolver
{
    const float Epsilon = 1e-8f;

    private JVector v0;
    private JVector v1;
    private JVector v2;
    private JVector v3;

    private uint usageMask;

    public void Reset()
    {
        usageMask = 0;
    }

    private JVector ClosestSegment(int i0, int i1, out uint mask)
    {
        var ptr = (JVector*)Unsafe.AsPointer(ref this.v0);
        JVector a = ptr[i0];
        JVector b = ptr[i1];

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

        return lambda0 * a + lambda1 * b;
    }

    private JVector ClosestTriangle(int i0, int i1, int i2, out uint mask)
    {
        mask = 0;

        var ptr = (JVector*)Unsafe.AsPointer(ref this.v0);
        JVector a = ptr[i0];
        JVector b = ptr[i1];
        JVector c = ptr[i2];

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

        if (lambda0 < 0.0f || degenerate)
        {
            var closest = ClosestSegment(i1, i2, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < 0.0f || degenerate)
        {
            var closest = ClosestSegment(i0, i2, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < 0.0f || degenerate)
        {
            var closest = ClosestSegment(i0, i1, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                closestPt = closest;
            }
        }

        if (mask != 0) return closestPt;

        mask = (1u << i0) | (1u << i1) | (1u << i2);
        return lambda0 * a + lambda1 * b + lambda2 * c;
    }

    private JVector ClosestTetrahedron(out uint mask)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float Determinant(in JVector a, in JVector b, in JVector c, in JVector d)
        {
            return JVector.Dot(b - a, JVector.Cross(c - a, d - a));
        }

        float detT = Determinant(v0, v1, v2, v3);
        float inverseDetT = 1.0f / detT;

        bool degenerate = detT * detT < Epsilon;

        float lambda0 = Determinant(JVector.Zero, v1, v2, v3) * inverseDetT;
        float lambda1 = Determinant(v0, JVector.Zero, v2, v3) * inverseDetT;
        float lambda2 = Determinant(v0, v1, JVector.Zero, v3) * inverseDetT;
        float lambda3 = 1.0f - lambda0 - lambda1 - lambda2;

        float bestDistance = float.MaxValue;

        Unsafe.SkipInit(out JVector closestPt);

        mask = 0;

        if (lambda0 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(1, 2, 3, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(0, 2, 3, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 3, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda3 < 0.0f || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 2, out uint m);
            float dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (mask != 0) return closestPt;

        mask = 0b1111;
        return JVector.Zero;
    }

    public bool AddVertex(in JVector vertex, out JVector closest)
    {
        Unsafe.SkipInit(out closest);

        var ptr = (JVector*)Unsafe.AsPointer(ref this.v0);
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
                closest = ptr[i0];
                usageMask = 1u << i0;
                return true;
            }
            case 2:
            {
                int i0 = ix[0], i1 = ix[1];
                closest = ClosestSegment(i0, i1, out usageMask);
                return true;
            }
            case 3:
            {
                int i0 = ix[0], i1 = ix[1], i2 = ix[2];
                closest = ClosestTriangle(i0, i1, i2, out usageMask);
                return true;
            }
            case 4:
            {
                closest = ClosestTetrahedron(out usageMask);
                return usageMask != 0b1111;
            }
        }

        Debug.Assert(false, "Unreachable.");
        return false;
    }
}