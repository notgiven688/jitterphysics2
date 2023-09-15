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
using System.Collections.Generic;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Provides helper methods for calculating the properties of implicitly defined shapes.
/// </summary>
public static class ShapeHelper
{
    private struct ClipTriangle
    {
        public JVector V1;
        public JVector V2;
        public JVector V3;
        public int Division;
    }

    /// <summary>
    /// Calculates the convex hull of an implicitly defined shape.
    /// </summary>
    /// <param name="support">The support map interface implemented by the shape.</param>
    /// <param name="subdivisions">The number of subdivisions used for hull generation. Defaults to 3.</param>
    /// <returns>An enumeration of triangles forming the convex hull.</returns>
    public static IEnumerable<JTriangle> MakeHull(ISupportMap support, int subdivisions = 3)
    {
        Stack<ClipTriangle> sphereTesselation = new();
        float gr = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;

        JVector[] vertices = new JVector[12]
        {
            new(0, +1, +gr), new(0, -1, +gr), new(0, +1, -gr), new(0, -1, -gr),
            new(+1, +gr, 0), new(+1, -gr, 0), new(-1, +gr, 0), new(-1, -gr, 0),
            new(+gr, 0, +1), new(+gr, 0, -1), new(-gr, 0, +1), new(-gr, 0, -1)
        };

        int[,] indices = new int[20, 3]
        {
            { 1, 0, 10 }, { 0, 1, 8 }, { 0, 4, 6 }, { 4, 0, 8 }, { 0, 6, 10 }, { 5, 1, 7 }, { 1, 5, 8 }, { 7, 1, 10 },
            { 2, 3, 11 }, { 3, 2, 9 }, { 4, 2, 6 }, { 2, 4, 9 }, { 6, 2, 11 }, { 3, 5, 7 }, { 5, 3, 9 }, { 3, 7, 11 },
            { 4, 8, 9 }, { 8, 5, 9 }, { 10, 6, 11 }, { 7, 10, 11 }
        };

        for (int i = 0; i < 20; i++)
        {
            ClipTriangle tri = new()
            {
                V1 = vertices[indices[i, 0]],
                V2 = vertices[indices[i, 1]],
                V3 = vertices[indices[i, 2]],
                Division = 0
            };
            sphereTesselation.Push(tri);
        }

        while (sphereTesselation.Count > 0)
        {
            ClipTriangle tri = sphereTesselation.Pop();

            if (tri.Division < subdivisions)
            {
                ClipTriangle tri1, tri2, tri3, tri4;
                JVector n;

                tri1.Division = tri.Division + 1;
                tri2.Division = tri.Division + 1;
                tri3.Division = tri.Division + 1;
                tri4.Division = tri.Division + 1;

                tri1.V1 = tri.V1;
                tri2.V2 = tri.V2;
                tri3.V3 = tri.V3;

                n = (tri.V1 + tri.V2) * 0.5f;
                tri1.V2 = n;
                tri2.V1 = n;
                tri4.V3 = n;

                n = (tri.V2 + tri.V3) * 0.5f;
                tri2.V3 = n;
                tri3.V2 = n;
                tri4.V1 = n;

                n = (tri.V3 + tri.V1) * 0.5f;
                tri1.V3 = n;
                tri3.V1 = n;
                tri4.V2 = n;

                sphereTesselation.Push(tri1);
                sphereTesselation.Push(tri2);
                sphereTesselation.Push(tri3);
                sphereTesselation.Push(tri4);
            }
            else
            {
                support.SupportMap(tri.V1, out JVector p1);
                support.SupportMap(tri.V2, out JVector p2);
                support.SupportMap(tri.V3, out JVector p3);
                JVector n = (p3 - p1) % (p2 - p1);

                if (n.LengthSquared() > 1e-24d)
                {
                    yield return new JTriangle(p1, p2, p3);
                }
            }
        }
    }

    /// <summary>
    /// Calculates the mass properties of an implicitly defined shape, assuming unit mass density.
    /// </summary>
    /// <param name="support">The support map interface implemented by the shape.</param>
    /// <param name="inertia">Output parameter for the calculated inertia matrix.</param>
    /// <param name="centerOfMass">Output parameter for the calculated center of mass vector.</param>
    /// <param name="mass">Output parameter for the calculated mass.</param>
    public static void CalculateMassInertia(ISupportMap support, out JMatrix inertia, out JVector centerOfMass,
        out float mass)
    {
        centerOfMass = JVector.Zero;
        inertia = JMatrix.Zero;
        mass = 0;

        float a = 1.0f / 60.0f, b = 1.0f / 120.0f;
        JMatrix C = new(a, b, b, b, a, b, b, b, a);

        foreach (JTriangle triangle in MakeHull(support))
        {
            JVector column0 = triangle.V0;
            JVector column1 = triangle.V1;
            JVector column2 = triangle.V2;

            JMatrix A = new(
                column0.X, column1.X, column2.X,
                column0.Y, column1.Y, column2.Y,
                column0.Z, column1.Z, column2.Z);

            float detA = A.Determinant();

            // now transform this canonical tetrahedron to the target tetrahedron
            // inertia by a linear transformation A
            JMatrix tetrahedronInertia = JMatrix.Multiply(A * C * JMatrix.Transpose(A), detA);

            JVector tetrahedronCOM = 1.0f / 4.0f * (column0 + column1 + column2);
            float tetrahedronMass = 1.0f / 6.0f * detA;

            inertia += tetrahedronInertia;
            centerOfMass += tetrahedronMass * tetrahedronCOM;
            mass += tetrahedronMass;
        }

        inertia = JMatrix.Multiply(JMatrix.Identity, inertia.Trace()) - inertia;
        centerOfMass *= 1.0f / mass;
    }
}