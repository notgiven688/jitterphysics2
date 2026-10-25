namespace JitterTests.Behavior;

public class IslandSplitTests
{
    private static World NewWorld() => new() { Gravity = JVector.Zero };

    [Test]
    public void BecomingStatic_SplitsIslandIntoAllComponentsAtOnce()
    {
        using var world = NewWorld();
        var hub = world.CreateRigidBody();
        var leaves = new RigidBody[3];
        for (int i = 0; i < leaves.Length; i++)
        {
            leaves[i] = world.CreateRigidBody();
            world.CreateConstraint<BallSocket>(hub, leaves[i]).Initialize(JVector.Zero);
        }

        Assert.That(leaves.Select(leaf => leaf.Island).Distinct().Count(), Is.EqualTo(1));
        Assert.That(leaves[0].Island, Is.SameAs(hub.Island));
        int islandsBefore = world.Islands.Count;

        // Removes every connection of the hub; each removal splits off a leaf.
        hub.MotionType = MotionType.Static;

        Assert.That(world.Islands.Count, Is.EqualTo(islandsBefore + 3));
        Assert.That(hub.Island.Bodies.Count, Is.EqualTo(1));
        Assert.That(leaves.Select(leaf => leaf.Island).Distinct().Count(), Is.EqualTo(3));
        foreach (var leaf in leaves)
        {
            Assert.That(leaf.Island, Is.Not.SameAs(hub.Island));
            Assert.That(leaf.Island.Bodies.Count, Is.EqualTo(1));
        }
    }

    [Test]
    public void RemovingConnections_SplitsOnlyWhenTheIslandIsDisconnected()
    {
        using var world = NewWorld();
        var a = world.CreateRigidBody();
        var b = world.CreateRigidBody();
        var c = world.CreateRigidBody();
        var ab = world.CreateConstraint<BallSocket>(a, b);
        ab.Initialize(JVector.Zero);
        var bc = world.CreateConstraint<BallSocket>(b, c);
        bc.Initialize(JVector.Zero);
        world.CreateConstraint<BallSocket>(c, a).Initialize(JVector.Zero);
        var island = a.Island;

        // The triangle stays connected through c.
        world.Remove(ab);
        Assert.That(b.Island, Is.SameAs(island));
        Assert.That(c.Island, Is.SameAs(island));
        Assert.That(island.Bodies.Count, Is.EqualTo(3));

        // The larger component keeps the island; b moves to a new one.
        world.Remove(bc);
        Assert.That(a.Island, Is.SameAs(island));
        Assert.That(c.Island, Is.SameAs(island));
        Assert.That(island.Bodies.Count, Is.EqualTo(2));
        Assert.That(b.Island, Is.Not.SameAs(island));
        Assert.That(b.Island.Bodies.Count, Is.EqualTo(1));
    }

    [Test]
    public void LongAlternativePaths_AreFoundTheLongWayRound()
    {
        // A ring of 200 bodies: the only alternative path runs around the whole ring.
        using var world = NewWorld();
        const int count = 200;
        var bodies = new RigidBody[count];
        var links = new BallSocket[count];
        for (int i = 0; i < count; i++)
        {
            bodies[i] = world.CreateRigidBody();
            Real angle = (Real)(2 * Math.PI * i / count);
            bodies[i].Position = new JVector(10 * (Real)Math.Cos(angle), 0, 10 * (Real)Math.Sin(angle));
        }
        for (int i = 0; i < count; i++)
        {
            links[i] = world.CreateConstraint<BallSocket>(bodies[i], bodies[(i + 1) % count]);
            links[i].Initialize(JVector.Zero);
        }
        var island = bodies[0].Island;
        Assert.That(island.Bodies.Count, Is.EqualTo(count));

        // Still one ring the long way round.
        world.Remove(links[0]);
        Assert.That(bodies.Select(body => body.Island).Distinct().Count(), Is.EqualTo(1));
        Assert.That(bodies[0].Island.Bodies.Count, Is.EqualTo(count));

        // Two chains of 100, bodies 1..100 and 101..199 plus 0.
        world.Remove(links[100]);
        Assert.That(bodies.Select(body => body.Island).Distinct().Count(), Is.EqualTo(2));
        Assert.That(bodies[1].Island, Is.Not.SameAs(bodies[0].Island));
        Assert.That(bodies[1].Island, Is.SameAs(bodies[100].Island));
        Assert.That(bodies[101].Island, Is.SameAs(bodies[0].Island));
        Assert.That(bodies[1].Island.Bodies.Count, Is.EqualTo(100));
        Assert.That(bodies[0].Island.Bodies.Count, Is.EqualTo(100));
    }

    [Test]
    public void RemovingABridge_MovesTheSmallerComponent([Values] bool chainFirst)
    {
        // A 300-body chain joined to a triangle by one bridge. The chain's search front stays
        // cheap, so a search that balances only the queued work would walk the whole chain and
        // move its 300 bodies; the triangle must be the side that runs dry and moves.
        using var world = NewWorld();
        const int length = 300;
        var chain = new RigidBody[length];
        for (int i = 0; i < length; i++)
        {
            chain[i] = world.CreateRigidBody();
            chain[i].Position = new JVector(i, 0, 0);
            if (i > 0) world.CreateConstraint<BallSocket>(chain[i - 1], chain[i]).Initialize(JVector.Zero);
        }
        var triangle = new RigidBody[3];
        for (int i = 0; i < 3; i++)
        {
            triangle[i] = world.CreateRigidBody();
            triangle[i].Position = new JVector(length + 2 + i, 0, 0);
        }
        for (int i = 0; i < 3; i++)
            world.CreateConstraint<BallSocket>(triangle[i], triangle[(i + 1) % 3]).Initialize(JVector.Zero);
        var bridge = chainFirst
            ? world.CreateConstraint<BallSocket>(chain[^1], triangle[0])
            : world.CreateConstraint<BallSocket>(triangle[0], chain[^1]);
        bridge.Initialize(JVector.Zero);
        var island = chain[0].Island;
        Assert.That(island.Bodies.Count, Is.EqualTo(length + 3));

        world.Remove(bridge);
        Assert.That(chain[0].Island, Is.SameAs(island), "The chain keeps its island.");
        Assert.That(chain[^1].Island, Is.SameAs(island));
        Assert.That(island.Bodies.Count, Is.EqualTo(length));
        Assert.That(triangle[0].Island, Is.Not.SameAs(island), "The triangle moves to a new island.");
        Assert.That(triangle[0].Island.Bodies.Count, Is.EqualTo(3));
    }

    [Test]
    public void BrokenContacts_SplitTheIslandWithinTheStepThatRetiresThem([Values] bool multiThread)
    {
        using var world = NewWorld();
        var left = world.CreateRigidBody();
        left.AddShape(new BoxShape(1));
        var right = world.CreateRigidBody();
        right.AddShape(new BoxShape(1));
        right.Position = new JVector(0.98f, 0, 0);

        world.Step(0.01f, multiThread);
        Assert.That(left.Island, Is.SameAs(right.Island), "Touching boxes share an island.");

        // Moving the body clears its contact cache; the emptied manifold is retired one step
        // after the narrow phase failed to refill it, which splits the island.
        right.Position = new JVector(10, 0, 0);
        world.Step(0.01f, multiThread);
        world.Step(0.01f, multiThread);
        Assert.That(left.Island, Is.Not.SameAs(right.Island));
        Assert.That(left.Island.Bodies.Count, Is.EqualTo(1));
        Assert.That(right.Island.Bodies.Count, Is.EqualTo(1));
    }
}
