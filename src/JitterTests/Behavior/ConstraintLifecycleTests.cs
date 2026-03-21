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
        world.Remove(constraint);

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
        world.Remove(bodyA);

        Assert.That(bodyB.Constraints, Does.Not.Contain(constraint));
        Assert.That(bodyB.Connections, Does.Not.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void CreateConstraint_WithSameBody_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();

        Assert.Throws<ArgumentException>(() => world.CreateConstraint<BallSocket>(body, body));
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
}
