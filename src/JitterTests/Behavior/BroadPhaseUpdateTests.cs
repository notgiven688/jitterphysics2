using Jitter2.Collision;

namespace JitterTests.Behavior;

public class BroadPhaseUpdateTests
{
    [TestCase]
    public void MovingBody_UpdatesDynamicTreeQueryImmediately()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var shape = new SphereShape(1);
        body.AddShape(shape);

        List<IDynamicTreeProxy> hits = [];
        world.DynamicTree.Query(hits, new JBoundingBox(new JVector(-2, -2, -2), new JVector(2, 2, 2)));
        Assert.That(hits, Does.Contain(shape));

        body.Position = new JVector(10, 0, 0);

        hits.Clear();
        world.DynamicTree.Query(hits, new JBoundingBox(new JVector(-2, -2, -2), new JVector(2, 2, 2)));
        Assert.That(hits, Does.Not.Contain(shape));

        hits.Clear();
        world.DynamicTree.Query(hits, new JBoundingBox(new JVector(8, -2, -2), new JVector(12, 2, 2)));
        Assert.That(hits, Does.Contain(shape));
        world.Dispose();
    }

    [TestCase]
    public void RotatingBody_UpdatesShapeWorldBoundingBox()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var shape = new BoxShape(4, 2, 2);
        body.AddShape(shape);

        var before = shape.WorldBoundingBox;
        var widthXBefore = before.Max.X - before.Min.X;
        var widthYBefore = before.Max.Y - before.Min.Y;

        body.Orientation = JQuaternion.CreateRotationZ(MathR.PI / (Real)2.0);

        var after = shape.WorldBoundingBox;
        var widthXAfter = after.Max.X - after.Min.X;
        var widthYAfter = after.Max.Y - after.Min.Y;

        Assert.That(widthXBefore, Is.GreaterThan(widthYBefore));
        Assert.That(widthYAfter, Is.GreaterThan(widthXAfter));
        world.Dispose();
    }

    [TestCase]
    public void MovingSleepingBody_ActivatesProxyOnNextStep()
    {
        var world = new World();
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        var shape = new SphereShape(1);
        body.AddShape(shape);
        body.DeactivationTime = TimeSpan.FromSeconds(1);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        Assert.That(body.IsActive, Is.False);
        Assert.That(world.DynamicTree.IsActive(shape), Is.False);

        body.Position = new JVector(5, 0, 0);

        Assert.That(world.DynamicTree.IsActive(shape), Is.False);

        world.Step(1f / 100f, false);

        Assert.That(body.IsActive, Is.True);
        Assert.That(world.DynamicTree.IsActive(shape), Is.True);
        world.Dispose();
    }

    [TestCase]
    public void DynamicTreeSweepCast_ReturnsClosestBroadPhaseHit()
    {
        var world = new World();

        var nearBody = world.CreateRigidBody();
        var nearShape = new SphereShape(1);
        nearBody.AddShape(nearShape);
        nearBody.Position = new JVector(5, 0, 0);

        var farBody = world.CreateRigidBody();
        var farShape = new SphereShape(1);
        farBody.AddShape(farShape);
        farBody.Position = new JVector(9, 0, 0);

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        bool hit = world.DynamicTree.SweepCast(query,
            JQuaternion.Identity, JVector.Zero, new JVector(10, 0, 0),
            null, null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real lambda);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.EqualTo(nearShape));
        Assert.That(lambda, Is.EqualTo((Real)0.3).Within((Real)1e-6));
        world.Dispose();
    }

    [TestCase]
    public void DynamicTreeSweepCast_DefaultOverloadIsUnbounded()
    {
        var world = new World();

        var body = world.CreateRigidBody();
        var shape = new SphereShape(1);
        body.AddShape(shape);
        body.Position = new JVector(5, 0, 0);

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        bool hit = world.DynamicTree.SweepCast(query,
            JQuaternion.Identity, JVector.Zero, new JVector(1, 0, 0),
            null, null,
            out IDynamicTreeProxy? proxy, out _, out _, out _, out Real lambda);

        Assert.That(hit, Is.True);
        Assert.That(proxy, Is.EqualTo(shape));
        Assert.That(lambda, Is.EqualTo((Real)3.0).Within((Real)1e-6));
        world.Dispose();
    }

    [TestCase]
    public void DynamicTreeSweepCast_PostFilterCanCaptureCandidatesWithinMaxLambda()
    {
        var world = new World();

        var nearBody = world.CreateRigidBody();
        var nearShape = new SphereShape(1);
        nearBody.AddShape(nearShape);
        nearBody.Position = new JVector(5, 0, 0);

        var farBody = world.CreateRigidBody();
        var farShape = new SphereShape(1);
        farBody.AddShape(farShape);
        farBody.Position = new JVector(9, 0, 0);

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        List<DynamicTree.SweepCastResult> hits = [];

        bool hit = world.DynamicTree.SweepCast(query,
            JQuaternion.Identity, JVector.Zero, new JVector(10, 0, 0),
            (Real)0.5,
            null,
            result =>
            {
                hits.Add(result);
                return false;
            },
            out _, out _, out _, out _, out _);

        Assert.That(hit, Is.False);
        Assert.That(hits, Has.Count.EqualTo(1));
        Assert.That(hits[0].Entity, Is.EqualTo(nearShape));
        Assert.That(hits[0].Lambda, Is.EqualTo((Real)0.3).Within((Real)1e-6));
        world.Dispose();
    }
}
