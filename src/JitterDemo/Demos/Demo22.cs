using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo22 : IDemo
{
    private static class Curve
    {
        public static JVector Path(double time)
        {
            return new JVector(8.5d * Math.Sin(time * 0.5d),
                14 + Math.Cos(time * 0.5d) * 5.0d, 0);
        }

        public static JVector Derivative(double time)
        {
            return new JVector(4.25d * Math.Cos(time * 0.5d),
                -2.5d * Math.Sin(time * 0.5d), 0);
        }
    }

    public string Name => "Kinematic bodies";

    private Playground pg = null!;
    private World world = null!;
    private Player player = null!;

    private RigidBody platform = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene(true);

        var leftPlank = world.CreateRigidBody();
        leftPlank.AddShape(new BoxShape(20, 0.1d, 6));
        leftPlank.IsStatic = true;

        var rightPlank = world.CreateRigidBody();
        rightPlank.AddShape(new BoxShape(20, 0.1d, 6));
        rightPlank.IsStatic = true;

        leftPlank.Position = new JVector(-21, 13, 0);
        rightPlank.Position = new JVector(21, 16, 0);

        // (*) The mass is specified as the inverse mass, so an inverse mass of 0.01 corresponds to a mass of 100.
        // Lower the inverse mass (i.e., increase the mass) to make the body behave more kinematically,
        // akin to an unstoppable object. However, be cautious when setting the inverse mass to zero (infinite mass),
        // as it can cause the solver to fail if a collision occurs between an unstoppable object (e.g., a platform)
        // and an immovable object (e.g., a static object).

        platform = world.CreateRigidBody();
        platform.AddShape(new BoxShape(4, 0.1d, 6));
        platform.AddShape(new SphereShape(0.2d)); // guide to the eye
        platform.SetMassInertia(JMatrix.Zero, 0.01d, true); // (*)
        platform.AffectedByGravity = false;
        platform.Position = new JVector(0, 12, 0);

        player = new Player(world, new JVector(-20, 15, 0));
        player.Body.Orientation = JQuaternion.CreateRotationY(-MathF.PI / 2.0d);
    }

    public void Draw()
    {
        // What is happening in the following *two* lines of code?
        //
        // We want to platform to follow a path p(t). We can not set the position
        // directly, as it will just teleport the body in tiny steps, which
        // does not produce the kinematic physics we are after.
        //
        // We introduce k(t) and manually set the velocity of the platform to be
        //
        // v(t) = alpha(k(t) - x(t))
        // <=> x'(t) = alpha(k(t) - x(t))
        // <=> 1/alpha x'(t) + x(t) = k(t)
        //
        // where alpha is a constant.
        //
        // If we now set k(t) to be
        //  ______________________________
        // | k(t) = 1/alpha p'(t) + p(t) |
        // -------------------------------
        // , we arrive at
        //
        // x(t) = C*exp(-alpha*t) + p(t)
        //
        // where the first term describes the offset of the target path p(t)
        // and the position of the platform x(t). This term vanishes with larger t.

        JVector k = Curve.Derivative((float)pg.Time) + Curve.Path((float)pg.Time);
        platform.Velocity = k - platform.Position;

        // Draw the curve

        const int stepMax = 100;
        const double maxTime = 4.0d * MathF.PI;

        for (int step = 0; step < stepMax; step++)
        {
            double ta = maxTime / stepMax * step;
            double tb = maxTime / stepMax * (step + 1);

            pg.DebugRenderer.PushLine(DebugRenderer.Color.Green,
                Conversion.FromJitter(Curve.Path(ta)),
                Conversion.FromJitter(Curve.Path(tb)));
        }

        // Player handling with keyboard

        Keyboard kb = Keyboard.Instance;

        if (kb.IsKeyDown(Keyboard.Key.Left)) player.SetAngularInput(-1.0d);
        else if (kb.IsKeyDown(Keyboard.Key.Right)) player.SetAngularInput(1.0d);
        else player.SetAngularInput(0.0d);

        if (kb.IsKeyDown(Keyboard.Key.Up)) player.SetLinearInput(-JVector.UnitZ);
        else if (kb.IsKeyDown(Keyboard.Key.Down)) player.SetLinearInput(JVector.UnitZ);
        else player.SetLinearInput(JVector.Zero);

        if (kb.IsKeyDown(Keyboard.Key.LeftControl)) player.Jump();
    }
}