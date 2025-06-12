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
using System.Linq;
using System.Runtime.InteropServices;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a generic convex shape.
/// </summary>
public class ConvexHullShape : RigidBodyShape
{
    private struct CHullVector : IEquatable<CHullVector>
    {
        public readonly JVector Vertex;
        public ushort NeighborMinIndex;
        public ushort NeighborMaxIndex;

        public CHullVector(in JVector vertex)
        {
            Vertex = vertex;
            NeighborMaxIndex = 0;
            NeighborMinIndex = 0;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is CHullVector other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return Vertex.GetHashCode();
        }

        public readonly bool Equals(CHullVector other)
        {
            return Vertex.Equals(other.Vertex);
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

    private JBoundingBox cachedBoundingBox;
    private JMatrix cachedInertia;
    private Real cachedMass;
    private JVector cachedCenter;

    private CHullVector[] vertices = null!;
    private CHullTriangle[] indices = null!;
    private List<ushort> neighborList = null!;

    private JVector shifted;

    /// <summary>
    /// Initializes a new instance of the ConvexHullShape class, creating a convex hull.
    /// </summary>
    /// <param name="triangles">A list containing all vertices defining the convex hull. The vertices must strictly lie
    /// on the surface of the convex hull to avoid incorrect results or indefinite hangs in the collision algorithm.
    /// Note that the passed triangle list is not referenced and can be modified after calling the constructor
    /// without side effects.</param>
    public ConvexHullShape(IReadOnlyList<JTriangle> triangles)
    {
        Build(triangles);
    }

    private void Build(IReadOnlyList<JTriangle> triangles)
    {
        Dictionary<CHullVector, ushort> tmpIndices = new();
        List<CHullVector> tmpVertices = new();

        ushort PushVector(CHullVector v)
        {
            if (tmpIndices.TryGetValue(v, out ushort result)) return result;

            if (tmpVertices.Count > ushort.MaxValue)
            {
                throw new InvalidOperationException(
                    $"The convex hull consists of too many triangles (>{ushort.MaxValue})");
            }

            result = (ushort)tmpVertices.Count;
            tmpIndices.Add(v, result);
            tmpVertices.Add(v);

            return result;
        }

        foreach (var tti in triangles)
        {
            PushVector(new CHullVector(tti.V0));
            PushVector(new CHullVector(tti.V1));
            PushVector(new CHullVector(tti.V2));
        }

        var tmpNeighbors = new List<ushort>[tmpVertices.Count];
        indices = new CHullTriangle[triangles.Count];

        for (int i = 0; i < triangles.Count; i++)
        {
            JTriangle tti = triangles[i];

            ushort a = PushVector(new CHullVector(tti.V0));
            ushort b = PushVector(new CHullVector(tti.V1));
            ushort c = PushVector(new CHullVector(tti.V2));

            indices[i] = new CHullTriangle(a, b, c);

            tmpNeighbors[a] ??= new List<ushort>();
            tmpNeighbors[b] ??= new List<ushort>();
            tmpNeighbors[c] ??= new List<ushort>();

            tmpNeighbors[a].Add(b);
            tmpNeighbors[a].Add(c);
            tmpNeighbors[b].Add(a);
            tmpNeighbors[b].Add(c);
            tmpNeighbors[c].Add(a);
            tmpNeighbors[c].Add(b);
        }

        neighborList = new List<ushort>();

        var tmpVerticesSpan = CollectionsMarshal.AsSpan(tmpVertices);

        for (int i = 0; i < tmpVerticesSpan.Length; i++)
        {
            ref var element = ref tmpVerticesSpan[i];
            element.NeighborMinIndex = (ushort)neighborList.Count;
            neighborList.AddRange(tmpNeighbors[i].Distinct());
            element.NeighborMaxIndex = (ushort)neighborList.Count;
            tmpNeighbors[i].Clear();
        }

        vertices = tmpVertices.ToArray();

        tmpIndices.Clear();
        tmpVertices.Clear();

        UpdateShape();
    }

    private ConvexHullShape()
    {
    }

    /// <summary>
    /// Creates a clone of the convex hull shape. Note that the underlying data structure is shared among instances.
    /// </summary>
    /// <returns>A new instance of the ConvexHullShape class that shares the same underlying data structure as the
    /// original instance.
    /// </returns>
    public ConvexHullShape Clone()
    {
        ConvexHullShape result = new()
        {
            neighborList = neighborList,
            vertices = vertices,
            indices = indices,
            cachedBoundingBox = cachedBoundingBox,
            cachedCenter = cachedCenter,
            cachedInertia = cachedInertia,
            cachedMass = cachedMass,
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
            UpdateShape();
        }
    }

    public void UpdateShape()
    {
        CalculateMassInertia();
        CalcInitBox();
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        inertia = cachedInertia;
        com = cachedCenter;
        mass = cachedMass;
    }

    public void CalculateMassInertia()
    {
        cachedCenter = JVector.Zero;
        cachedInertia = JMatrix.Zero;
        cachedMass = 0;

        const Real a = (Real)(1.0 / 60.0);
        const Real b = (Real)(1.0 / 120.0);
        JMatrix canonicalInertia = new(a, b, b, b, a, b, b, b, a);

        JVector pointWithin = JVector.Zero;

        for (int i = 0; i < vertices.Length; i++)
        {
            pointWithin += vertices[i].Vertex;
        }

        pointWithin = pointWithin * ((Real)1.0 / vertices.Length) + shifted;

        foreach (CHullTriangle tri in indices)
        {
            JVector column0 = vertices[tri.IndexA].Vertex + shifted;
            JVector column1 = vertices[tri.IndexB].Vertex + shifted;
            JVector column2 = vertices[tri.IndexC].Vertex + shifted;

            // check winding
            {
                JVector normal = (column1 - column0) % (column2 - column0);
                Real dot = JVector.Dot(normal, column0 - pointWithin);
                if (dot < (Real)0.0)
                {
                    (column0, column1) = (column1, column0);
                }
            }

            JMatrix transformation = new(
                column0.X, column1.X, column2.X,
                column0.Y, column1.Y, column2.Y,
                column0.Z, column1.Z, column2.Z);

            Real detA = transformation.Determinant();

            JMatrix tetrahedronInertia = JMatrix.Multiply(transformation * canonicalInertia * JMatrix.Transpose(transformation), detA);

            JVector tetrahedronCom = (Real)(1.0 / 4.0) * (column0 + column1 + column2);
            Real tetrahedronMass = (Real)(1.0 / 6.0) * detA;

            cachedInertia += tetrahedronInertia;
            cachedCenter += tetrahedronMass * tetrahedronCom;
            cachedMass += tetrahedronMass;
        }

        cachedInertia = JMatrix.Multiply(JMatrix.Identity, cachedInertia.Trace()) - cachedInertia;
        cachedCenter *= (Real)1.0 / cachedMass;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        JVector halfSize = (Real)0.5 * (cachedBoundingBox.Max - cachedBoundingBox.Min);
        JVector center = (Real)0.5 * (cachedBoundingBox.Max + cachedBoundingBox.Min);

        JMatrix ori = JMatrix.CreateFromQuaternion(orientation);
        JMatrix.Absolute(in ori, out JMatrix abs);
        JVector.Transform(halfSize, abs, out JVector temp);
        JVector.Transform(center, orientation, out JVector temp2);

        box.Max = temp;
        JVector.Negate(temp, out box.Min);

        JVector.Add(box.Min, position + temp2, out box.Min);
        JVector.Add(box.Max, position + temp2, out box.Max);
    }

    private void CalcInitBox()
    {
        JVector vec = JVector.UnitX;
        InternalSupportMap(vec, out JVector res);
        cachedBoundingBox.Max.X = res.X;

        vec = JVector.UnitY;
        InternalSupportMap(vec, out res);
        cachedBoundingBox.Max.Y = res.Y;

        vec = JVector.UnitZ;
        InternalSupportMap(vec, out res);
        cachedBoundingBox.Max.Z = res.Z;

        vec = -JVector.UnitX;
        InternalSupportMap(vec, out res);
        cachedBoundingBox.Min.X = res.X;

        vec = -JVector.UnitY;
        InternalSupportMap(vec, out res);
        cachedBoundingBox.Min.Y = res.Y;

        vec = -JVector.UnitZ;
        InternalSupportMap(vec, out res);
        cachedBoundingBox.Min.Z = res.Z;
    }

    private ushort InternalSupportMap(in JVector direction, out JVector result)
    {
        ushort current = 0;
        Real dotProduct = JVector.Dot(vertices[current].Vertex, direction);

        main:
        bool needsVerify = false;
        JVector verifyDir = JVector.Arbitrary;

        var min = vertices[current].NeighborMinIndex;
        var max = vertices[current].NeighborMaxIndex;

        const Real epsilonIncrement = 1e-12f;

        for (int i = min; i < max; i++)
        {
            ushort nb = neighborList[i];
            Real nbProduct = JVector.Dot(vertices[nb].Vertex, direction);

            if (MathR.Abs(nbProduct - dotProduct) < epsilonIncrement)
            {
                verifyDir = vertices[nb].Vertex - vertices[current].Vertex;
                needsVerify = true;
            }

            if (nbProduct > dotProduct + epsilonIncrement)
            {
                // no need to find the "best" neighbor - as soon as we found a better
                // candidate we move there.
                dotProduct = nbProduct;
                current = nb;
                goto main;
            }
        }

        // A secondary hill climbing algorithm in case of a plateau.
        if (needsVerify)
        {
            Real d0 = JVector.Dot(verifyDir, vertices[current].Vertex);

            secondary:
            min = vertices[current].NeighborMinIndex;
            max = vertices[current].NeighborMaxIndex;

            for (int i = min; i < max; i++)
            {
                ushort nb = neighborList[i];
                Real nbProduct = JVector.Dot(vertices[nb].Vertex, direction);

                if (nbProduct > dotProduct + epsilonIncrement)
                {
                    dotProduct = nbProduct;
                    current = nb;
                    goto main;
                }

                if (MathR.Abs(nbProduct - dotProduct) < epsilonIncrement)
                {
                    Real d1 = JVector.Dot(verifyDir, vertices[nb].Vertex);

                    if (d1 > d0 + epsilonIncrement)
                    {
                        d0 = d1;
                        current = nb;
                        goto secondary;
                    }
                }
            }
        }

        result = vertices[current].Vertex + shifted;
        return current;
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        InternalSupportMap(direction, out result);
    }

    public override void GetCenter(out JVector point)
    {
        point = cachedCenter;
    }
}