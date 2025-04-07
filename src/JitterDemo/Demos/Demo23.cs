using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo23 : IDemo
{
    public string Name => "Rotating Cube";

    private Playground pg = null!;
    private World world = null!;

    private RigidBody rotatingBox = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene(false);

        rotatingBox = world.CreateRigidBody();

        double size = 50;

        var bs0 = new TransformedShape(new BoxShape(size, 1, size), new JVector(0, +size / 2, 0));
        var bs1 = new TransformedShape(new BoxShape(size, 1, size), new JVector(0, -size / 2, 0));

        var bs2 = new TransformedShape(new BoxShape(1, size, size), new JVector(+size / 2,0, 0));
        var bs3 = new TransformedShape(new BoxShape(1, size, size), new JVector(-size / 2,0, 0));

        var bs4 = new TransformedShape(new BoxShape(size, size, 1), new JVector(0,0, +size / 2));
        var bs5 = new TransformedShape(new BoxShape(size, size, 1), new JVector(0,0, -size / 2));

        rotatingBox.AddShape([bs0, bs1, bs2, bs3, bs4, bs5]);
        rotatingBox.Tag = new RigidBodyTag(true);

        rotatingBox.IsStatic = true;

        rotatingBox.DeactivationTime = TimeSpan.MaxValue;
        rotatingBox.SetActivationState(true);

        for (int i = -10; i < 10; i++)
        {
            for (int e = -10; e < 10; e++)
            {
                for (int j = -10; j < 10; j++)
                {
                    RigidBody rb = world.CreateRigidBody();
                    rb.AddShape(new BoxShape(1.5d));
                    rb.Position = new JVector(i, e, j) * 2;
                }
            }
        }
    }

    public void Draw()
    {
        rotatingBox.AngularVelocity = new JVector(0.14d, 0.02d, 0.03d);
    }
}