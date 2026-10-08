namespace JitterTests.Constraints;

[TestFixture]
public class ConstraintBlockMassTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-10;
#else
    private const Real Tolerance = 1e-4f;
#endif

    [Test]
    public void BallSocket_SingularResponseStopsAnchorMotionAndPreservesFreeRotation(
        [Values] bool irrelevantLocks, [Values] bool obliqueAnchor)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Identity, 0, true);
        if (irrelevantLocks) body.AllowedMotion = MotionAxes.Angular;

        JVector anchor = obliqueAnchor ? JVector.Normalize(new JVector(1, 2, 3)) : JVector.UnitX;
        var joint = world.CreateConstraint<BallSocket>(world.NullBody, body);
        joint.Initialize(anchor);
        joint.Softness = joint.Bias = 0;
        JVector initial = new(3, 2, 1);
        body.AngularVelocity = initial;

        BallSocket.PrepareForIterationBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
        BallSocket.IterateBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));

        // With no translation response, only rotation about the anchor direction is free.
        JVector expected = anchor * (anchor * initial);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - expected), Is.LessThan(Tolerance));
        Assert.That((body.AngularVelocity % anchor).Length(), Is.LessThan(Tolerance));
        Assert.That(body.AngularVelocity.LengthSquared(), Is.LessThanOrEqualTo(initial.LengthSquared()));
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
    }

    [Test]
    public void FixedAngle_HalfTurnStillStopsResponsiveAngularMotion(
        [Values(MotionAxes.All, MotionAxes.Angular, MotionAxes.Linear | MotionAxes.AngularY | MotionAxes.AngularZ)]
        MotionAxes axes)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.AllowedMotion = axes;
        var joint = world.CreateConstraint<FixedAngle>(world.NullBody, body);
        joint.Initialize();
        joint.Softness = joint.Bias = 0;
        body.Orientation = new JQuaternion(1, 0, 0, 0);
        body.AngularVelocity = JVector.UnitY;

        FixedAngle.PrepareForIterationFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
        FixedAngle.IterateFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));

        // Y rotation is responsive even where a quaternion-vector row for X is singular.
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(Real.IsFinite(joint.Impulse.LengthSquared()), Is.True);
    }

    [Test]
    public void BallSocket_SingularResponsePreservesIndependentWeakRow([Values] bool irrelevantLocks)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        Real weakResponse = Precision.IsDoublePrecision ? (Real)1e-15 : (Real)1e-9;
        body.SetMassInertia(JSymmetricMatrix.CreateScale(0, weakResponse, 1), 0, true);
        if (irrelevantLocks) body.AllowedMotion = MotionAxes.Angular;
        var joint = world.CreateConstraint<BallSocket>(world.NullBody, body);
        joint.Initialize(JVector.UnitX);
        joint.Softness = joint.Bias = 0;
        body.AngularVelocity = new JVector(0, 2, 3);

        BallSocket.PrepareForIterationBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
        BallSocket.IterateBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));

        // Both transverse rows are independently solvable, despite their different response scales.
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(Real.IsFinite(JVector.MaxAbs(joint.Impulse)), Is.True);
    }

    [Test]
    public void FixedAngle_SingularInertiaStopsResponsiveAxesWithoutLockFlags([Values] bool irrelevantLocks)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.CreateScale(0, 1, 1), 0, true);
        if (irrelevantLocks) body.AllowedMotion = MotionAxes.Angular;
        var joint = world.CreateConstraint<FixedAngle>(world.NullBody, body);
        joint.Initialize();
        joint.Softness = joint.Bias = 0;
        body.AngularVelocity = new JVector(3, 2, 1);

        FixedAngle.PrepareForIterationFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
        FixedAngle.IterateFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));

        // Infinite inertia about X cannot respond, but must not disable the Y/Z rows.
        Assert.That(JVector.MaxAbs(body.AngularVelocity - new JVector(3, 0, 0)), Is.LessThan(Tolerance));
        Assert.That(Real.IsFinite(JVector.MaxAbs(joint.Impulse)), Is.True);
    }

    [Test]
    public void BallSocket_UniformMassScaleDoesNotDisableTranslation(
        [Values] bool largeInverseMass, [Values] bool irrelevantLocks)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        Real inverseMass = ExtremeScale(largeInverseMass);
        body.SetMassInertia(JSymmetricMatrix.Zero, inverseMass, true);
        if (irrelevantLocks) body.AllowedMotion = MotionAxes.Linear;
        var joint = world.CreateConstraint<BallSocket>(world.NullBody, body);
        joint.Initialize(JVector.Zero);
        joint.Softness = joint.Bias = 0;
        body.Velocity = new JVector(1, -2, 3);

        BallSocket.PrepareForIterationBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
        BallSocket.IterateBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));

        Assert.That(JVector.MaxAbs(body.Velocity), Is.LessThan(Tolerance));
        // Multiplying back by inverse mass avoids overflow when checking the tiny-mass impulse.
        Assert.That(Real.IsFinite(JVector.MaxAbs(joint.Impulse)), Is.True);
        Assert.That(JVector.MaxAbs(joint.Impulse * inverseMass + new JVector(1, -2, 3)),
            Is.LessThan(Tolerance));
    }

    [Test]
    public void FixedAngle_UniformInertiaScaleDoesNotDisableAngularResponse(
        [Values] bool largeInverseInertia, [Values] bool angularLock)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        Real inverseInertia = ExtremeScale(largeInverseInertia);
        body.SetMassInertia(JSymmetricMatrix.Identity * inverseInertia, 0, true);
        if (angularLock) body.AllowedMotion = MotionAxes.AngularY | MotionAxes.AngularZ;
        var joint = world.CreateConstraint<FixedAngle>(world.NullBody, body);
        joint.Initialize();
        joint.Softness = joint.Bias = 0;
        body.AngularVelocity = new JVector(0, -2, 3);

        FixedAngle.PrepareForIterationFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
        FixedAngle.IterateFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));

        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(Real.IsFinite(JVector.MaxAbs(joint.Impulse)), Is.True);
    }

    [Test]
    public void FixedAngle_MixedExtremeInertiaScalesKeepEveryResponsiveRow()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        Real strong = Precision.IsDoublePrecision ? (Real)1e200 : (Real)1e20;
        Real weak = Precision.IsDoublePrecision ? (Real)1e-200 : (Real)1e-20;
        body.SetMassInertia(JSymmetricMatrix.CreateScale(strong, strong, weak), 0, true);
        var joint = world.CreateConstraint<FixedAngle>(world.NullBody, body);
        joint.Initialize();
        joint.Softness = joint.Bias = 0;
        body.AngularVelocity = new JVector(1, -2, 3);

        FixedAngle.PrepareForIterationFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
        FixedAngle.IterateFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));

        // The determinant is finite even when an unscaled inverse's cofactors overflow.
        Assert.That(Real.IsFinite(JVector.MaxAbs(body.AngularVelocity)), Is.True);
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(Real.IsFinite(JVector.MaxAbs(joint.Impulse)), Is.True);
    }

    [Test]
    public void UnrepresentableInverseResponseDoesNotCorruptVelocity([Values] bool angular)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JVector initial = new(1, -2, 3);
        if (angular)
        {
            body.SetMassInertia(JSymmetricMatrix.Identity * Real.Epsilon, 0, true);
            var joint = world.CreateConstraint<FixedAngle>(world.NullBody, body);
            joint.Initialize();
            joint.Softness = joint.Bias = 0;
            body.AngularVelocity = initial;
            FixedAngle.PrepareForIterationFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
            FixedAngle.IterateFixedAngle(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
            Assert.That(body.AngularVelocity, Is.EqualTo(initial));
            Assert.That(joint.Impulse, Is.EqualTo(JVector.Zero));
        }
        else
        {
            body.SetMassInertia(JSymmetricMatrix.Zero, Real.Epsilon, true);
            var joint = world.CreateConstraint<BallSocket>(world.NullBody, body);
            joint.Initialize(JVector.Zero);
            joint.Softness = joint.Bias = 0;
            body.Velocity = initial;
            BallSocket.PrepareForIterationBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
            BallSocket.IterateBallSocket(ref joint.Handle.Data, new TimeStep((Real)1.0 / 100));
            Assert.That(body.Velocity, Is.EqualTo(initial));
            Assert.That(joint.Impulse, Is.EqualTo(JVector.Zero));
        }
    }

    private static Real ExtremeScale(bool large) => Precision.IsDoublePrecision
        ? (large ? (Real)1e110 : (Real)1e-110)
        : (large ? (Real)1e20 : (Real)1e-13);
}
