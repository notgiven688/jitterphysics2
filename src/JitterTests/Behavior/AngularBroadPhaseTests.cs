namespace JitterTests.Behavior;

public class AngularBroadPhaseTests
{
    [TestCase(false, 0.03)]
    [TestCase(false, 0.5)]
    [TestCase(false, 3.0)]
    [TestCase(false, 12.566370614359172)]
    [TestCase(true, 0.03)]
    [TestCase(true, 0.5)]
    [TestCase(true, 3.0)]
    [TestCase(true, 12.566370614359172)]
    public void SweptBoundsContainIntermediatePoses(bool offsetShape, double angle)
    {
        using var world = new World { Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        RigidBodyShape shape = offsetShape
            ? new TransformedShape(new SphereShape((Real)0.25), new JVector(3, -1, 2))
            : new BoxShape(4, (Real)0.2, (Real)0.6);
        body.AddShape(shape, MassInertiaUpdateMode.Preserve);
        body.Position = new JVector(3, -2, 4);
        body.Orientation = JQuaternion.CreateRotationY((Real)0.4) * JQuaternion.CreateRotationX((Real)0.3);
        body.EnableCcd = true;
        const Real dt = (Real)0.02;
        body.AngularVelocity = JVector.Normalize(new JVector(7, -11, 9)) * ((Real)angle / dt);
        body.Velocity = new JVector(-3, 2, -1);
        shape.UpdateWorldBoundingBox(dt);
        JBoundingBox swept = shape.WorldBoundingBox;

        JVector violation = JVector.Zero;
        for (int i = 0; i <= 256; i++)
        {
            Real time = dt * i / 256;
            JQuaternion orientation = MathHelper.RotationQuaternion(body.AngularVelocity, time) * body.Orientation;
            shape.CalculateBoundingBox(orientation, body.Position + time * body.Velocity, out JBoundingBox actual);
            violation = JVector.Max(violation, swept.Min - actual.Min);
            violation = JVector.Max(violation, actual.Max - swept.Max);
        }
        Assert.That(JVector.MaxAbs(violation), Is.LessThan((Real)3e-5));
    }

    [Test]
    public void FastRotationStaysBoundedAndDoesNotExpandAlongItsAxis()
    {
        using var world = new World { Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        var shape = new BoxShape(4, (Real)0.2, (Real)0.2);
        body.AddShape(shape);
        body.EnableCcd = true;
        body.AngularVelocity = new JVector(0, 0, 100000);
        shape.UpdateWorldBoundingBox((Real)0.1);

        JBoundingBox box = shape.WorldBoundingBox;
        Assert.That(box.Max.X, Is.InRange((Real)2.0, (Real)2.01));
        Assert.That(box.Max.Y, Is.InRange((Real)2.0, (Real)2.01));
        Assert.That(box.Min.X, Is.InRange((Real)(-2.01), (Real)(-2.0)));
        Assert.That(box.Min.Y, Is.InRange((Real)(-2.01), (Real)(-2.0)));
        Assert.That(box.Min.Z, Is.EqualTo((Real)(-0.1)));
        Assert.That(box.Max.Z, Is.EqualTo((Real)0.1));
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(true, true, true)]
    public void AngularVelocityAndShapeAttachmentUpdateTreeQueries(bool kinematic, bool attachLast, bool enableCcd)
    {
        using var world = new World { Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        if (kinematic) body.MotionType = MotionType.Kinematic;
        body.EnableCcd = enableCcd;
        var shape = new BoxShape(4, (Real)0.2, (Real)0.2);
        if (!attachLast) body.AddShape(shape);
        body.AngularVelocity = new JVector(0, 0, 1000);
        if (attachLast) body.AddShape(shape);

        AssertQuery(enableCcd);
        body.AngularVelocity = JVector.Zero;
        AssertQuery(false);
        body.AngularVelocity = new JVector(0, 0, 1000);
        AssertQuery(enableCcd);
        body.EnableCcd = false;
        AssertQuery(false);

        void AssertQuery(bool expected)
        {
            List<IDynamicTreeProxy> hits = [];
            world.DynamicTree.Query(hits, new JBoundingBox(new JVector(0, (Real)1.5, 0), new JVector((Real)0.1, (Real)1.6, (Real)0.1)));
            Assert.That(hits.Contains(shape), Is.EqualTo(expected));
        }
    }

    private sealed class RejectPair : IBroadPhaseFilter
    {
        public int Calls;
        public bool Filter(IDynamicTreeProxy a, IDynamicTreeProxy b)
        {
            Interlocked.Increment(ref Calls);
            return false;
        }
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(true, true, true)]
    public void FullTurnNotifiesBroadphaseBeforeIntegration(bool enableFirst, bool launchInPreStep, bool multiThread)
    {
        using var world = new World { Gravity = JVector.Zero };
        var wall = world.CreateRigidBody();
        wall.AddShape(new BoxShape((Real)0.1, 10, 10));
        wall.Position = new JVector((Real)1.6, 0, 0);
        wall.MotionType = MotionType.Static;
        var rotor = world.CreateRigidBody();
        rotor.AddShape(new BoxShape(4, (Real)0.2, (Real)0.2));
        rotor.Orientation = JQuaternion.CreateRotationZ(MathR.PI / 2);
        rotor.Damping = (0, 0);
        var filter = new RejectPair();
        world.BroadPhaseFilter = filter;
        const Real dt = (Real)0.01;

        void Launch(TimeStep _)
        {
            if (enableFirst) rotor.EnableCcd = true;
            rotor.AngularVelocity = new JVector(0, 0, -2 * MathR.PI / dt);
            if (!enableFirst) rotor.EnableCcd = true;
        }
        if (launchInPreStep) world.PreStep += Launch;
        else Launch(default);

        world.Step(dt, multiThread);
        Assert.That(filter.Calls, Is.GreaterThan(0));
    }

    [Test]
    public void AutomaticBroadphaseUpdateIncludesAngularVelocityFromTheSolver()
    {
        using var world = new World { Gravity = JVector.Zero };
        var wall = world.CreateRigidBody();
        wall.AddShape(new BoxShape((Real)0.1, 10, 10));
        wall.Position = new JVector((Real)1.6, 0, 0);
        wall.MotionType = MotionType.Static;
        var rotor = world.CreateRigidBody();
        rotor.AddShape(new BoxShape(4, (Real)0.2, (Real)0.2));
        rotor.Orientation = JQuaternion.CreateRotationZ(MathR.PI / 2);
        rotor.Damping = (0, 0);
        rotor.EnableCcd = true;
        var filter = new RejectPair();
        world.BroadPhaseFilter = filter;
        const Real dt = (Real)0.01;
        // Solver writes bypass the public setter. The regular end-of-step tree update
        // must still include the resulting angular motion when preparing the next frame.
        rotor.Data.AngularVelocity = new JVector(0, 0, -2 * MathR.PI / dt);
        world.Step(dt, false);
        filter.Calls = 0;
        world.Step(dt, false);
        Assert.That(filter.Calls, Is.GreaterThan(0));
    }

    [Test]
    public void DisabledCcdAndZeroTimestepKeepTightBounds()
    {
        using var world = new World { Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        var shape = new BoxShape(4, (Real)0.2, (Real)0.2);
        body.AddShape(shape);
        JBoundingBox tight = shape.WorldBoundingBox;
        body.AngularVelocity = new JVector(0, 0, 1000);
        shape.UpdateWorldBoundingBox((Real)0.1);
        Assert.That(shape.WorldBoundingBox, Is.EqualTo(tight));
        body.EnableCcd = true;
        shape.UpdateWorldBoundingBox(0);
        Assert.That(shape.WorldBoundingBox, Is.EqualTo(tight));
    }
}
