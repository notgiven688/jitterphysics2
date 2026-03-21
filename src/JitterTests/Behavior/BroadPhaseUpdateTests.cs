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
}
