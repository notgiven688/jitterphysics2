using System;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

namespace JitterDemo;

public class SoftBodyCube : SoftBody
{
    public static readonly ValueTuple<int, int>[] Edges =
    {
        (0, 1), (1, 2), (2, 3), (3, 0),
        (4, 5), (5, 6), (6, 7), (7, 4),
        (0, 4), (1, 5), (2, 6), (3, 7)
    };

    public RigidBody Center { get; }

    public SoftBodyCube(World world, JVector offset) : base(world)
    {
        JVector[] vertices = new JVector[8];

        vertices[0] = new JVector(+1, -1, +1);
        vertices[1] = new JVector(+1, -1, -1);
        vertices[2] = new JVector(-1, -1, -1);
        vertices[3] = new JVector(-1, -1, +1);
        vertices[4] = new JVector(+1, +1, +1);
        vertices[5] = new JVector(+1, +1, -1);
        vertices[6] = new JVector(-1, +1, -1);
        vertices[7] = new JVector(-1, +1, +1);

        for (int i = 0; i < 8; i++)
        {
            var rb = world.CreateRigidBody();
            rb.SetMassInertia(JMatrix.Zero, 5.0d, true);
            rb.Position = vertices[i] + offset;
            Vertices.Add(rb);
        }

        SoftBodyTetrahedron[] tetrahedra = new SoftBodyTetrahedron[5];
        tetrahedra[0] = new SoftBodyTetrahedron(this, Vertices[0], Vertices[1], Vertices[5], Vertices[2]);
        tetrahedra[1] = new SoftBodyTetrahedron(this, Vertices[2], Vertices[5], Vertices[6], Vertices[7]);
        tetrahedra[2] = new SoftBodyTetrahedron(this, Vertices[3], Vertices[0], Vertices[2], Vertices[7]);
        tetrahedra[3] = new SoftBodyTetrahedron(this, Vertices[0], Vertices[4], Vertices[5], Vertices[7]);
        tetrahedra[4] = new SoftBodyTetrahedron(this, Vertices[0], Vertices[2], Vertices[5], Vertices[7]);

        for (int i = 0; i < 5; i++)
        {
            tetrahedra[i].UpdateWorldBoundingBox();
            world.DynamicTree.AddProxy(tetrahedra[i]);
            Shapes.Add(tetrahedra[i]);
        }

        Center = world.CreateRigidBody();
        Center.Position = offset;
        Center.SetMassInertia(JMatrix.Identity * 0.05d, 0.1d);

        for (int i = 0; i < 8; i++)
        {
            var constraint = world.CreateConstraint<BallSocket>(Center, Vertices[i]);
            constraint.Initialize(Vertices[i].Position);
            constraint.Softness = 1;
        }
    }
}