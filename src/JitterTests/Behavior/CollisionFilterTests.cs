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

    [TestCase]
    public void TriangleEdgeFilter_LeavesDynamicTriangleContactsUnmodified()
    {
        var world = new World();
        var triangleBody = world.CreateRigidBody();
        TriangleMesh mesh = new(
            [new JVector(-1, 0, 0), new JVector(1, 0, 0), new JVector(0, 1, 0)],
            [0, 1, 2]);
        var triangle = new TriangleShape(mesh, 0);
        triangleBody.AddShape(triangle, MassInertiaUpdateMode.Preserve);

        var otherBody = world.CreateRigidBody();
        var other = new BoxShape(1);
        otherBody.AddShape(other);
        otherBody.MotionType = MotionType.Static;

        JVector pointA = new(1, 2, 3);
        JVector pointB = new(4, 5, 6);
        JVector normal = new(0, 1, 0);
        Real penetration = (Real)0.25;

        var filter = new TriangleEdgeCollisionFilter();
        bool keep = filter.Filter(triangle, other, ref pointA, ref pointB, ref normal, ref penetration);

        Assert.That(keep, Is.True);
        Assert.That(pointA, Is.EqualTo(new JVector(1, 2, 3)));
        Assert.That(pointB, Is.EqualTo(new JVector(4, 5, 6)));
        Assert.That(normal, Is.EqualTo(new JVector(0, 1, 0)));
        Assert.That(penetration, Is.EqualTo((Real)0.25));
        world.Dispose();
    }

    [TestCase]
    public void TriangleEdgeFilter_CanFilterDynamicTrianglesWhenEnabled()
    {
        var world = new World();
        var triangleBody = world.CreateRigidBody();
        TriangleMesh mesh = new(
            [new JVector(-1, 0, 0), new JVector(1, 0, 0), new JVector(0, 1, 0)],
            [0, 1, 2]);
        var triangle = new TriangleShape(mesh, 0);
        triangleBody.AddShape(triangle, MassInertiaUpdateMode.Preserve);

        var otherBody = world.CreateRigidBody();
        var other = new BoxShape(1);
        otherBody.AddShape(other);
        otherBody.MotionType = MotionType.Static;

        JVector pointA = JVector.Zero;
        JVector pointB = JVector.Zero;
        JVector normal = -JVector.UnitZ;
        Real penetration = (Real)0.25;

        var filter = new TriangleEdgeCollisionFilter
        {
            FilterDynamicBodies = true
        };

        Assert.That(filter.Filter(triangle, other, ref pointA, ref pointB, ref normal, ref penetration), Is.False);
        world.Dispose();
    }
}
