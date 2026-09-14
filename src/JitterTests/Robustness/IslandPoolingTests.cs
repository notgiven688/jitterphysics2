using System.Reflection;

namespace JitterTests.Robustness;

public class IslandPoolingTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

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
