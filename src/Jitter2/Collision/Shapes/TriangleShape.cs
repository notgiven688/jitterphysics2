/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a triangle shape defined by a reference to a <see cref="TriangleMesh"/> and an index.
/// </summary>
public class TriangleShape : RigidBodyShape
{
    /// <summary>
    /// The triangle mesh to which this triangle belongs.
    /// </summary>
    public readonly TriangleMesh Mesh;

    /// <summary>
    /// The index representing the position of the triangle within the mesh.
    /// </summary>
    public readonly int Index;

    /// <summary>
    /// Initializes a new instance of the TriangleShape class.
    /// </summary>
    /// <param name="mesh">The triangle mesh to which this triangle belongs.</param>
    /// <param name="index">The index representing the position of the triangle within the mesh.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mesh"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is negative or greater than or equal to the number of triangles in the mesh.
    /// </exception>
    public TriangleShape(TriangleMesh mesh, int index)
    {
        ArgumentNullException.ThrowIfNull(mesh, nameof(mesh));
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, mesh.Indices.Length, nameof(index));

        Mesh = mesh;
        Index = index;

        UpdateWorldBoundingBox();
    }

    /// <summary>
    /// Creates and returns all instances of type <see cref="TriangleShape"/>
    /// for as given <see cref="TriangleMesh"/>.
    /// </summary>
    public static IEnumerable<TriangleShape> CreateAllShapes(TriangleMesh mesh)
    {
        for (int index = 0; index < mesh.Indices.Length; index++)
        {
            yield return new TriangleShape(mesh, index);
        }
    }

    /// <exception cref="NotSupportedException">
    /// Always thrown because a triangle has no volume and therefore no mass properties.
    /// </exception>
    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        throw new NotSupportedException($"{nameof(TriangleShape)} has no mass properties. " +
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
        ref readonly var triangle = ref Mesh.Indices[Index];
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

    /// <inheritdoc/>
    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        const Real extraMargin = (Real)0.01;

        ref readonly var triangle = ref Mesh.Indices[Index];
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

    /// <inheritdoc/>
    public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        ref readonly var meshTriangle = ref Mesh.Indices[Index];

        var triangle = new JTriangle(Mesh.Vertices[meshTriangle.IndexA],
            Mesh.Vertices[meshTriangle.IndexB], Mesh.Vertices[meshTriangle.IndexC]);

        return triangle.RayIntersect(origin, direction, JTriangle.CullMode.BackFacing, out normal, out lambda);
    }

    /// <inheritdoc/>
    public override void GetCenter(out JVector point)
    {
        ref readonly var triangle = ref Mesh.Indices[Index];

        JVector a = Mesh.Vertices[triangle.IndexA];
        JVector b = Mesh.Vertices[triangle.IndexB];
        JVector c = Mesh.Vertices[triangle.IndexC];

        point = (Real)(1.0 / 3.0) * (a + b + c);
    }

    /// <inheritdoc/>
    public override void SupportMap(in JVector direction, out JVector result)
    {
        ref readonly var triangle = ref Mesh.Indices[Index];

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