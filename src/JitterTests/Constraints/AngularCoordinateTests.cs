using System.Runtime.CompilerServices;
using Jitter2.Unmanaged;

namespace JitterTests.Constraints;

[TestFixture]
public unsafe class AngularCoordinateTests
{
#if USE_DOUBLE_PRECISION
    private const Real Delta = 1e-6;
    private const Real DerivativeTolerance = 1e-7;
#else
    private const Real Delta = 1e-4f;
    private const Real DerivativeTolerance = 0.006f;
#endif

    [Test]
    public void RotationLogJacobianMatchesBothBodies(
        [Values(0.2, 2.1, 3.13)] double angle, [Values] bool hinge, [Values] bool perturbBody2)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody(), body2 = world.CreateRigidBody();
        body1.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        body2.Orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 5));
        JVector axis = JVector.Normalize(new JVector(1, 3, 2));
        JQuaternion reference;
        JMatrix jacobian;
        JVector localAxis = JVector.Zero;
        if (hinge)
        {
            var constraint = world.CreateConstraint<HingeAngle>(body1, body2);
            constraint.Initialize(axis, AngularLimit.Fixed);
            reference = constraint.Data.Q0;
            localAxis = constraint.Data.Axis;
            body2.Orientation = JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(2, 1, -3)),
                (Real)angle) * body2.Orientation;
            HingeAngle.PrepareForIterationHingeAngle(ref constraint.Handle.Data, 100);
            jacobian = JMatrix.Transpose(constraint.Data.Jacobian);
        }
        else
        {
            var constraint = world.CreateConstraint<FixedAngle>(body1, body2);
            constraint.Initialize();
            reference = constraint.Data.Q0;
            body2.Orientation = JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(2, 1, -3)),
                (Real)angle) * body2.Orientation;
            FixedAngle.PrepareForIterationFixedAngle(ref constraint.Handle.Data, 100);
            jacobian = constraint.Data.Jacobian;
        }

        RigidBody perturbed = perturbBody2 ? body2 : body1;
        JQuaternion original = perturbed.Orientation;
        foreach (JVector direction in new[] { JVector.UnitX, JVector.UnitY, JVector.UnitZ })
        {
            perturbed.Orientation = JQuaternion.CreateFromAxisAngle(direction, Delta) * original;
            JVector plus = AngularConstraintMath.RotationLog(reference * body1.Orientation.Conjugate() * body2.Orientation);
            perturbed.Orientation = JQuaternion.CreateFromAxisAngle(direction, -Delta) * original;
            JVector minus = AngularConstraintMath.RotationLog(reference * body1.Orientation.Conjugate() * body2.Orientation);
            perturbed.Orientation = original;
            JVector measured = (plus - minus) * ((Real)1 / (2 * Delta));
            if (hinge)
            {
                JVector p0 = MathHelper.CreateOrthonormal(localAxis);
                JVector p1 = localAxis % p0;
                measured = new JVector(p0 * measured, p1 * measured, localAxis * measured);
            }
            else measured *= -2;
            JVector expected = JVector.Transform(direction * (perturbBody2 ? -1 : 1), jacobian);
            Assert.That(JVector.MaxAbs(measured - expected), Is.LessThan(DerivativeTolerance));
        }
    }

    [Test]
    public void TwistProjectionAngleJacobianMatchesBothBodies(
        [Values(0.2, 2.1, 3.13)] double angle, [Values] bool perturbBody2)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody(), body2 = world.CreateRigidBody();
        body1.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        body2.Orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 5));
        JVector axis = JVector.Normalize(new JVector(1, 3, 2));
        var twist = world.CreateConstraint<TwistAngle>(body1, body2);
        twist.Initialize(axis, axis, AngularLimit.Fixed);
        body2.Orientation = JQuaternion.CreateFromAxisAngle(axis, (Real)angle) * body2.Orientation;
        body2.Orientation = JQuaternion.CreateFromAxisAngle(MathHelper.CreateOrthonormal(axis), (Real)0.3) * body2.Orientation;
        TwistAngle.PrepareForIterationTwistAngle(ref twist.Handle.Data, 100);
        RigidBody perturbed = perturbBody2 ? body2 : body1;
        JQuaternion original = perturbed.Orientation;
        foreach (JVector direction in new[] { JVector.UnitX, JVector.UnitY, JVector.UnitZ })
        {
            perturbed.Orientation = JQuaternion.CreateFromAxisAngle(direction, Delta) * original;
            Real plus = (Real)twist.Angle;
            perturbed.Orientation = JQuaternion.CreateFromAxisAngle(direction, -Delta) * original;
            Real minus = (Real)twist.Angle;
            perturbed.Orientation = original;
            Real expected = twist.Data.Jacobian * direction * (perturbBody2 ? -1 : 1);
            Assert.That((plus - minus) / (2 * Delta), Is.EqualTo(expected).Within(DerivativeTolerance));
        }
    }

    [Test]
    public void ConeAngleJacobianMatchesBothBodies(
        [Values(0.2, 0.6, 2.1, 3.13)] double angle, [Values] bool perturbBody2)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody(), body2 = world.CreateRigidBody();
        body1.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        body2.Orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 5));
        JVector axis = JVector.Normalize(new JVector(1, 3, 2));
        var cone = world.CreateConstraint<ConeLimit>(body1, body2);
        cone.Initialize(axis, axis, AngularLimit.FromDegree(30, 30));
        body2.Orientation = JQuaternion.CreateFromAxisAngle(MathHelper.CreateOrthonormal(axis), (Real)angle) * body2.Orientation;
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, 100);
        JVector jacobian = Unsafe.As<MemoryHelper.MemBlock6Real, JVector>(ref cone.Data.J0);
        RigidBody perturbed = perturbBody2 ? body2 : body1;
        JQuaternion original = perturbed.Orientation;
        foreach (JVector direction in new[] { JVector.UnitX, JVector.UnitY, JVector.UnitZ })
        {
            perturbed.Orientation = JQuaternion.CreateFromAxisAngle(direction, Delta) * original;
            Real plus = (Real)cone.Angle;
            perturbed.Orientation = JQuaternion.CreateFromAxisAngle(direction, -Delta) * original;
            Real minus = (Real)cone.Angle;
            perturbed.Orientation = original;
            Real expected = jacobian * direction * (perturbBody2 ? -1 : 1);
            Assert.That((plus - minus) / (2 * Delta), Is.EqualTo(expected).Within(DerivativeTolerance));
        }
    }

    [TestCase(119.99)]
    [TestCase(120.01)]
    [TestCase(179)]
    public void TwistSoftnessUsesTheSameAngularUnitsThroughout(double degrees)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var twist = world.CreateConstraint<TwistAngle>(world.NullBody, body);
        twist.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.Fixed);
        twist.Bias = 0;
        body.Orientation = JQuaternion.CreateRotationZ((Real)degrees * MathR.PI / 180);
        body.AngularVelocity = JVector.UnitZ;
        TwistAngle.PrepareForIterationTwistAngle(ref twist.Handle.Data, 100);
        TwistAngle.IterateTwistAngle(ref twist.Handle.Data, 100);
        Real gamma = twist.Softness * 100;
        Assert.That(body.AngularVelocity.Z, Is.EqualTo(gamma / (1 + gamma)).Within((Real)1e-5));
    }

    [TestCase(29.99)]
    [TestCase(30.01)]
    [TestCase(149.99)]
    [TestCase(150.01)]
    public void ConeSoftnessUsesTheSameAngularUnitsThroughout(double degrees)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(0, 10));
        cone.Bias = 0;
        body.Orientation = JQuaternion.CreateRotationX((Real)degrees * MathR.PI / 180);
        body.AngularVelocity = JVector.UnitX;
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, 100);
        ConeLimit.IterateConeLimit(ref cone.Handle.Data, 100);
        Real gamma = cone.Softness * 100;
        Assert.That(body.AngularVelocity.X, Is.EqualTo(gamma / (1 + gamma)).Within((Real)1e-5));
    }

    [Test]
    public void AngularBlocksRecoverFromHalfTurns(
        [Values] bool hinge, [Values] bool pureTwist, [Values(0, 0.001)] double softness,
        [Values(0, 1e-5)] double offset)
    {
        using World world = new() { Gravity = JVector.Zero, AllowDeactivation = false, SubstepCount = 1 };
        RigidBody body = world.CreateRigidBody();
        body.Damping = (0, 0);
        body.EnableGyroscopicForces = false;
        if (hinge)
        {
            var angular = world.CreateConstraint<HingeAngle>(world.NullBody, body);
            angular.Initialize(JVector.UnitZ, AngularLimit.Fixed);
            angular.Softness = angular.LimitSoftness = (Real)softness;
        }
        else
        {
            var angular = world.CreateConstraint<FixedAngle>(world.NullBody, body);
            angular.Initialize();
            angular.Softness = (Real)softness;
        }
        JVector axis = pureTwist ? JVector.UnitZ : JVector.Normalize(new JVector(1, 2, 3));
        body.Orientation = offset == 0 ? new JQuaternion(0, axis) :
            JQuaternion.CreateFromAxisAngle(axis, MathR.PI - (Real)offset);
        for (int i = 0; i < 200; i++)
        {
            world.Step((Real)0.01, false);
            Assert.That(body.AngularVelocity.Length(), Is.LessThan((Real)100));
            Assert.That(Real.IsFinite(body.Orientation.LengthSquared()), Is.True);
        }
        Assert.That(AngularConstraintMath.RotationLog(body.Orientation).Length(), Is.LessThan((Real)0.001));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void CrossingTheHalfTurnClearsThePreviousBranchImpulse(int kind)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        if (kind == 0)
        {
            var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
            hinge.Initialize(JVector.UnitZ, AngularLimit.Fixed);
            body.Orientation = JQuaternion.CreateRotationZ(MathR.PI - (Real)0.01);
            HingeAngle.PrepareForIterationHingeAngle(ref hinge.Handle.Data, 100);
            hinge.Data.AccumulatedImpulse = JVector.UnitZ;
            body.Orientation = JQuaternion.CreateRotationZ(MathR.PI + (Real)0.01);
            HingeAngle.PrepareForIterationHingeAngle(ref hinge.Handle.Data, 100);
            Assert.That(hinge.Impulse, Is.EqualTo(JVector.Zero));
        }
        else if (kind == 1)
        {
            var fixedAngle = world.CreateConstraint<FixedAngle>(world.NullBody, body);
            fixedAngle.Initialize();
            body.Orientation = JQuaternion.CreateRotationZ(MathR.PI - (Real)0.01);
            FixedAngle.PrepareForIterationFixedAngle(ref fixedAngle.Handle.Data, 100);
            fixedAngle.Data.AccumulatedImpulse = JVector.UnitZ;
            body.Orientation = JQuaternion.CreateRotationZ(MathR.PI + (Real)0.01);
            FixedAngle.PrepareForIterationFixedAngle(ref fixedAngle.Handle.Data, 100);
            Assert.That(fixedAngle.Impulse, Is.EqualTo(JVector.Zero));
        }
        else
        {
            var twist = world.CreateConstraint<TwistAngle>(world.NullBody, body);
            twist.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.Fixed);
            body.Orientation = JQuaternion.CreateRotationZ(MathR.PI - (Real)0.01);
            TwistAngle.PrepareForIterationTwistAngle(ref twist.Handle.Data, 100);
            twist.Data.AccumulatedImpulse = 1;
            body.Orientation = JQuaternion.CreateRotationZ(MathR.PI + (Real)0.01);
            TwistAngle.PrepareForIterationTwistAngle(ref twist.Handle.Data, 100);
            Assert.That(twist.Impulse, Is.Zero);
        }
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
    }

    [TestCase(0.000001)]
    [TestCase(0.000000001)]
    public void ConeTinyLimitsRemainDistinct(double lower)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ,
            new AngularLimit((JAngle)(Real)lower, (JAngle)(Real)(2 * lower)));
        Assert.That((Real)cone.Limit.From, Is.EqualTo((Real)lower));
        Assert.That((Real)cone.Limit.To, Is.EqualTo((Real)(2 * lower)));
        body.Orientation = JQuaternion.CreateRotationX((Real)(3 * lower));
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, 100);
        Assert.That(cone.Data.Clamp, Is.EqualTo(1));
    }

    [Test]
    public void ConeFixedTiltConstrainsBothVelocityDirections(
        [Values(-1, 1)] int direction, [Values(0, 0.001)] double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(30, 30));
        cone.Bias = 0;
        cone.Softness = (Real)softness;
        body.Orientation = JQuaternion.CreateRotationX(MathR.PI / 6);
        body.AngularVelocity = JVector.UnitX * direction;
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, 100);
        ConeLimit.IterateConeLimit(ref cone.Handle.Data, 100);
        Real gamma = (Real)softness * 100;
        Assert.That(cone.Data.Clamp, Is.EqualTo(3));
        Assert.That(body.AngularVelocity.X, Is.EqualTo(direction * gamma / (1 + gamma)).Within((Real)1e-5));
    }

    [Test]
    public void WarmStartsRetainTheirUnitsAcrossFormerThresholds([Values] bool twist)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        if (twist)
        {
            var constraint = world.CreateConstraint<TwistAngle>(world.NullBody, body);
            constraint.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.Fixed);
            body.Orientation = JQuaternion.CreateRotationZ((Real)119.99 * MathR.PI / 180);
            TwistAngle.PrepareForIterationTwistAngle(ref constraint.Handle.Data, 100);
            constraint.Data.AccumulatedImpulse = (Real)0.25;
            body.Orientation = JQuaternion.CreateRotationZ((Real)120.01 * MathR.PI / 180);
            TwistAngle.PrepareForIterationTwistAngle(ref constraint.Handle.Data, 100);
            Assert.That(constraint.Impulse, Is.EqualTo((Real)0.25));
            Assert.That(body.AngularVelocity.Z, Is.EqualTo((Real)0.25).Within((Real)1e-5));
        }
        else
        {
            var constraint = world.CreateConstraint<ConeLimit>(world.NullBody, body);
            constraint.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(30, 30));
            body.Orientation = JQuaternion.CreateRotationX((Real)29.99 * MathR.PI / 180);
            ConeLimit.PrepareForIterationConeLimit(ref constraint.Handle.Data, 100);
            constraint.Data.AccumulatedImpulse = (Real)0.25;
            body.Orientation = JQuaternion.CreateRotationX((Real)30.01 * MathR.PI / 180);
            ConeLimit.PrepareForIterationConeLimit(ref constraint.Handle.Data, 100);
            Assert.That(constraint.Impulse, Is.EqualTo((Real)0.25));
            Assert.That(body.AngularVelocity.X, Is.EqualTo((Real)0.25).Within((Real)1e-5));
        }
    }

    [Test]
    public void ConeInteriorBoundaryStopsOnlyOutwardVelocity(
        [Values] bool upper, [Values] bool outgoing, [Values(0, 0.001)] double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(0, 180));
        body.Orientation = JQuaternion.CreateRotationX((Real)0.5);
        JAngle boundary = cone.Angle;
        cone.Limit = upper ? new AngularLimit((JAngle)0, boundary) :
            new AngularLimit(boundary, (JAngle)MathR.PI);
        cone.Bias = 0;
        cone.Softness = (Real)softness;
        int direction = upper == outgoing ? 1 : -1;
        body.AngularVelocity = JVector.UnitX * direction;
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, 100);
        ConeLimit.IterateConeLimit(ref cone.Handle.Data, 100);
        Real gamma = (Real)softness * 100;
        Real expected = direction * (outgoing ? gamma / (1 + gamma) : 1);
        Assert.That(body.AngularVelocity.X, Is.EqualTo(expected).Within((Real)1e-5));
    }

    [Test]
    public void ConeNaturalBoundariesAllowAngularMotion([Values] bool antiparallel)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(0, 180));
        body.Orientation = antiparallel ? new JQuaternion(1, 0, 0, 0) : JQuaternion.Identity;
        body.AngularVelocity = new JVector(1, 2, 3);
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, 100);
        ConeLimit.IterateConeLimit(ref cone.Handle.Data, 100);
        Assert.That(cone.Data.Clamp, Is.Zero);
        Assert.That(body.AngularVelocity, Is.EqualTo(new JVector(1, 2, 3)));
    }

    [Test]
    public void FullHingeAndTwistRangesAllowCrossingHalfTurns(
        [Values] bool hinge, [Values(-1, 1)] int poseSign, [Values(-1, 1)] int velocitySign)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        if (hinge)
        {
            var constraint = world.CreateConstraint<HingeAngle>(world.NullBody, body);
            constraint.Initialize(JVector.UnitZ, AngularLimit.Full);
            constraint.Bias = constraint.LimitBias = 0;
            body.Orientation = new JQuaternion(0, 0, poseSign, 0);
            body.AngularVelocity = JVector.UnitZ * velocitySign;
            HingeAngle.PrepareForIterationHingeAngle(ref constraint.Handle.Data, 100);
            HingeAngle.IterateHingeAngle(ref constraint.Handle.Data, 100);
            Assert.That(constraint.Data.Clamp, Is.Zero);
        }
        else
        {
            var constraint = world.CreateConstraint<TwistAngle>(world.NullBody, body);
            constraint.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.Full);
            constraint.Bias = 0;
            body.Orientation = new JQuaternion(0, 0, poseSign, 0);
            body.AngularVelocity = JVector.UnitZ * velocitySign;
            TwistAngle.PrepareForIterationTwistAngle(ref constraint.Handle.Data, 100);
            TwistAngle.IterateTwistAngle(ref constraint.Handle.Data, 100);
            Assert.That(constraint.Data.Clamp, Is.Zero);
        }
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.UnitZ * velocitySign));
    }

    [Test]
    public void PartialHingeAndTwistRangesKeepTheHalfTurnBoundaryActive(
        [Values] bool hinge, [Values(-1, 1)] int sign)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        AngularLimit range = sign == 1 ? AngularLimit.FromDegree(0, 180) : AngularLimit.FromDegree(-180, 0);
        if (hinge)
        {
            var constraint = world.CreateConstraint<HingeAngle>(world.NullBody, body);
            constraint.Initialize(JVector.UnitZ, range);
            constraint.Bias = constraint.LimitBias = constraint.Softness = constraint.LimitSoftness = 0;
            body.Orientation = new JQuaternion(0, 0, sign, 0);
            body.AngularVelocity = JVector.UnitZ * sign;
            HingeAngle.PrepareForIterationHingeAngle(ref constraint.Handle.Data, 100);
            HingeAngle.IterateHingeAngle(ref constraint.Handle.Data, 100);
            Assert.That(constraint.Data.Clamp, Is.EqualTo(sign == 1 ? 1 : 2));
        }
        else
        {
            var constraint = world.CreateConstraint<TwistAngle>(world.NullBody, body);
            constraint.Initialize(JVector.UnitZ, JVector.UnitZ, range);
            constraint.Bias = constraint.Softness = 0;
            body.Orientation = new JQuaternion(0, 0, sign, 0);
            body.AngularVelocity = JVector.UnitZ * sign;
            TwistAngle.PrepareForIterationTwistAngle(ref constraint.Handle.Data, 100);
            TwistAngle.IterateTwistAngle(ref constraint.Handle.Data, 100);
            Assert.That(constraint.Data.Clamp, Is.EqualTo(sign == 1 ? 1 : 2));
        }
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan((Real)1e-5));
    }

    [Test]
    public void FixedHalfTurnTargetsDoNotBiasEquivalentQuaternionRepresentations(
        [Values] bool hinge, [Values(-1, 1)] int targetSign,
        [Values(-1, 1)] int quaternionSign, [Values] bool nonidentityReference)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody(), body2 = world.CreateRigidBody();
        if (nonidentityReference)
        {
            body1.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
            body2.Orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 5));
        }
        JQuaternion reference2 = body2.Orientation;
        JVector axis = nonidentityReference ? JVector.Normalize(new JVector(1, 3, 2)) : JVector.UnitZ;
        AngularLimit limit = AngularLimit.FromDegree(targetSign * 180, targetSign * 180);
        Real bias;
        if (hinge)
        {
            var constraint = world.CreateConstraint<HingeAngle>(body1, body2);
            constraint.Initialize(axis, limit);
            body2.Orientation = reference2 * new JQuaternion(0, constraint.Data.Axis * quaternionSign);
            HingeAngle.PrepareForIterationHingeAngle(ref constraint.Handle.Data, 100);
            bias = constraint.Data.Bias.Z;
        }
        else
        {
            var constraint = world.CreateConstraint<TwistAngle>(body1, body2);
            constraint.Initialize(axis, axis, limit);
            body2.Orientation = reference2 * new JQuaternion(0, constraint.Data.B * quaternionSign);
            TwistAngle.PrepareForIterationTwistAngle(ref constraint.Handle.Data, 100);
            bias = constraint.Data.Bias;
        }
        Real tolerance = Precision.IsDoublePrecision ? (Real)1e-10 : (Real)1e-4;
        Assert.That(MathR.Abs(bias), Is.LessThan(tolerance));
    }

    [TestCase(-1)]
    [TestCase(1)]
    public void FixedAngularTargetWrappingHandlesLargeFiniteValues(int sign)
    {
        Real angle = (Real)sign * Real.MaxValue;
        Real wrapped = AngularConstraintMath.ShortestAngle(angle, MathR.PI);
        Assert.That(Real.IsFinite(wrapped), Is.True);
        Assert.That(wrapped, Is.InRange(-MathR.PI, MathR.PI));
    }
}
