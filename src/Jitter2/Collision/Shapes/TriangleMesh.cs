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
using System.Collections.Generic;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents an exception thrown when a degenerate triangle is detected.
/// </summary>
public sealed class DegenerateTriangleException : Exception
{
    public DegenerateTriangleException() { }
    public DegenerateTriangleException(string message) : base(message) { }
    public DegenerateTriangleException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Encapsulates the data of a triangle mesh. An instance of this can be supplied to the <see cref="Jitter2.Collision.Shapes.TriangleShape"/>.
/// The triangles within this class contain indices pointing to neighboring triangles.
/// </summary>
public class TriangleMesh
{
    private readonly struct Edge : IEquatable<Edge>
    {
        public int IndexA { get; }
        public int IndexB { get; }

        public Edge(int indexA, int indexB)
        {
            IndexA = indexA;
            IndexB = indexB;
        }

        public bool Equals(Edge other) => IndexA == other.IndexA && IndexB == other.IndexB;

        public override bool Equals(object? obj) => obj is Edge other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(IndexA, IndexB);
    }

    /// <summary>
    /// This structure encapsulates vertex indices along with indices pointing to
    /// neighboring triangles.
    /// </summary>
    public struct Triangle
    {
        public int IndexA, IndexB, IndexC;
        public int NeighborA, NeighborB, NeighborC;
        public JVector Normal;

        public Triangle(int a, int b, int c)
        {
            IndexA = a;
            IndexB = b;
            IndexC = c;
            NeighborA = -1;
            NeighborB = -1;
            NeighborC = -1;
            Normal = default;
        }

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

        foreach (var t in triangles)
        {
            int a = GetOrAddVertex(t.V0);
            int b = GetOrAddVertex(t.V1);
            int c = GetOrAddVertex(t.V2);

            var normal = (t.V1 - t.V0) % (t.V2 - t.V0);
            if (MathHelper.CloseToZero(normal, (Real)1e-12))
            {
                if (ignoreDegenerated)
                {
                    Logger.Warning("{0}, Degenerate triangle found in mesh. Ignoring.", nameof(TriangleMesh));
                    continue;
                }
                throw new DegenerateTriangleException("Degenerate triangle found in mesh. " +
                                                      "Try to clean the mesh in the editor of your choice first.");
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