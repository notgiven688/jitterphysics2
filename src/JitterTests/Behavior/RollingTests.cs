namespace JitterTests.Behavior;

/// <summary>
/// A body rolling without slipping on a level floor, with damping and restitution switched off,
/// must keep its speed. A contact point displaced sideways from the point below the center turns a
/// vertical contact impulse into a torque, and friction then couples that torque into a drift of the
/// rolling speed. With the original MPR point reconstruction a sphere starting at 0.1 m/s reached
/// about 0.18 m/s after thirty seconds; a lying capsule or cylinder, whose auxiliary contact points
/// were sampled beside the resting line and whose cached contacts lagged behind it, braked, sped up
/// or sank.
/// </summary>
public class RollingTests
{
    private const Real Dt = (Real)0.01;
    private const Real Speed = (Real)0.1;
    private const int Seconds = 30;

    private static World CreateWorld()
    {
        World world = new()
        {
            AllowDeactivation = false,
            SolverIterations = (6, 4),
            SubstepCount = 1,
            Gravity = new JVector(0, (Real)(-9.81), 0)
        };

        RigidBody floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(20));
        floor.Position = new JVector(0, -10, 0);
        floor.MotionType = MotionType.Static;
        floor.Friction = (Real)0.2;
        floor.Restitution = 0;

        return world;
    }

    private static RigidBody CreateRoller(World world, RigidBodyShape shape, in JQuaternion orientation, Real radius)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(shape);
        body.Orientation = orientation;
        // Start a millimetre inside the floor. A body placed exactly touching keeps zero penetration,
        // where the narrow phase flickers between hit and miss; every missed step leaves the cached
        // contacts, body-fixed points that have rotated behind the contact, carrying the weight.
        body.Position = new JVector(0, radius - (Real)1e-3, 0);
        body.SetMassInertia(1);
        body.Friction = (Real)0.2;
        body.Restitution = 0;
        body.Damping = (0, 0);
        return body;
    }

    /// <summary>
    /// Lets the body settle for a second, starts it rolling along +X about <paramref name="spinAxis"/>
    /// and returns the largest deviation of its speed from the initial one over the run.
    /// </summary>
    private static Real RollAndMeasureDrift(World world, RigidBody body, Real radius, in JVector spinAxis)
    {
        Helper.AdvanceWorld(world, 1, Dt, false);

        body.Velocity = new JVector(Speed, 0, 0);
        body.AngularVelocity = spinAxis * (Speed / radius);

        Real drift = 0;
        int steps = (int)(Seconds / Dt);
        for (int i = 0; i < steps; i++)
        {
            world.Step(Dt, false);
            drift = MathR.Max(drift, MathR.Abs(body.Velocity.Length() - Speed));
        }

        TestContext.Out.WriteLine($"{body.Shapes[0].GetType().Name}: final speed {body.Velocity.Length():F7}, worst drift {drift:E2}");
        return drift;
    }

    [TestCase]
    public void Sphere_KeepsRollingSpeed()
    {
        using World world = CreateWorld();
        const Real radius = (Real)0.5;
        RigidBody sphere = CreateRoller(world, new SphereShape(radius), JQuaternion.Identity, radius);

        Real drift = RollAndMeasureDrift(world, sphere, radius, -JVector.UnitZ);
        Assert.That(drift, Is.LessThan((Real)1e-4));
    }

    // Lying capsules and cylinders rest on a line. The manifold must place its auxiliary points on
    // that line rather than 0.01 rad around the curved side, and the contact cache must not keep
    // body-fixed anchors that have rolled away from the contact.
    [TestCase]
    public void Capsule_KeepsRollingSpeed()
    {
        using World world = CreateWorld();
        const Real radius = (Real)0.25;
        // Capsules are built along Y. Lay the axis along Z so the capsule rolls along X.
        JQuaternion lying = JQuaternion.CreateRotationX(MathR.PI * (Real)0.5);
        RigidBody capsule = CreateRoller(world, new CapsuleShape(radius, 1), lying, radius);

        Real drift = RollAndMeasureDrift(world, capsule, radius, -JVector.UnitZ);
        Assert.That(drift, Is.LessThan((Real)1e-4));
    }

    [TestCase]
    public void Cylinder_KeepsRollingSpeed()
    {
        using World world = CreateWorld();
        const Real radius = (Real)0.25;
        JQuaternion lying = JQuaternion.CreateRotationX(MathR.PI * (Real)0.5);
        RigidBody cylinder = CreateRoller(world, new CylinderShape(1, radius), lying, radius);

        Real drift = RollAndMeasureDrift(world, cylinder, radius, -JVector.UnitZ);
        Assert.That(drift, Is.LessThan((Real)1e-4));
    }
}
