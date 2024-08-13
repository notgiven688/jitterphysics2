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
    public string Name => "Kinematic bodies";

    private Playground pg = null!;
    private World world = null!;
    private Player player = null!;

    private RigidBody platform = null!;

    private LinearMotor linearMotor = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene(true);

        var plankA = world.CreateRigidBody();
        plankA.AddShape(new BoxShape(20,0.1f,6));
        plankA.IsStatic = true;

        var plankC = world.CreateRigidBody();
        plankC.AddShape(new BoxShape(20,0.1f,6));
        plankC.IsStatic = true;

        plankA.Position = new JVector(-20, 13, 0);
        plankC.Position = new JVector(20, 16, 0);

        // (*) The mass is set as inverse mass, so we have a mass of 100 here.
        // Reduce the mass to make the body "more" kinematic, i.e. it behaves
        // more like an unstoppable object. Be careful when setting the mass to infinity (zero
        // inverse mass) - if Jitter detects a collision between and unstoppable object
        // (the platform) and an immovable object (static object) the solver explodes.

        platform = world.CreateRigidBody();
        platform.AddShape(new BoxShape(4,0.1f,6));
        platform.SetMassInertia(JMatrix.Zero, 0.01f, true); // (*)
        platform.AffectedByGravity = false;
        platform.Position = new JVector(0, 12, 0);

        player = new Player(world, new JVector(-20, 15, 0));
        player.Body.Orientation = JQuaternion.CreateRotationY(-MathF.PI / 2.0f);
    }

    public void Draw()
    {
        Keyboard kb = Keyboard.Instance;

        JVector path = new JVector(8.5f * MathF.Sin((float)pg.Time * 0.5f),
            14 + MathF.Cos((float)pg.Time * 0.5f) * 5.0f, 0);

        JVector delta = path - platform.Position;

        if (delta.LengthSquared() > 0.001f)
        {
            // setting velocities is absolutely fine, don't ever set
            // positions - the solver wont properly deal with it.
            platform.Velocity = delta;
        }

        if (kb.IsKeyDown(Keyboard.Key.Left)) player.SetAngularInput(-1.0f);
        else if (kb.IsKeyDown(Keyboard.Key.Right)) player.SetAngularInput(1.0f);
        else player.SetAngularInput(0.0f);

        if (kb.IsKeyDown(Keyboard.Key.Up)) player.SetLinearInput(-JVector.UnitZ);
        else if (kb.IsKeyDown(Keyboard.Key.Down)) player.SetLinearInput(JVector.UnitZ);
        else player.SetLinearInput(JVector.Zero);

        if (kb.IsKeyDown(Keyboard.Key.LeftControl)) player.Jump();
    }
}