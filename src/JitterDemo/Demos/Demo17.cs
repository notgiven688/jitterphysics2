using System.Collections.Generic;
using System.Linq;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo17 : IDemo, ICleanDemo
{
    public string Name => "Cloth";

    private Playground pg = null!;
    private SoftBodyCloth cloth = null!;
    private World world = null!;

    private Renderer.Cloth clothRenderer = null!;
    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        const int len = 40;
        const float scale = 0.2f;
        const int leno2 = len / 2;
        
        List<JTriangle> tris = new();

        for (int i = 0; i < len; i++)
        {
            for (int e = 0; e < len; e++)
            {
                JVector v0 = new JVector((-leno2 + e + 0) * scale, 6, (-leno2 + i + 0) * scale);
                JVector v1 = new JVector((-leno2 + e + 0) * scale, 6, (-leno2 + i + 1) * scale);
                JVector v2 = new JVector((-leno2 + e + 1) * scale, 6, (-leno2 + i + 0) * scale);
                JVector v3 = new JVector((-leno2 + e + 1) * scale, 6, (-leno2 + i + 1) * scale);

                bool even = (e + i) % 2 == 0;

                if (even)
                {
                    tris.Add(new JTriangle(v0, v2, v1));
                    tris.Add(new JTriangle(v2, v3, v1));
                }
                else
                {
                    tris.Add(new JTriangle(v0, v3, v1));
                    tris.Add(new JTriangle(v0, v2, v3));
                }
            }
        }
        
        cloth = new SoftBodyCloth(world, tris);
        
        clothRenderer = pg.CSMRenderer.GetInstance<Cloth>();
        clothRenderer.SetIndices(cloth.Triangles.ToArray());
        SetUVCoordinates();
        
        var b0 = world.CreateRigidBody();
        b0.Position = new JVector(-1, 10, 0);
        b0.AddShape(new BoxShape(1));
        b0.Orientation = JQuaternion.CreateRotationX(0.4f);

        var b1 = world.CreateRigidBody();
        b1.Position = new JVector(0, 10, 0);
        b1.AddShape(new CapsuleShape(0.4f));
        b1.Orientation = JQuaternion.CreateRotationX(1f);

        var b2 = world.CreateRigidBody();
        b2.Position = new JVector(1, 11, 0);
        b2.AddShape(new SphereShape(0.5f));

        world.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
        world.BroadPhaseFilter = new BroadPhaseCollisionFilter(world);

        RigidBody fb0 = cloth.Vertices.OrderByDescending(item => +item.Position.X + item.Position.Z).First();
        var c0 = world.CreateConstraint<BallSocket>(fb0, world.NullBody);
        c0.Initialize(fb0.Position);

        RigidBody fb1 = cloth.Vertices.OrderByDescending(item => +item.Position.X - item.Position.Z).First();
        var c1 = world.CreateConstraint<BallSocket>(fb1, world.NullBody);
        c1.Initialize(fb1.Position);

        RigidBody fb2 = cloth.Vertices.OrderByDescending(item => -item.Position.X + item.Position.Z).First();
        var c2 = world.CreateConstraint<BallSocket>(fb2, world.NullBody);
        c2.Initialize(fb2.Position);

        RigidBody fb3 = cloth.Vertices.OrderByDescending(item => -item.Position.X - item.Position.Z).First();
        var c3 = world.CreateConstraint<BallSocket>(fb3, world.NullBody);
        c3.Initialize(fb3.Position);

        world.SolverIterations = 4;
        world.NumberSubsteps = 4;
    }

    private void SetUVCoordinates()
    {
        var vertices = clothRenderer.Vertices;
        
        for (int i = 0; i<cloth.Vertices.Count; i++)
        {
            ref var pos = ref cloth.Vertices[i].Data.Position;
            vertices[i].Texture = new Vector2(pos.X, pos.Z);
        }
    }

    private void UpdateRenderVertices()
    {
        var vertices = clothRenderer.Vertices;
        
        for (int i = 0; i<cloth.Vertices.Count; i++)
        {
            vertices[i].Position = Conversion.FromJitter(cloth.Vertices[i].Position);
        }
        
        clothRenderer.VerticesChanged();
    }

    public void Draw()
    {
        UpdateRenderVertices();
        clothRenderer.PushMatrix(Matrix4.Identity, Vector3.UnitY);
    }

    public void CleanUp()
    {
        cloth.Destroy();
    }
}