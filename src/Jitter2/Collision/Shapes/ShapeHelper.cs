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
/// Provides helper methods for calculating the properties of implicitly defined shapes.
/// </summary>
public static class ShapeHelper
{
    private const Real GoldenRatio = (Real)1.6180339887498948482045;
    private static readonly JMatrix canonicalTetrahedronInertia = new(
        (Real)(1.0 / 60.0), (Real)(1.0 / 120.0), (Real)(1.0 / 120.0),
        (Real)(1.0 / 120.0), (Real)(1.0 / 60.0), (Real)(1.0 / 120.0),
        (Real)(1.0 / 120.0), (Real)(1.0 / 120.0), (Real)(1.0 / 60.0));

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

    /// <inheritdoc cref="Tessellate{TSupport}(in TSupport, int)"/>
    /// <param name="hullCollection">A collection to which the triangles are added.</param>
    public static void Tessellate<TSupport, TCollection>(in TSupport support, TCollection hullCollection, int subdivisions = 3)
        where TSupport : ISupportMappable
        where TCollection : class, ICollection<JTriangle>
    {
        var sink = new CollectionSink<JTriangle>(hullCollection);
        Tessellate(in support, ref sink, subdivisions);
    }

    /// <summary>
    /// Creates a tessellation of a shape defined by its support map and appends all generated triangles to the specified sink.
    /// </summary>
    /// <typeparam name="TSupport">The support shape type.</typeparam>
    /// <typeparam name="TSink">The sink type receiving the generated triangles.</typeparam>
    /// <param name="support">The support map interface implemented by the shape.</param>
    /// <param name="hullSink">The sink receiving the generated triangles.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation.</param>
    /// <remarks>
    /// The tessellated hull may not be perfectly convex. It is therefore not suited to be used with
    /// <see cref="ConvexHullShape"/>. The time complexity is O(4^n), where n is the number of subdivisions.
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
    /// Creates a tessellation of a shape defined by its support map.
    /// </summary>
    /// <param name="support">The support map interface implemented by the shape.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation.</param>
    /// <remarks>
    /// The tessellated hull may not be perfectly convex. It is therefore not suited to be used with
    /// <see cref="ConvexHullShape"/>. The time complexity is O(4^n), where n is the number of subdivisions.
    /// </remarks>
    public static List<JTriangle> Tessellate<TSupport>(in TSupport support, int subdivisions = 3)
        where TSupport : ISupportMappable
    {
        List<JTriangle> triangles = new(GetTessellationTriangleCapacity(subdivisions));
        Tessellate(in support, triangles, subdivisions);
        return triangles;
    }

    /// <summary>
    /// Creates a tessellation of the convex hull of a given set of 3D vertices.
    /// </summary>
    /// <param name="vertices">The vertices used to approximate the hull.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation.</param>
    /// <returns>A list of triangles representing the convex hull.</returns>
    /// <remarks>
    /// The tessellated hull may not be perfectly convex. It is therefore not suited to be used with
    /// <see cref="ConvexHullShape"/>. The time complexity is O(4^n), where n is the number of subdivisions.
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
    /// <param name="support">The support map interface implemented by the shape.</param>
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
    /// Approximates the convex hull of a given set of 3D vertices by sampling support points
    /// generated through recursive subdivision of an icosahedron.
    /// </summary>
    /// <param name="vertices">The vertices used to approximate the hull.</param>
    /// <param name="subdivisions">
    /// The number of recursive subdivisions applied to each icosahedron triangle.
    /// Higher values produce more sampling directions and better coverage,
    /// but increase computation. Default is 3.
    /// </param>
    /// <returns>
    /// A list of <see cref="JVector"/> points on the convex hull, sampled using directional support mapping from a
    /// refined spherical distribution.
    /// </returns>
    /// <remarks>
    /// This method begins with a regular icosahedron and recursively subdivides each triangular face into smaller
    /// triangles, projecting new vertices onto the unit sphere. Each final vertex direction is passed to the support
    /// mapper to generate a hull point. The time complexity is O(4^n), where n is the number of subdivisions.
    /// </remarks>
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
    /// Samples a convex shape's hull by evaluating support directions generated through recursive subdivision of
    /// an icosahedron.
    /// </summary>
    /// <param name="support">
    /// An object implementing <see cref="ISupportMappable"/>, representing a convex shape that can be queried
    /// with directional support mapping.
    /// </param>
    /// <param name="subdivisions">
    /// The number of recursive subdivisions applied to each icosahedron triangle. Higher values produce more sampling
    /// directions and better coverage, but increase computation. Default is 3.
    /// </param>
    /// <returns>
    /// A list of <see cref="JVector"/> points on the convex hull, sampled using directional support mapping from a
    /// refined spherical distribution.
    /// </returns>
    /// <remarks>
    /// This method begins with a regular icosahedron and recursively subdivides each triangular face into smaller
    /// triangles, projecting new vertices onto the unit sphere. Each final vertex direction is passed to the support
    /// mapper to generate a hull point. The time complexity is O(4^n), where n is the number of subdivisions.
    /// </remarks>
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
        public JMatrix Inertia;
        public JVector CenterOfMass;
        public Real Mass;

        public void Add(in JTriangle triangle)
        {
            JMatrix transformation = JMatrix.FromColumns(triangle.V0, triangle.V1, triangle.V2);
            Real detA = transformation.Determinant();

            JMatrix tetrahedronInertia =
                JMatrix.Multiply(transformation * canonicalTetrahedronInertia * JMatrix.Transpose(transformation), detA);

            JVector tetrahedronCom = (Real)(1.0 / 4.0) * (triangle.V0 + triangle.V1 + triangle.V2);
            Real tetrahedronMass = (Real)(1.0 / 6.0) * detA;

            Inertia += tetrahedronInertia;
            CenterOfMass += tetrahedronMass * tetrahedronCom;
            Mass += tetrahedronMass;
        }
    }

    /// <summary>
    /// Calculates the mass properties of an implicitly defined shape, assuming unit mass density.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape is approximated via surface tessellation using the specified number of <paramref name="subdivisions"/>.
    /// </para>
    /// <para>
    /// <b>Note on Reference Frame:</b>
    /// The calculated <paramref name="inertia"/> tensor is expressed relative to the <b>coordinate system origin (0,0,0)</b>,
    /// <em>not</em> the calculated <paramref name="centerOfMass"/>.
    /// </para>
    /// </remarks>
    /// <param name="support">The support map interface implemented by the shape.</param>
    /// <param name="inertia">
    /// Output parameter for the inertia tensor calculated relative to the <b>Origin (0,0,0)</b>.
    /// </param>
    /// <param name="centerOfMass">Output parameter for the calculated center of mass vector (relative to the Origin).</param>
    /// <param name="mass">Output parameter for the calculated mass (Volume * density 1.0).</param>
    /// <param name="subdivisions">The recursion depth for the surface tessellation (default 4).</param>
    public static void CalculateMassInertia<TSupport>(in TSupport support, out JMatrix inertia, out JVector centerOfMass,
        out Real mass, int subdivisions = 4)
        where TSupport : ISupportMappable
    {
        MassInertiaSink sink = default;
        Tessellate(in support, ref sink, subdivisions);

        inertia = JMatrix.Multiply(JMatrix.Identity, sink.Inertia.Trace()) - sink.Inertia;
        centerOfMass = sink.CenterOfMass * ((Real)1.0 / sink.Mass);
        mass = sink.Mass;
    }
}
