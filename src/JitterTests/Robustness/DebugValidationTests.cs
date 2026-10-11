#if DEBUG

namespace JitterTests.Robustness;

public class DebugValidationTests
{
    [Test]
    public void RigidBodyPosition_WithNaN_Throws()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();

        Assert.Throws<ArgumentException>(() => body.Position = new JVector(Real.NaN, 0, 0));
    }

    [Test]
    public void RigidBodyOrientation_WithNonUnitQuaternion_Throws()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();

        Assert.Throws<ArgumentException>(() => body.Orientation = new JQuaternion(0, 0, 0, 2));
    }

    [Test]
    public void InvalidRigidBody_DataAccessThrowsButIdentityRemainsInspectable([Values] bool disposeWorld)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        ulong id = body.RigidBodyId;

        if (disposeWorld) world.Dispose();
        else world.Remove(body);

        Assert.That(body.IsValid, Is.False);
        Assert.That(body.RigidBodyId, Is.EqualTo(id));
        Assert.DoesNotThrow(() => _ = body.Handle);

        (string Name, TestDelegate Access)[] accesses =
        [
            ("Data", () => _ = body.Data),
            ("Position getter", () => _ = body.Position),
            ("Position setter", () => body.Position = JVector.UnitX),
            ("Velocity getter", () => _ = body.Velocity),
            ("Velocity setter", () => body.Velocity = JVector.UnitX),
            ("AngularVelocity getter", () => _ = body.AngularVelocity),
            ("AngularVelocity setter", () => body.AngularVelocity = JVector.UnitY),
            ("ApplyImpulse", () => body.ApplyImpulse(JVector.UnitX))
        ];

        foreach (var (name, access) in accesses)
        {
            Assert.Throws<InvalidOperationException>(access, name);
        }
    }

    [Test]
    public void CreateArbiter_WithRemovedBody_ThrowsWithoutChangingWorld([Values] bool removedFirst)
    {
        using World world = new();
        RigidBody removed = world.CreateRigidBody();
        RigidBody other = world.CreateRigidBody();
        world.Remove(removed);

        Assert.Throws<InvalidOperationException>(() =>
        {
            if (removedFirst) world.GetOrCreateArbiter(1, 2, removed, other, out _);
            else world.GetOrCreateArbiter(1, 2, other, removed, out _);
        });

        AssertNoContactCreated(world, removed, other);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void CreateArbiter_WithForeignBody_ThrowsWithoutChangingWorld(bool foreignFirst, bool foreignSecond)
    {
        using World world = new();
        using World foreignWorld = new();
        RigidBody body1 = (foreignFirst ? foreignWorld : world).CreateRigidBody();
        RigidBody body2 = (foreignSecond ? foreignWorld : world).CreateRigidBody();

        Assert.Throws<ArgumentException>(() => world.GetOrCreateArbiter(1, 2, body1, body2, out _));

        AssertNoContactCreated(world, body1, body2);
        Assert.That(foreignWorld.RawData.Contacts.Length, Is.Zero);
        Assert.DoesNotThrow(() => foreignWorld.Step((Real)(1.0 / 60.0), false));
    }

    private static void AssertNoContactCreated(World world, RigidBody body1, RigidBody body2)
    {
        Assert.That(world.GetArbiter(1, 2, out _), Is.False);
        Assert.That(world.RawData.Contacts.Length, Is.Zero);
        Assert.That(body1.Contacts, Is.Empty);
        Assert.That(body2.Contacts, Is.Empty);
        Assert.That(body1.Connections, Is.Empty);
        Assert.That(body2.Connections, Is.Empty);
        Assert.DoesNotThrow(() => world.Step((Real)(1.0 / 60.0), false));
        Assert.That(world.RawData.Contacts.Length, Is.Zero);
        Assert.That(body1.Contacts, Is.Empty);
        Assert.That(body2.Contacts, Is.Empty);
    }
}

#endif
