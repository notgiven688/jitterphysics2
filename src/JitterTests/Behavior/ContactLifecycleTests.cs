namespace JitterTests.Behavior;

public class ContactLifecycleTests
{
    [TestCase]
    public void BeginCollide_FiresOnce_WhenBodiesStartTouching()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        int beginA = 0, beginB = 0;
        bodyA.BeginCollide += _ => beginA++;
        bodyB.BeginCollide += _ => beginB++;

        world.Step(1f / 60f, false);

        Assert.That(beginA, Is.EqualTo(1));
        Assert.That(beginB, Is.EqualTo(1));
        Assert.That(bodyA.Contacts, Has.Count.EqualTo(1));
        Assert.That(bodyB.Contacts, Has.Count.EqualTo(1));
        Assert.That(bodyA.Connections, Does.Contain(bodyB));
        Assert.That(bodyB.Connections, Does.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void EndCollide_FiresOnce_WhenBodiesSeparate()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        int endA = 0, endB = 0;
        bodyA.EndCollide += _ => endA++;
        bodyB.EndCollide += _ => endB++;

        world.Step(1f / 60f, false);
        bodyB.Position = new JVector(5, 0, 0);
        world.Step(1f / 60f, false);
        world.Step(1f / 60f, false);

        Assert.That(endA, Is.EqualTo(1));
        Assert.That(endB, Is.EqualTo(1));
        Assert.That(bodyA.Contacts, Is.Empty);
        Assert.That(bodyB.Contacts, Is.Empty);
        Assert.That(bodyA.Connections, Does.Not.Contain(bodyB));
        Assert.That(bodyB.Connections, Does.Not.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void RemovingBody_InContact_CleansOtherBodyContactsAndConnections()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        world.Step(1f / 60f, false);
        Assert.That(bodyA.Contacts, Has.Count.EqualTo(1));

        world.Remove(bodyB);

        Assert.That(bodyA.Contacts, Is.Empty);
        Assert.That(bodyA.Connections, Is.Empty);
        world.Dispose();
    }
}
