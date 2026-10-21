using Jitter2.Collision;

namespace JitterTests.Behavior;

public class BroadPhaseUpdateTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void CcdBoundsPreserveLinearExpansionWithoutRotation(bool enableFirst, bool hasLinearVelocity)
    {
        using var world = new World { Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        var shape = new BoxShape(4, (Real)0.2, (Real)0.2);
        body.AddShape(shape);
        var tightBox = shape.WorldBoundingBox;

        if (enableFirst) body.EnableCcd = true;
        body.Velocity = hasLinearVelocity ? new JVector(3, 4, 0) : JVector.Zero;
        if (!enableFirst) body.EnableCcd = true;

        shape.UpdateWorldBoundingBox((Real)0.02);

        JVector padding = hasLinearVelocity ? new JVector((Real)0.1) : JVector.Zero;
        Assert.That((shape.WorldBoundingBox.Min - (tightBox.Min - padding)).Length(),
            Is.LessThan((Real)1e-5));
        Assert.That((shape.WorldBoundingBox.Max - (tightBox.Max + padding)).Length(),
            Is.LessThan((Real)1e-5));

        body.EnableCcd = false;
        Assert.That(shape.WorldBoundingBox.Min, Is.EqualTo(tightBox.Min));
        Assert.That(shape.WorldBoundingBox.Max, Is.EqualTo(tightBox.Max));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AngularOnlyImpulseUpdatesMotionBoundsImmediately(bool enableCcd)
    {
        using var world = new World { Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        var shape = new BoxShape(4, (Real)0.2, (Real)0.2);
        body.AddShape(shape);
        body.SetMassInertia(JSymmetricMatrix.Identity, 1);
        body.AllowedMotion = MotionAxes.AngularZ;
        body.EnableCcd = enableCcd;

        var tightBox = shape.WorldBoundingBox;
        body.ApplyImpulse(new JVector(0, 100, 0), new JVector(1, 0, 0));

        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.AngularVelocity.Z, Is.GreaterThan((Real)0.0));
        if (enableCcd)
        {
            Assert.That(shape.WorldBoundingBox.Max.Y, Is.GreaterThan((Real)1.5));
            Assert.That(shape.WorldBoundingBox.Min.Y, Is.LessThan((Real)(-1.5)));
        }
        else Assert.That(shape.WorldBoundingBox, Is.EqualTo(tightBox));
        Assert.That(shape.WorldBoundingBox.Min.Z, Is.EqualTo(tightBox.Min.Z));
        Assert.That(shape.WorldBoundingBox.Max.Z, Is.EqualTo(tightBox.Max.Z));

        List<IDynamicTreeProxy> hits = [];
        world.DynamicTree.Query(hits, new JBoundingBox(new JVector(0, (Real)1.5, 0), new JVector((Real)0.1, (Real)1.6, (Real)0.1)));
        Assert.That(hits.Contains(shape), Is.EqualTo(enableCcd));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void CcdVelocityChangeUpdatesBroadphaseBeforeFirstStep(bool enableFirst, bool launchInPreStep)
    {
        using var world = new World { Gravity = JVector.Zero };

        var wall = world.CreateRigidBody();
        wall.AddShape(new BoxShape(10, 10, (Real)0.02));
        wall.Position = new JVector(0, 0, (Real)(-0.8));
        wall.MotionType = MotionType.Static;

        var bullet = world.CreateRigidBody();
        var shape = new SphereShape((Real)0.1);
        bullet.AddShape(shape);

        void Launch(TimeStep _)
        {
            if (enableFirst) bullet.EnableCcd = true;
            bullet.Velocity = new JVector(0, 0, -100);
            if (!enableFirst) bullet.EnableCcd = true;

            Assert.That(shape.WorldBoundingBox.Min.Z, Is.LessThan((Real)(-0.8)));
        }

        if (launchInPreStep) world.PreStep += Launch;
        else Launch(default);

        world.Step((Real)0.01, false);

        Assert.That(bullet.Position.Z, Is.GreaterThan((Real)(-0.8)));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void VelocityChangeAfterImpulseOrShapeAttachmentUpdatesBroadphase(bool useImpulse, bool enableCcd)
    {
        using var world = new World { Gravity = JVector.Zero };
        world.Step((Real)0.005, false);
        world.Stabilize((Real)0.02, 1, 0, false);

        var bullet = world.CreateRigidBody();
        bullet.EnableCcd = enableCcd;
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

        Real expectedMin = enableCcd ? (Real)(-0.6) : (Real)(-0.1);
        Assert.That(shape.WorldBoundingBox.Min.Z, Is.EqualTo(expectedMin).Within((Real)1e-4));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MovingBodyKeepsInitialSweptBoundingBox(bool enableCcd)
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        var shape = new SphereShape((Real)0.1);
        body.AddShape(shape);
        body.EnableCcd = enableCcd;
        body.Velocity = new JVector(0, 0, -10);

        body.Position = new JVector(0, 0, 1);

        Real expectedMin = enableCcd ? (Real)(-0.1) : (Real)0.9;
        Assert.That(shape.WorldBoundingBox.Min.Z, Is.EqualTo(expectedMin).Within((Real)1e-4));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StoppingMotionUpdatesBoundsImmediately(bool makeStatic)
    {
        using var world = new World { Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        var shape = new BoxShape(4, (Real)0.2, (Real)0.2);
        body.AddShape(shape);
        JBoundingBox tight = shape.WorldBoundingBox;
        body.EnableCcd = true;
        body.Velocity = new JVector(0, 20, 0);
        body.AngularVelocity = new JVector(0, 0, 1000);
        Assert.That(shape.WorldBoundingBox.Max.Y, Is.GreaterThan((Real)1.5));

        if (makeStatic) body.MotionType = MotionType.Static;
        else body.AllowedMotion = MotionAxes.None;

        Assert.That(shape.WorldBoundingBox, Is.EqualTo(tight));
        List<IDynamicTreeProxy> hits = [];
        world.DynamicTree.Query(hits, new JBoundingBox(new JVector(0, (Real)1.5, 0),
            new JVector((Real)0.1, (Real)1.6, (Real)0.1)));
        Assert.That(hits, Does.Not.Contain(shape));
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
}
