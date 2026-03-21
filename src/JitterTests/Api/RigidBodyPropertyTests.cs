namespace JitterTests.Api;

/// <summary>
/// Tests for RigidBody property getters and setters in isolation.
/// </summary>
public class RigidBodyPropertyTests
{
    // -------------------------------------------------------------------------
    // Position
    // -------------------------------------------------------------------------

    [TestCase]
    public void Position_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = new JVector(1, 2, 3);
        body.Position = expected;
        Assert.That(body.Position, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void Position_SetToZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Position = new JVector(5, 5, 5);
        body.Position = JVector.Zero;
        Assert.That(body.Position, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void Position_SetNegative()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = new JVector(-10, -20, -30);
        body.Position = expected;
        Assert.That(body.Position, Is.EqualTo(expected));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Orientation
    // -------------------------------------------------------------------------

    [TestCase]
    public void Orientation_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = JQuaternion.CreateFromAxisAngle(JVector.UnitY, MathF.PI / 4);
        body.Orientation = expected;
        Assert.That(body.Orientation.X, Is.EqualTo(expected.X).Within(1e-5f));
        Assert.That(body.Orientation.Y, Is.EqualTo(expected.Y).Within(1e-5f));
        Assert.That(body.Orientation.Z, Is.EqualTo(expected.Z).Within(1e-5f));
        Assert.That(body.Orientation.W, Is.EqualTo(expected.W).Within(1e-5f));
        world.Dispose();
    }

    [TestCase]
    public void Orientation_SetToIdentity()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitX, MathF.PI / 2);
        body.Orientation = JQuaternion.Identity;
        Assert.That(body.Orientation, Is.EqualTo(JQuaternion.Identity));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Velocity
    // -------------------------------------------------------------------------

    [TestCase]
    public void Velocity_RoundTrip_Dynamic()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = new JVector(1, 2, 3);
        body.Velocity = expected;
        Assert.That(body.Velocity, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void Velocity_SetToZero_IsZero()
    {
        // Regression: velocity setter must store zero even when value is JVector.Zero.
        var world = new World();
        var body = world.CreateRigidBody();
        body.Velocity = new JVector(5, 0, 0);
        body.Velocity = JVector.Zero;
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void Velocity_SetNegative()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = new JVector(-3, -2, -1);
        body.Velocity = expected;
        Assert.That(body.Velocity, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void Velocity_OnKinematic_Allowed()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.MotionType = MotionType.Kinematic;
        var expected = new JVector(1, 0, 0);
        body.Velocity = expected;
        Assert.That(body.Velocity, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void Velocity_OnStatic_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;
        Assert.Throws<InvalidOperationException>(() => body.Velocity = new JVector(1, 0, 0));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // AngularVelocity
    // -------------------------------------------------------------------------

    [TestCase]
    public void AngularVelocity_RoundTrip_Dynamic()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = new JVector(0, MathF.PI, 0);
        body.AngularVelocity = expected;
        Assert.That(body.AngularVelocity, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void AngularVelocity_SetToZero_IsZero()
    {
        // Mirrors the velocity-zero regression for angular velocity.
        var world = new World();
        var body = world.CreateRigidBody();
        body.AngularVelocity = new JVector(0, 1, 0);
        body.AngularVelocity = JVector.Zero;
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void AngularVelocity_OnStatic_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;
        Assert.Throws<InvalidOperationException>(() => body.AngularVelocity = new JVector(0, 1, 0));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Force / Torque
    // -------------------------------------------------------------------------

    [TestCase]
    public void Force_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = new JVector(10, 0, 0);
        body.Force = expected;
        Assert.That(body.Force, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void Force_SetToZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Force = new JVector(10, 0, 0);
        body.Force = JVector.Zero;
        Assert.That(body.Force, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void Torque_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var expected = new JVector(0, 5, 0);
        body.Torque = expected;
        Assert.That(body.Torque, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void Torque_SetToZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Torque = new JVector(0, 5, 0);
        body.Torque = JVector.Zero;
        Assert.That(body.Torque, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Material / misc
    // -------------------------------------------------------------------------

    [TestCase]
    public void Friction_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Friction = 0.7f;
        Assert.That(body.Friction, Is.EqualTo(0.7f).Within(1e-6f));
        world.Dispose();
    }

    [TestCase]
    public void Friction_Negative_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.Friction = -0.1f);
        world.Dispose();
    }

    [TestCase]
    public void Restitution_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Restitution = 0.5f;
        Assert.That(body.Restitution, Is.EqualTo(0.5f).Within(1e-6f));
        world.Dispose();
    }

    [TestCase]
    public void Restitution_Negative_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.Restitution = -0.1f);
        world.Dispose();
    }

    [TestCase]
    public void Restitution_GreaterThanOne_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.Restitution = 1.1f);
        world.Dispose();
    }

    [TestCase]
    public void AffectedByGravity_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AffectedByGravity = false;
        Assert.That(body.AffectedByGravity, Is.False);
        body.AffectedByGravity = true;
        Assert.That(body.AffectedByGravity, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void Tag_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var tag = new object();
        body.Tag = tag;
        Assert.That(body.Tag, Is.SameAs(tag));
        world.Dispose();
    }

    [TestCase]
    public void Tag_CanBeCleared()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Tag = new object();
        body.Tag = null;
        Assert.That(body.Tag, Is.Null);
        world.Dispose();
    }

    [TestCase]
    public void Damping_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Damping = (0.1f, 0.2f);
        Assert.That(body.Damping.linear, Is.EqualTo(0.1f).Within(1e-6f));
        Assert.That(body.Damping.angular, Is.EqualTo(0.2f).Within(1e-6f));
        world.Dispose();
    }

    [TestCase]
    public void Damping_LinearNegative_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.Damping = (-0.1f, 0.2f));
        world.Dispose();
    }

    [TestCase]
    public void Damping_LinearGreaterThanOne_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.Damping = (1.1f, 0.2f));
        world.Dispose();
    }

    [TestCase]
    public void Damping_AngularNegative_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.Damping = (0.1f, -0.2f));
        world.Dispose();
    }

    [TestCase]
    public void Damping_AngularGreaterThanOne_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.Damping = (0.1f, 1.2f));
        world.Dispose();
    }

    [TestCase]
    public void DeactivationThreshold_RoundTrip()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.DeactivationThreshold = (0.3f, 0.4f);
        Assert.That(body.DeactivationThreshold.angular, Is.EqualTo(0.3f).Within(1e-6f));
        Assert.That(body.DeactivationThreshold.linear, Is.EqualTo(0.4f).Within(1e-6f));
        world.Dispose();
    }

    [TestCase]
    public void DeactivationThreshold_AngularNegative_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.DeactivationThreshold = (-0.3f, 0.4f));
        world.Dispose();
    }

    [TestCase]
    public void DeactivationThreshold_LinearNegative_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.DeactivationThreshold = (0.3f, -0.4f));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Damping — simulation behavior
    // -------------------------------------------------------------------------

    [TestCase]
    public void Damping_Linear_SlowsBodyOverTime()
    {
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Damping = (0.1f, 0f);
        body.Velocity = new JVector(10, 0, 0);
        var speedBefore = body.Velocity.X;
        Helper.AdvanceWorld(world, 1, 1f / 100f, false);
        Assert.That(body.Velocity.X, Is.LessThan(speedBefore));
        world.Dispose();
    }

    [TestCase]
    public void Damping_Angular_SlowsRotationOverTime()
    {
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Damping = (0f, 0.1f);
        body.AngularVelocity = new JVector(0, 5, 0);
        var angSpeedBefore = body.AngularVelocity.Y;
        Helper.AdvanceWorld(world, 1, 1f / 100f, false);
        Assert.That(body.AngularVelocity.Y, Is.LessThan(angSpeedBefore));
        world.Dispose();
    }

    [TestCase]
    public void Damping_Zero_DoesNotSlowBody()
    {
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Damping = (0f, 0f);
        body.Velocity = new JVector(10, 0, 0);
        Helper.AdvanceWorld(world, 1, 1f / 100f, false);
        Assert.That(body.Velocity.X, Is.EqualTo(10f).Within(1e-3f));
        world.Dispose();
    }
}
