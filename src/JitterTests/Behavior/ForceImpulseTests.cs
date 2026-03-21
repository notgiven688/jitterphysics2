namespace JitterTests.Behavior;

/// <summary>
/// Tests for AddForce and ApplyImpulse behavior.
/// </summary>
public class ForceImpulseTests
{
    // -------------------------------------------------------------------------
    // ApplyImpulse — immediate velocity change, no step required
    // -------------------------------------------------------------------------

    [TestCase]
    public void ApplyImpulse_ChangesVelocityImmediately()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.ApplyImpulse(new JVector(1, 0, 0));
        Assert.That(body.Velocity.X, Is.GreaterThan(0));
        world.Dispose();
    }

    [TestCase]
    public void ApplyImpulse_ScaledByInverseMass()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var impulse = new JVector(10, 0, 0);
        body.ApplyImpulse(impulse);
        var expectedVelocityX = impulse.X / body.Mass;
        Assert.That(body.Velocity.X, Is.EqualTo(expectedVelocityX).Within(1e-4f));
        world.Dispose();
    }

    [TestCase]
    public void ApplyImpulse_WithPosition_AlsoChangesAngularVelocity()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        // Apply impulse off-center to produce angular velocity
        body.ApplyImpulse(new JVector(0, 1, 0), new JVector(1, 0, 0));
        Assert.That(body.AngularVelocity.LengthSquared(), Is.GreaterThan(0));
        world.Dispose();
    }

    [TestCase]
    public void ApplyImpulse_OnKinematic_HasNoEffect()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.MotionType = MotionType.Kinematic;
        body.ApplyImpulse(new JVector(1, 0, 0));
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void ApplyImpulse_OnStatic_HasNoEffect()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.MotionType = MotionType.Static;
        body.ApplyImpulse(new JVector(1, 0, 0));
        // Static bodies have InverseMass == 0; velocity stays zero
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void ApplyImpulse_ZeroImpulse_HasNoEffect()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.ApplyImpulse(JVector.Zero);
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // AddForce — accumulated and applied next step
    // -------------------------------------------------------------------------

    [TestCase]
    public void AddForce_AccumulatesInForceProperty()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var force = new JVector(10, 0, 0);
        body.AddForce(force);
        Assert.That(body.Force.X, Is.EqualTo(force.X).Within(1e-5f));
        world.Dispose();
    }

    [TestCase]
    public void AddForce_Accumulates_MultipleCalls()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.AddForce(new JVector(1, 0, 0));
        body.AddForce(new JVector(2, 0, 0));
        Assert.That(body.Force.X, Is.EqualTo(3).Within(1e-5f));
        world.Dispose();
    }

    [TestCase]
    public void AddForce_IsResetAfterStep()
    {
        var world = new World();
        world.AllowDeactivation = false;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.AddForce(new JVector(100, 0, 0));
        world.Step(1f / 60f, false);
        Assert.That(body.Force, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void AddForce_ChangesVelocityAfterStep()
    {
        // Force applied before step N is converted to DeltaVelocity in UpdateBodies at
        // the end of step N, and applied to Velocity at the start of step N+1.
        // Two steps are therefore needed to observe the velocity change.
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        world.Step(1f / 60f, false);   // warmup: body active, no forces yet
        body.AddForce(new JVector(100, 0, 0));
        world.Step(1f / 60f, false);   // DeltaVelocity computed from Force
        world.Step(1f / 60f, false);   // DeltaVelocity applied to Velocity
        Assert.That(body.Velocity.X, Is.GreaterThan(0));
        world.Dispose();
    }

    [TestCase]
    public void AddForce_OnKinematic_HasNoEffect()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.MotionType = MotionType.Kinematic;
        body.AddForce(new JVector(100, 0, 0));
        Assert.That(body.Force, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void AddForce_OnStatic_HasNoEffect()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.MotionType = MotionType.Static;
        body.AddForce(new JVector(100, 0, 0));
        Assert.That(body.Force, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void AddForce_WithPosition_AccumulatesTorque()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        // Force applied off-center should produce torque
        body.AddForce(new JVector(0, 10, 0), new JVector(1, 0, 0));
        Assert.That(body.Torque.LengthSquared(), Is.GreaterThan(0));
        world.Dispose();
    }

    [TestCase]
    public void AddForce_SleepingBody_WakeupFalse_HasNoEffect()
    {
        var world = new World();
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(body.IsActive, Is.False);

        body.AddForce(new JVector(10, 0, 0), wakeup: false);

        Assert.That(body.Force, Is.EqualTo(JVector.Zero));
        Assert.That(body.IsActive, Is.False);
        world.Dispose();
    }

    [TestCase]
    public void AddForce_SleepingBody_WakeupTrue_QueuesForceAndReactivatesNextStep()
    {
        var world = new World();
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(body.IsActive, Is.False);

        body.AddForce(new JVector(10, 0, 0), wakeup: true);

        Assert.That(body.Force.X, Is.EqualTo(10).Within(1e-6f));
        Assert.That(body.IsActive, Is.False);

        world.Step(1f / 100f, false);
        Assert.That(body.IsActive, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void ApplyImpulse_SleepingBody_WakeupFalse_HasNoEffect()
    {
        var world = new World();
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(body.IsActive, Is.False);

        body.ApplyImpulse(new JVector(10, 0, 0), wakeup: false);

        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.IsActive, Is.False);
        world.Dispose();
    }

    [TestCase]
    public void ApplyImpulse_SleepingBody_WakeupTrue_ChangesVelocityAndReactivatesNextStep()
    {
        var world = new World();
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(body.IsActive, Is.False);

        body.ApplyImpulse(new JVector(10, 0, 0), wakeup: true);

        Assert.That(body.Velocity.X, Is.GreaterThan(0));
        Assert.That(body.IsActive, Is.False);

        world.Step(1f / 100f, false);
        Assert.That(body.IsActive, Is.True);
        world.Dispose();
    }
}
