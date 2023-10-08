using System.Collections.Generic;
using System.Linq;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

namespace JitterDemo;

public class SoftBodyCloth : SoftBody
{
    private struct Edge : IEqualityComparer<Edge>
    {
        public readonly ushort IndexA;
        public readonly ushort IndexB;

        public Edge(ushort u0, ushort u1)
        {
            IndexA = u0;
            IndexB = u1;
        }

        public bool Equals(Edge x, Edge y)
        {
            return (x.IndexA == y.IndexA && x.IndexB == y.IndexB) ||
                   (x.IndexB == y.IndexA && x.IndexA == y.IndexB);
        }

        public int GetHashCode(Edge obj)
        {
            return IndexA ^ IndexB;
        }
    }

    private struct Triangle
    {
        public Triangle(ushort u0, ushort u1, ushort u2)
        {
            IndexA = u0;
            IndexB = u1;
            IndexC = u2;
        }

        public readonly ushort IndexA;
        public readonly ushort IndexB;
        public readonly ushort IndexC;
    }

    private List<JVector> vertices = null!;
    private List<Triangle> triangles = null!;
    private List<Edge> edges = null!;

    private List<SpringConstraint> MatchConstraints = new();

    public SoftBodyCloth(World world, IEnumerable<JTriangle> triangles) : base(world)
    {
        this.world = world;
        LoadMesh(triangles);
        Build();
    }

    private void LoadMesh(IEnumerable<JTriangle> tris)
    {
        Dictionary<JVector, ushort> verts = new();
        HashSet<Edge> edgs = new();

        ushort AddVertex(in JVector vertex)
        {
            if (!verts.TryGetValue(vertex, out ushort ind))
            {
                ind = (ushort)verts.Count;
                verts.Add(vertex, ind);
            }

            return ind;
        }

        triangles = new List<Triangle>();

        foreach (var tri in tris)
        {
            ushort u0 = AddVertex(tri.V0);
            ushort u1 = AddVertex(tri.V1);
            ushort u2 = AddVertex(tri.V2);

            Triangle t = new Triangle(u0, u1, u2);
            triangles.Add(t);

            edgs.Add(new Edge(u0, u1));
            edgs.Add(new Edge(u0, u2));
            edgs.Add(new Edge(u1, u2));
        }

        vertices = verts.Keys.ToList();
        edges = edgs.ToList();
    }

    private void Build()
    {
        foreach (var vertex in vertices)
        {
            RigidBody body = world.CreateRigidBody();
            body.SetMassInertia(JMatrix.Identity * 1000, 0.1f);
            body.Position = vertex;
            this.Add(body);
        }

        foreach (var edge in edges)
        {
            var constraint = world.CreateConstraint<SpringConstraint>(Points[edge.IndexA], Points[edge.IndexB]);
            constraint.Initialize(Points[edge.IndexA].Position, Points[edge.IndexB].Position);
            constraint.Softness = 0.1f;
            this.Add(constraint);
        }

        foreach (var triangle in triangles)
        {
            var tri = new SoftBodyTriangle(this, Points[triangle.IndexA], Points[triangle.IndexB], Points[triangle.IndexC]);
            tri.UpdateWorldBoundingBox();
            world.AddShape(tri);
            this.Add(tri);
        }
    }
}