/*
 * Copyright (c) 2009-2023 Thorben Linneweber and others
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
using Jitter2.UnmanagedMemory;

namespace Jitter2.Collision;

/// <summary>
/// A convex polytope builder used for collision detection in <see cref="NarrowPhase"/>.
/// <see cref="ConvexPolytope.InitHeap"/> has to be called at least once before usage
/// in order to allocate memory for vertices and triangles.
/// </summary>
public unsafe struct ConvexPolytope
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        public short A, B, C;
        public bool FacingOrigin;

        public short this[int i] => ((short*)Unsafe.AsPointer(ref this))[i];

        public JVector Normal;
        public JVector ClosestToOrigin;

        public float NormalSq;
        public float ClosestToOriginSq;
    }

    /// <summary>
    /// Represents a vertex used in algorithms operating on the Minkowski sum of two shapes.
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

    private struct Edge
    {
        public readonly short A;
        public readonly short B;

        public Edge(short a, short b)
        {
            A = a;
            B = b;
        }

        public static bool Equals(in Edge a, in Edge b)
        {
            return (a.A == b.A && a.B == b.B) || (a.A == b.B && a.B == b.A);
        }
    }

    private const float NumericEpsilon = 1e-16f;

    private const int MaxVertices = 100;
    private const int MaxTriangles = 3 * MaxVertices;

    public Triangle* Triangles;
    public Vertex* Vertices;

    private short tCount;
    private short vPointer;

    private bool originEnclosed;

    private JVector center;

    /// <summary>
    /// Returns whether or not the origin is enclosed. Important: This only returns
    /// correct results if called after <see cref="GetClosestTriangle"/> and is invalidated
    /// after a call to <see cref="AddVertex"/> or <see cref="AddPoint"/>.
    /// </summary>
    public readonly bool OriginEnclosed => originEnclosed;

    /// <summary>
    /// Calculates the barycentric coordinates of the origin projected onto the triangle
    /// which are used to return points in A- and B-space.
    /// </summary>
    public void CalculatePoints(in Triangle ctri, out JVector pA, out JVector pB)
    {
        CalcBarycentric(ctri, out JVector bc, !originEnclosed);
        pA = bc.X * Vertices[ctri.A].A + bc.Y * Vertices[ctri.B].A + bc.Z * Vertices[ctri.C].A;
        pB = bc.X * Vertices[ctri.A].B + bc.Y * Vertices[ctri.B].B + bc.Z * Vertices[ctri.C].B;
    }

    private bool CalcBarycentric(in Triangle tri, out JVector result, bool clamp = false)
    {
        bool clamped = false;

        JVector a = Vertices[tri.A].V;
        JVector b = Vertices[tri.B].V;
        JVector c = Vertices[tri.C].V;

        // Calculate the barycentric coordinates of the origin (0,0,0) projected
        // onto the plane of the triangle.
        //
        // [W. Heidrich, Journal of Graphics, GPU, and Game Tools,Volume 10, Issue 3, 2005.]
#pragma warning disable IDE0018
        JVector u, v, w, tmp;
#pragma warning restore IDE0018

        JVector.Subtract(a, b, out u);
        JVector.Subtract(a, c, out v);

        float t = tri.NormalSq;

        JVector.Cross(u, a, out tmp);
        float gamma = JVector.Dot(tmp, tri.Normal) / t;
        JVector.Cross(a, v, out tmp);
        float beta = JVector.Dot(tmp, tri.Normal) / t;
        float alpha = 1.0f - gamma - beta;

        if (clamp)
        {
            // Clamp the projected barycentric coordinates to lie within the triangle,
            // such that the clamped coordinates are closest (euclidean) to the original point.
            //
            // [https://math.stackexchange.com/questions/1092912/find-closest-point-in-triangle-given-barycentric-coordinates-outside]
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

                clamped = true;
            }
            else if (beta >= 0.0f && gamma < 0.0f)
            {
                JVector.Subtract(b, c, out w);
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

                clamped = true;
            }
            else if (gamma >= 0.0f && alpha < 0.0f)
            {
                JVector.Subtract(b, c, out w);
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

                clamped = true;
            }
        }

        result.X = alpha;
        result.Y = beta;
        result.Z = gamma;
        return clamped;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool IsLit(int candidate, int w)
    {
        // Checks if the triangle would be lit, if there would
        // be a light at the origin.
        ref Triangle tr = ref Triangles[candidate];
        JVector deltaA = Vertices[w].V - Vertices[tr.A].V;
        return JVector.Dot(deltaA, tr.Normal) > 0;
    }

    private bool CreateTriangle(short a, short b, short c)
    {
        ref Triangle triangle = ref Triangles[tCount];
        triangle.A = a;
        triangle.B = b;
        triangle.C = c;

        JVector.Subtract(Vertices[a].V, Vertices[b].V, out JVector u);
        JVector.Subtract(Vertices[a].V, Vertices[c].V, out JVector v);
        JVector.Cross(u, v, out triangle.Normal);
        triangle.NormalSq = triangle.Normal.LengthSquared();

        // return on degenerate triangles
        if (triangle.NormalSq < NumericEpsilon)
        {
            return false;
        }

        // do we need to flip the triangle? (the origin of the md has to be enclosed)
        float delta = JVector.Dot(triangle.Normal, Vertices[a].V - center);

        if (delta < 0)
        {
            (triangle.A, triangle.B) = (triangle.B, triangle.A);
            triangle.Normal.Negate();
        }

        delta = JVector.Dot(triangle.Normal, Vertices[a].V);
        triangle.FacingOrigin = delta >= 0.0f;

        if (!originEnclosed && CalcBarycentric(triangle, out JVector bc, true))
        {
            triangle.ClosestToOrigin = bc.X * Vertices[triangle.A].V + bc.Y * Vertices[triangle.B].V +
                                       bc.Z * Vertices[triangle.C].V;
            triangle.ClosestToOriginSq = triangle.ClosestToOrigin.LengthSquared();
        }
        else
        {
            JVector.Multiply(triangle.Normal, delta / triangle.NormalSq, out triangle.ClosestToOrigin);
            triangle.ClosestToOriginSq = triangle.ClosestToOrigin.LengthSquared();
        }

        tCount++;
        return true;
    }

    /// <summary>
    /// Loops through all triangles of the convex polytope and returns the one with
    /// the smallest distance to the origin (0, 0, 0).
    /// </summary>
    public ref Triangle GetClosestTriangle()
    {
        int closestIndex = -1;
        float currentMin = float.MaxValue;

        originEnclosed = true;

        for (int i = 0; i < tCount; i++)
        {
            if (Triangles[i].ClosestToOriginSq < currentMin)
            {
                currentMin = Triangles[i].ClosestToOriginSq;
                closestIndex = i;
            }

            if (!Triangles[i].FacingOrigin) originEnclosed = false;
        }

        return ref Triangles[closestIndex];
    }

    /// <summary>
    /// Constructs a tetrahedron using the first four vertices in <see cref="Vertices"/>.
    /// </summary>
    public void InitTetrahedron()
    {
        originEnclosed = false;
        vPointer = 3;
        tCount = 0;

        center = 0.25f * (Vertices[0].V + Vertices[1].V + Vertices[2].V + Vertices[3].V);

        CreateTriangle(0, 2, 1);
        CreateTriangle(0, 1, 3);
        CreateTriangle(0, 3, 2);
        CreateTriangle(1, 2, 3);
    }

    /// <summary>
    /// Constructs a small tetrahedron enclosing the given point.
    /// </summary>
    public void InitTetrahedron(in JVector point)
    {
        originEnclosed = false;
        vPointer = 3;
        tCount = 0;
        center = point;

        const float scale = 1e-2f; // minkowski sums not allowed to be thinner
        Vertices[0] = new Vertex(center + scale * new JVector(MathF.Sqrt(8.0f / 9.0f), 0.0f, -1.0f / 3.0f));
        Vertices[1] = new Vertex(center + scale * new JVector(-MathF.Sqrt(2.0f / 9.0f), MathF.Sqrt(2.0f / 3.0f), -1.0f / 3.0f));
        Vertices[2] = new Vertex(center + scale * new JVector(-MathF.Sqrt(2.0f / 9.0f), -MathF.Sqrt(2.0f / 3.0f), -1.0f / 3.0f));
        Vertices[3] = new Vertex(center + scale * new JVector(0.0f, 0.0f, 1.0f));

        CreateTriangle(2, 0, 1);
        CreateTriangle(1, 0, 3);
        CreateTriangle(3, 0, 2);
        CreateTriangle(2, 1, 3);
    }

    /// <summary>
    /// Must be called, before calling any other method. Initializes memory for
    /// <see cref="Vertices"/> and <see cref="Triangles"/>. Can be called multiple
    /// times, initialization is only done once.
    /// </summary>
    public void InitHeap()
    {
        if (Vertices == (void*)0)
        {
            Vertices = MemoryHelper.AllocateHeap<Vertex>(MaxVertices);
            Triangles = MemoryHelper.AllocateHeap<Triangle>(MaxTriangles);
        }
    }

    /// <summary>
    /// Adds a single point to the polyhedron. Ignores A- and B-space. Compare with
    /// <see cref="AddVertex"/>.
    /// </summary>
    /// <returns>True if the polyhedron could be extended by this point, false otherwise.</returns>
    public bool AddPoint(in JVector point)
    {
        Unsafe.SkipInit(out Vertex v);
        v.V = point;
        return AddVertex(v);
    }

    /// <summary>
    /// Adds a vertex to the polyhedron. Important: Invalidates the reference from previous calls
    /// to <see cref="GetClosestTriangle"/>, even if false is returned.
    /// </summary>
    /// <returns>True if the polyhedron could be extended by this point, false otherwise.</returns>
    public bool AddVertex(in Vertex vertex)
    {
        Edge* edges = stackalloc Edge[256];

        vPointer++;
        Vertices[vPointer] = vertex;

        int ePointer = 0;
        for (int index = tCount; index-- > 0;)
        {
            if (!IsLit(index, vPointer)) continue;
            Edge edge;
            bool added;

            for (int k = 0; k < 3; k++)
            {
                edge = new Edge(Triangles[index][(k + 0) % 3], Triangles[index][(k + 1) % 3]);
                added = true;
                for (int e = ePointer; e-- > 0;)
                {
                    if (Edge.Equals(edges[e], edge))
                    {
                        edges[e] = edges[--ePointer];
                        added = false;
                    }
                }

                if (added) edges[ePointer++] = edge;
            }

            Triangles[index] = Triangles[--tCount];
        }

        for (int i = 0; i < ePointer; i++)
        {
            if (!CreateTriangle(edges[i].A, edges[i].B, vPointer))
                return false;
        }

        return ePointer > 0;
    }
}