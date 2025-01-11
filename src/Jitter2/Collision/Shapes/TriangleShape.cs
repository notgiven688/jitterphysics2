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
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a single triangle within a mesh.
/// </summary>
public class TriangleShape : RigidBodyShape
{
    public readonly TriangleMesh Mesh;
    public readonly int Index;

    /// <summary>
    /// Initializes a new instance of the TriangleShape class.
    /// </summary>
    /// <param name="mesh">The triangle mesh to which this triangle belongs.</param>
    /// <param name="index">The index representing the position of the triangle within the mesh.</param>
    public TriangleShape(TriangleMesh mesh, int index)
    {
        Mesh = mesh;
        Index = index;

        UpdateWorldBoundingBox();
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        // This method is not supported for 2D objects in a 3D world as they have no mass/inertia.
        throw new NotSupportedException($"{nameof(TriangleShape)} has no mass properties." +
                                        $"If you encounter this while calling RigidBody.AddShape, " +
                                        $"call AddShape with setMassInertia set to false.");
    }

    /// <summary>
    /// Retrieves the vertices transformed to world space coordinates, as affected by the rigid body's transformation.
    /// </summary>
    /// <param name="a">The transformed coordinate of the first vertex.</param>
    /// <param name="b">The transformed coordinate of the second vertex.</param>
    /// <param name="c">The transformed coordinate of the third vertex.</param>
    public void GetWorldVertices(out JVector a, out JVector b, out JVector c)
    {
        ref var triangle = ref Mesh.Indices[Index];
        a = Mesh.Vertices[triangle.IndexA];
        b = Mesh.Vertices[triangle.IndexB];
        c = Mesh.Vertices[triangle.IndexC];

        if (RigidBody == null) return;

        ref JQuaternion orientation = ref RigidBody.Data.Orientation;
        ref JVector position = ref RigidBody.Data.Position;

        JVector.Transform(a, orientation, out a);
        JVector.Transform(b, orientation, out b);
        JVector.Transform(c, orientation, out c);

        a += position;
        b += position;
        c += position;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        const Real extraMargin = (Real)0.01;

        ref var triangle = ref Mesh.Indices[Index];
        var a = Mesh.Vertices[triangle.IndexA];
        var b = Mesh.Vertices[triangle.IndexB];
        var c = Mesh.Vertices[triangle.IndexC];

        JVector.Transform(a, orientation, out a);
        JVector.Transform(b, orientation, out b);
        JVector.Transform(c, orientation, out c);

        box = JBBox.SmallBox;

        box.AddPoint(a);
        box.AddPoint(b);
        box.AddPoint(c);

        // prevent a degenerate bounding box
        JVector extra = new JVector(extraMargin);
        box.Min += position - extra;
        box.Max += position + extra;
    }

    public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        ref var triangle = ref Mesh.Indices[Index];
        var a = Mesh.Vertices[triangle.IndexA];
        var b = Mesh.Vertices[triangle.IndexB];
        var c = Mesh.Vertices[triangle.IndexC];

        JVector u = b - a;
        JVector v = c - a;

        normal = v % u;
        Real it = (Real)1.0 / normal.LengthSquared();

        Real denominator = JVector.Dot(direction, normal);

        if (Math.Abs(denominator) < (Real)1e-06)
        {
            // triangle and ray are parallel
            lambda = 0;
            normal = JVector.Zero;
            return false;
        }

        lambda = JVector.Dot(a - origin, normal);
        if (lambda > (Real)0.0) return false;
        lambda /= denominator;

        // point where the ray intersects the plane of the triangle.
        JVector hitPoint = origin + lambda * direction;
        JVector at = a - hitPoint;

        JVector.Cross(u, at, out JVector tmp);
        Real gamma = JVector.Dot(tmp, normal) * it;
        JVector.Cross(at, v, out tmp);
        Real beta = JVector.Dot(tmp, normal) * it;
        Real alpha = (Real)1.0 - gamma - beta;

        normal *= MathR.Sqrt(it);
        return alpha > 0 && beta > 0 && gamma > 0;
    }

    public override void GetCenter(out JVector point)
    {
        ref var triangle = ref Mesh.Indices[Index];

        JVector a = Mesh.Vertices[triangle.IndexA];
        JVector b = Mesh.Vertices[triangle.IndexB];
        JVector c = Mesh.Vertices[triangle.IndexC];

        point = (Real)(1.0 / 3.0) * (a + b + c);
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        ref var triangle = ref Mesh.Indices[Index];

        JVector a = Mesh.Vertices[triangle.IndexA];
        JVector b = Mesh.Vertices[triangle.IndexB];
        JVector c = Mesh.Vertices[triangle.IndexC];

        Real min = JVector.Dot(a, direction);
        Real dot = JVector.Dot(b, direction);

        result = a;

        if (dot > min)
        {
            min = dot;
            result = b;
        }

        dot = JVector.Dot(c, direction);

        if (dot > min)
        {
            result = c;
        }
    }
}