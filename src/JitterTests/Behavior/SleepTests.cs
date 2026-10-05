namespace JitterTests.Behavior;

public class SleepTests
{
    [TestCase]
    public void StaticBodySleepingAndWake()
    {
        var world = new World();

        // Create a static body
        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.MotionType = MotionType.Static;
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);
        Assert.That(!body.IsActive, "Static body should always be inactive.");

        body.SetActivationState(true);
        Assert.That(!body.IsActive, "Static body should always be inactive.");
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);
        Assert.That(!body.IsActive, "Static body should always be inactive.");

        body.Position = new JVector(0, 10, 0);
        Assert.That(!body.IsActive, "Static body should always be inactive.");
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);
        Assert.That(!body.IsActive, "Static body should always be inactive.");

        var dynamicBody = world.CreateRigidBody();
        dynamicBody.AddShape(new BoxShape(1));
        dynamicBody.Position = new JVector(0, 12, 0);
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);
        Assert.That(!body.IsActive, "Static body should always be inactive.");

        world.Dispose();
    }

    [TestCase]
    public void DeactivationTime()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var body = world.CreateRigidBody();
        body.DeactivationTime = TimeSpan.FromSeconds(3);

        // Initially, a newly created body is always active.
        Assert.That(body.IsActive);

        // Advance simulation by 1 second — should still be active,
        // because the deactivation timeout (3s) hasn’t elapsed yet.
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);
        Assert.That(body.IsActive);

        // After 3 more seconds (total 4s), the body should deactivate,
        // since it hasn’t moved and its timeout was 3s.
        Helper.AdvanceWorld(world, 3, (Real)(1.0 / 100.0), false);
        Assert.That(!body.IsActive);

        // Change the deactivation timeout to 5 seconds and try to
        // manually reactivate the body. The activation request
        // takes effect only after the next simulation step.
        body.DeactivationTime = TimeSpan.FromSeconds(5);
        body.SetActivationState(true);

        // Until the next step, the body remains inactive.
        Assert.That(!body.IsActive);

        // Step the world once — now the activation flag is processed,
        // and the body should become active again.
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);
        Assert.That(body.IsActive);

        // Advance 3 seconds — still within the 5-second timeout, so active.
        Helper.AdvanceWorld(world, 3, (Real)(1.0 / 100.0), false);
        Assert.That(body.IsActive);

        // Advance 2 more seconds (total 6s) — exceeds the timeout,
        // so the body should deactivate again.
        Helper.AdvanceWorld(world, 2, (Real)(1.0 / 100.0), false);
        Assert.That(!body.IsActive);

        world.Dispose();
    }

    [TestCase]
    public void KinematicTriggerWake()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        // Static (infinite mass) body acting as a moving platform.
        var platform = world.CreateRigidBody();
        platform.AddShape(new BoxShape(2));
        platform.MotionType = MotionType.Kinematic;
        platform.Position = new JVector(0, 0, 0);
        platform.DeactivationTime = TimeSpan.FromSeconds(1);

        // Dynamic body resting slightly above the platform.
        var box = world.CreateRigidBody();
        box.AddShape(new BoxShape(1));
        box.Position = new JVector(0, 2, 0);
        box.DeactivationTime = TimeSpan.FromSeconds(1);

        // Let the dynamic body settle and go to sleep.
        Helper.AdvanceWorld(world, 3, (Real)(1.0 / 100.0), false);
        Assert.That(!box.IsActive, "Box should be inactive after settling.");

        // Give the static body an upward velocity — simulating a moving platform.
        platform.Velocity = new JVector(0, 1, 0);

        // Advance one step to process motion and new contact generation.
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);

        // The sleeping box should wake up due to contact with the moving platform.
        Assert.That(box.IsActive, "Box should wake up after being touched by moving static body.");

        // Optionally verify that the platform can still move even though it is static (infinite mass).
        Assert.That(platform.Position.Y > 0, "Platform should have moved due to integration.");

        world.Dispose();
    }


    [TestCase]
    public void StackedSleeping()
    {
        var world = new World();

        List<RigidBody> bodies = new();

        for (int i = 0; i < 3; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(1));
            body.Position = new JVector(0, i, 0);
            body.DeactivationTime = TimeSpan.FromSeconds(1);
            bodies.Add(body);
        }

        bodies[0].MotionType = MotionType.Static;
        Helper.AdvanceWorld(world, 3, (Real)(1.0 / 100.0), false);

        // Make sure all bodies are inactive
        foreach (var body in bodies)
        {
            Assert.That(!body.IsActive, $"Body at {body.Position} should be inactive.");
        }

        // Request reactivation of the middle body
        bodies[1].SetActivationState(true);

        // Process activation in the next step
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);

        // Now both the middle and top bodies should be active
        Assert.That(bodies[1].IsActive, "Middle body should be reactivated.");
        Assert.That(bodies[2].IsActive, "Top body should be reactivated due to contact propagation.");

        // Advance world to allow them to fall asleep again
        Helper.AdvanceWorld(world, 2, (Real)(1.0 / 100.0), false);

        // Verify that both dynamic bodies went inactive again
        Assert.That(!bodies[1].IsActive, "Middle body should be inactive again.");
        Assert.That(!bodies[2].IsActive, "Top body should be inactive again.");

        world.Dispose();
    }

    [TestCase]
    public void StackedWakeOnRemoval()
    {
        var world = new World();

        List<RigidBody> bodies = new();

        // Create a vertical stack of three boxes
        for (int i = 0; i < 3; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(1));
            body.Position = new JVector(0, i, 0);
            body.DeactivationTime = TimeSpan.FromSeconds(1);
            bodies.Add(body);
        }

        // The bottom box is static (acts as the floor)
        bodies[0].MotionType = MotionType.Static;

        // Let them settle and go to sleep
        Helper.AdvanceWorld(world, 3, (Real)(1.0 / 100.0), false);

        // Ensure all non-static bodies are inactive
        foreach (var body in bodies)
        {
            Assert.That(!body.IsActive, $"Body at {body.Position} should be inactive.");
        }

        // Remove the middle body — this breaks contact between bottom and top
        world.Remove(bodies[1]);

        // After removing it, the island should update on the next step
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);

        // The top body should now be awake because its supporting contact vanished
        Assert.That(bodies[2].IsActive, "Top body should wake up after its support is removed.");

        world.Dispose();
    }

    [TestCase]
    public void StaticBaseMovementWakesDynamic()
    {
        var world = new World();

        // Bottom body: static (infinite mass) base
        var bottom = world.CreateRigidBody();
        bottom.AddShape(new BoxShape(2));
        bottom.Position = new JVector(0, 0, 0);
        bottom.MotionType = MotionType.Static;
        bottom.DeactivationTime = TimeSpan.FromSeconds(1);

        // Top body: dynamic box resting on the static base
        var top = world.CreateRigidBody();
        top.AddShape(new BoxShape(1));
        top.Position = new JVector(0, 2, 0);
        top.DeactivationTime = TimeSpan.FromSeconds(1);

        // Let them settle — both should go to sleep
        Helper.AdvanceWorld(world, 3, (Real)(1.0 / 100.0), false);
        Assert.That(!bottom.IsActive, "Bottom (static) body should be inactive after settling.");
        Assert.That(!top.IsActive, "Top body should be inactive after settling.");

        // Move the static body downward slightly
        bottom.Position -= new JVector(0, 0.5f, 0);

        // Process one step so the contact system updates
        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);

        // The top body should now wake up because its contact was disturbed
        Assert.That(top.IsActive, "Top body should wake up after its static support moves.");

        world.Dispose();
    }

    [TestCase]
    public void ForceSleepIsland_DeactivatesWholeIslandImmediately()
    {
        using var world = new World();
        world.Gravity = JVector.Zero;

        var body1 = world.CreateRigidBody();
        body1.AddShape(new SphereShape(1));
        body1.Velocity = new JVector(1, 0, 0);

        var body2 = world.CreateRigidBody();
        body2.AddShape(new SphereShape(1));
        body2.Position = new JVector(2, 0, 0);
        body2.Velocity = new JVector(-1, 0, 0);

        var constraint = world.CreateConstraint<BallSocket>(body1, body2);
        constraint.Initialize(JVector.Zero);

        Island island = body1.Island;
        Assert.That(body2.Island, Is.SameAs(island));
        Assert.That(world.Islands.IsActive(island), Is.True);

        int deactivated = 0;
        bool callbackSawInactiveIsland = false;

        world.IslandDeactivated += deactivatedIsland =>
        {
            deactivated++;
            callbackSawInactiveIsland = !world.Islands.IsActive(deactivatedIsland);
        };

        world.ForceSleepIsland(island);

        Assert.That(deactivated, Is.EqualTo(1));
        Assert.That(callbackSawInactiveIsland, Is.True);
        Assert.That(world.Islands.IsActive(island), Is.False);
        Assert.That(world.RigidBodies.IsActive(body1), Is.False);
        Assert.That(world.RigidBodies.IsActive(body2), Is.False);
        Assert.That(body1.IsActive, Is.False);
        Assert.That(body2.IsActive, Is.False);
        Assert.That(body1.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body2.Velocity, Is.EqualTo(JVector.Zero));
    }

    [Test]
    public void SlowKinematicBody_KeepsMovingAndStaysAwake([Values] bool angular)
    {
        using var world = new World { Gravity = JVector.Zero };

        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.MotionType = MotionType.Kinematic;
        body.DeactivationTime = TimeSpan.FromSeconds(0.5);

        if (angular) body.AngularVelocity = new JVector(0, (Real)0.2, 0);
        else body.Velocity = new JVector((Real)0.2, 0, 0);

        Helper.AdvanceWorld(world, 3, (Real)(1.0 / 100.0), false);

        Assert.That(body.IsActive, Is.True);
        if (angular)
        {
            JVector forward = JVector.Transform(JVector.UnitX, body.Orientation);
            Assert.That(MathR.Atan2(-forward.Z, forward.X), Is.EqualTo((Real)0.6).Within((Real)0.02));
        }
        else
        {
            Assert.That(body.Position.X, Is.EqualTo((Real)0.6).Within((Real)0.02));
        }
    }

    [TestCase]
    public void SlowKinematicPlatform_CarriesItsRider()
    {
        using var world = new World();

        var platform = world.CreateRigidBody();
        platform.AddShape(new BoxShape(4, 1, 4));
        platform.MotionType = MotionType.Kinematic;
        platform.DeactivationTime = TimeSpan.FromSeconds(0.5);

        var rider = world.CreateRigidBody();
        rider.AddShape(new BoxShape(1));
        rider.Position = new JVector(0, 1, 0);
        rider.DeactivationTime = TimeSpan.FromSeconds(0.5);

        Helper.AdvanceWorld(world, 1, (Real)(1.0 / 100.0), false);
        Real gap = rider.Position.Y - platform.Position.Y;

        platform.Velocity = new JVector(0, (Real)0.1, 0);
        Helper.AdvanceWorld(world, 4, (Real)(1.0 / 100.0), false);

        Assert.That(platform.Position.Y, Is.EqualTo((Real)0.4).Within((Real)0.02));
        Assert.That(rider.Position.Y - platform.Position.Y, Is.EqualTo(gap).Within((Real)0.02));
    }

    [TestCase]
    public void ForceSleepIsland_ZeroesKinematicVelocityImmediately()
    {
        using var world = new World();

        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.MotionType = MotionType.Kinematic;
        body.Velocity = new JVector(1, 2, 3);
        body.AngularVelocity = new JVector(4, 5, 6);
        body.Force = new JVector(7, 8, 9);
        body.Torque = new JVector(10, 11, 12);

        world.ForceSleepIsland(body.Island);

        Assert.That(body.IsActive, Is.False);
        Assert.That(world.Islands.IsActive(body.Island), Is.False);
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.Force, Is.EqualTo(JVector.Zero));
        Assert.That(body.Torque, Is.EqualTo(JVector.Zero));
    }

    [TestCase]
    public void ForceSleepIsland_WithForeignIsland_Throws()
    {
        using var world = new World();
        using var otherWorld = new World();

        var foreignBody = otherWorld.CreateRigidBody();

        Assert.Throws<ArgumentException>(() => world.ForceSleepIsland(foreignBody.Island));
    }

    [TestCase]
    public void IslandDeactivatedEventIsRaisedAfterIslandSleeps()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        int deactivated = 0;
        bool callbackSawBody = false;
        bool callbackSawInactiveBody = false;
        bool callbackSawInactivePartition = false;

        world.IslandDeactivated += island =>
        {
            deactivated++;
            callbackSawBody = island.Bodies.Contains(body);
            callbackSawInactiveBody = !body.IsActive;
            callbackSawInactivePartition = !world.Islands.IsActive(island);
        };

        Helper.AdvanceWorld(world, 2, (Real)(1.0 / 100.0), false);

        Assert.That(deactivated, Is.EqualTo(1));
        Assert.That(callbackSawBody, Is.True);
        Assert.That(callbackSawInactiveBody, Is.True);
        Assert.That(callbackSawInactivePartition, Is.True);

        world.Dispose();
    }

    [TestCase]
    public void IslandActivatedEventIsRaisedAfterSleepingIslandWakes()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        Helper.AdvanceWorld(world, 2, (Real)(1.0 / 100.0), false);
        Assert.That(body.IsActive, Is.False);

        int activated = 0;
        bool callbackSawBody = false;
        bool callbackSawActiveBody = false;
        bool callbackSawActivePartition = false;

        world.IslandActivated += island =>
        {
            activated++;
            callbackSawBody = island.Bodies.Contains(body);
            callbackSawActiveBody = body.IsActive;
            callbackSawActivePartition = world.Islands.IsActive(island);
        };

        body.SetActivationState(true);
        world.Step((Real)(1.0 / 100.0), false);

        Assert.That(activated, Is.EqualTo(1));
        Assert.That(callbackSawBody, Is.True);
        Assert.That(callbackSawActiveBody, Is.True);
        Assert.That(callbackSawActivePartition, Is.True);

        world.Dispose();
    }

}
