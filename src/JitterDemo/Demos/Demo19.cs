using Jitter2;
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
            JVector dir = JVector.Transform(rayVector, JMatrix.CreateRotationX(0.1f + 0.0001f * i));
            dir = 60.0f * JVector.Transform(dir, JMatrix.CreateRotationY(0.004f * i));

            dr.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(origin),
                Conversion.FromJitter(origin + dir));

            bool hit = world.Raycast(origin, dir, null, null, out Shape? shape,
                out JVector normal, out float frac);

            if (hit)
            {
                dr.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(origin + frac * dir), 0.2f);
            }
        }

        JVector offset = new JVector(0.1f, 0.2f, 0.3f);

        Raycast(new JVector(0, 0.5f, 0) + offset, box.Position - offset);
    }

    private void Raycast(JVector from, JVector to)
    {
        var dr = pg.DebugRenderer;
        bool hit = world.Raycast(from, to - from, null, null, out Shape? shape,
            out JVector normal, out float frac);

        dr.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(from),
            Conversion.FromJitter(to));

        if (hit)
        {
            dr.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(from + frac * (to - from)), 0.2f);
        }
    }

    public string Name => "RaycastTest";

    private Playground pg = null!;
    private World world = null!;

    private Shape testShape;

    private RigidBody box = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        testShape = new ConeShape(1f);
        pg.ResetScene();

        box = world.CreateRigidBody();
        box.AddShape(new BoxShape(1));
        box.Position = new JVector(0, 0.5f, -6);
        box.IsStatic = true;
    }
}