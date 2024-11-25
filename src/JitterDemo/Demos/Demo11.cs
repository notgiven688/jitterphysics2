using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo11 : IDemo
{
    public string Name => "Double Pendulum";

    private RigidBody b0 = null!, b1 = null!;

    private World world = null!;

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        b0 = world.CreateRigidBody();
        b0.AddShape(new SphereShape(0.2d));
        b0.Position = new JVector(0, 12, 0);
        b0.Velocity = new JVector(0.01d, 0, 0);
        b0.DeactivationTime = TimeSpan.MaxValue;

        b1 = world.CreateRigidBody();
        b1.AddShape(new SphereShape(0.2d));
        b1.Velocity = new JVector(0, 0, 0.01d);
        b1.Position = new JVector(0, 13, 0);

        var c0 = world.CreateConstraint<DistanceLimit>(world.NullBody, b0);
        c0.Initialize(new JVector(0, 8, 0), b0.Position);

        var c1 = world.CreateConstraint<DistanceLimit>(b0, b1);
        c1.Initialize(b0.Position, b1.Position);

        world.SubstepCount = 10;
        world.SolverIterations = (2, 2);

        b0.Damping = (0, 0);
        b1.Damping = (0, 0);
    }

    public void Draw()
    {
        double ekin = 0.5d * (b0.Velocity.LengthSquared() + b1.Velocity.LengthSquared());
        double epot = -world.Gravity.Y * (b0.Position.Y + b1.Position.Y);

        Console.WriteLine($"Energy: {ekin + epot} Kinetic {ekin}; Potential {epot}");

        var dr = RenderWindow.Instance.DebugRenderer;
        dr.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(new JVector(0, 8, 0)), Conversion.FromJitter(b0.Position));
        dr.PushLine(DebugRenderer.Color.White, Conversion.FromJitter(b0.Position), Conversion.FromJitter(b1.Position));
    }
}