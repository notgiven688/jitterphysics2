using Jitter2.Dynamics.Constraints;

namespace JitterTests.Regression;

public class HistoricalRegressionTests
{
    [TestCase]
    public void Velocity_SetToZero_OnSleepingBody_DoesNotWakeBody_ButStoresZero()
    {
        var world = new World
        {
            Gravity = JVector.Zero
        };

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);
        body.DeactivationThreshold = ((Real)10.0, (Real)10.0);
        body.Velocity = new JVector(1, 0, 0);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(body.IsActive, Is.False);

        body.Velocity = JVector.Zero;

        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.IsActive, Is.False);
        world.Dispose();
    }

    [TestCase]
    public void AngularVelocity_SetToZero_OnSleepingBody_DoesNotWakeBody_ButStoresZero()
    {
        var world = new World
        {
            Gravity = JVector.Zero
        };

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);
        body.DeactivationThreshold = ((Real)10.0, (Real)10.0);
        body.AngularVelocity = new JVector(0, 1, 0);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(body.IsActive, Is.False);

        body.AngularVelocity = JVector.Zero;

        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.IsActive, Is.False);
        world.Dispose();
    }

    [TestCase]
    public void ConeLimit_Limit_RoundTripsAngles()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();

        var cone = world.CreateConstraint<ConeLimit>(bodyA, bodyB);
        var expected = AngularLimit.FromDegree(15, 60);
        cone.Initialize(JVector.UnitY, expected);

        var actual = cone.Limit;

        Assert.That(actual.From.Radian, Is.EqualTo(expected.From.Radian).Within((Real)1e-5));
        Assert.That(actual.To.Radian, Is.EqualTo(expected.To.Radian).Within((Real)1e-5));
        world.Dispose();
    }

    [TestCase]
    public void ConeLimit_Limit_UpperBoundAbovePi_Throws()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();

        var cone = world.CreateConstraint<ConeLimit>(bodyA, bodyB);
        cone.Initialize(JVector.UnitY, AngularLimit.FromDegree(0, 45));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cone.Limit = new AngularLimit(JAngle.FromRadian((Real)0.0), JAngle.FromRadian((Real)(MathR.PI + 0.1))));
        world.Dispose();
    }

    [TestCase]
    public void TwistAngle_Initialize_WithPureSwing_StartsAtZeroTwist()
    {
        var world = new World();

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Orientation = JQuaternion.CreateRotationX((Real)(MathR.PI / 2.0));

        var twist = world.CreateConstraint<TwistAngle>(bodyA, bodyB);
        twist.Initialize(JVector.UnitY, JVector.UnitZ);

        Assert.That(twist.Angle.Radian, Is.EqualTo((Real)0.0).Within((Real)1e-5));
        world.Dispose();
    }
}
