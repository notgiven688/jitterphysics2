using System;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

namespace JitterDemo;

public class SoftBodyCube : SoftBody
{
    public readonly ValueTuple<int, int>[] Edges = new ValueTuple<int, int>[12];

    public RigidBody Center { get; private set; }

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
            rb.SetMassInertia(JMatrix.Identity * 100000, 0.2f);
            rb.Position = vertices[i] + offset;
            Points.Add(rb);
        }

        SoftBodyTetrahedron[] tetrahedra = new SoftBodyTetrahedron[5];
        tetrahedra[0] = new SoftBodyTetrahedron(this, Points[0], Points[1], Points[5], Points[2]);
        tetrahedra[1] = new SoftBodyTetrahedron(this, Points[2], Points[5], Points[6], Points[7]);
        tetrahedra[2] = new SoftBodyTetrahedron(this, Points[3], Points[0], Points[2], Points[7]);
        tetrahedra[3] = new SoftBodyTetrahedron(this, Points[0], Points[4], Points[5], Points[7]);
        tetrahedra[4] = new SoftBodyTetrahedron(this, Points[0], Points[2], Points[5], Points[7]);

        for (int i = 0; i < 5; i++)
        {
            tetrahedra[i].UpdateWorldBoundingBox();
            world.AddShape(tetrahedra[i]);
            Shapes.Add(tetrahedra[i]);
        }

        Center = world.CreateRigidBody();
        Center.Position = offset;
        Center.SetMassInertia(JMatrix.Identity * 0.05f, 0.1f);

        for (int i = 0; i < 8; i++)
        {
            var constraint = world.CreateConstraint<BallSocket>(Center, Points[i]);
            constraint.Initialize(Points[i].Position);
            constraint.Softness = 1;
        }
        
        Edges[0] = new ValueTuple<int, int>(0, 1);
        Edges[1] = new ValueTuple<int, int>(1, 2);
        Edges[2] = new ValueTuple<int, int>(2, 3);
        Edges[3] = new ValueTuple<int, int>(3, 0);
        Edges[4] = new ValueTuple<int, int>(4, 5);
        Edges[5] = new ValueTuple<int, int>(5, 6);
        Edges[6] = new ValueTuple<int, int>(6, 7);
        Edges[7] = new ValueTuple<int, int>(7, 4);
        Edges[8] = new ValueTuple<int, int>(0, 4);
        Edges[9] = new ValueTuple<int, int>(1, 5);
        Edges[10] = new ValueTuple<int, int>(2, 6);
        Edges[11] = new ValueTuple<int, int>(3, 7);
    }
}