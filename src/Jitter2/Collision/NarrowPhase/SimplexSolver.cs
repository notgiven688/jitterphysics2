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

using System;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

// This is a GJK-Implementation "by the book".

public unsafe struct SimplexSolver
{
    private JVector v0;
    private JVector v1;
    private JVector v2;
    private JVector v3;

    private uint usedMask;

    public void Reset()
    {
        usedMask = 0;
    }

    public JVector ClosestSegment(int i0, int i1, out uint mask)
    {
        var ptr = (JVector*)Unsafe.AsPointer(ref this.v0);
        JVector a = ptr[i0];
        JVector b = ptr[i1];

        // Compute vector v = b - a (the direction vector of the segment)

        JVector v = b - a;

        // Compute vector w = p - a (the vector from a to the point p)
        JVector w = JVector.Zero - a;

        // Project w onto v using dot products
        float t = JVector.Dot(w, v) / JVector.Dot(v, v);

        // Compute barycentric coordinates
        float lambda0 = 1 - t;
        float lambda1 = t;

        mask = (1u << i0 | 1u << i1); // binary mask for two coordinates
        if (lambda0 < 0) mask = 1u << i1; // 0b0010;
        else if (lambda1 < 0) mask = 1u << i0; //0b0001;

        return a + Math.Clamp(t, 0.0f, 1.0f) * v;
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

        float t = 1.0f / normal.LengthSquared();

        JVector.Cross(u, a, out var c1);
        JVector.Cross(a, v, out var c2);

        float gamma = JVector.Dot(c1, normal) * t;
        float beta = JVector.Dot(c2, normal) * t;
        float alpha = 1.0f - gamma - beta;

        float bestDistance = float.MaxValue;
        Unsafe.SkipInit(out JVector closestPt);

        if (alpha < 0.0f)
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

        if (beta < 0.0f)
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

        if (gamma < 0.0f)
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
        return alpha * a + beta * b + gamma * c;
    }

    public JVector ClosestTetrahedron(out uint mask)
    {
        float detT = 1.0f / Determinant(v0, v1, v2, v3);
        float lambda0 = Determinant(JVector.Zero, v1, v2, v3) * detT;
        float lambda1 = Determinant(v0, JVector.Zero, v2, v3) * detT;
        float lambda2 = Determinant(v0, v1, JVector.Zero, v3) * detT;
        float lambda3 = 1.0f - lambda0 - lambda1 - lambda2;

        float bestDistance = float.MaxValue;
        Unsafe.SkipInit(out JVector closestPt);

        mask = 0;

        if (lambda0 < 0.0f)
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

        if (lambda1 < 0.0f)
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

        if (lambda2 < 0.0f)
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

        if (lambda3 < 0.0f)
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

        return closestPt;
    }


    // Helper method to calculate determinant (scalar triple product)
    private static float Determinant(JVector v0, JVector v1, JVector v2, JVector v3)
    {
        JVector v10 = v1 - v0;
        JVector v20 = v2 - v0;
        JVector v30 = v3 - v0;

        return JVector.Dot(v10, JVector.Cross(v20, v30));
    }


    public bool AddVertex(in JVector vertex, out JVector closest)
    {
        const float epsilon = 1e-8f;

        Unsafe.SkipInit(out closest);

        int* ix = stackalloc int[4];

        int useCount = 0;
        int freeSlot = 0;

        if ((usedMask & 0b0001) != 0) ix[useCount++] = 0;
        else freeSlot = 0;
        if ((usedMask & 0b0010) != 0) ix[useCount++] = 1;
        else freeSlot = 1;
        if ((usedMask & 0b0100) != 0) ix[useCount++] = 2;
        else freeSlot = 2;
        if ((usedMask & 0b1000) != 0) ix[useCount++] = 3;
        else freeSlot = 3;

        ix[useCount++] = freeSlot;

        // If the vertex is not able to "extend" the simplex (in any dimension)
        // we return false and do nothing.

        var ptr = (JVector*)Unsafe.AsPointer(ref this.v0);

        ptr[freeSlot] = vertex;

        switch (useCount)
        {
            case 1:
            {
                int i0 = ix[0];
                closest = ptr[i0];
                usedMask = 1u << i0;
                return true;
            }
            case 2:
            {
                int i0 = ix[0], i1 = ix[1];
                if ((ptr[i0] - ptr[i1]).LengthSquared() < epsilon * epsilon)
                {
                    return false;
                }

                closest = ClosestSegment(i0, i1, out usedMask);

                return true;
            }
            case 3:
            {
                int i0 = ix[0], i1 = ix[1], i2 = ix[2];
                JVector u = ptr[i0] - ptr[i2];
                JVector v = ptr[i1] - ptr[i2];

                if ((u % v).LengthSquared() < epsilon * epsilon)
                {
                    return false;
                }

                closest = ClosestTriangle(i0, i1, i2, out usedMask);

                return true;
            }
            case 4:
            {
                int i0 = ix[0], i1 = ix[1], i2 = ix[2], i3 = ix[3];

                JVector u = ptr[i0] - ptr[i2];
                JVector v = ptr[i1] - ptr[i2];

                JVector normal = u % v;

                if (MathF.Abs(JVector.Dot(normal, ptr[i3] - ptr[i0])) < epsilon)
                {
                    return false;
                }

                closest = ClosestTetrahedron(out usedMask);
                if (usedMask == 0) return false;

                return true;
            }

            default:
                throw new Exception();
        }
    }
}