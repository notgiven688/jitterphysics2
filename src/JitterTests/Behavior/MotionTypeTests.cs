namespace JitterTests.Behavior;

public class MotionTypeTests
{
    [Test]
    public void BecomingStatic_RemovesDuplicateConnectionsAndSplitsIslands(
        [Values] SolveMode mode, [Values] bool multiThread)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var center = world.CreateRigidBody();
        var left = world.CreateRigidBody();
        var right = world.CreateRigidBody();
        var tail = world.CreateRigidBody();

        world.CreateConstraint<BallSocket>(center, left).Initialize(JVector.Zero);
        world.CreateConstraint<BallSocket>(center, left).Initialize(JVector.Zero);
        world.CreateConstraint<BallSocket>(center, right).Initialize(JVector.Zero);
        world.CreateConstraint<BallSocket>(left, tail).Initialize(JVector.Zero);

        for (int i = 0; i < 3; i++)
        {
            Assert.That(center.Connections, Has.Count.EqualTo(3));
            Assert.That(left.Connections, Has.Count.EqualTo(3));
            Assert.That(right.Island, Is.SameAs(left.Island));

            center.MotionType = MotionType.Static;

            Assert.That(center.Connections, Is.Empty);
            Assert.That(right.Connections, Is.Empty);
            Assert.That(left.Connections, Is.EqualTo(new[] { tail }));
            Assert.That(tail.Connections, Is.EqualTo(new[] { left }));
            Assert.That(center.Island.Bodies, Has.Count.EqualTo(1));
            Assert.That(right.Island.Bodies, Has.Count.EqualTo(1));
            Assert.That(left.Island, Is.SameAs(tail.Island));
            Assert.That(left.Island.Bodies, Has.Count.EqualTo(2));
            Assert.That(center.Constraints, Has.Count.EqualTo(3));
            world.Step((Real)0.01, multiThread);

            center.MotionType = MotionType.Dynamic;
            Assert.That(center.Island, Is.SameAs(left.Island));
            Assert.That(center.Island, Is.SameAs(right.Island));
            Assert.That(center.Island.Bodies, Has.Count.EqualTo(4));
            world.Step((Real)0.01, multiThread);
        }
    }

    [Test]
    public void JointToTheWorld_SurvivesASwitchAwayFromDynamicAndBack(
        [Values] SolveMode mode, [Values] bool multiThread,
        [Values(MotionType.Kinematic, MotionType.Static)] MotionType temporary)
    {
        using var world = new World { SolveMode = mode };
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape((Real)0.5));
        body.Position = new JVector(0, -2, 0);

        var socket = world.CreateConstraint<BallSocket>(world.NullBody, body);
        socket.Initialize(JVector.Zero);
        Helper.AdvanceWorld(world, 1, (Real)0.01, multiThread);

        body.MotionType = temporary;
        Assert.That(socket.IsValid, Is.True);
        Assert.That(body.Constraints, Does.Contain(socket));
        Helper.AdvanceWorld(world, 1, (Real)0.01, multiThread);

        body.MotionType = MotionType.Dynamic;
        Assert.That(socket.IsValid, Is.True);
        Assert.That(body.Constraints, Does.Contain(socket));
        socket.Softness = (Real)0.001;

        Helper.AdvanceWorld(world, 2, (Real)0.01, multiThread);
        Assert.That((body.Position - new JVector(0, -2, 0)).Length(), Is.LessThan((Real)0.05));
    }

    [Test]
    public void JointBetweenTwoBodies_SurvivesBothBecomingKinematicAndBack(
        [Values] SolveMode mode, [Values] bool multiThread)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape((Real)0.5));
        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape((Real)0.5));
        bodyB.Position = new JVector(2, 0, 0);

        var distance = world.CreateConstraint<DistanceLimit>(bodyA, bodyB);
        distance.Initialize(bodyA.Position, bodyB.Position);

        bodyA.MotionType = MotionType.Kinematic;
        bodyB.MotionType = MotionType.Kinematic;
        Assert.That(distance.IsValid, Is.True);
        world.Step((Real)0.01, multiThread);

        bodyA.MotionType = MotionType.Dynamic;
        bodyB.MotionType = MotionType.Dynamic;
        Assert.That(bodyA.Connections, Does.Contain(bodyB));
        bodyB.Velocity = new JVector(1, 0, 0);

        Helper.AdvanceWorld(world, 1, (Real)0.01, multiThread);
        Assert.That((bodyB.Position - bodyA.Position).Length(), Is.EqualTo((Real)2).Within((Real)0.05));
    }

    [Test]
    public void MovingKinematicBodyJointedToTheWorld_IsNotDisturbedByTheJoint(
        [Values] SolveMode mode, [Values] bool multiThread)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape((Real)0.5));
        body.Position = new JVector(0, -2, 0);
        var socket = world.CreateConstraint<BallSocket>(world.NullBody, body);
        socket.Initialize(JVector.Zero);

        body.MotionType = MotionType.Kinematic;
        body.Velocity = new JVector(1, 0, 0);
        Helper.AdvanceWorld(world, 1, (Real)0.01, multiThread);

        Assert.That(body.Velocity, Is.EqualTo(JVector.UnitX));
        Assert.That(body.Position.X, Is.EqualTo((Real)1).Within((Real)0.02));
        Assert.That(socket.IsValid, Is.True);
    }

    [TestCase]
    public void CheckInternalMass()
    {
        var world = new World();

        var sphere = world.CreateRigidBody();
        sphere.AddShape(new SphereShape(1));

        var sphereMass = sphere.Mass;

        sphere.MotionType = MotionType.Kinematic;

        Assert.That(sphere.Data.InverseMassVector, Is.EqualTo(JVector.Zero));
        Assert.That(sphere.Mass, Is.EqualTo(sphereMass));

        sphere.MotionType = MotionType.Dynamic;

        Assert.That(sphere.Data.InverseMassVector, Is.EqualTo(new JVector(1 / sphereMass)));
        Assert.That(sphere.Mass, Is.EqualTo(sphereMass));

        sphere.MotionType = MotionType.Static;

        Assert.That(sphere.Data.InverseMassVector, Is.EqualTo(JVector.Zero));
        Assert.That(sphere.Mass, Is.EqualTo(sphereMass));

        world.Dispose();
    }

    private void PrepareTwoStack(World world, out RigidBody platform, out List<RigidBody> boxes)
    {
        // Create a static body
        platform = world.CreateRigidBody();
        platform.AddShape(new BoxShape(10, 2, 10));
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
        var world = new World();

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
        var world = new World();

        PrepareTwoStack(world, out var platform, out var boxes);

        boxes[0].MotionType = MotionType.Kinematic;

        // Stop motion inherited from the dynamic state so this island can sleep.
        boxes[0].Velocity = JVector.Zero;
        boxes[0].AngularVelocity = JVector.Zero;

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
