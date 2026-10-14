using System.Reflection;

namespace JitterTests.Robustness;

public class IslandPoolingTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Test]
    public void RemovingBridge_SplitsUnequalIslandsAndPreservesPendingWake(
        [Values] bool reverseOrder, [Values] bool sleeping)
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody[] left = [world.CreateRigidBody(), world.CreateRigidBody(), world.CreateRigidBody()];
        RigidBody[] right = [world.CreateRigidBody(), world.CreateRigidBody()];

        world.CreateConstraint<BallSocket>(left[0], left[1]).Initialize(JVector.Zero);
        world.CreateConstraint<BallSocket>(left[1], left[2]).Initialize(JVector.Zero);
        world.CreateConstraint<BallSocket>(right[0], right[1]).Initialize(JVector.Zero);
        var bridge = reverseOrder
            ? world.CreateConstraint<BallSocket>(right[0], left[0])
            : world.CreateConstraint<BallSocket>(left[0], right[0]);
        bridge.Initialize(JVector.Zero);

        Assert.That(left[0].Island.Bodies, Is.EquivalentTo(left.Concat(right)));
        if (sleeping) world.ForceSleepIsland(left[0].Island);

        world.Remove(bridge);

        Assert.That(left[0].Island.Bodies, Is.EquivalentTo(left));
        Assert.That(right[0].Island.Bodies, Is.EquivalentTo(right));
        Assert.That(world.Islands.IsActive(left[0].Island), Is.True);
        Assert.That(world.Islands.IsActive(right[0].Island), Is.True);

        world.Step((Real)0.01, multiThread: false);

        Assert.That(left.Concat(right).All(body => body.IsActive), Is.True);
    }

    [Test]
    public void RemovedBodyIsland_IsReusedAcrossThreadsInOwningWorld()
    {
        using var world = new World();
        RigidBody body = world.CreateRigidBody();
        Island island = body.Island;

        Thread worker = new(() => world.Remove(body)) { IsBackground = true };
        worker.Start();
        Assert.That(worker.Join(Timeout), Is.True, "Worker did not finish.");

        var islandPool = GetField<Stack<Island>>(world, "islandPool");
        Assert.That(islandPool, Does.Contain(island));

        RigidBody reused = world.CreateRigidBody();
        Assert.That(reused.Island, Is.SameAs(island));
    }

    [Test]
    public void RemovedBodyIsland_IsScopedToOwningWorld()
    {
        using var world1 = new World();
        using var world2 = new World();

        RigidBody body = world1.CreateRigidBody();
        Island island = body.Island;
        world1.Remove(body);

        RigidBody otherWorldBody = world2.CreateRigidBody();
        Assert.That(otherWorldBody.Island, Is.Not.SameAs(island));

        RigidBody reused = world1.CreateRigidBody();
        Assert.That(reused.Island, Is.SameAs(island));
    }

    private static T GetField<T>(object instance, string name)
    {
        return (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    }
}
