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
using System.Runtime.InteropServices;
using Jitter2.LinearMath;
using static Jitter2.Collision.ConvexPolytope;

namespace Jitter2.Collision;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SimplexSolver
{
    public Vertex Vertex0, Vertex1, Vertex2, Vertex3;
    public Vertex Closest;
    public int Count;

    private const float Epsilon = 1e-12f;

    private bool BarycentricTriangle(int i0, int i1, int i2, out JVector bc)
    {
        var vertices = (Vertex*)Unsafe.AsPointer<Vertex>(ref Vertex0);
        JVector a = vertices[i0].V;
        JVector b = vertices[i1].V;
        JVector c = vertices[i2].V;

        JVector u = a - b;
        JVector v = a - c;
        JVector n = u % v;

        Unsafe.SkipInit<JVector>(out bc);

        float nsq = n.LengthSquared();

        if(nsq < Epsilon * Epsilon) return false;

        float t = 1.0f / nsq;

        float gamma = JVector.Dot(u % a, n) * t;
        float beta = JVector.Dot(a % v, n) * t;
        float alpha = 1.0f - gamma - beta;

        if (alpha >= 0.0f && beta < 0.0f)
        {
            t = JVector.Dot(a, u);
            if (gamma < 0.0f && t > 0.0f)
            {
                beta = MathF.Min(1.0f, t / u.LengthSquared());
                alpha = 1.0f - beta;
                gamma = 0.0f;
            }
            else
            {
                gamma = MathF.Min(1.0f, MathF.Max(0.0f, JVector.Dot(a, v) / v.LengthSquared()));
                alpha = 1.0f - gamma;
                beta = 0.0f;
            }

        }
        else if (beta >= 0.0f && gamma < 0.0f)
        {
            JVector.Subtract(b, c, out JVector w);
            t = JVector.Dot(b, w);
            if (alpha < 0.0f && t > 0.0f)
            {
                gamma = MathF.Min(1.0f, t / w.LengthSquared());
                beta = 1.0f - gamma;
                alpha = 0.0f;
            }
            else
            {
                alpha = MathF.Min(1.0f, MathF.Max(0.0f, -JVector.Dot(b, u) / u.LengthSquared()));
                beta = 1.0f - alpha;
                gamma = 0.0f;
            }
        }
        else if (gamma >= 0.0f && alpha < 0.0f)
        {
            JVector.Subtract(b, c, out JVector w);
            t = -JVector.Dot(c, v);
            if (beta < 0.0f && t > 0.0f)
            {
                alpha = MathF.Min(1.0f, t / v.LengthSquared());
                gamma = 1.0f - alpha;
                beta = 0.0f;
            }
            else
            {
                beta = MathF.Min(1.0f, MathF.Max(0.0f, -JVector.Dot(c, w) / w.LengthSquared()));
                gamma = 1.0f - beta;
                alpha = 0.0f;
            }
        }

        bc = new(alpha, beta, gamma);
        return true;
    }

    private bool BarycentricSegment(out float t)
    {
        var vertices = (Vertex*)Unsafe.AsPointer<Vertex>(ref Vertex0);
        JVector a = vertices[0].V;
        JVector b = vertices[1].V;

        JVector v = b - a;

        t = 0.0f;

        float lsq = v.LengthSquared();
        if (lsq < Epsilon * Epsilon) return false;

        t = -JVector.Dot(a, v) / lsq;
        t = Math.Clamp(t, 0, 1);
        return true;
    }

    public bool IsLit(in JVector v, int i0, int i1, int i2, int io)
    {
        var vertices = (Vertex*)Unsafe.AsPointer<Vertex>(ref Vertex0);
        JVector normal = (vertices[i0].V - vertices[i1].V) % (vertices[i0].V - vertices[i2].V);
        float ddot = JVector.Dot(normal, vertices[i0].V - v) * JVector.Dot(normal, vertices[i0].V - vertices[io].V);
        return ddot < 0.0f;
    }

    public void Reset()
    {
        Count = 0;
    }

    public bool AddPoint(in JVector point)
    {
        Unsafe.SkipInit(out Vertex v);
        v.V = point;
        return AddVertex(v);
    }

    public bool AddVertex(in Vertex vertex)
    {
        var vertices = (Vertex*)Unsafe.AsPointer<Vertex>(ref Vertex0);

        switch (Count)
        {
            case 0:
                // ### void -> point
                vertices[Count++] = vertex;
                Closest = vertex;
                return true;
            case 1:
                // ### point -> segment
                vertices[Count++] = vertex;

                if(!BarycentricSegment(out float t)) return false;
                Closest = (1.0f - t) * vertices[0] + t * vertices[1];
                return true;
            case 2:
                // ### segment -> triangle
                vertices[Count++] = vertex;

                if(!BarycentricTriangle(0, 1, 2, out JVector bc)) return false;
                Closest = bc.X * vertices[0] + bc.Y * vertices[1] + bc.Z * vertices[2];
                return true;
            case 3:
                // ### triangle -> tetrahedron
                vertices[Count++] = vertex;
                break;
            case 4:
                // ### tetrahedron -> tetrahedron
                if (IsLit(vertex.V, 0, 2, 3, 1))
                {
                    // 0, 2, 3 can be seen from vertex and becomes new base
                    vertices[1] = vertices[3];
                    vertices[3] = vertex;
                    break;
                }
                else if (IsLit(vertex.V, 2, 1, 3, 0))
                {
                    // 2, 1, 3 can be seen from vertex and becomes new base
                    vertices[0] = vertices[3];
                    vertices[3] = vertex;
                    break;
                }
                else if (IsLit(vertex.V, 3, 1, 0, 2))
                {
                    // 3, 1, 0 can be seen from vertex and becomes new base
                    vertices[2] = vertices[3];
                    vertices[3] = vertex;
                    break;
                }

                return false;
        }

        // Calculate closest point on tetrahedron

        if(!BarycentricTriangle(0, 2, 3, out JVector bc1)) return false;
        if(!BarycentricTriangle(2, 1, 3, out JVector bc2)) return false;
        if(!BarycentricTriangle(3, 1, 0, out JVector bc3)) return false;

        Vertex v1 = bc1.X * vertices[0] + bc1.Y * vertices[2] + bc1.Z * vertices[3];
        Vertex v2 = bc2.X * vertices[2] + bc2.Y * vertices[1] + bc2.Z * vertices[3];
        Vertex v3 = bc3.X * vertices[3] + bc3.Y * vertices[1] + bc3.Z * vertices[0];

        float d1 = v1.V.LengthSquared();
        float d2 = v2.V.LengthSquared();
        float d3 = v3.V.LengthSquared();

        if (d1 <= d2 && d1 <= d3) Closest = v1;
        else if (d2 <= d1 && d2 <= d3) Closest = v2;
        else Closest = v3;

        return true;
    }
}