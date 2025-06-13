/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.LinearMath;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Jitter2.Collision;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SimplexSolver
{
    const Real Epsilon = (Real)1e-8;

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

        if (lambda0 < (Real)0.0 || degenerate)
        {
            var closest = ClosestSegment(i1, i2, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < (Real)0.0 || degenerate)
        {
            var closest = ClosestSegment(i0, i2, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < (Real)0.0 || degenerate)
        {
            var closest = ClosestSegment(i0, i1, out uint m);
            Real dist = closest.LengthSquared();
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
        static Real Determinant(in JVector a, in JVector b, in JVector c, in JVector d)
        {
            return JVector.Dot(b - a, JVector.Cross(c - a, d - a));
        }

        Real detT = Determinant(v0, v1, v2, v3);
        Real inverseDetT = (Real)1.0 / detT;

        bool degenerate = detT * detT < Epsilon;

        Real lambda0 = Determinant(JVector.Zero, v1, v2, v3) * inverseDetT;
        Real lambda1 = Determinant(v0, JVector.Zero, v2, v3) * inverseDetT;
        Real lambda2 = Determinant(v0, v1, JVector.Zero, v3) * inverseDetT;
        Real lambda3 = (Real)1.0 - lambda0 - lambda1 - lambda2;

        Real bestDistance = Real.MaxValue;

        Unsafe.SkipInit(out JVector closestPt);

        mask = 0;

        if (lambda0 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(1, 2, 3, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda1 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(0, 2, 3, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda2 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 3, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                bestDistance = dist;
                closestPt = closest;
            }
        }

        if (lambda3 < (Real)0.0 || degenerate)
        {
            var closest = ClosestTriangle(0, 1, 2, out uint m);
            Real dist = closest.LengthSquared();
            if (dist < bestDistance)
            {
                mask = m;
                // bestDistance = dist;
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