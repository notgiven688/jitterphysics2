using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo09 : IDemo
{
    public string Name => "Restitution and Friction";
    public string Description => "Varying restitution and friction values across rows of bodies.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        world.SolverIterations = (20, 4);

        if (pg.FloorShape != null)
        {
            pg.FloorShape.RigidBody.Friction = 0.0f;
            pg.FloorShape.RigidBody.Restitution = 0.0f;
        }

        for (int i = 0; i < 11; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(0.5f));
            body.Position = new JVector(-10 + i * 1, 4, -10);
            body.Restitution = i * 0.1f;
            body.Damping = (0.001f, 0.001f);
        }

        for (int i = 0; i < 11; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(0.5f));
            body.Position = new JVector(2 + i, 0.25f, 0);
            body.Friction = 1.0f - i * 0.1f;
            body.Velocity = new JVector(0, 0, -10);
            body.Damping = (0.001f, 0.001f);
        }
    }
}