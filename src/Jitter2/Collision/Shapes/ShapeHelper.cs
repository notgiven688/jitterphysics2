/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using Jitter2.DataStructures;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Approximates convex surfaces and calculates their bounds and mass properties.
/// </summary>
public static class ShapeHelper
{
    private const Real GoldenRatio = (Real)1.6180339887498948482045;
    private static readonly JSymmetricMatrix canonicalTetrahedronInertia = new(
        (Real)(1.0 / 60.0), (Real)(1.0 / 120.0), (Real)(1.0 / 120.0),
        (Real)(1.0 / 60.0), (Real)(1.0 / 120.0),
        (Real)(1.0 / 60.0));

    private static readonly JVector[] icosahedronVertices =
    [
        new(0, +1, +GoldenRatio), new(0, -1, +GoldenRatio), new(0, +1, -GoldenRatio), new(0, -1, -GoldenRatio),
        new(+1, +GoldenRatio, 0), new(+1, -GoldenRatio, 0), new(-1, +GoldenRatio, 0), new(-1, -GoldenRatio, 0),
        new(+GoldenRatio, 0, +1), new(+GoldenRatio, 0, -1), new(-GoldenRatio, 0, +1), new(-GoldenRatio, 0, -1)
    ];

    private static readonly int[,] icosahedronIndices = new int[20, 3]
    {
        { 1, 0, 10 }, { 0, 1, 8 }, { 0, 4, 6 }, { 4, 0, 8 }, { 0, 6, 10 }, { 5, 1, 7 }, { 1, 5, 8 }, { 7, 1, 10 },
        { 2, 3, 11 }, { 3, 2, 9 }, { 4, 2, 6 }, { 2, 4, 9 }, { 6, 2, 11 }, { 3, 5, 7 }, { 5, 3, 9 }, { 3, 7, 11 },
        { 4, 8, 9 }, { 8, 5, 9 }, { 10, 6, 11 }, { 7, 10, 11 }
    };

    private static int GetTessellationTriangleCapacity(int subdivisions)
    {
        int capacity = 20;

        for (int i = 1; i < subdivisions; i++)
        {
            capacity = checked(capacity * 4);
        }

        return capacity;
    }

    /// <summary>
    /// Approximates a convex surface with triangles and appends them to the collection.
    /// </summary>
    /// <remarks>
    /// The surface may contain folds, overlapping triangles, or inward-facing normals, even for a smooth shape.
    /// Do not use these triangles to construct a <see cref="ConvexHullShape"/>.
    /// Each additional subdivision level increases the work roughly fourfold.
    /// </remarks>
    /// <param name="support">The convex shape.</param>
    /// <param name="hullCollection">A collection to which the triangles are added.</param>
    /// <param name="subdivisions">Sampling detail. Each level above 1 splits every triangle into four; default is 3.</param>
    public static void Tessellate<TSupport, TCollection>(in TSupport support, TCollection hullCollection, int subdivisions = 3)
        where TSupport : ISupportMappable
        where TCollection : class, ICollection<JTriangle>
    {
        var sink = new CollectionSink<JTriangle>(hullCollection);
        Tessellate(in support, ref sink, subdivisions);
    }

    /// <summary>
    /// Approximates a convex surface with triangles and appends them to the sink.
    /// </summary>
    /// <typeparam name="TSupport">The support shape type.</typeparam>
    /// <typeparam name="TSink">The sink type receiving the generated triangles.</typeparam>
    /// <param name="support">The convex shape.</param>
    /// <param name="hullSink">The sink receiving the generated triangles.</param>
    /// <param name="subdivisions">Sampling detail. Each level above 1 splits every triangle into four;
    /// the default of 3 splits twice.</param>
    /// <remarks>
    /// The surface may contain folds, overlapping triangles, or inward-facing normals, even for a smooth shape.
    /// Do not use these triangles to construct a <see cref="ConvexHullShape"/>.
    /// Each additional subdivision level increases the work roughly fourfold.
    /// </remarks>
    public static void Tessellate<TSupport, TSink>(in TSupport support, ref TSink hullSink, int subdivisions = 3)
        where TSupport : ISupportMappable
        where TSink : ISink<JTriangle>
    {
        for (int i = 0; i < 20; i++)
        {
            JVector v1 = icosahedronVertices[icosahedronIndices[i, 0]];
            JVector v2 = icosahedronVertices[icosahedronIndices[i, 1]];
            JVector v3 = icosahedronVertices[icosahedronIndices[i, 2]];

            support.SupportMap(v1, out JVector sv1);
            support.SupportMap(v2, out JVector sv2);
            support.SupportMap(v3, out JVector sv3);

            Subdivide(in support, ref hullSink, v1, v2, v3, sv1, sv2, sv3, subdivisions);
        }
    }

    private static void Subdivide<TSupport, TSink>(in TSupport support, ref TSink hullSink,
        JVector v1, JVector v2, JVector v3, JVector p1, JVector p2, JVector p3,
        int subdivisions) where TSupport : ISupportMappable where TSink : ISink<JTriangle>
    {
        if (subdivisions <= 1)
        {
            JVector n = (p3 - p1) % (p2 - p1);

            if (n.LengthSquared() > (Real)1e-16)
            {
                hullSink.Add(new JTriangle(p1, p2, p3));
            }

            return;
        }

        // Deliberately skip re-projecting the midpoint directions onto the sphere.
        // The resulting samples are still good enough for this approximation, and
        // avoiding those normalization calls keeps tessellation inexpensive.
        JVector h1 = (v1 + v2) * (Real)0.5;
        JVector h2 = (v2 + v3) * (Real)0.5;
        JVector h3 = (v3 + v1) * (Real)0.5;

        support.SupportMap(h1, out JVector sp1);
        support.SupportMap(h2, out JVector sp2);
        support.SupportMap(h3, out JVector sp3);

        subdivisions -= 1;

        Subdivide(in support, ref hullSink, v1, h1, h3, p1, sp1, sp3, subdivisions);
        Subdivide(in support, ref hullSink, h1, v2, h2, sp1, p2, sp2, subdivisions);
        Subdivide(in support, ref hullSink, h3, h2, v3, sp3, sp2, p3, subdivisions);
        Subdivide(in support, ref hullSink, h2, h3, h1, sp2, sp3, sp1, subdivisions);
    }

    /// <summary>
    /// Approximates a convex surface with triangles.
    /// </summary>
    /// <param name="support">The convex shape.</param>
    /// <param name="subdivisions">Sampling detail. Each level above 1 splits every triangle into four;
    /// the default of 3 splits twice.</param>
    /// <remarks>
    /// The surface may contain folds, overlapping triangles, or inward-facing normals, even for a smooth shape.
    /// Do not use these triangles to construct a <see cref="ConvexHullShape"/>.
    /// Each additional subdivision level increases the work roughly fourfold.
    /// </remarks>
    public static List<JTriangle> Tessellate<TSupport>(in TSupport support, int subdivisions = 3)
        where TSupport : ISupportMappable
    {
        List<JTriangle> triangles = new(GetTessellationTriangleCapacity(subdivisions));
        Tessellate(in support, triangles, subdivisions);
        return triangles;
    }

    /// <summary>
    /// Approximates the surface of the convex hull of the supplied vertices with triangles.
    /// </summary>
    /// <param name="vertices">The vertices used to approximate the hull.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation.</param>
    /// <returns>A list of triangles representing the convex hull.</returns>
    /// <remarks>
    /// The surface may contain folds, overlapping triangles, or inward-facing normals, even for a smooth shape.
    /// Do not use these triangles to construct a <see cref="ConvexHullShape"/>.
    /// Each additional subdivision level increases the work roughly fourfold.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="vertices"/> is empty.
    /// </exception>
    public static List<JTriangle> Tessellate(ReadOnlySpan<JVector> vertices, int subdivisions = 3)
    {
        return Tessellate(new VertexSupportMap(vertices), subdivisions);
    }

    /// <inheritdoc cref="Tessellate(System.ReadOnlySpan{Jitter2.LinearMath.JVector}, int)"/>
    public static List<JTriangle> Tessellate(IEnumerable<JVector> vertices, int subdivisions = 3)
    {
        return Tessellate(new VertexSupportMap(SpanHelper.AsReadOnlySpan(vertices, out _)), subdivisions);
    }

    #region Obsolete MakeHull - Use Tessellate instead

    [Obsolete("Use Tessellate instead.", true)]
    public static List<JTriangle> MakeHull(IEnumerable<JVector> vertices, int subdivisions = 3) =>
        Tessellate(vertices, subdivisions);

    [Obsolete("Use Tessellate instead.", true)]
    public static List<JTriangle> MakeHull(ReadOnlySpan<JVector> vertices, int subdivisions = 3) =>
        Tessellate(vertices, subdivisions);

    [Obsolete("Use Tessellate instead.", true)]
    public static List<JTriangle> MakeHull<TSupport>(in TSupport support, int subdivisions = 3)
        where TSupport : ISupportMappable =>
        Tessellate(in support, subdivisions);

    [Obsolete("Use Tessellate instead.", true)]
    public static void MakeHull<TSupport, TCollection>(in TSupport support, TCollection hullCollection, int subdivisions = 3)
        where TSupport : ISupportMappable
        where TCollection : class, ICollection<JTriangle> =>
        Tessellate(in support, hullCollection, subdivisions);

    #endregion

    /// <summary>
    /// Calculates the axis-aligned bounding box of a shape given its orientation and position.
    /// </summary>
    /// <param name="support">The convex shape.</param>
    /// <param name="orientation">The orientation of the shape.</param>
    /// <param name="position">The position of the shape.</param>
    /// <param name="box">The resulting bounding box.</param>
    public static void CalculateBoundingBox<TSupport>(in TSupport support,
        in JQuaternion orientation, in JVector position, out JBoundingBox box)
        where TSupport : ISupportMappable
    {
        JMatrix oriT = JMatrix.Transpose(JMatrix.CreateFromQuaternion(orientation));

        JVector axisX = oriT.GetColumn(0);
        JVector axisY = oriT.GetColumn(1);
        JVector axisZ = oriT.GetColumn(2);

        support.SupportMap(axisX, out JVector res);
        box.Max.X = JVector.Dot(axisX, res);

        support.SupportMap(axisY, out res);
        box.Max.Y = JVector.Dot(axisY, res);

        support.SupportMap(axisZ, out res);
        box.Max.Z = JVector.Dot(axisZ, res);

        support.SupportMap(-axisX, out res);
        box.Min.X = JVector.Dot(axisX, res);

        support.SupportMap(-axisY, out res);
        box.Min.Y = JVector.Dot(axisY, res);

        support.SupportMap(-axisZ, out res);
        box.Min.Z = JVector.Dot(axisZ, res);

        JVector.Add(box.Min, position, out box.Min);
        JVector.Add(box.Max, position, out box.Max);
    }

    /// <summary>
    /// Samples points on the convex hull of the supplied vertices.
    /// </summary>
    /// <param name="vertices">The vertices used to approximate the hull.</param>
    /// <param name="subdivisions">
    /// Sampling detail. Higher values sample more directions, at roughly four times the work per level.
    /// Default is 3.
    /// </param>
    /// <returns>Sampled surface points in local coordinates, with exact duplicates removed.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="vertices"/> is empty.
    /// </exception>
    public static List<JVector> SampleHull(ReadOnlySpan<JVector> vertices, int subdivisions = 3)
    {
        return SampleHull(new VertexSupportMap(vertices), subdivisions);
    }

    /// <inheritdoc cref="SampleHull(ReadOnlySpan{JVector}, int)"/>
    public static List<JVector> SampleHull(IEnumerable<JVector> vertices, int subdivisions = 3)
    {
        return SampleHull(new VertexSupportMap(vertices), subdivisions);
    }

    /// <summary>
    /// Samples points on the surface of a convex shape.
    /// </summary>
    /// <param name="support">The convex shape.</param>
    /// <param name="subdivisions">
    /// Sampling detail. Higher values sample more directions, at roughly four times the work per level.
    /// Default is 3.
    /// </param>
    /// <returns>Sampled surface points in local coordinates, with exact duplicates removed.</returns>
    public static List<JVector> SampleHull<TSupport>(in TSupport support, int subdivisions = 3)
        where TSupport : ISupportMappable
    {
        int triangleCapacity = GetTessellationTriangleCapacity(subdivisions);
        Stack<(JTriangle triangle, int depth)> stack = new(triangleCapacity);

        for (int i = 0; i < 20; i++)
        {
            JVector v1 = icosahedronVertices[icosahedronIndices[i, 0]];
            JVector v2 = icosahedronVertices[icosahedronIndices[i, 1]];
            JVector v3 = icosahedronVertices[icosahedronIndices[i, 2]];
            stack.Push((new JTriangle(v1, v2, v3), subdivisions));
        }

        HashSet<JVector> hull = new(triangleCapacity);

        while (stack.Count > 0)
        {
            var (tri, depth) = stack.Pop();

            if (depth <= 1)
            {
                support.SupportMap(tri.V0, out JVector sv0);
                support.SupportMap(tri.V1, out JVector sv1);
                support.SupportMap(tri.V2, out JVector sv2);
                hull.Add(sv0); hull.Add(sv1); hull.Add(sv2);
                continue;
            }

            JVector ab = JVector.Normalize((tri.V0 + tri.V1) * (Real)0.5);
            JVector bc = JVector.Normalize((tri.V1 + tri.V2) * (Real)0.5);
            JVector ca = JVector.Normalize((tri.V2 + tri.V0) * (Real)0.5);

            stack.Push((new JTriangle(tri.V0, ab, ca), depth - 1));
            stack.Push((new JTriangle(ab, tri.V1, bc), depth - 1));
            stack.Push((new JTriangle(ca, bc, tri.V2), depth - 1));
            stack.Push((new JTriangle(ab, bc, ca), depth - 1));
        }

        return new List<JVector>(hull);
    }

    private struct MassInertiaSink : ISink<JTriangle>
    {
        public JSymmetricMatrix Inertia;
        public JVector CenterOfMass;
        public Real Mass;

        public void Add(in JTriangle triangle)
        {
            JMatrix transformation = JMatrix.FromColumns(triangle.V0, triangle.V1, triangle.V2);
            Real detA = transformation.Determinant();

            JSymmetricMatrix tetrahedronInertia =
                JSymmetricMatrix.Transform(canonicalTetrahedronInertia, transformation) * detA;

            JVector tetrahedronCom = (Real)(1.0 / 4.0) * (triangle.V0 + triangle.V1 + triangle.V2);
            Real tetrahedronMass = (Real)(1.0 / 6.0) * detA;

            Inertia += tetrahedronInertia;
            CenterOfMass += tetrahedronMass * tetrahedronCom;
            Mass += tetrahedronMass;
        }
    }

    /// <summary>
    /// Approximates a convex shape's mass, center of mass, and inertia at unit density.
    /// </summary>
    /// <remarks>
    /// Uses a triangle approximation of the surface. The inertia is about the shape's local origin,
    /// not its center of mass.
    /// </remarks>
    /// <param name="support">The convex shape.</param>
    /// <param name="inertia">The inertia tensor about the local origin.</param>
    /// <param name="centerOfMass">The center of mass in local coordinates.</param>
    /// <param name="mass">The mass at unit density, equal to the shape's volume.</param>
    /// <param name="subdivisions">Surface sampling detail; default is 4. Higher values increase accuracy and cost.</param>
    public static void CalculateMassInertia<TSupport>(in TSupport support, out JSymmetricMatrix inertia, out JVector centerOfMass,
        out Real mass, int subdivisions = 4)
        where TSupport : ISupportMappable
    {
        MassInertiaSink sink = default;
        // Preserve the generated winding, including inward-facing triangles. Signed tetrahedral
        // contributions cancel overlapping coplanar patches; flipping triangles would break that cancellation.
        Tessellate(in support, ref sink, subdivisions);

        inertia = JSymmetricMatrix.Identity * sink.Inertia.Trace() - sink.Inertia;
        centerOfMass = sink.CenterOfMass * ((Real)1.0 / sink.Mass);
        mass = sink.Mass;
    }
}
