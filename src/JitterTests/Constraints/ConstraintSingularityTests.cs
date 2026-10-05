using Jitter2.SoftBodies;

namespace JitterTests.Constraints;

[TestFixture]
public class ConstraintSingularityTests
{
    [TestCase(1e-7, 0)]
    [TestCase(1e-10, 0)]
    [TestCase(1e-7, 1e-5)]
    [TestCase(1e-10, 1e-5)]
    public void Spring_TinySeparationPreservesResponseAndWarmStart(double distance, double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var spring = world.CreateConstraint<SpringConstraint>(world.NullBody, body);
        spring.Initialize(JVector.Zero, JVector.UnitY * (Real)distance);
        spring.Softness = (Real)softness;
        spring.Bias = 0;
        body.Velocity = JVector.UnitY;

        SpringConstraint.PrepareForIterationSpringConstraint(ref spring.Handle.Data, 100);
        SpringConstraint.IterateSpringConstraint(ref spring.Handle.Data, 100);
        Real expectedImpulse = -(Real)1 / (1 + 100 * (Real)softness);
        Assert.That(spring.Impulse, Is.EqualTo(expectedImpulse).Within((Real)1e-5));
        Assert.That(body.Velocity.Y, Is.EqualTo(1 + expectedImpulse).Within((Real)1e-5));

        spring.Anchor2 = JVector.UnitY * (Real)1e-5;
        SpringConstraint.PrepareForIterationSpringConstraint(ref spring.Handle.Data, 100);
        Assert.That(body.Velocity.Y, Is.EqualTo(1 + 2 * expectedImpulse).Within((Real)1e-5));
    }

    [TestCase(0)]
    [TestCase(1e-5)]
    public void Spring_CoincidentAnchorsClearImpulseAndResumeAtNonzeroSeparation(double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var spring = world.CreateConstraint<SpringConstraint>(world.NullBody, body);
        spring.Initialize(JVector.Zero, JVector.UnitY);
        spring.Softness = (Real)softness;
        spring.Bias = 0;
        body.Velocity = JVector.UnitY;
        SpringConstraint.PrepareForIterationSpringConstraint(ref spring.Handle.Data, 100);
        SpringConstraint.IterateSpringConstraint(ref spring.Handle.Data, 100);
        Assert.That(spring.Impulse, Is.Not.Zero);

        spring.Anchor2 = JVector.Zero;
        spring.Bias = (Real)0.2;
        body.Velocity = JVector.UnitY;
        SpringConstraint.PrepareForIterationSpringConstraint(ref spring.Handle.Data, 100);
        SpringConstraint.IterateSpringConstraint(ref spring.Handle.Data, 100);
        Assert.That(spring.Data.AccumulatedImpulse, Is.Zero);
        Assert.That(body.Velocity, Is.EqualTo(JVector.UnitY));

        spring.Anchor2 = JVector.UnitY;
        SpringConstraint.PrepareForIterationSpringConstraint(ref spring.Handle.Data, 100);
        SpringConstraint.IterateSpringConstraint(ref spring.Handle.Data, 100);
        Assert.That(body.Velocity.Y, Is.LessThan((Real)0.01));
    }

    [Test]
    public void SpringParameters_TinyDefinedDirectionHonorsTranslationLocks()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.AllowedMotion = MotionAxes.LinearY;
        var spring = world.CreateConstraint<SpringConstraint>(world.NullBody, body);
        spring.Initialize(JVector.Zero, JVector.UnitX * (Real)1e-7);
        spring.SetSpringParameters(2, 1, (Real)0.01);
        Assert.That(spring.Softness, Is.Zero);
    }

    private static World CreateWorld(SolveMode mode) => new()
    {
        SolveMode = mode, Gravity = JVector.Zero, AllowDeactivation = false,
        SubstepCount = 1, SolverIterations = (8, 0)
    };

    private static RigidBody CreateBody(World world, MotionAxes axes)
    {
        RigidBody body = world.CreateRigidBody();
        body.Damping = (0, 0);
        body.EnableGyroscopicForces = false;
        body.AllowedMotion = axes;
        return body;
    }

    [Test]
    public void Twist_HalfTurnRecoversWithoutHugeAngularVelocity(
        [Values(0, 1e-5, 0.005)] double offset,
        [Values(-1, 1)] int sign,
        [Values(0, 0.001)] double softness,
        [Values(MotionAxes.All, MotionAxes.AngularZ)] MotionAxes axes,
        [Values(SolveMode.Regular, SolveMode.Deterministic)] SolveMode mode)
    {
        using World world = CreateWorld(mode);
        RigidBody body = CreateBody(world, axes);
        var twist = world.CreateConstraint<TwistAngle>(world.NullBody, body);
        twist.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.Fixed);
        twist.Softness = (Real)softness;
        body.Orientation = offset == 0 ? new JQuaternion(0, 0, sign, 0) :
            JQuaternion.CreateRotationZ(sign * (MathR.PI - (Real)offset));

        for (int i = 0; i < 100; i++)
        {
            world.Step((Real)0.01, false);
            Assert.That(body.AngularVelocity.Length(), Is.LessThan((Real)100));
            Assert.That(Real.IsFinite(body.Orientation.LengthSquared()), Is.True);
        }
        Assert.That(MathR.Abs((Real)twist.Angle), Is.LessThan((Real)0.001));
    }

    [Test]
    public void Cone_EndpointPosesRecoverIntoAllowedRange(
        [Values(0, 1e-5, 0.002, 0.1)] double offset,
        [Values] bool antiparallel,
        [Values(0, 0.001)] double softness,
        [Values(MotionAxes.All, MotionAxes.AngularX)] MotionAxes axes,
        [Values(SolveMode.Regular, SolveMode.Deterministic)] SolveMode mode)
    {
        using World world = CreateWorld(mode);
        RigidBody body = CreateBody(world, axes);
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(30, 60));
        cone.Softness = (Real)softness;
        body.Orientation = antiparallel && offset == 0 ? new JQuaternion(1, 0, 0, 0) :
            JQuaternion.CreateRotationX(antiparallel ? MathR.PI - (Real)offset : (Real)offset);

        for (int i = 0; i < 100; i++)
        {
            world.Step((Real)0.01, false);
            Assert.That(body.AngularVelocity.Length(), Is.LessThan((Real)100));
            Assert.That(Real.IsFinite(body.Orientation.LengthSquared()), Is.True);
        }
        Assert.That((Real)cone.Angle, Is.InRange(MathR.PI / 6 - (Real)0.001, MathR.PI / 3 + (Real)0.001));
    }

    [Test]
    public void Twist_AngleFallbackJacobianMatchesFiniteDifference([Values(0, 0.005)] double swing)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody(), body2 = world.CreateRigidBody();
        body1.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        body2.Orientation = body1.Orientation;
        JVector axis = JVector.Normalize(new JVector(1, 3, 2));
        var twist = world.CreateConstraint<TwistAngle>(body1, body2);
        twist.Initialize(axis, axis, AngularLimit.Fixed);
        body2.Orientation = JQuaternion.CreateFromAxisAngle(axis, MathR.PI - (Real)0.01) * body2.Orientation;
        body2.Orientation = JQuaternion.CreateFromAxisAngle(MathHelper.CreateOrthonormal(axis), (Real)swing) * body2.Orientation;
        TwistAngle.PrepareForIterationTwistAngle(ref twist.Handle.Data, 100);
        JVector jacobian = twist.Data.Jacobian;
        JQuaternion initial = body1.Orientation;
        Real delta = (Real)0.0001;
        foreach (JVector direction in new[] { JVector.UnitX, JVector.UnitY, JVector.UnitZ })
        {
            body1.Orientation = JQuaternion.CreateFromAxisAngle(direction, delta) * initial;
            // Evaluate the angle using atan2: asin loses accuracy near the half turn.
            JQuaternion q = twist.Data.Q0 * body1.Orientation.Conjugate() * body2.Orientation;
            Real projection = JVector.Dot(twist.Data.B, q.Vector);
            Real angleAfter = 2 * MathR.Atan2(projection,
                MathR.Sqrt(q.W * q.W + (q.Vector - twist.Data.B * projection).LengthSquared()));
            body1.Orientation = JQuaternion.CreateFromAxisAngle(direction, -delta) * initial;
            q = twist.Data.Q0 * body1.Orientation.Conjugate() * body2.Orientation;
            projection = JVector.Dot(twist.Data.B, q.Vector);
            Real angleBefore = 2 * MathR.Atan2(projection,
                MathR.Sqrt(q.W * q.W + (q.Vector - twist.Data.B * projection).LengthSquared()));
            Assert.That((angleAfter - angleBefore) / (2 * delta),
                Is.EqualTo(jacobian * direction).Within((Real)0.004));
        }
    }

    [TestCase(-1)]
    [TestCase(1)]
    public void Twist_HalfTurnHonorsRangeLimits(int sign)
    {
        using World world = CreateWorld(SolveMode.Regular);
        RigidBody body = CreateBody(world, MotionAxes.AngularZ);
        var twist = world.CreateConstraint<TwistAngle>(world.NullBody, body);
        twist.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(-30, 30));
        twist.Softness = 0;
        body.Orientation = new JQuaternion(0, 0, sign, 0);
        for (int i = 0; i < 100; i++) world.Step((Real)0.01, false);
        Assert.That(MathR.Abs((Real)twist.Angle), Is.LessThanOrEqualTo(MathR.PI / 6 + (Real)0.001));
    }

    [Test]
    public void Cone_TinyUpperLimitCorrectsTiltDespiteRoundedCosine()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ,
            new AngularLimit((JAngle)0, (JAngle)(Real)5e-6));
        cone.Softness = 0;
        body.Orientation = JQuaternion.CreateRotationX((Real)1e-5);
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, 100);
        ConeLimit.IterateConeLimit(ref cone.Handle.Data, 100);
        Assert.That(body.AngularVelocity.X, Is.EqualTo((Real)(-0.0001)).Within((Real)1e-6));
    }
}
