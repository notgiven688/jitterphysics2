#pragma warning disable CS8602

using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo09 : IDemo
{
    public string Name => "Restitution and Friction";

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        world.SolverIterations = (20, 4);

        if (pg.FloorShape != null)
        {
            pg.FloorShape.RigidBody.Friction = 0.0d;
            pg.FloorShape.RigidBody.Restitution = 0.0d;
        }

        for (int i = 0; i < 11; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(0.5d));
            body.Position = new JVector(-10 + i * 1, 4, -10);
            body.Restitution = i * 0.1d;
            body.Damping = (0.001d, 0.001d);
        }

        for (int i = 0; i < 11; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(0.5d));
            body.Position = new JVector(2 + i, 0.25d, 0);
            body.Friction = 1.0d - i * 0.1d;
            body.Velocity = new JVector(0, 0, -10);
            body.Damping = (0.001d, 0.001d);
        }
    }

    public void Draw()
    {
    }
}