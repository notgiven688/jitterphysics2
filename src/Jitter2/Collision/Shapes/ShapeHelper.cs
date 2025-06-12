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

using System.Collections.Generic;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Provides helper methods for calculating the properties of implicitly defined shapes.
/// </summary>
public static class ShapeHelper
{
    private const Real GoldenRatio = (Real)1.6180339887498948482045;
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

    /// <summary>
    /// Approximates the convex hull of a given set of 3D vertices.
    /// </summary>
    /// <param name="support">The support map interface implemented by the shape.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation.</param>
    /// <param name="hullCollection">An ICollection to which the triangles are added too.</param>
    /// <remarks>The hull may not be perfectly convex. It is therefore not suited to be used with
    /// <see cref="ConvexHullShape"/>.</remarks>
    /// <remarks>The time complexity is O(4^n), where n is the number of subdivisions.</remarks>
    public static void MakeHull<T>(ISupportMappable support, T hullCollection, int subdivisions = 3) where T : ICollection<JTriangle>
    {
        for (int i = 0; i < 20; i++)
        {
            JVector v1 = icosahedronVertices[icosahedronIndices[i, 0]];
            JVector v2 = icosahedronVertices[icosahedronIndices[i, 1]];
            JVector v3 = icosahedronVertices[icosahedronIndices[i, 2]];

            support.SupportMap(v1, out JVector sv1);
            support.SupportMap(v2, out JVector sv2);
            support.SupportMap(v3, out JVector sv3);

            Subdivide(support, hullCollection, v1, v2, v3, sv1, sv2, sv3, subdivisions);
        }
    }

    private static void Subdivide<T>(ISupportMappable support, T hullCollection,
        JVector v1, JVector v2, JVector v3, JVector p1, JVector p2, JVector p3,
        int subdivisions) where T : ICollection<JTriangle>
    {
        if (subdivisions <= 1)
        {
            JVector n = (p3 - p1) % (p2 - p1);

            if (n.LengthSquared() > (Real)1e-16)
            {
                hullCollection.Add(new JTriangle(p1, p2, p3));
            }

            return;
        }

        JVector h1 = (v1 + v2) * (Real)0.5;
        JVector h2 = (v2 + v3) * (Real)0.5;
        JVector h3 = (v3 + v1) * (Real)0.5;

        support.SupportMap(h1, out JVector sp1);
        support.SupportMap(h2, out JVector sp2);
        support.SupportMap(h3, out JVector sp3);

        subdivisions -= 1;

        Subdivide(support, hullCollection, v1, h1, h3, p1, sp1, sp3, subdivisions);
        Subdivide(support, hullCollection, h1, v2, h2, sp1, p2, sp2, subdivisions);
        Subdivide(support, hullCollection, h3, h2, v3, sp3, sp2, p3, subdivisions);
        Subdivide(support, hullCollection, h2, h3, h1, sp2, sp3, sp1, subdivisions);
    }

    /// <summary>
    /// Approximates the convex hull of a support map.
    /// </summary>
    /// <param name="support">The support map.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation.</param>
    /// <returns>A list of triangles representing the convex hull.</returns>
    /// <remarks>The hull may not be perfectly convex. It is therefore not suited to be used with
    /// <see cref="ConvexHullShape"/>.</remarks>
    /// <remarks>The time complexity is O(4^n), where n is the number of subdivisions.</remarks>
    public static List<JTriangle> MakeHull(ISupportMappable support, int subdivisions = 3)
    {
        List<JTriangle> triangles = new();
        MakeHull(support, triangles, subdivisions);
        return triangles;
    }

    /// <summary>
    /// Approximates the convex hull of a given set of 3D vertices.
    /// </summary>
    /// <param name="vertices">The vertices used to approximate the hull.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation.</param>
    /// <returns>A list of triangles representing the convex hull.</returns>
    /// <remarks>The hull may not be perfectly convex. It is therefore not suited to be used with
    /// <see cref="ConvexHullShape"/>.</remarks>
    /// <remarks>The time complexity is O(4^n), where n is the number of subdivisions.</remarks>
    public static List<JTriangle> MakeHull(IReadOnlyList<JVector> vertices, int subdivisions = 3)
    {
        return MakeHull(new VertexSupportMap(vertices), subdivisions);
    }

    public static void CalculateBoundingBox(ISupportMappable support,
        in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        JMatrix oriT = JMatrix.Transpose(JMatrix.CreateFromQuaternion(orientation));

        support.SupportMap(oriT.GetColumn(0), out JVector res);
        box.Max.X = JVector.Dot(oriT.GetColumn(0), res);

        support.SupportMap(oriT.GetColumn(1), out res);
        box.Max.Y = JVector.Dot(oriT.GetColumn(1), res);

        support.SupportMap(oriT.GetColumn(2), out res);
        box.Max.Z = JVector.Dot(oriT.GetColumn(2), res);

        support.SupportMap(-oriT.GetColumn(0), out res);
        box.Min.X = JVector.Dot(oriT.GetColumn(0), res);

        support.SupportMap(-oriT.GetColumn(1), out res);
        box.Min.Y = JVector.Dot(oriT.GetColumn(1), res);

        support.SupportMap(-oriT.GetColumn(2), out res);
        box.Min.Z = JVector.Dot(oriT.GetColumn(2), res);

        JVector.Add(box.Min, position, out box.Min);
        JVector.Add(box.Max, position, out box.Max);
    }

    /// <summary>
    /// Approximates the convex hull of a given set of 3D vertices by sampling support points
    /// generated through recursive subdivision of an icosahedron.
    /// </summary>
    /// <param name="vertices">The list of vertices that define the object.</param>
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
    /// mapper to generate a hull point.
    /// </remarks>
    /// <remarks>The time complexity is O(4^n), where n is the number of subdivisions.</remarks>
    public static List<JVector> SampleHull(IReadOnlyList<JVector> vertices, int subdivisions = 3)
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
    /// mapper to generate a hull point.
    /// </remarks>
    /// <remarks>The time complexity is O(4^n), where n is the number of subdivisions.</remarks>
    public static List<JVector> SampleHull(ISupportMappable support, int subdivisions = 3)
    {
        Stack<(JTriangle triangle, int depth)> stack = new();

        for (int i = 0; i < 20; i++)
        {
            JVector v1 = icosahedronVertices[icosahedronIndices[i, 0]];
            JVector v2 = icosahedronVertices[icosahedronIndices[i, 1]];
            JVector v3 = icosahedronVertices[icosahedronIndices[i, 2]];
            stack.Push((new JTriangle(v1, v2, v3), subdivisions));
        }

        HashSet<JVector> hull = new();

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

            JVector ab = JVector.Normalize((tri.V0 + tri.V1) * 0.5f);
            JVector bc = JVector.Normalize((tri.V1 + tri.V2) * 0.5f);
            JVector ca = JVector.Normalize((tri.V2 + tri.V0) * 0.5f);

            stack.Push((new JTriangle(tri.V0, ab, ca), depth - 1));
            stack.Push((new JTriangle(ab, tri.V1, bc), depth - 1));
            stack.Push((new JTriangle(ca, bc, tri.V2), depth - 1));
            stack.Push((new JTriangle(ab, bc, ca), depth - 1));
        }

        return new List<JVector>(hull);
    }

    /// <summary>
    /// Calculates the mass properties of an implicitly defined shape, assuming unit mass density.
    /// </summary>
    /// <param name="support">The support map interface implemented by the shape.</param>
    /// <param name="inertia">Output parameter for the calculated inertia matrix.</param>
    /// <param name="centerOfMass">Output parameter for the calculated center of mass vector.</param>
    /// <param name="mass">Output parameter for the calculated mass.</param>
    public static void CalculateMassInertia(ISupportMappable support, out JMatrix inertia, out JVector centerOfMass,
        out Real mass, int subdivisions = 4)
    {
        centerOfMass = JVector.Zero;
        inertia = JMatrix.Zero;
        mass = 0;

        const Real a = (Real)(1.0 / 60.0), b = (Real)(1.0 / 120.0);
        JMatrix canonicalInertia = new(a, b, b, b, a, b, b, b, a);

        foreach (JTriangle triangle in MakeHull(support, subdivisions))
        {
            JVector column0 = triangle.V0;
            JVector column1 = triangle.V1;
            JVector column2 = triangle.V2;

            JMatrix transformation = new(
                column0.X, column1.X, column2.X,
                column0.Y, column1.Y, column2.Y,
                column0.Z, column1.Z, column2.Z);

            Real detA = transformation.Determinant();

            // now transform this canonical tetrahedron to the target tetrahedron
            // inertia by a linear transformation A
            JMatrix tetrahedronInertia = JMatrix.Multiply(transformation * canonicalInertia * JMatrix.Transpose(transformation), detA);

            JVector tetrahedronCom = (Real)(1.0 / 4.0) * (column0 + column1 + column2);
            Real tetrahedronMass = (Real)(1.0 / 6.0) * detA;

            inertia += tetrahedronInertia;
            centerOfMass += tetrahedronMass * tetrahedronCom;
            mass += tetrahedronMass;
        }

        inertia = JMatrix.Multiply(JMatrix.Identity, inertia.Trace()) - inertia;
        centerOfMass *= (Real)1.0 / mass;
    }
}