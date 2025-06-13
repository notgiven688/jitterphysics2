/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Encapsulates the data of a triangle mesh. An instance of this can be supplied to the <see cref="Jitter2.Collision.Shapes.TriangleShape"/>.
/// The triangles within this class contain indices pointing to neighboring triangles.
/// </summary>
public class TriangleMesh
{
    /// <summary>
    /// Represents an exception thrown when a degenerate triangle is detected.
    /// </summary>
    public sealed class DegenerateTriangleException(JTriangle triangle) :
        Exception($"Degenerate triangle found: {triangle}.");

    private readonly struct Edge(int indexA, int indexB) : IEquatable<Edge>
    {
        public readonly int IndexA = indexA;
        public readonly int IndexB = indexB;

        public bool Equals(Edge other) => IndexA == other.IndexA && IndexB == other.IndexB;

        public override bool Equals(object? obj) => obj is Edge other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(IndexA, IndexB);
    }

    /// <summary>
    /// This structure encapsulates vertex indices along with indices pointing to
    /// neighboring triangles.
    /// </summary>
    public struct Triangle(int a, int b, int c)
    {
        public readonly int IndexA = a, IndexB = b, IndexC = c;
        public int NeighborA = -1, NeighborB = -1, NeighborC = -1;
        public JVector Normal = default;

        public readonly bool Equals(Triangle other)
        {
            return IndexA == other.IndexA &&
                   IndexB == other.IndexB &&
                   IndexC == other.IndexC;
        }

        public readonly override bool Equals(object? obj) => obj is Triangle t && Equals(t);

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(IndexA, IndexB, IndexC);
        }

        public static bool operator ==(Triangle left, Triangle right) => left.Equals(right);
        public static bool operator !=(Triangle left, Triangle right) => !left.Equals(right);
    }

    public readonly JVector[] Vertices;
    public readonly Triangle[] Indices;

    public TriangleMesh(IReadOnlyList<JTriangle> triangles, bool ignoreDegenerated = false)
    {
        var vertexMap = new Dictionary<JVector, int>();
        var vertexList = new List<JVector>();
        var triangleList = new List<Triangle>();

        int GetOrAddVertex(JVector v)
        {
            if (!vertexMap.TryGetValue(v, out int index))
            {
                index = vertexList.Count;
                vertexMap[v] = index;
                vertexList.Add(v);
            }
            return index;
        }

        foreach (var tri in triangles)
        {
            int a = GetOrAddVertex(tri.V0);
            int b = GetOrAddVertex(tri.V1);
            int c = GetOrAddVertex(tri.V2);

            var normal = (tri.V1 - tri.V0) % (tri.V2 - tri.V0);
            if (MathHelper.CloseToZero(normal, (Real)1e-12))
            {
                if (ignoreDegenerated)
                {
                    Logger.Warning("{0}, Degenerate triangle found in mesh. Ignoring.", nameof(TriangleMesh));
                    continue;
                }
                throw new DegenerateTriangleException(tri);
            }

            var triangle = new Triangle(a, b, c);
            JVector.Normalize(normal, out triangle.Normal);
            triangleList.Add(triangle);
        }

        Vertices = vertexList.ToArray();
        Indices = triangleList.ToArray();

        AssignNeighbors();
    }

    private void AssignNeighbors()
    {
        var edgeToTriangle = new Dictionary<Edge, int>();

        for (int i = 0; i < Indices.Length; i++)
        {
            var tri = Indices[i];
            edgeToTriangle.TryAdd(new Edge(tri.IndexA, tri.IndexB), i);
            edgeToTriangle.TryAdd(new Edge(tri.IndexB, tri.IndexC), i);
            edgeToTriangle.TryAdd(new Edge(tri.IndexC, tri.IndexA), i);
        }

        for (int i = 0; i < Indices.Length; i++)
        {
            ref var tri = ref Indices[i];
            tri.NeighborA = edgeToTriangle.GetValueOrDefault(new Edge(tri.IndexC, tri.IndexB), -1);
            tri.NeighborB = edgeToTriangle.GetValueOrDefault(new Edge(tri.IndexA, tri.IndexC), -1);
            tri.NeighborC = edgeToTriangle.GetValueOrDefault(new Edge(tri.IndexB, tri.IndexA), -1);
        }
    }
}