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

public class CollideConnectedTests
{
    private const Real Dt = (Real)(1.0 / 60.0);

    // Two overlapping spheres with no gravity, joined at the middle by a ball socket.
    private static (World World, RigidBody A, RigidBody B, BallSocket Socket) Joined(bool staticB = false)
    {
        var world = new World { Gravity = JVector.Zero };

        var a = world.CreateRigidBody();
        a.AddShape(new SphereShape(1));

        var b = world.CreateRigidBody();
        b.AddShape(new SphereShape(1));
        b.Position = new JVector((Real)1.5, 0, 0);
        if (staticB) b.MotionType = MotionType.Static;

        var socket = world.CreateConstraint<BallSocket>(a, b);
        socket.Initialize(new JVector((Real)0.75, 0, 0));
        return (world, a, b, socket);
    }

    private static bool Touching(RigidBody a, RigidBody b)
        => a.Contacts.Any(arbiter => (arbiter.Body1 == a && arbiter.Body2 == b) || (arbiter.Body1 == b && arbiter.Body2 == a));

    private static void Step(World world, int steps = 1)
    {
        for (int i = 0; i < steps; i++) world.Step(Dt, false);
    }

    [Test]
    public void JoinedBodiesCollideByDefault()
    {
        var (world, a, b, socket) = Joined();
        using (world)
        {
            Assert.That(socket.CollideConnected, Is.True);
            Step(world);
            Assert.That(Touching(a, b), Is.True);
        }
    }

    [Test]
    public void JoinedBodiesKeptApartNeverTouch([Values] bool staticB)
    {
        var (world, a, b, socket) = Joined(staticB);
        using (world)
        {
            socket.CollideConnected = false;
            Step(world, 30);
            Assert.That(Touching(a, b), Is.False);
        }
    }

    [Test]
    public void KeepingJoinedBodiesApartRemovesTheirContactsAtOnce()
    {
        var (world, a, b, socket) = Joined();
        using (world)
        {
            Step(world);
            Assume.That(Touching(a, b), Is.True);

            socket.CollideConnected = false;
            Assert.That(Touching(a, b), Is.False);

            Step(world, 10);
            Assert.That(Touching(a, b), Is.False);
        }
    }

    [Test]
    public void LettingJoinedBodiesCollideAgainBringsTheirContactsBack()
    {
        var (world, a, b, socket) = Joined();
        using (world)
        {
            socket.CollideConnected = false;
            Step(world);

            socket.CollideConnected = true;
            Step(world);
            Assert.That(Touching(a, b), Is.True);
        }
    }

    [Test]
    public void RemovingTheConstraintLetsTheBodiesCollide()
    {
        var (world, a, b, socket) = Joined();
        using (world)
        {
            socket.CollideConnected = false;
            Step(world);

            world.Remove(socket);
            Step(world);
            Assert.That(Touching(a, b), Is.True);
        }
    }

    [Test]
    public void AnyConstraintKeepingTheBodiesApartWins()
    {
        var (world, a, b, socket) = Joined();
        using (world)
        {
            var second = world.CreateConstraint<BallSocket>(a, b);
            second.Initialize(new JVector((Real)0.75, (Real)0.1, 0));
            second.CollideConnected = false;

            Step(world, 5);
            Assert.That(socket.CollideConnected, Is.True);
            Assert.That(Touching(a, b), Is.False);
        }
    }

    [Test]
    public void BodiesNotJoinedStillCollide()
    {
        var (world, a, b, socket) = Joined();
        using (world)
        {
            socket.CollideConnected = false;

            var c = world.CreateRigidBody();
            c.AddShape(new SphereShape(1));
            c.Position = new JVector((Real)(-1.5), 0, 0);

            Step(world);
            Assert.That(Touching(a, c), Is.True);
            Assert.That(Touching(a, b), Is.False);
        }
    }
}
