using Jitter2.Collision;
using Jitter2.SoftBodies;

namespace JitterTests.Behavior;

public class BroadPhaseUpdateTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void SpeculativeVelocityChangeUpdatesBroadphaseBeforeFirstStep(bool enableFirst, bool launchInPreStep)
    {
        using var world = new World { Gravity = JVector.Zero };

        var wall = world.CreateRigidBody();
        wall.AddShape(new BoxShape(10, 10, (Real)0.02));
        wall.Position = new JVector(0, 0, (Real)(-0.8));
        wall.MotionType = MotionType.Static;

        var bullet = world.CreateRigidBody();
        var shape = new SphereShape((Real)0.1);
        bullet.AddShape(shape);

        void Launch(Real _)
        {
            if (enableFirst) bullet.EnableSpeculativeContacts = true;
            bullet.Velocity = new JVector(0, 0, -100);
            if (!enableFirst) bullet.EnableSpeculativeContacts = true;

            Assert.That(shape.WorldBoundingBox.Min.Z, Is.LessThan((Real)(-0.8)));
        }

        if (launchInPreStep) world.PreStep += Launch;
        else Launch(0);

        world.Step((Real)0.01, false);

        Assert.That(bullet.Position.Z, Is.GreaterThan((Real)(-0.8)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SpeculativeVelocityChangeAfterImpulseOrShapeAttachmentUpdatesBroadphase(bool useImpulse)
    {
        using var world = new World { Gravity = JVector.Zero };
        world.Step((Real)0.005, false);
        world.Stabilize((Real)0.02, 1, 0, false);

        var bullet = world.CreateRigidBody();
        bullet.EnableSpeculativeContacts = true;
        var shape = new SphereShape((Real)0.1);
        if (useImpulse)
        {
            bullet.AddShape(shape);
            bullet.SetMassInertia(1);
            bullet.ApplyImpulse(new JVector(0, 0, -100));
        }
        else
        {
            bullet.Velocity = new JVector(0, 0, -100);
            bullet.AddShape(shape);
        }

        Assert.That(shape.WorldBoundingBox.Min.Z, Is.EqualTo((Real)(-0.6)).Within((Real)1e-4));
    }

    [TestCase]
    public void MovingSpeculativeBodyKeepsInitialSweptBoundingBox()
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        var shape = new SphereShape((Real)0.1);
        body.AddShape(shape);
        body.EnableSpeculativeContacts = true;
        body.Velocity = new JVector(0, 0, -10);

        body.Position = new JVector(0, 0, 1);

        Assert.That(shape.WorldBoundingBox.Min.Z, Is.EqualTo((Real)(-0.1)).Within((Real)1e-4));
    }

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

    [TestCase]
    public void DynamicTreeOverlap_ReturnsEveryOverlappingProxy()
    {
        var world = new World();

        var rightBody = world.CreateRigidBody();
        var rightShape = new SphereShape(1);
        rightBody.AddShape(rightShape);
        rightBody.Position = new JVector((Real)1.5, 0, 0);

        var leftBody = world.CreateRigidBody();
        var leftShape = new SphereShape(1);
        leftBody.AddShape(leftShape);
        leftBody.Position = new JVector((Real)(-1.8), 0, 0);

        var farBody = world.CreateRigidBody();
        farBody.AddShape(new SphereShape(1));
        farBody.Position = new JVector(9, 0, 0);

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        List<DynamicTree.OverlapResult> results = [];

        int count = world.DynamicTree.Overlap(query, JQuaternion.Identity, JVector.Zero, null, null, results);

        Assert.That(count, Is.EqualTo(2));
        var right = results.Single(r => r.Entity == rightShape);
        var left = results.Single(r => r.Entity == leftShape);
        Assert.That(right.Penetration, Is.EqualTo((Real)0.5).Within((Real)1e-4));
        Assert.That(left.Penetration, Is.EqualTo((Real)0.2).Within((Real)1e-4));
        Assert.That(right.Normal.X, Is.EqualTo((Real)1.0).Within((Real)1e-4));
        Assert.That(left.Normal.X, Is.EqualTo((Real)(-1.0)).Within((Real)1e-4));
        world.Dispose();
    }

    [TestCase]
    public void DynamicTreeOverlap_MovingAgainstTheNormalByTheDepthSeparates()
    {
        var world = new World();

        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(2));
        body.Orientation = JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(1, 2, 3)), (Real)0.7);

        var query = SupportPrimitives.CreateBox(new JVector((Real)0.5, (Real)0.3, (Real)0.8));
        var orientation = JQuaternion.CreateRotationY((Real)0.4);
        var position = new JVector((Real)1.4, (Real)0.3, (Real)(-0.2));
        List<DynamicTree.OverlapResult> results = [];

        Assert.That(world.DynamicTree.Overlap(query, orientation, position, null, null, results), Is.EqualTo(1));
        var hit = results[0];
        Assert.That(hit.Penetration, Is.GreaterThan((Real)0.0));
        Assert.That(hit.Normal.Length(), Is.EqualTo((Real)1.0).Within((Real)1e-4));

        var separated = position - hit.Normal * (hit.Penetration + (Real)1e-3);
        Assert.That(world.DynamicTree.Overlap(query, orientation, separated, null, null, results), Is.EqualTo(0));
        world.Dispose();
    }

    [TestCase]
    public void DynamicTreeOverlap_SkipsProxiesThatOnlyTouch()
    {
        var world = new World();

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(2, 0, 0);

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        List<DynamicTree.OverlapResult> results = [];

        Assert.That(world.DynamicTree.Overlap(query, JQuaternion.Identity, JVector.Zero, null, null, results), Is.EqualTo(0));
        Assert.That(results, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void DynamicTreeOverlap_PreAndPostFiltersSkipProxies()
    {
        var world = new World();

        var rightBody = world.CreateRigidBody();
        var rightShape = new SphereShape(1);
        rightBody.AddShape(rightShape);
        rightBody.Position = new JVector((Real)1.5, 0, 0);

        var leftBody = world.CreateRigidBody();
        var leftShape = new SphereShape(1);
        leftBody.AddShape(leftShape);
        leftBody.Position = new JVector((Real)(-1.5), 0, 0);

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        List<DynamicTree.OverlapResult> results = [];

        world.DynamicTree.Overlap(query, JQuaternion.Identity, JVector.Zero, proxy => proxy != rightShape, null, results);
        Assert.That(results.Select(r => r.Entity), Is.EquivalentTo(new IDynamicTreeProxy[] { leftShape }));

        world.DynamicTree.Overlap(query, JQuaternion.Identity, JVector.Zero, null, result => result.Entity != leftShape, results);
        Assert.That(results.Select(r => r.Entity), Is.EquivalentTo(new IDynamicTreeProxy[] { rightShape }));
        world.Dispose();
    }

    [TestCase]
    public void DynamicTreeOverlap_FindsSoftBodyShapes()
    {
        var world = new World();

        var softBody = new SoftBody(world);
        var v1 = world.CreateRigidBody();
        var v2 = world.CreateRigidBody();
        var v3 = world.CreateRigidBody();
        v1.Position = new JVector(-2, 0, -2);
        v2.Position = new JVector(2, 0, -2);
        v3.Position = new JVector(0, 0, 2);
        softBody.Vertices.AddRange([v1, v2, v3]);
        var triangle = new SoftBodyTriangle(softBody, v1, v2, v3);
        softBody.AddShape(triangle);
        world.Step((Real)(1.0 / 60.0), false);

        var query = SupportPrimitives.CreateSphere((Real)1.0);
        List<DynamicTree.OverlapResult> results = [];

        Assert.That(world.DynamicTree.Overlap(query, JQuaternion.Identity, new JVector(0, (Real)0.6, 0), null, null, results), Is.EqualTo(1));
        Assert.That(results[0].Entity, Is.EqualTo(triangle));
        Assert.That(results[0].Normal.Y, Is.EqualTo((Real)(-1.0)).Within((Real)1e-4));
        world.Dispose();
    }
}
