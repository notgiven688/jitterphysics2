/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
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

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        const Real extraMargin = (Real)0.01;

        ref var triangle = ref Mesh.Indices[Index];
        var a = Mesh.Vertices[triangle.IndexA];
        var b = Mesh.Vertices[triangle.IndexB];
        var c = Mesh.Vertices[triangle.IndexC];

        JVector.Transform(a, orientation, out a);
        JVector.Transform(b, orientation, out b);
        JVector.Transform(c, orientation, out c);

        box = JBoundingBox.SmallBox;

        JBoundingBox.AddPointInPlace(ref box, a);
        JBoundingBox.AddPointInPlace(ref box, b);
        JBoundingBox.AddPointInPlace(ref box, c);

        // prevent a degenerate bounding box
        JVector extra = new JVector(extraMargin);
        box.Min += position - extra;
        box.Max += position + extra;
    }

    public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        ref var meshTriangle = ref Mesh.Indices[Index];

        var triangle = new JTriangle(Mesh.Vertices[meshTriangle.IndexA],
            Mesh.Vertices[meshTriangle.IndexB], Mesh.Vertices[meshTriangle.IndexC]);

        return triangle.RayIntersect(origin, direction, JTriangle.CullMode.BackFacing, out normal, out lambda);
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