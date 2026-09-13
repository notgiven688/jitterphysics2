using System.Reflection;
using Jitter2.Collision;
using Jitter2.DataStructures;
using Jitter2.Unmanaged;

namespace JitterTests.Robustness;

public class WorldTrimTests
{
    [Test]
    public void Trim_ReducesWorldStorageAndKeepsRemainingObjectsValid()
    {
        using var world = new World();
        var bodies = new List<RigidBody>();

        for (int i = 0; i < 1100; i++)
        {
            RigidBody body = world.CreateRigidBody();
            bodies.Add(body);
        }

        var rigidBodyBuffer = GetField<PartitionedBuffer<RigidBodyData>>(world, "memRigidBodies");
        var bodySet = GetField<PartitionedSet<RigidBody>>(world, "bodies");
        var islandSet = GetField<PartitionedSet<Island>>(world, "islands");

        Assert.That(bodySet.Capacity, Is.GreaterThan(1024));
        Assert.That(islandSet.Capacity, Is.GreaterThan(1024));
        Assert.That(GetPartitionedBufferCapacity(rigidBodyBuffer), Is.GreaterThan(1024));

        RigidBody body1 = bodies[0];
        RigidBody body2 = bodies[1];

        for (int i = bodies.Count - 1; i >= 2; i--)
        {
            world.Remove(bodies[i]);
        }

        int beforeBodySet = bodySet.Capacity;
        int beforeIslandSet = islandSet.Capacity;
        int beforeRigidBodies = GetPartitionedBufferCapacity(rigidBodyBuffer);

        world.Trim();

        Assert.That(bodySet.Capacity, Is.LessThan(beforeBodySet));
        Assert.That(islandSet.Capacity, Is.LessThan(beforeIslandSet));
        Assert.That(GetPartitionedBufferCapacity(rigidBodyBuffer), Is.EqualTo(beforeRigidBodies));

        Assert.That(bodySet.Capacity, Is.EqualTo(1024));
        Assert.That(islandSet.Capacity, Is.EqualTo(1024));
        Assert.That(world.RigidBodies.Count, Is.EqualTo(3));
        Assert.That(body1.IsValid, Is.True);
        Assert.That(body2.IsValid, Is.True);
        Assert.That(world.NullBody.IsValid, Is.True);
    }

    [Test]
    public void Trim_OnDisposedWorld_Throws()
    {
        var world = new World();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.Trim());
    }

    private static T GetField<T>(object instance, string name)
    {
        return (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    }

    private static int GetPartitionedBufferCapacity<T>(PartitionedBuffer<T> buffer) where T : unmanaged
    {
        return GetField<int>(buffer, "size");
    }
}
