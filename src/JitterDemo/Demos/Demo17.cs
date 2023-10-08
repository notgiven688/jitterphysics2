using System.Collections.Generic;
using System.Linq;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo17 : IDemo, ICleanDemo
{
    public string Name => "Cloth";

    private Playground pg = null!;
    private SoftBodyCloth cloth = null!;
    private World world = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        const int len = 30;
        const float scale = 0.3f;

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
                tris.Add(new JTriangle(v0, v1, v2));
                tris.Add(new JTriangle(v1, v2, v3));
                tris.Add(new JTriangle(v0, v2, v3));
            }
        }

        var b0 = world.CreateRigidBody();
        b0.Position = new JVector(-1, 10, 0);
        b0.AddShape(new BoxShape(1));
        b0.Orientation = JMatrix.CreateRotationX(0.4f);

        var b1 = world.CreateRigidBody();
        b1.Position = new JVector(0, 10, 0);
        b1.AddShape(new CapsuleShape(0.4f));
        b1.Orientation = JMatrix.CreateRotationX(1f);

        var b2 = world.CreateRigidBody();
        b2.Position = new JVector(1, 11, 0);
        b2.AddShape(new SphereShape(0.5f));

        cloth = new SoftBodyCloth(world, tris);

        world.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
        world.BroadPhaseFilter = new BroadPhaseCollisionFilter(world);

        RigidBody fb0 = cloth.Points.OrderByDescending(item => +item.Position.X + item.Position.Z).First();
        var c0 = world.CreateConstraint<BallSocket>(fb0, world.NullBody);
        c0.Initialize(fb0.Position);

        RigidBody fb1 = cloth.Points.OrderByDescending(item => +item.Position.X - item.Position.Z).First();
        var c1 = world.CreateConstraint<BallSocket>(fb1, world.NullBody);
        c1.Initialize(fb1.Position);

        RigidBody fb2 = cloth.Points.OrderByDescending(item => -item.Position.X + item.Position.Z).First();
        var c2 = world.CreateConstraint<BallSocket>(fb2, world.NullBody);
        c2.Initialize(fb2.Position);

        RigidBody fb3 = cloth.Points.OrderByDescending(item => -item.Position.X - item.Position.Z).First();
        var c3 = world.CreateConstraint<BallSocket>(fb3, world.NullBody);
        c3.Initialize(fb3.Position);

        world.SolverIterations = 4;
        world.NumberSubsteps = 4;
    }

    public void Draw()
    {
        var dr = RenderWindow.Instance.DebugRenderer;

        foreach (var spring in cloth.Springs)
        {
            dr.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(spring.Body1.Position),
                Conversion.FromJitter(spring.Body2.Position));
        }
    }

    public void CleanUp()
    {
        cloth.Destroy();
    }
}