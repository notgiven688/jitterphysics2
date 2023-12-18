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

using System.Collections.Generic;
using System.Linq;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a generic convex shape.
/// </summary>
public class ConvexHullShape : Shape
{
    private struct CHullVector
    {
        public readonly JVector Vertex;
        public List<ushort> Neighbors;

        public CHullVector(in JVector vertex)
        {
            Vertex = vertex;
            Neighbors = null!;
        }

        public override int GetHashCode()
        {
            return Vertex.GetHashCode();
        }
    }

    private readonly struct CHullTriangle
    {
        public readonly ushort IndexA;
        public readonly ushort IndexB;
        public readonly ushort IndexC;

        public CHullTriangle(ushort a, ushort b, ushort c)
        {
            IndexA = a;
            IndexB = b;
            IndexC = c;
        }
    }

    private CHullVector[] vertices;
    private CHullTriangle[] indices;

    private JVector shifted;

    /// <summary>
    /// Initializes a new instance of the ConvexHullShape class, creating a convex hull.
    /// </summary>
    /// <param name="triangles">A list containing all vertices defining the convex hull. The vertices must strictly lie on the surface of the convex hull to avoid incorrect results or indefinite hangs in the collision algorithm. Note that the passed triangle list is not referenced and can be modified after calling the constructor without side effects.</param>
    public ConvexHullShape(List<JTriangle> triangles)
    {
        Dictionary<CHullVector, ushort> tmpIndices = new();
        List<CHullVector> tmpVertices = new();

        indices = new CHullTriangle[triangles.Count];

        ushort PushVector(CHullVector v)
        {
            if (!tmpIndices.TryGetValue(v, out ushort result))
            {
                result = (ushort)tmpVertices.Count;
                tmpIndices.Add(v, result);

                v.Neighbors = new List<ushort>();
                tmpVertices.Add(v);
            }

            return result;
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            JTriangle tti = triangles[i];

            CHullVector cha = new(tti.V0);
            CHullVector chb = new(tti.V1);
            CHullVector chc = new(tti.V2);

            ushort a = PushVector(cha);
            ushort b = PushVector(chb);
            ushort c = PushVector(chc);

            indices[i] = new CHullTriangle(a, b, c);

            tmpVertices[a].Neighbors.Add(b);
            tmpVertices[a].Neighbors.Add(c);
            tmpVertices[b].Neighbors.Add(a);
            tmpVertices[b].Neighbors.Add(c);
            tmpVertices[c].Neighbors.Add(a);
            tmpVertices[c].Neighbors.Add(b);
        }

        vertices = tmpVertices.ToArray();

        for (int i = 0; i < vertices.Length; i++)
        {
            List<ushort> nb = vertices[i].Neighbors;
            var dist = nb.Distinct().ToArray();
            nb.Clear();
            nb.AddRange(dist);
        }

        CalcInitBox();
        UpdateShape();
    }

    private ConvexHullShape()
    {
        vertices = null!;
        indices = null!;
    }

    /// <summary>
    /// Creates a clone of the convex hull shape. Note that the underlying data structure is shared among instances.
    /// </summary>
    /// <returns>A new instance of the ConvexHullShape class that shares the same underlying data structure as the original instance.</returns>
    public ConvexHullShape Clone()
    {
        ConvexHullShape result = new()
        {
            vertices = vertices,
            indices = indices,
            initBox = initBox,
            Inertia = Inertia,
            GeometricCenter = GeometricCenter,
            Mass = Mass,
            shifted = shifted
        };
        return result;
    }

    public JVector Shift
    {
        get => shifted;
        set
        {
            shifted = value;
            CalcInitBox();
            UpdateShape();
        }
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)
    {
        com = JVector.Zero;
        inertia = JMatrix.Zero;
        mass = 0;

        float a = 1.0f / 60.0f, b = 1.0f / 120.0f;
        JMatrix C = new(a, b, b, b, a, b, b, b, a);

        JVector pointWithin = JVector.Zero;
        for (int i = 0; i < vertices.Length; i++)
        {
            pointWithin += vertices[i].Vertex;
        }

        pointWithin = pointWithin * (1.0f / vertices.Length) + shifted;

        foreach (CHullTriangle tri in indices)
        {
            JVector column0 = vertices[tri.IndexA].Vertex + shifted;
            JVector column1 = vertices[tri.IndexB].Vertex + shifted;
            JVector column2 = vertices[tri.IndexC].Vertex + shifted;

            // check winding
            {
                JVector normal = (column1 - column0) % (column2 - column0);
                float ddot = JVector.Dot(normal, column0 - pointWithin);
                if (ddot < 0.0f)
                {
                    (column0, column1) = (column1, column0);
                }
            }

            JMatrix A = new(
                column0.X, column1.X, column2.X,
                column0.Y, column1.Y, column2.Y,
                column0.Z, column1.Z, column2.Z);

            float detA = A.Determinant();

            JMatrix tetrahedronInertia = JMatrix.Multiply(A * C * JMatrix.Transpose(A), detA);

            JVector tetrahedronCOM = 1.0f / 4.0f * (column0 + column1 + column2);
            float tetrahedronMass = 1.0f / 6.0f * detA;

            inertia += tetrahedronInertia;
            com += tetrahedronMass * tetrahedronCOM;
            mass += tetrahedronMass;
        }

        inertia = JMatrix.Multiply(JMatrix.Identity, inertia.Trace()) - inertia;
        com *= 1.0f / mass;
    }

    public override void CalculateBoundingBox(in JMatrix orientation, in JVector position, out JBBox box)
    {
        JVector halfSize = 0.5f * (initBox.Max - initBox.Min);
        JVector center = 0.5f * (initBox.Max + initBox.Min);

        JMatrix.Absolute(in orientation, out JMatrix abs);
        JVector.Transform(halfSize, abs, out JVector temp);
        JVector.Transform(center, orientation, out JVector temp2);

        box.Max = temp;
        JVector.Negate(temp, out box.Min);

        JVector.Add(box.Min, position + temp2, out box.Min);
        JVector.Add(box.Max, position + temp2, out box.Max);
    }

    private JBBox initBox;

    public void CalcInitBox()
    {
        JVector vec = JVector.UnitX;
        InternalSupportMap(vec, out JVector res);
        initBox.Max.X = res.X;

        vec = JVector.UnitY;
        InternalSupportMap(vec, out res);
        initBox.Max.Y = res.Y;

        vec = JVector.UnitZ;
        InternalSupportMap(vec, out res);
        initBox.Max.Z = res.Z;

        vec = -JVector.UnitX;
        InternalSupportMap(vec, out res);
        initBox.Min.X = res.X;

        vec = -JVector.UnitY;
        InternalSupportMap(vec, out res);
        initBox.Min.Y = res.Y;

        vec = -JVector.UnitZ;
        InternalSupportMap(vec, out res);
        initBox.Min.Z = res.Z;
    }

    private ushort InternalSupportMap(in JVector direction, out JVector result)
    {
        ushort current = 0;
        float dotProduct = JVector.Dot(vertices[current].Vertex, direction);

        again:
        var neighbors = vertices[current].Neighbors;

        for (int i = 0; i < neighbors.Count; i++)
        {
            ushort nb = neighbors[i];
            float nbProduct = JVector.Dot(vertices[nb].Vertex, direction);

            if (nbProduct > dotProduct)
            {
                // no need to find the "best" neighbor - as soon as we found a better
                // candidate we move there.
                dotProduct = nbProduct;
                current = nb;
                goto again;
            }
        }

        result = vertices[current].Vertex + shifted;
        return current;
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        // if(current >= vertices.Length) current = (ushort) vertices.Length;
        InternalSupportMap(direction, out result);
    }
}