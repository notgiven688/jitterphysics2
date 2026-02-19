using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo10 : IDemo
{
    public string Name => "Stacked Cubes";
    public string Description => "Tall stacks of cubes and cones using sub-stepping for stability.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        for (int i = 0; i < 32; i++)
        {
            var body = world.CreateRigidBody();

            body.Position = new JVector(0, 0.5f + i * 0.999f, 0);
            body.AddShape(new BoxShape(1));
            body.Damping = (0.002f, 0.002f);
        }

        for (int i = 0; i < 32; i++)
        {
            var body = world.CreateRigidBody();

            body.Position = new JVector(10, 0.5f + i * 0.999f, 0);
            body.AddShape(new TransformedShape(new ConeShape(), JVector.Zero, JMatrix.CreateScale(0.4f, 1, 1)));
            body.Damping = (0.002f, 0.002f);
        }

        world.SolverIterations = (4, 2);
        world.SubstepCount = 3;
    }
}