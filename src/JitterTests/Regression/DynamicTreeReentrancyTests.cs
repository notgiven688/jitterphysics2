using Jitter2.DataStructures;

namespace JitterTests.Regression;

public class DynamicTreeReentrancyTests
{
    private static SphereShape CreateStaticSphere(World world, JVector position)
    {
        var body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;
        body.Position = position;

        var shape = new SphereShape((Real)1.0);
        body.AddShape(shape);
        return shape;
    }

    private static BoxShape CreateStaticBox(World world, JVector position)
    {
        var body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;
        body.Position = position;

        var shape = new BoxShape((Real)2.0);
        body.AddShape(shape);
        return shape;
    }

    [Test]
    public void BoxQuery_CanRunNestedQueryFromSink()
    {
        using var world = new World();

        var shapes = new IDynamicTreeProxy[]
        {
            CreateStaticSphere(world, new JVector((Real)0.0, (Real)0.0, (Real)0.0)),
            CreateStaticSphere(world, new JVector((Real)4.0, (Real)0.0, (Real)0.0)),
            CreateStaticSphere(world, new JVector((Real)8.0, (Real)0.0, (Real)0.0)),
            CreateStaticSphere(world, new JVector((Real)12.0, (Real)0.0, (Real)0.0))
        };

        var nestedBox = new JBoundingBox(
            new JVector(-(Real)2.0, -(Real)2.0, -(Real)2.0),
            new JVector((Real)2.0, (Real)2.0, (Real)2.0));
        var sink = new NestedQuerySink(world.DynamicTree, nestedBox);

        var queryBox = new JBoundingBox(
            new JVector(-(Real)2.0, -(Real)2.0, -(Real)2.0),
            new JVector((Real)14.0, (Real)2.0, (Real)2.0));
        world.DynamicTree.Query(ref sink, queryBox);

        Assert.That(sink.Hits, Is.EquivalentTo(shapes));
    }

    [Test]
    public void RayCast_PostFilterCanRunNestedRayCast()
    {
        using var world = new World();

        var near = CreateStaticSphere(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));
        var far = CreateStaticSphere(world, new JVector((Real)9.0, (Real)0.0, (Real)0.0));
        bool nested = false;

        bool hit = world.DynamicTree.RayCast(
            JVector.Zero, JVector.UnitX,
            null,
            result =>
            {
                RunNestedRayCastOnce(world, ref nested);
                return !ReferenceEquals(result.Entity, near);
            },
            out IDynamicTreeProxy? proxy, out _, out _);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(far));
    }

    [Test]
    public void FindNearest_PostFilterCanRunNestedFindNearest()
    {
        using var world = new World();

        var near = CreateStaticBox(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));
        var far = CreateStaticBox(world, new JVector((Real)8.0, (Real)0.0, (Real)0.0));
        bool nested = false;

        bool hit = world.DynamicTree.FindNearestSphere(
            (Real)1.0, JVector.Zero,
            null,
            result =>
            {
                RunNestedFindNearestOnce(world, ref nested);
                return !ReferenceEquals(result.Entity, near);
            },
            out IDynamicTreeProxy? proxy, out _, out _, out _, out _);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(far));
    }

    [Test]
    public void SweepCast_PostFilterCanRunNestedSweepCast()
    {
        using var world = new World();

        var near = CreateStaticSphere(world, new JVector((Real)5.0, (Real)0.0, (Real)0.0));
        var far = CreateStaticSphere(world, new JVector((Real)9.0, (Real)0.0, (Real)0.0));
        var query = SupportPrimitives.CreateSphere((Real)1.0);
        bool nested = false;

        bool hit = world.DynamicTree.SweepCast(
            query, JQuaternion.Identity, JVector.Zero, new JVector((Real)10.0, (Real)0.0, (Real)0.0),
            null,
            result =>
            {
                RunNestedSweepCastOnce(world, ref nested);
                return !ReferenceEquals(result.Entity, near);
            },
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real lambda);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(far));
        Assert.That(lambda, Is.EqualTo((Real)0.7).Within((Real)1e-6));
    }

    [Test]
    public void Overlap_PostFilterCanRunNestedOverlap()
    {
        using var world = new World();

        var near = CreateStaticSphere(world, new JVector((Real)1.5, (Real)0.0, (Real)0.0));
        var far = CreateStaticSphere(world, new JVector((Real)(-1.5), (Real)0.0, (Real)0.0));
        var query = SupportPrimitives.CreateSphere((Real)1.0);
        List<DynamicTree.OverlapResult> results = [];
        bool nested = false;

        int count = world.DynamicTree.Overlap(
            query, JQuaternion.Identity, JVector.Zero,
            null,
            result =>
            {
                RunNestedOverlapOnce(world, ref nested);
                return !ReferenceEquals(result.Entity, near);
            },
            results);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(results[0].Entity, Is.SameAs(far));
    }

    private static void RunNestedOverlapOnce(World world, ref bool nested)
    {
        if (nested) return;
        nested = true;

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        List<DynamicTree.OverlapResult> results = [];
        int count = world.DynamicTree.Overlap(query, JQuaternion.Identity, JVector.Zero, null, null, results);

        Assert.That(count, Is.EqualTo(2));
    }

    private static void RunNestedRayCastOnce(World world, ref bool nested)
    {
        if (nested) return;
        nested = true;

        bool hit = world.DynamicTree.RayCast(
            JVector.Zero, JVector.UnitX,
            null, null,
            out IDynamicTreeProxy? proxy, out _, out _);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.Not.Null);
    }

    private static void RunNestedFindNearestOnce(World world, ref bool nested)
    {
        if (nested) return;
        nested = true;

        bool hit = world.DynamicTree.FindNearestPoint(
            JVector.Zero, null, null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out _);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.Not.Null);
    }

    private static void RunNestedSweepCastOnce(World world, ref bool nested)
    {
        if (nested) return;
        nested = true;

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        bool hit = world.DynamicTree.SweepCast(
            query, JQuaternion.Identity, JVector.Zero, new JVector((Real)10.0, (Real)0.0, (Real)0.0),
            null, null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out _);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.Not.Null);
    }

    private sealed class NestedQuerySink(DynamicTree tree, JBoundingBox nestedBox) : ISink<IDynamicTreeProxy>
    {
        private bool nested;

        public List<IDynamicTreeProxy> Hits { get; } = [];

        public void Add(in IDynamicTreeProxy item)
        {
            Hits.Add(item);

            if (nested) return;
            nested = true;

            List<IDynamicTreeProxy> nestedHits = [];
            tree.Query(nestedHits, nestedBox);

            Assert.That(nestedHits, Is.Not.Empty);
        }
    }
}
