using Jitter2.SoftBodies;

namespace JitterTests.Api;

public class DynamicTreeAngularSweepTests
{
    private static SphereShape AddSphere(World world, JVector position)
    {
        RigidBody body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;
        SphereShape shape = new((Real)0.25);
        body.AddShape(shape);
        body.Position = position;
        return shape;
    }

    private static TransformedShape CreateOrbit() =>
        new(new SphereShape((Real)0.25), new JVector(-2, 0, 0));

    [TestCase(false)]
    [TestCase(true)]
    public void PureRotationFindsWallOutsideInitialBounds(bool transformWorld)
    {
        using var world = new World();
        JQuaternion frame = transformWorld
            ? JQuaternion.CreateRotationX((Real)0.7) * JQuaternion.CreateRotationY((Real)0.35)
            : JQuaternion.Identity;
        JVector origin = transformWorld ? new JVector(3, -2, 4) : JVector.Zero;
        RigidBody wall = world.CreateRigidBody();
        wall.MotionType = MotionType.Static;
        wall.AddShape(new BoxShape((Real)0.2, 20, 20));
        wall.Orientation = frame;
        wall.Position = origin + JVector.Transform(new JVector((Real)1.6, 0, 0), frame);
        AddSphere(world, new JVector(100, 100, 100)); // Force traversal of internal tree nodes.

        BoxShape rod = new(4, (Real)0.2, (Real)0.2);
        JQuaternion orientation = frame * JQuaternion.CreateRotationZ(MathR.PI / 2);
        JVector angular = JVector.Transform(-JVector.UnitZ, frame);
        bool hit = world.DynamicTree.SweepCast(rod, orientation, origin, JVector.Zero, angular, 2,
            null, null, out var proxy, out var pointA, out var pointB, out var normal, out Real time);

        Real expected = MathR.Asin((Real)1.5 / MathR.Sqrt((Real)4.01)) - MathR.Atan((Real)0.05);
        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(wall.Shapes[0]));
        Assert.That(time, Is.EqualTo(expected).Within((Real)2e-4));
        JVector impactPoint = origin + JVector.Transform(pointA - origin, MathHelper.RotationQuaternion(angular, time));
        Assert.That((impactPoint - pointB).Length(), Is.LessThan((Real)2e-4));
        Assert.That(normal.Length(), Is.EqualTo((Real)1).Within((Real)1e-5));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FullTurnFindsIntermediateImpactForOffsetShape(bool attachedTarget)
    {
        using var world = new World();
        Shape target;
        if (attachedTarget)
        {
            SphereShape sphere = AddSphere(world, new JVector(0, -2, 0));
            sphere.RigidBody.MotionType = MotionType.Dynamic;
            sphere.RigidBody.Velocity = new JVector(100, 100, 100);
            sphere.RigidBody.AngularVelocity = new JVector(20, 30, 40);
            target = sphere;
        }
        else
        {
            target = new TransformedShape(new SphereShape((Real)0.25), new JVector(0, -2, 0));
            target.UpdateWorldBoundingBox();
            world.DynamicTree.AddProxy(target, false);
        }
        AddSphere(world, new JVector(100, 100, 100));

        bool hit = world.DynamicTree.SweepCast(CreateOrbit(), JQuaternion.Identity, JVector.Zero,
            JVector.Zero, JVector.UnitZ, 2 * MathR.PI,
            null, null, out var proxy, out var pointA, out var pointB, out _, out Real time);

        Real expected = MathR.PI / 2 - 2 * MathR.Asin((Real)0.125);
        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(target));
        Assert.That(time, Is.EqualTo(expected).Within((Real)2e-4));
        Assert.That((JVector.Transform(pointA, MathHelper.RotationQuaternion(JVector.UnitZ, time)) - pointB).Length(),
            Is.LessThan((Real)2e-4));
    }

    [Test]
    public void RotationCanHitWhileTheOriginTranslatesAway()
    {
        using var world = new World();
        SphereShape target = AddSphere(world, new JVector((Real)1.1, 0, 0));
        AddSphere(world, new JVector(100, 100, 100));

        bool hit = world.DynamicTree.SweepCast(CreateOrbit(), JQuaternion.Identity, JVector.Zero,
            new JVector((Real)(-0.2), 0, 0), JVector.UnitZ, (Real)3.2,
            null, null, out var proxy, out _, out _, out _, out Real time);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(target));
        Assert.That(time, Is.EqualTo((Real)2.9293466801522443).Within((Real)2e-4));
    }

    [Test]
    public void TargetExtentIncludesLocalShapeOffset()
    {
        using var world = new World();
        RigidBody body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;
        TransformedShape target = new(new SphereShape((Real)0.25), new JVector(-10, -3, -3));
        body.AddShape(target);
        body.Position = new JVector(10, 1, 3);
        AddSphere(world, new JVector(100, 100, 100));

        bool hit = world.DynamicTree.SweepCast(CreateOrbit(), JQuaternion.Identity, JVector.Zero,
            JVector.Zero, JVector.UnitZ, MathR.PI,
            null, null, out var proxy, out _, out _, out _, out Real time);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(target));
        Assert.That(time, Is.EqualTo(MathR.PI / 2 - 2 * MathR.Asin((Real)0.125)).Within((Real)2e-4));
    }

    [Test]
    public void SoftBodyTriangleIsTestedAtItsCurrentVertexPositions()
    {
        using var world = new World();
        SoftBody softBody = new(world);
        foreach (JVector position in new JVector[] { new(-10, -2, -10), new(10, -2, -10), new(0, -2, 10) })
        {
            RigidBody vertex = world.CreateRigidBody();
            vertex.Position = position;
            softBody.Vertices.Add(vertex);
        }
        SoftBodyTriangle target = new(softBody, softBody.Vertices[0], softBody.Vertices[1], softBody.Vertices[2])
        {
            Thickness = 0
        };
        softBody.AddShape(target);
        AddSphere(world, new JVector(100, 100, 100));

        bool hit = world.DynamicTree.SweepCast(CreateOrbit(), JQuaternion.Identity, JVector.Zero,
            JVector.Zero, JVector.UnitZ, MathR.PI / 2,
            null, null, out var proxy, out _, out _, out _, out Real time);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(target));
        Assert.That(time, Is.EqualTo(MathR.Asin((Real)0.875)).Within((Real)2e-4));
        softBody.Destroy();
    }

    [TestCase("none")]
    [TestCase("pre")]
    [TestCase("post")]
    public void ReturnsEarliestAcceptedHit(string filter)
    {
        using var world = new World();
        Real diagonal = -MathR.Sqrt(2);
        SphereShape near = AddSphere(world, new JVector(diagonal, diagonal, 0));
        SphereShape far = AddSphere(world, new JVector(0, -2, 0));
        DynamicTree.SweepCastFilterPre? pre = filter == "pre" ? proxy => proxy != near : null;
        DynamicTree.SweepCastFilterPost? post = filter == "post" ? result => result.Entity != near : null;

        bool hit = world.DynamicTree.SweepCast(CreateOrbit(), JQuaternion.Identity, JVector.Zero,
            JVector.Zero, JVector.UnitZ, MathR.PI,
            pre, post, out var proxy, out _, out _, out _, out Real time);

        Real expected = (filter == "none" ? MathR.PI / 4 : MathR.PI / 2) - 2 * MathR.Asin((Real)0.125);
        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(filter == "none" ? near : far));
        Assert.That(time, Is.EqualTo(expected).Within((Real)2e-4));
    }

    [Test]
    public void PostFilterCanCollectHitsWithinTheTimeLimit()
    {
        using var world = new World();
        Real diagonal = -MathR.Sqrt(2);
        SphereShape near = AddSphere(world, new JVector(diagonal, diagonal, 0));
        AddSphere(world, new JVector(0, -2, 0));
        List<DynamicTree.SweepCastResult> hits = [];

        bool hit = world.DynamicTree.SweepCast(CreateOrbit(), JQuaternion.Identity, JVector.Zero,
            JVector.Zero, JVector.UnitZ, 1,
            null, result => { hits.Add(result); return false; },
            out var proxy, out var pointA, out var pointB, out var normal, out _);

        Assert.That(hit, Is.False);
        Assert.That(proxy, Is.Null);
        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Entity, Is.SameAs(near));
        Assert.That(hits[0].Lambda, Is.LessThan((Real)1));
        Assert.That(pointA, Is.EqualTo(JVector.Zero));
        Assert.That(pointB, Is.EqualTo(JVector.Zero));
        Assert.That(normal, Is.EqualTo(JVector.Zero));
    }

    [Test]
    public void PostFilterCanRunNestedAngularSweep()
    {
        using var world = new World();
        Real diagonal = -MathR.Sqrt(2);
        SphereShape near = AddSphere(world, new JVector(diagonal, diagonal, 0));
        SphereShape far = AddSphere(world, new JVector(0, -2, 0));
        TransformedShape query = CreateOrbit();
        bool nested = false;

        bool hit = world.DynamicTree.SweepCast(query, JQuaternion.Identity, JVector.Zero,
            JVector.Zero, JVector.UnitZ, MathR.PI,
            null, result =>
            {
                if (!nested)
                {
                    nested = true;
                    bool innerHit = world.DynamicTree.SweepCast(query, JQuaternion.Identity, JVector.Zero,
                        JVector.Zero, JVector.UnitZ, MathR.PI, null, null,
                        out var innerProxy, out _, out _, out _, out _);
                    Assert.That(innerHit, Is.True);
                    Assert.That(innerProxy, Is.SameAs(near));
                }
                return result.Entity != near;
            }, out var proxy, out _, out _, out _, out _);

        Assert.That(nested, Is.True);
        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(far));
    }

    [Test]
    public void ZeroTimeStillReportsInitialOverlap()
    {
        using var world = new World();
        SphereShape target = AddSphere(world, JVector.Zero);

        bool hit = world.DynamicTree.SweepCast(new SphereShape(1), JQuaternion.Identity, JVector.Zero,
            JVector.UnitX, JVector.UnitZ, 0,
            null, null, out var proxy, out _, out _, out var normal, out Real time);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.SameAs(target));
        Assert.That(time, Is.Zero);
        Assert.That(normal, Is.EqualTo(JVector.Zero));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EmptyOrUnreachableSceneReturnsNoHit(bool empty)
    {
        using var world = new World();
        if (!empty)
        {
            AddSphere(world, new JVector(10, 0, 0));
            AddSphere(world, new JVector(-10, 0, 0));
        }

        bool hit = world.DynamicTree.SweepCast(CreateOrbit(), JQuaternion.Identity, JVector.Zero,
            JVector.Zero, JVector.UnitZ, 2 * MathR.PI,
            null, null, out var proxy, out var pointA, out var pointB, out var normal, out _);

        Assert.That(hit, Is.False);
        Assert.That(proxy, Is.Null);
        Assert.That(pointA, Is.EqualTo(JVector.Zero));
        Assert.That(pointB, Is.EqualTo(JVector.Zero));
        Assert.That(normal, Is.EqualTo(JVector.Zero));
    }

    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void InvalidTimeLimitIsRejected(double maximumTime)
    {
        DynamicTree tree = new(World.DefaultDynamicTreeFilter);
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.SweepCast(
            CreateOrbit(), JQuaternion.Identity, JVector.Zero, JVector.Zero, JVector.UnitZ, (Real)maximumTime,
            null, null, out _, out _, out _, out _, out _));
    }

    [Test]
    public void ExistingLinearOnlyProxiesStillWorkWhenAngularVelocityIsZero()
    {
        DynamicTree tree = new(World.DefaultDynamicTreeFilter);
        LinearOnlyProxy target = new();
        tree.AddProxy(target, false);
        var query = SupportPrimitives.CreateSphere((Real)0.25);

        Assert.That(tree.SweepCast(query, JQuaternion.Identity, JVector.Zero,
            JVector.UnitX, JVector.Zero, 1, null, null,
            out var proxy, out _, out _, out _, out _), Is.True);
        Assert.That(proxy, Is.SameAs(target));
        Assert.That(tree.SweepCast(query, JQuaternion.Identity, JVector.Zero,
            JVector.UnitX, JVector.UnitZ, 1, null, null,
            out _, out _, out _, out _, out _), Is.False);
    }

    private sealed class LinearOnlyProxy : IDynamicTreeProxy, ISweepTestable
    {
        public int SetIndex { get; set; } = -1;
        public int NodePtr { get; set; }
        public JVector Velocity => JVector.Zero;
        public JBoundingBox WorldBoundingBox => new(new JVector(-1), new JVector(1));

        public bool Sweep<T>(in T support, in JQuaternion orientation, in JVector position, in JVector sweep,
            out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) where T : ISupportMappable
        {
            pointA = pointB = normal = JVector.Zero;
            lambda = 0;
            return true;
        }
    }
}
