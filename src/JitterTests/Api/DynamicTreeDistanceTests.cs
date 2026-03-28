namespace JitterTests.Api;

public class DynamicTreeDistanceTests
{
    private static RigidBody CreateStaticBox(World world, JVector position, Real size = (Real)2.0)
    {
        var body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;
        body.Position = position;
        body.AddShape(new BoxShape(size));
        return body;
    }

    [Test]
    public void FindNearestPoint_ReturnsClosestSeparatedProxy()
    {
        using var world = new World();

        var nearBody = CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));
        CreateStaticBox(world, new JVector((Real)9.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestPoint(JVector.Zero, null, null,
            out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(nearBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)4.0).Within((Real)5e-5));
        Assert.That(pointA, Is.EqualTo(JVector.Zero));
        Assert.That(pointB.X, Is.EqualTo((Real)4.0).Within((Real)5e-5));
        Assert.That(pointB.Y, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(pointB.Z, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(normal.X, Is.EqualTo((Real)1.0).Within((Real)5e-5));
        Assert.That(normal.Y, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(normal.Z, Is.EqualTo((Real)0.0).Within((Real)1e-4));
    }

    [Test]
    public void FindNearestSphere_ReturnsClosestSeparatedProxy()
    {
        using var world = new World();

        var nearBody = CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));
        CreateStaticBox(world, new JVector((Real)9.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero, null, null,
            out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(nearBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)3.0).Within((Real)5e-5));
        Assert.That(pointA.X, Is.EqualTo((Real)1.0).Within((Real)5e-5));
        Assert.That(pointA.Y, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(pointA.Z, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(pointB.X, Is.EqualTo((Real)4.0).Within((Real)5e-5));
        Assert.That(pointB.Y, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(pointB.Z, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(normal.X, Is.EqualTo((Real)1.0).Within((Real)5e-5));
        Assert.That(normal.Y, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        Assert.That(normal.Z, Is.EqualTo((Real)0.0).Within((Real)1e-4));
    }

    [Test]
    public void FindNearestSphere_MaxDistanceExcludesFartherBodies()
    {
        using var world = new World();

        CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero, (Real)2.99, null, null,
            out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance);

        Assert.That(hit, Is.False);
        Assert.That(proxy, Is.Null);
        Assert.That(distance, Is.EqualTo((Real)2.99).Within((Real)1e-6));
        Assert.That(pointA, Is.EqualTo(default(JVector)));
        Assert.That(pointB, Is.EqualTo(default(JVector)));
        Assert.That(normal, Is.EqualTo(default(JVector)));
    }

    [Test]
    public void FindNearestSphere_MaxDistanceIncludesExactBound()
    {
        using var world = new World();

        var body = CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero, (Real)3.0, null, null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real distance);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(body.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)3.0).Within((Real)5e-5));
    }

    [Test]
    public void FindNearestSphere_ReturnsFalseAndZeroDistanceOnOverlap()
    {
        using var world = new World();

        var overlappingBody = CreateStaticBox(world, JVector.Zero);
        CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero, null, null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real distance);

        Assert.That(hit, Is.False);
        Assert.That(proxy, Is.SameAs(overlappingBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)0.0));
    }

    [Test]
    public void FindNearestSphere_ZeroMaxDistanceReturnsOverlap()
    {
        using var world = new World();

        var overlappingBody = CreateStaticBox(world, JVector.Zero);
        CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero, (Real)0.0, null, null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real distance);

        Assert.That(hit, Is.False);
        Assert.That(proxy, Is.SameAs(overlappingBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)0.0));
    }

    [Test]
    public void FindNearestSphere_OverlapRespectsPreFilter()
    {
        using var world = new World();

        var overlappingBody = CreateStaticBox(world, JVector.Zero);
        var separatedBody = CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));

        // Pre-filter excludes the overlapping body — should fall through to the separated one.
        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero,
            proxy => !ReferenceEquals(proxy, overlappingBody.Shapes[0]), null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real distance);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(separatedBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)3.0).Within((Real)5e-5));
    }

    [Test]
    public void FindNearestSphere_PostFilterCanSkipOverlapAndReturnSeparated()
    {
        using var world = new World();

        CreateStaticBox(world, JVector.Zero);
        var separatedBody = CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));

        // Post-filter rejects overlap results (distance == 0) — search continues to find separated proxy.
        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero, null,
            result => result.Distance > (Real)0.0,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real distance);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(separatedBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)3.0).Within((Real)5e-5));
    }

    [Test]
    public void FindNearestSphere_PreFilterCanExcludeCloserCandidate()
    {
        using var world = new World();

        var filteredBody = CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));
        var keptBody = CreateStaticBox(world, new JVector((Real)8.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero,
            proxy => !ReferenceEquals(proxy, filteredBody.Shapes[0]), null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real distance);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(keptBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)6.0).Within((Real)5e-5));
    }

    [Test]
    public void FindNearestSphere_PostFilterCanRejectNearestAcceptedResult()
    {
        using var world = new World();

        CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));
        var fartherBody = CreateStaticBox(world, new JVector((Real)8.0, (Real)0.0, (Real)0.0));

        bool hit = world.DynamicTree.FindNearestSphere((Real)1.0, JVector.Zero, null,
            result => result.Distance > (Real)3.5,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real distance);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(fartherBody.Shapes[0]));
        Assert.That(distance, Is.EqualTo((Real)6.0).Within((Real)5e-5));
    }
}
