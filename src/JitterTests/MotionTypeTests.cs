namespace JitterTests;

public class MotionTypeTests
{
    private readonly World.Capacity smallCapacity = new()
    {
        BodyCount = 32,
        ConstraintCount = 32,
        ContactCount = 32,
        SmallConstraintCount = 32
    };

    private void PrepareTwoStack(World world, out RigidBody platform, out List<RigidBody> boxes)
    {
        // Create a static body
        platform = world.CreateRigidBody();
        platform.AddShape(new BoxShape(10,2, 10));
        platform.Position = (0, -1, 0);
        platform.MotionType = MotionType.Static;

        boxes = new List<RigidBody>();

        // Create two boxes stacked
        for (int i = 0; i < 2; i++)
        {
            var box = world.CreateRigidBody();
            box.AddShape(new BoxShape(1));
            box.Position = (0, 0.5f + i, 0);
            boxes.Add(box);
        }

        Helper.AdvanceWorld(world, 1, 1.0f / 100.0f, false);

        // Static bodies actually do NOT build connections. We will have
        // two islands here.
        Assert.That(platform.Connections, Is.Empty);
        Assert.That(boxes[0].Connections, Has.Count.EqualTo(1));
        Assert.That(boxes[1].Connections, Has.Count.EqualTo(1));
        Assert.That(platform.Island, Is.Not.EqualTo(boxes[0].Island));
        Assert.That(boxes[1].Island, Is.EqualTo(boxes[0].Island));

        // We do store contacts/constraints
        Assert.That(platform.Contacts, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Contacts, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Contacts, Has.Count.EqualTo(1));
    }

    [TestCase]
    public void CheckContactGraph()
    {
        var world = new World(smallCapacity);

        PrepareTwoStack(world, out var platform, out var boxes);

        // Switch from static to dynamic. The platform should now be part of the island.
        platform.MotionType = MotionType.Dynamic;

        // Same as before
        Assert.That(platform.Contacts, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Contacts, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Contacts, Has.Count.EqualTo(1));

        // Different contact graph
        Assert.That(platform.Connections, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Connections, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Connections, Has.Count.EqualTo(1));
        Assert.That(platform.Island, Is.EqualTo(boxes[0].Island));
        Assert.That(boxes[1].Island, Is.EqualTo(boxes[0].Island));

        // Switch from dynamic to kinematic. Contact graph should stay the same
        platform.MotionType = MotionType.Kinematic;

        // Same as before
        Assert.That(platform.Contacts, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Contacts, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Contacts, Has.Count.EqualTo(1));

        // Same as before
        Assert.That(platform.Connections, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Connections, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Connections, Has.Count.EqualTo(1));
        Assert.That(platform.Island, Is.EqualTo(boxes[0].Island));
        Assert.That(boxes[1].Island, Is.EqualTo(boxes[0].Island));

        // Simulate a bit and check that nothing changed
        Helper.AdvanceWorld(world, 1, 1.0f / 100.0f, false);

        // Same as before
        Assert.That(platform.Contacts, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Contacts, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Contacts, Has.Count.EqualTo(1));

        // Same as before
        Assert.That(platform.Connections, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Connections, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Connections, Has.Count.EqualTo(1));
        Assert.That(platform.Island, Is.EqualTo(boxes[0].Island));
        Assert.That(boxes[1].Island, Is.EqualTo(boxes[0].Island));

        // Switch from kinematic to static.
        platform.MotionType = MotionType.Static;

        // Static bodies actually do NOT build connections. We will have
        // two islands here.
        Assert.That(platform.Connections, Is.Empty);
        Assert.That(boxes[0].Connections, Has.Count.EqualTo(1));
        Assert.That(boxes[1].Connections, Has.Count.EqualTo(1));
        Assert.That(platform.Island, Is.Not.EqualTo(boxes[0].Island));
        Assert.That(boxes[1].Island, Is.EqualTo(boxes[0].Island));

        // We do store contacts/constraints
        Assert.That(platform.Contacts, Has.Count.EqualTo(1));
        Assert.That(boxes[0].Contacts, Has.Count.EqualTo(2));
        Assert.That(boxes[1].Contacts, Has.Count.EqualTo(1));

        world.Dispose();
    }

    [TestCase]
    public void CheckNoStaticKinematicContacts()
    {
        var world = new World(smallCapacity);

        PrepareTwoStack(world, out var platform, out var boxes);

        boxes[0].MotionType = MotionType.Kinematic;

        // Jitter should now remove the contacts connecting a static and a kinematic body.
        // Only dynamic <-> kinematic contacts should remain.

        Assert.That(platform.Contacts, Is.Empty);
        Assert.That(boxes[0].Contacts, Has.Count.EqualTo(1));
        Assert.That(boxes[1].Contacts, Has.Count.EqualTo(1));

        Assert.That(platform.Island, Is.Not.EqualTo(boxes[0].Island));
        Helper.AdvanceWorld(world, 1, 1.0f / 100.0f, false);
        Assert.That(platform.Contacts, Is.Empty);

        Assert.That(platform.IsActive, Is.False);
        Assert.That(boxes[0].IsActive, Is.True);
        Assert.That(boxes[1].IsActive, Is.True);

        Helper.AdvanceWorld(world, 10, 1.0f / 100.0f, false);

        Assert.That(platform.IsActive, Is.False);
        Assert.That(boxes[0].IsActive, Is.False);
        Assert.That(boxes[1].IsActive, Is.False);

        world.Dispose();
    }

}