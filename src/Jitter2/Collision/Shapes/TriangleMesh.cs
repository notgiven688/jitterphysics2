/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

public class TriangleMesh
{
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

    public struct Triangle(int a, int b, int c)
    {
        public readonly int IndexA = a, IndexB = b, IndexC = c;
        public int NeighborA = -1, NeighborB = -1, NeighborC = -1;
        public JVector Normal = default;
    }

    private readonly JVector[] vertices = null!;
    private readonly Triangle[] indices = null!;

    public ReadOnlySpan<JVector> Vertices => vertices;
    public ReadOnlySpan<Triangle> Indices => indices;

    /// <summary>
    /// Creates a mesh from a "soup" of triangles. Vertices are automatically identified and deduplicated.
    /// </summary>
    public TriangleMesh(IEnumerable<JTriangle> soup, bool ignoreDegenerated = false)
    {
        ReadOnlySpan<JTriangle> span = soup switch
        {
            List<JTriangle> list => CollectionsMarshal.AsSpan(list),
            JTriangle[] array => array,
            _ => CollectionsMarshal.AsSpan([..soup])
        };

        BuildFromSoup(span, ignoreDegenerated);
    }

    /// <summary>
    /// Creates a mesh from a span of triangles. Vertices are automatically identified and deduplicated.
    /// </summary>
    public TriangleMesh(ReadOnlySpan<JTriangle> soup, bool ignoreDegenerated = false)
    {
        BuildFromSoup(soup, ignoreDegenerated);
    }

    /// <summary>
    /// Creates a mesh from existing vertices and indices.
    /// </summary>
    /// <param name="vertices">The vertex buffer.</param>
    /// <param name="indices">The index buffer (must be a multiple of 3).</param>
    public TriangleMesh(ReadOnlySpan<JVector> vertices, ReadOnlySpan<int> indices, bool ignoreDegenerated = false)
    {
        BuildFromIndexed(vertices, indices, ignoreDegenerated);
    }

    // Overload for ushort indices (common in graphics)
    public TriangleMesh(ReadOnlySpan<JVector> vertices, ReadOnlySpan<ushort> indices, bool ignoreDegenerated = false)
    {
        // Convert ushort indices to int on the fly
        int[] intIndices = new int[indices.Length];
        for (int i = 0; i < indices.Length; i++) intIndices[i] = indices[i];

        BuildFromIndexed(vertices, intIndices, ignoreDegenerated);
    }

    // Overload for uint indices
    public TriangleMesh(ReadOnlySpan<JVector> vertices, ReadOnlySpan<uint> indices, bool ignoreDegenerated = false)
    {
        int[] intIndices = new int[indices.Length];
        for (int i = 0; i < indices.Length; i++) intIndices[i] = (int)indices[i];

        BuildFromIndexed(vertices, intIndices, ignoreDegenerated);
    }

    /// <summary>
    /// Creates a mesh from a span of custom struct triangles (e.g. MyTriangle).
    /// <typeparamref name="T"/> must be binary compatible with <see cref="JTriangle"/>.
    /// </summary>
    public static TriangleMesh Create<T>(ReadOnlySpan<T> triangleSoup, bool ignoreDegenerated = false)
        where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() != Unsafe.SizeOf<JTriangle>())
            throw new ArgumentException($"Size mismatch: {typeof(T).Name} is not {Unsafe.SizeOf<JTriangle>()} bytes.");

        var castSoup = MemoryMarshal.Cast<T, JTriangle>(triangleSoup);
        return new TriangleMesh(castSoup, ignoreDegenerated);
    }

    /// <summary>
    /// Creates a mesh from custom vertices (e.g. System.Numerics.Vector3) and int indices.
    /// </summary>
    public static TriangleMesh Create<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices,
        bool ignoreDegenerated = false) where TVertex : unmanaged
    {
        return new TriangleMesh(CastVertices(vertices), indices, ignoreDegenerated);
    }

    /// <summary>
    /// Creates a mesh from custom vertices and uint indices.
    /// </summary>
    public static TriangleMesh Create<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<uint> indices,
        bool ignoreDegenerated = false) where TVertex : unmanaged
    {
        return new TriangleMesh(CastVertices(vertices), indices, ignoreDegenerated);
    }

    /// <summary>
    /// Creates a mesh from custom vertices and ushort indices.
    /// </summary>
    public static TriangleMesh Create<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<ushort> indices,
        bool ignoreDegenerated = false) where TVertex : unmanaged
    {
        return new TriangleMesh(CastVertices(vertices), indices, ignoreDegenerated);
    }

    // Helper to keep the casting logic in one place
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<JVector> CastVertices<TVertex>(ReadOnlySpan<TVertex> vertices)
        where TVertex : unmanaged
    {
        if (Unsafe.SizeOf<TVertex>() != Unsafe.SizeOf<JVector>())
        {
            throw new ArgumentException($"Size mismatch: {typeof(TVertex).Name} ({Unsafe.SizeOf<TVertex>()} bytes) " +
                                        $"does not match JVector ({Unsafe.SizeOf<JVector>()} bytes).");
        }

        return MemoryMarshal.Cast<TVertex, JVector>(vertices);
    }

    private void BuildFromSoup(ReadOnlySpan<JTriangle> triangles, bool ignoreDegenerated)
    {
        var vertexMap = new Dictionary<JVector, int>();
        var vertexList = new List<JVector>();
        var triangleList = new List<Triangle>(triangles.Length);

        // Helper to deduplicate vertices
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

        foreach (ref readonly var tri in triangles)
        {
            JVector normal = (tri.V1 - tri.V0) % (tri.V2 - tri.V0);

            if (MathHelper.CloseToZero(normal, (Real)1e-12))
            {
                if (ignoreDegenerated) continue;
                throw new DegenerateTriangleException(tri);
            }

            // Deduplicate
            int a = GetOrAddVertex(tri.V0);
            int b = GetOrAddVertex(tri.V1);
            int c = GetOrAddVertex(tri.V2);

            var internalTri = new Triangle(a, b, c);
            JVector.Normalize(normal, out internalTri.Normal);
            triangleList.Add(internalTri);
        }

        Unsafe.AsRef(in vertices) = vertexList.ToArray();
        Unsafe.AsRef(in indices) = triangleList.ToArray();

        AssignNeighbors();
    }

    private void BuildFromIndexed(ReadOnlySpan<JVector> vertices, ReadOnlySpan<int> indices, bool ignoreDegenerated)
    {
        if (indices.Length % 3 != 0) throw new ArgumentException("Indices must be a multiple of 3.");

        // Direct copy of vertices (Trusting the user's topology)
        Unsafe.AsRef(in this.vertices) = vertices.ToArray();

        var triangleList = new List<Triangle>(indices.Length / 3);

        for (int i = 0; i < indices.Length; i += 3)
        {
            int i0 = indices[i];
            int i1 = indices[i + 1];
            int i2 = indices[i + 2];

            // Safety check for bounds
            if ((uint)i0 >= this.vertices.Length || (uint)i1 >= this.vertices.Length || (uint)i2 >= this.vertices.Length)
                throw new IndexOutOfRangeException($"Indices {i0},{i1},{i2} out of bounds.");

            JVector v0 = this.vertices[i0];
            JVector v1 = this.vertices[i1];
            JVector v2 = this.vertices[i2];

            JVector normal = (v1 - v0) % (v2 - v0);

            if (MathHelper.CloseToZero(normal, (Real)1e-12))
            {
                if (ignoreDegenerated) continue;
                throw new DegenerateTriangleException(new JTriangle(v0, v1, v2));
            }

            var tri = new Triangle(i0, i1, i2);
            JVector.Normalize(normal, out tri.Normal);
            triangleList.Add(tri);
        }

        Unsafe.AsRef(in this.indices) = triangleList.ToArray();
        AssignNeighbors();
    }

    private void AssignNeighbors()
    {
        var edgeToTriangle = new Dictionary<Edge, int>();

        for (int i = 0; i < indices.Length; i++)
        {
            var tri = indices[i];
            edgeToTriangle.TryAdd(new Edge(tri.IndexA, tri.IndexB), i);
            edgeToTriangle.TryAdd(new Edge(tri.IndexB, tri.IndexC), i);
            edgeToTriangle.TryAdd(new Edge(tri.IndexC, tri.IndexA), i);
        }

        for (int i = 0; i < indices.Length; i++)
        {
            ref var tri = ref indices[i];
            tri.NeighborA = edgeToTriangle.GetValueOrDefault(new Edge(tri.IndexC, tri.IndexB), -1);
            tri.NeighborB = edgeToTriangle.GetValueOrDefault(new Edge(tri.IndexA, tri.IndexC), -1);
            tri.NeighborC = edgeToTriangle.GetValueOrDefault(new Edge(tri.IndexB, tri.IndexA), -1);
        }
    }
}