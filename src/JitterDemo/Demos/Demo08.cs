using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo08 : IDemo
{
    public string Name => "Contact Manifold Test";

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        world.SolverIterations = (4, 4);

        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(new JVector(5, 0.5, 0.5)));
        body.Position = new JVector(0, 1, 0);
        body.IsStatic = true;

        var body2 = world.CreateRigidBody();
        body2.AddShape(new CylinderShape(0.5, 3.0));
        body2.Position = new JVector(0, 2.5, 0);
        body2.Friction = 0;
    }

    public void Draw()
    {
    }
}