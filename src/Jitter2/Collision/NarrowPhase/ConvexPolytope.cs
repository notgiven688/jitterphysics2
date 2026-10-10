/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Collision.ExactArithmetic;
using Jitter2.LinearMath;
using Vertex = Jitter2.Collision.MinkowskiDifference.Vertex;

namespace Jitter2.Collision;

/// <summary>
/// EPA polytope with filtered exact visibility, orientation and
/// degeneracy predicates on the supplied floating-point coordinates.
/// </summary>
/// <remarks>
/// Distances, barycentric coordinates and contact outputs remain approximate and
/// are evaluated in double precision. Managed storage is allocated by
/// <see cref="InitHeap"/> and reused. Instances must not be shared between threads.
/// Each nonzero Minkowski vertex coordinate must have magnitude between 2^-200
/// and 2^200, inclusive. Seeds and expansions outside this range are rejected.
/// </remarks>
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

        public Real NormalSq;
        public Real ClosestToOriginSq;
    }

    public enum AddVertexResult
    {
        Added,
        NoHorizon,
        Degenerate,
        CapacityExceeded,
        NumericalFailure
    }

    private const int MaxVertices = 128;
    private const int MaxTriangles = 2 * MaxVertices;

    private readonly struct Edge(short a, short b)
    {
        public readonly short A = a, B = b;
        public bool Matches(in Edge other) =>
            (A == other.A && B == other.B) || (A == other.B && B == other.A);
    }

    private readonly struct Double3(double x, double y, double z)
    {
        public readonly double X = x, Y = y, Z = z;
        public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y) && double.IsFinite(Z);
        public JVector ToVector() => new((Real)X, (Real)Y, (Real)Z);
        public static Double3 FromVector(in JVector v) => new(v.X, v.Y, v.Z);
        public static double Dot(in Double3 a, in Double3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Double3 Cross(in Double3 a, in Double3 b) =>
            new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static Double3 operator +(Double3 a, Double3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Double3 operator -(Double3 a, Double3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Double3 operator *(Double3 a, double b) => new(a.X * b, a.Y * b, a.Z * b);
    }

    private struct FaceData
    {
        public PlaneFilter Filter;
        public Double3 Barycentric;
        public double DistanceSquared;
    }

    private Vertex[] vertices;
    private Triangle[] triangles;
    private FaceData[] faceData;
    private short vertexCount, triangleCount;
    private bool originEnclosed;

    public readonly Span<Triangle> HullTriangles => triangles.AsSpan(0, triangleCount);

    /// <summary>
    /// Indicates whether the origin is enclosed. Updated by <see cref="GetClosestTriangle"/>.
    /// </summary>
    public readonly bool OriginEnclosed => originEnclosed;

    public ref Vertex GetVertex(int index)
    {
        Debug.Assert(index < MaxVertices, "Out of bounds.");
        return ref vertices[index];
    }

    /// <summary>Allocates reusable managed buffers once. Call before setting vertices or initializing the hull.</summary>
    public void InitHeap()
    {
        if (vertices != null) return;
        vertices = new Vertex[MaxVertices];
        triangles = new Triangle[MaxTriangles];
        faceData = new FaceData[MaxTriangles];
    }

    /// <summary>Initializes from the first four vertices. False indicates an invalid or unsupported seed.</summary>
    public bool InitTetrahedron()
    {
        vertexCount = 4;
        triangleCount = 0;
        originEnclosed = false;
        for (int i = 0; i < 4; i++)
        {
            if (!PlaneFilter.IsSafe(vertices[i].V)) return false;
        }

        ReadOnlySpan<short> indices = [0, 2, 1, 0, 1, 3, 0, 3, 2, 1, 2, 3];
        for (int i = 0; i < 4; i++)
        {
            if (CreateTriangle(indices[3 * i], indices[3 * i + 1], indices[3 * i + 2],
                    out triangles[i], out faceData[i]) != AddVertexResult.Added) return false;
        }

        triangleCount = 4;
        return true;
    }

    /// <summary>Initializes a small tetrahedron centered at the specified point.</summary>
    /// <returns>False if the seed is degenerate, unsupported, or cannot be evaluated.</returns>
    public bool InitTetrahedron(in JVector point)
    {
        const Real scale = (Real)1e-2;
        vertices[0] = new Vertex(point + scale * new JVector(MathR.Sqrt((Real)(8.0 / 9.0)), 0, -(Real)(1.0 / 3.0)));
        vertices[1] = new Vertex(point + scale * new JVector(-MathR.Sqrt((Real)(2.0 / 9.0)), MathR.Sqrt((Real)(2.0 / 3.0)), -(Real)(1.0 / 3.0)));
        vertices[2] = new Vertex(point + scale * new JVector(-MathR.Sqrt((Real)(2.0 / 9.0)), -MathR.Sqrt((Real)(2.0 / 3.0)), -(Real)(1.0 / 3.0)));
        vertices[3] = new Vertex(point + scale * new JVector(0, 0, 1));
        return InitTetrahedron();
    }

    /// <summary>Finds the closest triangle using double-precision distances and updates origin enclosure.</summary>
    public ref Triangle GetClosestTriangle()
    {
        if (triangleCount == 0) throw new InvalidOperationException("The polytope is not initialized.");
        int closest = 0;
        double minimum = double.PositiveInfinity;
        bool skipTest = originEnclosed;
        originEnclosed = true;
        for (int i = 0; i < triangleCount; i++)
        {
            if (faceData[i].DistanceSquared < minimum)
            {
                minimum = faceData[i].DistanceSquared;
                closest = i;
            }

            if (!triangles[i].FacingOrigin) originEnclosed = skipTest;
        }

        return ref triangles[closest];
    }

    /// <summary>Calculates shape contact points from a triangle still belonging to the current hull.</summary>
    public void CalculatePoints(in Triangle triangle, out JVector pointA, out JVector pointB)
    {
        for (int i = 0; i < triangleCount; i++)
        {
            if (triangles[i].A != triangle.A || triangles[i].B != triangle.B || triangles[i].C != triangle.C) continue;
            Double3 bc = faceData[i].Barycentric;
            pointA = (Double3.FromVector(vertices[triangle.A].A) * bc.X +
                      Double3.FromVector(vertices[triangle.B].A) * bc.Y +
                      Double3.FromVector(vertices[triangle.C].A) * bc.Z).ToVector();
            pointB = (Double3.FromVector(vertices[triangle.A].B) * bc.X +
                      Double3.FromVector(vertices[triangle.B].B) * bc.Y +
                      Double3.FromVector(vertices[triangle.C].B) * bc.Z).ToVector();
            return;
        }

        throw new ArgumentException("Triangle no longer belongs to this hull.", nameof(triangle));
    }

    /// <summary>Adds a vertex. Use <see cref="AddVertexDetailed"/> to distinguish a clean stop from a failure.</summary>
    /// <remarks>
    /// Successful expansion invalidates previous triangle references. NoHorizon preserves the hull;
    /// any other failure requires reinitialization before the hull can be used again.
    /// </remarks>
    public bool AddVertex(in Vertex vertex) => AddVertexDetailed(vertex) == AddVertexResult.Added;

    /// <summary>
    /// Expands the hull in place. NoHorizon is a clean lack of expansion and leaves
    /// the active hull unchanged. Any other failure requires reinitialization.
    /// </summary>
    [SkipLocalsInit]
    public AddVertexResult AddVertexDetailed(in Vertex vertex)
    {
        if (triangleCount == 0 || !PlaneFilter.IsSafe(vertex.V)) return AddVertexResult.NumericalFailure;
        if (vertexCount == MaxVertices) return AddVertexResult.CapacityExceeded;
        vertices[vertexCount] = vertex;

        Span<bool> lit = stackalloc bool[MaxTriangles];
        Span<Edge> edges = stackalloc Edge[3 * MaxTriangles];
        int edgeCount = 0, litCount = 0;
        for (int index = triangleCount; index-- > 0;)
        {
            ref readonly Triangle face = ref triangles[index];
            lit[index] = Side(faceData[index], face.A, face.B, face.C, vertexCount) > 0;
            if (!lit[index]) continue;
            litCount++;
            Triangle tr = triangles[index];
            ReadOnlySpan<short> corners = [tr.A, tr.B, tr.C];
            for (int k = 0; k < 3; k++)
            {
                Edge edge = new(corners[k], corners[(k + 1) % 3]);
                bool added = true;
                for (int e = edgeCount; e-- > 0;)
                {
                    if (!edges[e].Matches(edge)) continue;
                    edges[e] = edges[--edgeCount];
                    added = false;
                    break;
                }

                if (added) edges[edgeCount++] = edge;
            }
        }

        if (edgeCount == 0) return AddVertexResult.NoHorizon;
        if (triangleCount - litCount + edgeCount > MaxTriangles) return AddVertexResult.CapacityExceeded;

        int survivorCount = 0;
        for (int i = 0; i < triangleCount; i++)
        {
            if (lit[i]) continue;
            if (survivorCount != i)
            {
                triangles[survivorCount] = triangles[i];
                faceData[survivorCount] = faceData[i];
            }
            survivorCount++;
        }

        // Build the replacement fan directly in its final slots. A failed face
        // invalidates the hull; EPA callers abandon the solve and reinitialize it.
        for (int i = 0; i < edgeCount; i++)
        {
            int index = survivorCount + i;
            AddVertexResult result = CreateTriangle(edges[i].A, edges[i].B, vertexCount,
                out triangles[index], out faceData[index]);
            if (result != AddVertexResult.Added)
            {
                triangleCount = 0;
                originEnclosed = false;
                return result;
            }
        }

        triangleCount = (short)(survivorCount + edgeCount);
        vertexCount++;
        return AddVertexResult.Added;
    }

    private AddVertexResult CreateTriangle(short a, short b, short c, out Triangle triangle, out FaceData data)
    {
        triangle = default;
        data = default;
        data.Filter = new PlaneFilter(vertices[a].V, vertices[b].V, vertices[c].V);
        // The four seed vertices remain inside the growing hull. A nonzero
        // side for any of them proves both nonzero area and face orientation;
        // this avoids constructing an exact centroid for every query.
        int orientation = 0;
        for (short i = 0; i < 4 && orientation == 0; i++)
        {
            if (i == a || i == b || i == c) continue;
            orientation = -Side(data, a, b, c, i);
        }
        if (orientation == 0) return AddVertexResult.Degenerate;
        if (orientation < 0)
        {
            (a, b) = (b, a);
            data.Filter.Negate();
        }

        bool facingOrigin = Side(data, a, b, c, -1) <= 0;
        Double3 rawNormal = new(data.Filter.NormalX, data.Filter.NormalY, data.Filter.NormalZ);
        if (!data.Filter.ReliableNormal)
        {
            ExpansionArithmetic.Normal(vertices[a].V, vertices[b].V, vertices[c].V, out double x, out double y, out double z);
            rawNormal = new(x, y, z);
        }
        double normalScale = Math.Max(Math.Abs(rawNormal.X), Math.Max(Math.Abs(rawNormal.Y), Math.Abs(rawNormal.Z)));
        if (!(normalScale > 0) || !double.IsFinite(normalScale)) return AddVertexResult.NumericalFailure;
        Double3 scaledNormal = new(rawNormal.X / normalScale, rawNormal.Y / normalScale, rawNormal.Z / normalScale);
        double normalSquared = Double3.Dot(scaledNormal, scaledNormal);

        Double3 va = Double3.FromVector(vertices[a].V), vb = Double3.FromVector(vertices[b].V), vc = Double3.FromVector(vertices[c].V);
        bool clamped = CalcBarycentric(va, vb, vc, scaledNormal, normalScale, normalSquared, out data.Barycentric);
        Double3 closest = clamped
            ? va * data.Barycentric.X + vb * data.Barycentric.Y + vc * data.Barycentric.Z
            : scaledNormal * (Double3.Dot(scaledNormal, va) / normalSquared);
        data.DistanceSquared = Double3.Dot(closest, closest);
        if (!closest.IsFinite || !data.Barycentric.IsFinite || !double.IsFinite(data.DistanceSquared)) return AddVertexResult.NumericalFailure;

        triangle.A = a;
        triangle.B = b;
        triangle.C = c;
        triangle.FacingOrigin = facingOrigin;
        triangle.Normal = scaledNormal.ToVector();
        triangle.NormalSq = triangle.Normal.LengthSquared();
        triangle.ClosestToOrigin = closest.ToVector();
        triangle.ClosestToOriginSq = (Real)data.DistanceSquared;
        return IsFinite(triangle.ClosestToOrigin) && Real.IsFinite(triangle.ClosestToOriginSq)
            ? AddVertexResult.Added : AddVertexResult.NumericalFailure;
    }

    private int Side(in FaceData data, short a, short b, short c, short pointIndex)
    {
        JVector point = pointIndex < 0 ? JVector.Zero : vertices[pointIndex].V;
        if (point == vertices[a].V || point == vertices[b].V || point == vertices[c].V) return 0;
        if (data.Filter.TrySide(vertices[a].V, point, out int sign)) return sign;
        return ExpansionArithmetic.Side(vertices[a].V, vertices[b].V, vertices[c].V, point);
    }

    private static bool CalcBarycentric(in Double3 a, in Double3 b, in Double3 c,
        in Double3 normal, double normalScale, double normalSquared, out Double3 result)
    {
        Double3 u = a - b, v = a - c;
        double gamma = (Double3.Dot(Double3.Cross(u, a), normal) / normalSquared) / normalScale;
        double beta = (Double3.Dot(Double3.Cross(a, v), normal) / normalSquared) / normalScale;
        double alpha = 1 - gamma - beta;
        bool clamped = false;
        if (alpha >= 0 && beta < 0)
        {
            double t = Double3.Dot(a, u);
            if (gamma < 0 && t > 0)
            {
                beta = Math.Min(1, t / Double3.Dot(u, u));
                alpha = 1 - beta;
                gamma = 0;
            }
            else
            {
                gamma = Math.Clamp(Double3.Dot(a, v) / Double3.Dot(v, v), 0, 1);
                alpha = 1 - gamma;
                beta = 0;
            }

            clamped = true;
        }
        else if (beta >= 0 && gamma < 0)
        {
            Double3 w = b - c;
            double t = Double3.Dot(b, w);
            if (alpha < 0 && t > 0)
            {
                gamma = Math.Min(1, t / Double3.Dot(w, w));
                beta = 1 - gamma;
                alpha = 0;
            }
            else
            {
                alpha = Math.Clamp(-Double3.Dot(b, u) / Double3.Dot(u, u), 0, 1);
                beta = 1 - alpha;
                gamma = 0;
            }

            clamped = true;
        }
        else if (gamma >= 0 && alpha < 0)
        {
            Double3 w = b - c;
            double t = -Double3.Dot(c, v);
            if (beta < 0 && t > 0)
            {
                alpha = Math.Min(1, t / Double3.Dot(v, v));
                gamma = 1 - alpha;
                beta = 0;
            }
            else
            {
                beta = Math.Clamp(-Double3.Dot(c, w) / Double3.Dot(w, w), 0, 1);
                gamma = 1 - beta;
                alpha = 0;
            }

            clamped = true;
        }

        result = new Double3(alpha, beta, gamma);
        return clamped;
    }

    private static bool IsFinite(in JVector v) => Real.IsFinite(v.X) && Real.IsFinite(v.Y) && Real.IsFinite(v.Z);
}
