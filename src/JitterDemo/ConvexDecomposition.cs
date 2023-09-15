using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class ConvexDecomposition<T> where T : MultiMesh
{
    private readonly World world;
    private readonly List<RigidBody> bodies = new();
    private JVector com = JVector.Zero;

    private readonly List<ConvexHullShape> shapesToAdd = new();

    public ConvexDecomposition(World world)
    {
        this.world = world;
    }

    public RigidBody Spawn(JVector position)
    {
        RigidBody body = world.CreateRigidBody();
        body.Position = position;
        
        foreach (ConvexHullShape s in shapesToAdd)
        {
            body.AddShape(s.Clone(), false);
        }

        body.SetMassInertia();
        bodies.Add(body);

        return body;
    }

    public void Load()
    {
        var csmInstance = RenderWindow.Instance.CSMRenderer.GetInstance<T>();
        Mesh mesh = csmInstance.mesh;

        float totalMass = 0.0f;

        foreach (var group in mesh.Groups)
        {
            List<JTriangle> hullTriangles = new();

            for (int i = group.FromInclusive; i < group.ToExlusive; i++)
            {
                ref TriangleVertexIndex tvi = ref mesh.Indices[i];

                JTriangle jt = new()
                {
                    V0 = Conversion.ToJitterVector(mesh.Vertices[tvi.T1].Position),
                    V1 = Conversion.ToJitterVector(mesh.Vertices[tvi.T2].Position),
                    V2 = Conversion.ToJitterVector(mesh.Vertices[tvi.T3].Position)
                };

                hullTriangles.Add(jt);
            }

            ConvexHullShape chs = new(hullTriangles);
            com += chs.GeometricCenter * chs.Mass;
            totalMass += chs.Mass;
            shapesToAdd.Add(chs);
        }

        com *= 1.0f / totalMass;

        foreach (Shape s in shapesToAdd)
        {
            ((ConvexHullShape)s).Shift = -com;
        }
    }

    public void Clear()
    {
        bodies.Clear();
    }

    public void PushMatrices()
    {
        var csmInstance = RenderWindow.Instance.CSMRenderer.GetInstance<T>();

        foreach (RigidBody body in bodies)
        {
            var mat = Conversion.FromJitter(body);
            Vector3 color = Vector3.Zero;
            //if (!body.Data.isActive) color = new Vector3(0.2f, 0.2f, 0.2f);
            csmInstance.PushMatrix(mat * MatrixHelper.CreateTranslation(Conversion.FromJitter(-com)), color);
        }
    }
}