using Jitter2.Dynamics.Constraints;

namespace JitterTests.Constraints;

public class FixedAndBoundaryLimitTests
{
    private static World CreateWorld(SolveMode mode) => new()
    {
        Gravity = JVector.Zero,
        AllowDeactivation = false,
        SubstepCount = 1,
        SolverIterations = (8, 0),
        SolveMode = mode
    };

    private static RigidBody CreateBody(World world, Real x = 0)
    {
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(x, 0, 0);
        body.Damping = (0, 0);
        return body;
    }

    [Test]
    public void PointOnPlane_FixedLimitBlocksVelocityInBothDirections(
        [Values] bool equalLimit,
        [Values(SolveMode.Regular, SolveMode.Deterministic)] SolveMode mode)
    {
        using var world = CreateWorld(mode);
        var body = CreateBody(world, equalLimit ? 3 : 0);
        var joint = world.CreateConstraint<PointOnPlane>(world.NullBody, body);
        joint.Initialize(JVector.UnitX, JVector.Zero, body.Position,
            equalLimit ? new LinearLimit(3, 3) : LinearLimit.Fixed);
        joint.Softness = 0;
        joint.Bias = 0;

        foreach (int speed in new[] { 10, -10 })
        {
            body.Velocity = new JVector(speed, 0, 0);
            world.Step((Real)0.01, false);
            Assert.That(body.Velocity.X, Is.EqualTo((Real)0).Within((Real)1e-5));
        }
    }

    [Test]
    public void PointOnLine_FixedLimitBlocksVelocityInBothDirections(
        [Values] bool equalLimit,
        [Values(SolveMode.Regular, SolveMode.Deterministic)] SolveMode mode)
    {
        using var world = CreateWorld(mode);
        var body = CreateBody(world, equalLimit ? 3 : 0);
        var joint = world.CreateConstraint<PointOnLine>(world.NullBody, body);
        joint.Initialize(JVector.UnitX, JVector.Zero, body.Position,
            equalLimit ? new LinearLimit(3, 3) : LinearLimit.Fixed);
        joint.LimitSoftness = 0;
        joint.LimitBias = 0;

        foreach (int speed in new[] { 10, -10 })
        {
            body.Velocity = new JVector(speed, 0, 0);
            world.Step((Real)0.01, false);
            Assert.That(body.Velocity.X, Is.EqualTo((Real)0).Within((Real)1e-5));
        }
    }

    [Test]
    public void DistanceLimit_FixedLimitBlocksVelocityInBothDirections(
        [Values] bool equalLimit,
        [Values(SolveMode.Regular, SolveMode.Deterministic)] SolveMode mode)
    {
        using var world = CreateWorld(mode);
        var body = CreateBody(world, 3);
        var joint = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
        joint.Initialize(JVector.Zero, body.Position,
            equalLimit ? new LinearLimit(0, 0) : LinearLimit.Fixed);
        joint.Softness = 0;
        joint.Bias = 0;

        foreach (int speed in new[] { 10, -10 })
        {
            body.Velocity = new JVector(speed, 0, 0);
            world.Step((Real)0.01, false);
            Assert.That(body.Velocity.X, Is.EqualTo((Real)0).Within((Real)1e-5));
        }
    }

    [Test]
    public void HingeAngle_FixedLimitBlocksVelocityInBothDirections(
        [Values] bool equalLimit,
        [Values(SolveMode.Regular, SolveMode.Deterministic)] SolveMode mode)
    {
        using var world = CreateWorld(mode);
        var body = CreateBody(world);
        var joint = world.CreateConstraint<HingeAngle>(world.NullBody, body);
        joint.Initialize(JVector.UnitY,
            equalLimit ? AngularLimit.FromDegree(0, 0) : AngularLimit.Fixed);
        joint.LimitSoftness = 0;
        joint.LimitBias = 0;

        foreach (int speed in new[] { 10, -10 })
        {
            body.AngularVelocity = new JVector(0, speed, 0);
            world.Step((Real)0.01, false);
            Assert.That(body.AngularVelocity.Y, Is.EqualTo((Real)0).Within((Real)1e-5));
        }
    }

    [Test]
    public void TwistAngle_FixedLimitBlocksVelocityInBothDirections(
        [Values] bool equalLimit,
        [Values(SolveMode.Regular, SolveMode.Deterministic)] SolveMode mode)
    {
        using var world = CreateWorld(mode);
        var body = CreateBody(world);
        var joint = world.CreateConstraint<TwistAngle>(world.NullBody, body);
        joint.Initialize(JVector.UnitY, JVector.UnitY,
            equalLimit ? AngularLimit.FromDegree(0, 0) : AngularLimit.Fixed);
        joint.Softness = 0;
        joint.Bias = 0;

        foreach (int speed in new[] { 10, -10 })
        {
            body.AngularVelocity = new JVector(0, speed, 0);
            world.Step((Real)0.01, false);
            Assert.That(body.AngularVelocity.Y, Is.EqualTo((Real)0).Within((Real)1e-5));
        }
    }

    [TestCase(1, 10, 0)]
    [TestCase(1, -10, -10)]
    [TestCase(-1, -10, 0)]
    [TestCase(-1, 10, 10)]
    public void PointOnPlane_RangeLimitResistsOutwardVelocityAtBoundary(
        int position, int velocity, int expectedVelocity)
    {
        using var world = CreateWorld(SolveMode.Regular);
        var body = CreateBody(world, position);
        var pointOnPlane = world.CreateConstraint<PointOnPlane>(world.NullBody, body);
        pointOnPlane.Initialize(JVector.UnitX, JVector.Zero, body.Position, new LinearLimit(-1, 1));
        pointOnPlane.Softness = 0;
        pointOnPlane.Bias = 0;
        body.Velocity = new JVector(velocity, 0, 0);

        world.Step((Real)0.01, false);
        Assert.That(body.Velocity.X, Is.EqualTo((Real)expectedVelocity).Within((Real)1e-5));
    }

    [TestCase(-1, -10, 0)]
    [TestCase(-1, 10, 10)]
    [TestCase(1, 10, 0)]
    [TestCase(1, -10, -10)]
    public void PointOnLine_RangeLimitHandlesVelocityAtBoundary(int position, int speed, int expected)
    {
        using var world = CreateWorld(SolveMode.Regular);
        var body = CreateBody(world, position);
        var joint = world.CreateConstraint<PointOnLine>(world.NullBody, body);
        joint.Initialize(JVector.UnitX, JVector.Zero, body.Position, new LinearLimit(-1, 1));
        joint.LimitSoftness = 0;
        joint.LimitBias = 0;
        body.Velocity = new JVector(speed, 0, 0);

        world.Step((Real)0.01, false);
        Assert.That(body.Velocity.X, Is.EqualTo((Real)expected).Within((Real)1e-5));
    }

    [TestCase(-1, -10, 0)]
    [TestCase(-1, 10, 10)]
    [TestCase(1, 10, 0)]
    [TestCase(1, -10, -10)]
    public void DistanceLimit_RangeLimitHandlesVelocityAtBoundary(int boundary, int speed, int expected)
    {
        using var world = CreateWorld(SolveMode.Regular);
        var body = CreateBody(world, 3);
        var joint = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
        joint.Initialize(JVector.Zero, body.Position,
            boundary == -1 ? new LinearLimit(0, 1) : new LinearLimit(-1, 0));
        joint.Softness = 0;
        joint.Bias = 0;
        body.Velocity = new JVector(speed, 0, 0);

        world.Step((Real)0.01, false);
        Assert.That(body.Velocity.X, Is.EqualTo((Real)expected).Within((Real)1e-5));
    }

    [TestCase(true, -10, 0)]
    [TestCase(true, 10, 10)]
    [TestCase(false, 10, 0)]
    [TestCase(false, -10, -10)]
    public void HingeAngle_RangeLimitHandlesVelocityAtBoundary(bool lower, int speed, int expected)
    {
        using var world = CreateWorld(SolveMode.Regular);
        var body = CreateBody(world);
        var joint = world.CreateConstraint<HingeAngle>(world.NullBody, body);
        joint.Initialize(JVector.UnitY,
            lower ? AngularLimit.FromDegree(0, 30) : AngularLimit.FromDegree(-30, 0));
        joint.LimitSoftness = 0;
        joint.LimitBias = 0;
        body.AngularVelocity = new JVector(0, speed, 0);

        world.Step((Real)0.01, false);
        Assert.That(body.AngularVelocity.Y, Is.EqualTo((Real)expected).Within((Real)1e-5));
    }

    [TestCase(true, -10, 0)]
    [TestCase(true, 10, 10)]
    [TestCase(false, 10, 0)]
    [TestCase(false, -10, -10)]
    public void TwistAngle_RangeLimitHandlesVelocityAtBoundary(bool lower, int speed, int expected)
    {
        using var world = CreateWorld(SolveMode.Regular);
        var body = CreateBody(world);
        var joint = world.CreateConstraint<TwistAngle>(world.NullBody, body);
        joint.Initialize(JVector.UnitY, JVector.UnitY,
            lower ? AngularLimit.FromDegree(0, 30) : AngularLimit.FromDegree(-30, 0));
        joint.Softness = 0;
        joint.Bias = 0;
        body.AngularVelocity = new JVector(0, speed, 0);

        world.Step((Real)0.01, false);
        Assert.That(body.AngularVelocity.Y, Is.EqualTo((Real)expected).Within((Real)1e-5));
    }

    [Test]
    public void ReversedLimitsCorrectTowardTheirMidpoints()
    {
        using (var world = CreateWorld(SolveMode.Regular))
        {
            var body = CreateBody(world);
            var joint = world.CreateConstraint<PointOnPlane>(world.NullBody, body);
            joint.Initialize(JVector.UnitX, JVector.Zero, body.Position, new LinearLimit(4, 2));
            for (int i = 0; i < 50; i++) world.Step((Real)0.01, false);
            Assert.That(body.Position.X, Is.EqualTo((Real)3).Within((Real)0.01));
        }

        using (var world = CreateWorld(SolveMode.Regular))
        {
            var body = CreateBody(world);
            var joint = world.CreateConstraint<PointOnLine>(world.NullBody, body);
            joint.Initialize(JVector.UnitX, JVector.Zero, body.Position, new LinearLimit(4, 2));
            for (int i = 0; i < 50; i++) world.Step((Real)0.01, false);
            Assert.That(joint.Distance, Is.EqualTo((Real)3).Within((Real)0.01));
        }

        using (var world = CreateWorld(SolveMode.Regular))
        {
            var body = CreateBody(world, 3);
            var joint = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
            joint.Initialize(JVector.Zero, body.Position, new LinearLimit(2, 0));
            for (int i = 0; i < 50; i++) world.Step((Real)0.01, false);
            Assert.That(joint.Distance, Is.EqualTo((Real)4).Within((Real)0.01));
        }

        using (var world = CreateWorld(SolveMode.Regular))
        {
            var body = CreateBody(world);
            var joint = world.CreateConstraint<HingeAngle>(world.NullBody, body);
            joint.Initialize(JVector.UnitY, AngularLimit.FromDegree(30, 10));
            for (int i = 0; i < 50; i++) world.Step((Real)0.01, false);
            Assert.That((Real)joint.Angle, Is.EqualTo((Real)AngularLimit.FromDegree(20, 20).From).Within((Real)0.01));
        }

        using (var world = CreateWorld(SolveMode.Regular))
        {
            var body = CreateBody(world);
            var joint = world.CreateConstraint<TwistAngle>(world.NullBody, body);
            joint.Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(30, 10));
            for (int i = 0; i < 50; i++) world.Step((Real)0.01, false);
            Assert.That((Real)joint.Angle, Is.EqualTo((Real)AngularLimit.FromDegree(20, 20).From).Within((Real)0.01));
        }
    }
}
