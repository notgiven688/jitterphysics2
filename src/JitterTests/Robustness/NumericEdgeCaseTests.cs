namespace JitterTests.Robustness;

public class NumericEdgeCaseTests
{
    [TestCase]
    public void Step_WithVerySmallDt_KeepsBodyStateFinite()
    {
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Velocity = new JVector(10, 20, 30);
        body.AngularVelocity = new JVector(4, 5, 6);

        world.Step((Real)1e-9, false);

        Assert.That(Real.IsFinite(body.Position.X));
        Assert.That(Real.IsFinite(body.Position.Y));
        Assert.That(Real.IsFinite(body.Position.Z));
        Assert.That(Real.IsFinite(body.Velocity.X));
        Assert.That(Real.IsFinite(body.Velocity.Y));
        Assert.That(Real.IsFinite(body.Velocity.Z));
        Assert.That(Real.IsFinite(body.AngularVelocity.X));
        Assert.That(Real.IsFinite(body.AngularVelocity.Y));
        Assert.That(Real.IsFinite(body.AngularVelocity.Z));
        world.Dispose();
    }

    [TestCase]
    public void TinyPositiveMass_ImpulseProducesFiniteVelocity()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.SetMassInertia((Real)1e-6);

        body.ApplyImpulse(new JVector(1, 0, 0));

        Assert.That(Real.IsFinite(body.Velocity.X));
        Assert.That(body.Velocity.X, Is.GreaterThan((Real)0.0));
        world.Dispose();
    }

    [TestCase]
    public void LargePosition_StepKeepsPositionFinite()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector((Real)1e6, (Real)(-1e6), (Real)1e6);
        body.Velocity = new JVector(100, -50, 25);

        world.Step(1f / 60f, false);

        Assert.That(Real.IsFinite(body.Position.X));
        Assert.That(Real.IsFinite(body.Position.Y));
        Assert.That(Real.IsFinite(body.Position.Z));
        world.Dispose();
    }
}
