using System;
using System.Collections.Generic;
using Godot;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

internal sealed class CubedSoftBody : SoftBody
{
    private const float GridUnit = 0.25f;

    private static readonly Vector3I[] CubeCorners =
    [
        new(1, -1, 1), new(1, -1, -1), new(-1, -1, -1), new(-1, -1, 1),
        new(1, 1, 1), new(1, 1, -1), new(-1, 1, -1), new(-1, 1, 1)
    ];

    private static readonly (int A, int B, int C, int D)[] Tetrahedra =
    [
        (0, 1, 5, 2), (5, 2, 6, 7), (0, 3, 2, 7), (4, 0, 5, 7), (0, 2, 5, 7)
    ];

    private static readonly (Vector3I Normal, int A, int B, int C, int D, bool DiagonalAC)[] Faces =
    [
        (new(0, 1, 0), 5, 4, 7, 6, true),
        (new(0, -1, 0), 2, 3, 0, 1, false),
        (new(-1, 0, 0), 6, 7, 3, 2, true),
        (new(1, 0, 0), 1, 0, 4, 5, false),
        (new(0, 0, 1), 3, 7, 4, 0, true),
        (new(0, 0, -1), 1, 5, 6, 2, false)
    ];

    internal readonly record struct SurfaceVertex(int Index, Vector2 Uv);
    private readonly record struct Quad(SurfaceVertex A, SurfaceVertex B, SurfaceVertex C, SurfaceVertex D,
        bool DiagonalAC);

    private readonly SurfaceVertex[] surface;
    internal ReadOnlySpan<SurfaceVertex> Surface => surface;

    internal CubedSoftBody(World world, ReadOnlySpan<Vector3I> cells) : base(world)
    {
        var vertexIndices = new Dictionary<Vector3I, int>();
        var exposedFaces = new Dictionary<Vector3I, Quad>();
        var cellCorners = new int[cells.Length * CubeCorners.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            Vector3I cell = cells[i];
            Span<int> indices = cellCorners.AsSpan(i * CubeCorners.Length, CubeCorners.Length);
            for (int corner = 0; corner < CubeCorners.Length; corner++)
            {
                Vector3I point = cell + CubeCorners[corner];
                if (!vertexIndices.TryGetValue(point, out int index))
                {
                    index = Vertices.Count;
                    vertexIndices.Add(point, index);
                    RigidBody vertex = world.CreateRigidBody();
                    vertex.SetMassInertia(JSymmetricMatrix.Zero, 8f, true);
                    vertex.Position = ToWorld(point);
                    vertex.Damping = (0.02f, 0.002f);
                    Vertices.Add(vertex);
                }
                indices[corner] = index;
            }

            // Adjacent cells share a face center. Keep only the faces on the outside.
            foreach (var face in Faces)
            {
                Vector3I key = cell + face.Normal;
                if (exposedFaces.Remove(key)) continue;

                exposedFaces.Add(key, new Quad(
                    new SurfaceVertex(indices[face.A], ProjectUv(CubeCorners[face.A], face.Normal)),
                    new SurfaceVertex(indices[face.B], ProjectUv(CubeCorners[face.B], face.Normal)),
                    new SurfaceVertex(indices[face.C], ProjectUv(CubeCorners[face.C], face.Normal)),
                    new SurfaceVertex(indices[face.D], ProjectUv(CubeCorners[face.D], face.Normal)),
                    face.DiagonalAC));
            }
        }

        for (int i = 0; i < cells.Length; i++)
        {
            ReadOnlySpan<int> indices = cellCorners.AsSpan(i * CubeCorners.Length, CubeCorners.Length);
            foreach (var (a, b, c, d) in Tetrahedra)
                AddShape(new SoftBodyTetrahedron(this, Vertices[indices[a]], Vertices[indices[b]],
                    Vertices[indices[c]], Vertices[indices[d]]));
        }

        for (int i = 0; i < cells.Length; i++)
        {
            RigidBody center = world.CreateRigidBody();
            center.Position = ToWorld(cells[i]);
            center.SetMassInertia(JSymmetricMatrix.Identity, 1f);
            Vertices.Add(center);

            ReadOnlySpan<int> indices = cellCorners.AsSpan(i * CubeCorners.Length, CubeCorners.Length);
            foreach (int index in indices)
            {
                var joint = world.CreateConstraint<BallSocket>(center, Vertices[index]);
                joint.Initialize(Vertices[index].Position);
                joint.Softness = 0.1f;
                joint.Bias = 0.3f;
                AddSpring(joint);
            }
        }

        // The topology and UVs are constant. Only the vertex positions deform.
        surface = new SurfaceVertex[exposedFaces.Count * 6];
        int next = 0;
        foreach (Quad quad in exposedFaces.Values)
        {
            surface[next++] = quad.A;
            surface[next++] = quad.B;
            surface[next++] = quad.DiagonalAC ? quad.C : quad.D;
            surface[next++] = quad.DiagonalAC ? quad.A : quad.B;
            surface[next++] = quad.C;
            surface[next++] = quad.D;
        }
    }

    private static JVector ToWorld(Vector3I point) => new JVector(point.X, point.Y, point.Z) * GridUnit;

    private static Vector2 ProjectUv(Vector3I point, Vector3I normal)
    {
        var uv = normal.X != 0 ? new Vector2(point.Y, point.Z)
            : normal.Y != 0 ? new Vector2(point.X, point.Z)
            : new Vector2(point.X, point.Y);
        return (uv + Vector2.One) * 0.5f;
    }
}
