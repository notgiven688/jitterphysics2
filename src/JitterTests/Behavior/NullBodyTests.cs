using Jitter2.Dynamics.Constraints;

namespace JitterTests.Behavior;

public class NullBodyTests
{
    [TestCase]
    public void NullBody_IsStaticAndInactive()
    {
        var world = new World();

        Assert.That(world.NullBody.MotionType, Is.EqualTo(MotionType.Static));
        Assert.That(world.NullBody.Data.InverseMass, Is.EqualTo((Real)0.0).Within((Real)1e-6));
        Assert.That(world.NullBody.Data.InverseInertiaWorld, Is.EqualTo(JMatrix.Zero));
        world.Dispose();
    }

    [TestCase]
    public void ConstraintAgainstNullBody_RegistersOnlyOnDynamicBodyConnection()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));

        var constraint = world.CreateConstraint<BallSocket>(body, world.NullBody);
        constraint.Initialize(body.Position);

        Assert.That(body.Constraints, Does.Contain(constraint));
        Assert.That(world.NullBody.Constraints, Does.Contain(constraint));
        Assert.That(body.Connections, Does.Not.Contain(world.NullBody));
        Assert.That(world.NullBody.Connections, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void NullBodyShape_ParticipatesInCollisionAndCanBeRemoved()
    {
        var world = new World
        {
            Gravity = JVector.Zero
        };

        var staticShape = new SphereShape(1);
        world.NullBody.AddShape(staticShape);

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(1.5f, 0, 0);

        world.Step(1f / 60f, false);
        Assert.That(body.Contacts, Has.Count.EqualTo(1));

        world.NullBody.RemoveShape(staticShape);
        world.Step(1f / 60f, false);
        world.Step(1f / 60f, false);

        Assert.That(body.Contacts, Is.Empty);
        Assert.That(world.NullBody.Shapes, Is.Empty);
        world.Dispose();
    }
}
