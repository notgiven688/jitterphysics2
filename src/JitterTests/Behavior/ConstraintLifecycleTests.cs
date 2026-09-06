using Jitter2.Dynamics.Constraints;

namespace JitterTests.Behavior;

public class ConstraintLifecycleTests
{
    [TestCase]
    public void CreateConstraint_RegistersOnBothBodies_AndAddsConnection()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();

        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);

        Assert.That(bodyA.Constraints, Does.Contain(constraint));
        Assert.That(bodyB.Constraints, Does.Contain(constraint));
        Assert.That(bodyA.Connections, Does.Contain(bodyB));
        Assert.That(bodyB.Connections, Does.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void RemoveConstraint_UnregistersFromBothBodies_AndRemovesConnection()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();

        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);

        Assert.That(constraint.IsValid, Is.True);
        world.Remove(constraint);

        Assert.That(constraint.IsValid, Is.False);
        Assert.That(bodyA.Constraints, Does.Not.Contain(constraint));
        Assert.That(bodyB.Constraints, Does.Not.Contain(constraint));
        Assert.That(bodyA.Connections, Does.Not.Contain(bodyB));
        Assert.That(bodyB.Connections, Does.Not.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void RemoveBody_RemovesAttachedConstraint_FromOtherBody()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();

        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);

        Assert.That(bodyA.IsValid, Is.True);
        Assert.That(bodyB.IsValid, Is.True);
        Assert.That(constraint.IsValid, Is.True);

        world.Remove(bodyA);

        Assert.That(bodyA.IsValid, Is.False);
        Assert.That(bodyB.IsValid, Is.True);
        Assert.That(constraint.IsValid, Is.False);
        Assert.That(bodyB.Constraints, Does.Not.Contain(constraint));
        Assert.That(bodyB.Connections, Does.Not.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void ConstraintRemoved_IsRaisedForExplicitRemoval()
    {
        using var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        Constraint? received = null;

        world.ConstraintRemoved += removedConstraint =>
        {
            Assert.That(removedConstraint.IsValid, Is.False);
            received = removedConstraint;
        };

        world.Remove(constraint);

        Assert.That(received, Is.SameAs(constraint));
    }

    [TestCase]
    public void ConstraintRemoved_IsRaisedForBodyRemoval()
    {
        using var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        Constraint? received = null;
        world.ConstraintRemoved += removedConstraint => received = removedConstraint;

        world.Remove(bodyA);

        Assert.That(received, Is.SameAs(constraint));
    }

    [TestCase]
    public void ConstraintRemoved_IsRaisedForMotionTypeChange()
    {
        using var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        Constraint? received = null;
        world.ConstraintRemoved += removedConstraint => received = removedConstraint;

        bodyA.MotionType = MotionType.Static;

        Assert.That(received, Is.Null);
        Assert.That(constraint.IsValid, Is.True);

        bodyB.MotionType = MotionType.Static;

        Assert.That(received, Is.SameAs(constraint));
    }

    [TestCase]
    public void ConstraintRemoved_IsNotRaisedForDisableOrDispose()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        var eventCount = 0;
        world.ConstraintRemoved += _ => eventCount++;

        constraint.IsEnabled = false;
        world.Dispose();

        Assert.That(eventCount, Is.Zero);
    }

    [TestCase]
    public void CreateConstraint_WithSameBody_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();

        var exception = Assert.Throws<SameBodyException>(() =>
            world.CreateConstraint<BallSocket>(body, body));
        Assert.That(exception!.ParamName, Is.EqualTo("body2"));
        world.Dispose();
    }

    [TestCase]
    public void ActivatingSleepingBody_WakesConstrainedPartnerOnNextStep()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));
        bodyA.DeactivationTime = TimeSpan.FromSeconds(1);

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(4, 0, 0);
        bodyB.DeactivationTime = TimeSpan.FromSeconds(1);

        world.CreateConstraint<BallSocket>(bodyA, bodyB);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(bodyA.IsActive, Is.False);
        Assert.That(bodyB.IsActive, Is.False);

        bodyA.SetActivationState(true);
        world.Step(1f / 100f, false);

        Assert.That(bodyA.IsActive, Is.True);
        Assert.That(bodyB.IsActive, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void ResetWarmStart_ClearsCachedConstraintImpulse()
    {
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));

        var socket = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        socket.Initialize(JVector.Zero);

        bodyB.Position = new JVector(1, 0, 0);
        bodyA.Velocity = JVector.Zero;
        bodyB.Velocity = JVector.Zero;
        bodyA.AngularVelocity = JVector.Zero;
        bodyB.AngularVelocity = JVector.Zero;

        world.Stabilize(1f / 60f, 4, 2, false);

        Assert.That(socket.Impulse.LengthSquared(), Is.GreaterThan((Real)0.0));

        socket.ResetWarmStart();

        Assert.That(socket.Impulse, Is.EqualTo(JVector.Zero));

        var motor = world.CreateConstraint<LinearMotor>(bodyA, bodyB);
        motor.Initialize(JVector.UnitX, JVector.UnitX);
        motor.TargetVelocity = (Real)2.0;
        motor.MaximumForce = (Real)100.0;

        bodyA.Velocity = JVector.Zero;
        bodyB.Velocity = JVector.Zero;

        world.Step(1f / 60f, false);

        Assert.That(MathR.Abs(motor.Impulse), Is.GreaterThan((Real)0.0));

        motor.ResetWarmStart();

        Assert.That(motor.Impulse, Is.EqualTo((Real)0.0));
        world.Dispose();
    }
}
