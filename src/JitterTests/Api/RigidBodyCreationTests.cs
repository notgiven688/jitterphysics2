namespace JitterTests.Api;

/// <summary>
/// Tests for the initial state of a RigidBody after creation.
/// </summary>
public class RigidBodyCreationTests
{
    [TestCase]
    public void DefaultPosition_IsZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Position, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void DefaultOrientation_IsIdentity()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Orientation, Is.EqualTo(JQuaternion.Identity));
        world.Dispose();
    }

    [TestCase]
    public void DefaultVelocity_IsZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void DefaultAngularVelocity_IsZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void DefaultMotionType_IsDynamic()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.MotionType, Is.EqualTo(MotionType.Dynamic));
        world.Dispose();
    }

    [TestCase]
    public void DefaultForce_IsZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Force, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void DefaultTorque_IsZero()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Torque, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void DefaultAffectedByGravity_IsTrue()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.AffectedByGravity, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void DefaultEnableSpeculativeContacts_IsFalse()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.EnableSpeculativeContacts, Is.False);
        world.Dispose();
    }

    [TestCase]
    public void DefaultTag_IsNull()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Tag, Is.Null);
        world.Dispose();
    }

    [TestCase]
    public void DefaultShapes_IsEmpty()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Shapes, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void DefaultContacts_IsEmpty()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Contacts, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void DefaultConnections_IsEmpty()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Connections, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void DefaultConstraints_IsEmpty()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.Constraints, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void CreatedBody_BelongsToWorld()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(body.World, Is.EqualTo(world));
        world.Dispose();
    }

    [TestCase]
    public void CreatedBodies_HaveUniqueIds()
    {
        var world = new World();
        var ids = new HashSet<ulong>();
        for (int i = 0; i < 100; i++)
        {
            ids.Add(world.CreateRigidBody().RigidBodyId);
        }
        Assert.That(ids, Has.Count.EqualTo(100));
        world.Dispose();
    }

    [TestCase]
    public void CreatedBody_AppearsInWorldRigidBodies()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.That(world.RigidBodies, Does.Contain(body));
        world.Dispose();
    }

    [TestCase]
    public void RemovedBody_DisappearsFromWorldRigidBodies()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        world.Remove(body);
        Assert.That(world.RigidBodies, Does.Not.Contain(body));
        world.Dispose();
    }
}
