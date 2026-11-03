using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo35 : IDemo
{
    public string Name => "Rolling line contacts";

    public string Description => "A sphere, a capsule and a cylinder start rolling at 1 m/s on a level floor with " +
                                 "friction but without damping, so an ideal roller keeps its speed.";

    public string Controls => "R - Launch again";

    private const float Speed = 1.0f;
    private const float LaunchX = -8.0f;

    private Playground pg = null!;
    private World world = null!;
    private bool allowDeactivation;

    private RigidBody sphere = null!;
    private RigidBody capsule = null!;
    private RigidBody cylinder = null!;

    public void Build(Playground pg)
    {
        this.pg = pg;
        world = pg.World;

        pg.AddFloor();

        // The rollers move slowly for a long time and must not fall asleep.
        allowDeactivation = world.AllowDeactivation;
        world.AllowDeactivation = false;
        world.SolverIterations = (6, 4);

        // Capsules and cylinders are built along Y. Lay them along Z so they roll along X.
        JQuaternion lying = JQuaternion.CreateRotationX(MathF.PI / 2.0f);

        sphere = CreateRoller(new SphereShape(0.5f), new Vector3(0.85f, 0.75f, 0.25f));
        capsule = CreateRoller(new CapsuleShape(0.25f, 1.0f), new Vector3(0.25f, 0.65f, 0.85f));
        cylinder = CreateRoller(new CylinderShape(1.0f, 0.25f), new Vector3(0.85f, 0.35f, 0.3f));

        capsule.Orientation = lying;
        cylinder.Orientation = lying;

        Launch();
    }

    private RigidBody CreateRoller(RigidBodyShape shape, in Vector3 color)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(shape);
        body.SetMassInertia(1.0f);
        body.Friction = 0.2f;
        body.Restitution = 0.0f;
        body.Damping = (0.0f, 0.0f);
        body.Tag = new RigidBodyTag(false) { Color = color };
        return body;
    }

    private void Launch()
    {
        Place(sphere, 0.5f, +3.0f);
        Place(capsule, 0.25f, 0.0f);
        Place(cylinder, 0.25f, -3.0f);
    }

    // Rolling without slipping along +X: the contact point is at rest when omega = -v / r about Z.
    // The body starts a millimetre inside the floor: placed exactly touching, it would keep zero
    // penetration, where the narrow phase flickers between hit and miss. Placed deeper, it would be
    // pushed out over the first steps and the sampled contact points would all sit at equal depth,
    // which hides the problem the demo is about.
    private static void Place(RigidBody body, float radius, float z)
    {
        body.Position = new JVector(LaunchX, radius - 0.001f, z);
        body.Velocity = new JVector(Speed, 0, 0);
        body.AngularVelocity = new JVector(0, 0, -Speed / radius);
    }

    public void DrawUpdate()
    {
        if (Keyboard.Instance.KeyPressBegin(Keyboard.Key.R)) Launch();

        Label(sphere, "Sphere");
        Label(capsule, "Capsule");
        Label(cylinder, "Cylinder");
    }

    private void Label(RigidBody body, string title)
    {
        float speed = body.Velocity.Length();
        pg.DrawWorldLabel(body.Position + JVector.UnitY * 0.9f, title,
            $"{speed:0.000} m/s, {(speed / Speed - 1.0f) * 100.0f:+0.0;-0.0} % since launch at {Speed:0.0} m/s");
    }

    public void CleanUp()
    {
        world.AllowDeactivation = allowDeactivation;
    }
}
