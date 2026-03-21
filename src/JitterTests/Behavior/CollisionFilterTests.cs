using Jitter2.Collision;

namespace JitterTests.Behavior;

public class CollisionFilterTests
{
    private sealed class RejectAllBroadPhaseFilter : IBroadPhaseFilter
    {
        public int Calls { get; private set; }

        public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
        {
            Calls++;
            return false;
        }
    }

    private sealed class RejectAllNarrowPhaseFilter : INarrowPhaseFilter
    {
        public int Calls { get; private set; }

        public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB,
            ref JVector pointA, ref JVector pointB, ref JVector normal, ref Real penetration)
        {
            Calls++;
            return false;
        }
    }

    [TestCase]
    public void BroadPhaseFilter_CanSuppressContactCreation()
    {
        var world = new World
        {
            Gravity = JVector.Zero
        };

        var filter = new RejectAllBroadPhaseFilter();
        world.BroadPhaseFilter = filter;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        world.Step(1f / 60f, false);

        Assert.That(filter.Calls, Is.GreaterThan(0));
        Assert.That(bodyA.Contacts, Is.Empty);
        Assert.That(bodyB.Contacts, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void NarrowPhaseFilter_CanSuppressContactCreation()
    {
        var world = new World
        {
            Gravity = JVector.Zero
        };

        var filter = new RejectAllNarrowPhaseFilter();
        world.NarrowPhaseFilter = filter;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        world.Step(1f / 60f, false);

        Assert.That(filter.Calls, Is.GreaterThan(0));
        Assert.That(bodyA.Contacts, Is.Empty);
        Assert.That(bodyB.Contacts, Is.Empty);
        world.Dispose();
    }
}
