using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo19 : IDemo
{
    public void Draw()
    {
        var dr = pg.DebugRenderer;

        var origin = new JVector(0, 20, 0);

        JVector rayVector = new JVector(0, -1, 0);

        for (int i = 0; i < 10000; i++)
        {
            JVector dir = JVector.Transform(rayVector, JMatrix.CreateRotationX(0.1d + 0.0001d * i));
            dir = 60.0d * JVector.Transform(dir, JMatrix.CreateRotationY(0.004d * i));

            dr.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(origin),
                Conversion.FromJitter(origin + dir));

            bool hit = world.DynamicTree.RayCast(origin, dir, null, null, out IDynamicTreeProxy? shape,
                out JVector normal, out var frac);

            if (hit)
            {
                dr.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(origin + frac * dir), 0.2f);
            }
        }
    }

    public string Name => "RayCastTest";

    private Playground pg = null!;
    private World world = null!;

    private RigidBody box = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        box = world.CreateRigidBody();
        box.AddShape(new BoxShape(1));
        box.Position = new JVector(0, 0.5d, -6);
        box.IsStatic = true;
    }
}