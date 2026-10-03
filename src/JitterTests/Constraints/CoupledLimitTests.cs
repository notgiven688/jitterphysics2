namespace JitterTests.Constraints;

[TestFixture]
public class CoupledLimitTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-10;
#else
    private const Real Tolerance = 2e-5f;
#endif

    [Test]
    public void PointOnLine_InwardMotionIsUnaffectedByClampedLimit(
        [Values] bool upper, [Values] bool warmStart, [Values(0, 1e-5)] double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JVector anchor = new(1, 1, 1);
        var line = world.CreateConstraint<PointOnLine>(world.NullBody, body);
        line.Initialize(JVector.UnitZ, anchor, anchor, upper ? new LinearLimit(-1, 0) : new LinearLimit(0, 1));
        line.Bias = line.LimitBias = 0;
        line.Softness = line.LimitSoftness = (Real)softness;
        Real direction = upper ? -1 : 1;
        JVector originalVelocity = JVector.UnitZ * direction;
        body.Velocity = originalVelocity;
        if (warmStart) line.Data.AccumulatedImpulse = new JVector((Real)0.2, (Real)(-0.3), direction * (Real)0.4);

        PointOnLine.PrepareForIterationPointOnLine(ref line.Handle.Data, 100);
        PointOnLine.IteratePointOnLine(ref line.Handle.Data, 100);

        Assert.That(line.Impulse.Z, Is.Zero);
        Assert.That(JVector.MaxAbs(body.Velocity - originalVelocity), Is.LessThan(Tolerance));
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
    }

    [Test]
    public void PointOnLine_BlockSolveSatisfiesActiveRowsAndDissipatesEnergy(
        [Values] bool upper, [Values] bool inward, [Values(0, 1e-5)] double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JSymmetricMatrix inverseInertia = new(2, (Real)0.3, (Real)(-0.4), 1, (Real)0.2, 3);
        body.SetMassInertia(inverseInertia, 1, true);
        Assert.That(JSymmetricMatrix.Inverse(inverseInertia, out JSymmetricMatrix inertia), Is.True);
        JVector anchor = new(1, 1, 1);
        var line = world.CreateConstraint<PointOnLine>(world.NullBody, body);
        line.Initialize(JVector.UnitZ, anchor, anchor, upper ? new LinearLimit(-1, 0) : new LinearLimit(0, 1));
        line.Bias = line.LimitBias = 0;
        line.Softness = line.LimitSoftness = (Real)softness;
        body.Velocity = new JVector((Real)0.5, (Real)(-0.25), (upper ? -5 : 5) * (inward ? 1 : -1));
        body.AngularVelocity = new JVector((Real)0.1, (Real)0.2, (Real)(-0.1));
        Real initialEnergy = body.Velocity.LengthSquared() +
            body.AngularVelocity * JVector.Transform(body.AngularVelocity, inertia);

        PointOnLine.PrepareForIterationPointOnLine(ref line.Handle.Data, 100);
        PointOnLine.IteratePointOnLine(ref line.Handle.Data, 100);

        JVector pointVelocity = body.Velocity + body.AngularVelocity % anchor;
        Real residualX = pointVelocity.Y + 100 * (Real)softness * line.Impulse.X;
        Real residualY = -pointVelocity.X + 100 * (Real)softness * line.Impulse.Y;
        if (inward)
        {
            Assert.That(line.Impulse.Z, Is.Zero);
        }
        else
        {
            Assert.That(line.Impulse.Z * (upper ? -1 : 1), Is.GreaterThan(0));
            Assert.That(MathR.Abs(pointVelocity.Z + 100 * (Real)softness * line.Impulse.Z), Is.LessThan(Tolerance));
        }
        Assert.That(MathR.Abs(residualX), Is.LessThan(Tolerance));
        Assert.That(MathR.Abs(residualY), Is.LessThan(Tolerance));
        Real finalEnergy = body.Velocity.LengthSquared() +
            body.AngularVelocity * JVector.Transform(body.AngularVelocity, inertia);
        Assert.That(finalEnergy, Is.LessThanOrEqualTo(initialEnergy + Tolerance));
    }

    [Test]
    public void PointOnLine_SingularCoupledBlockStillLeavesInwardMotionFree([Values] bool upper)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.CreateScale(1, 1, 0), 0, true);
        JVector anchor = new(1, 1, 1);
        var line = world.CreateConstraint<PointOnLine>(world.NullBody, body);
        line.Initialize(JVector.UnitZ, anchor, anchor, upper ? new LinearLimit(-1, 0) : new LinearLimit(0, 1));
        line.Bias = line.LimitBias = line.Softness = line.LimitSoftness = 0;
        JVector velocity = JVector.UnitZ * (upper ? -1 : 1);
        body.Velocity = velocity;

        PointOnLine.PrepareForIterationPointOnLine(ref line.Handle.Data, 100);
        PointOnLine.IteratePointOnLine(ref line.Handle.Data, 100);

        Assert.That(line.Impulse.Z, Is.Zero);
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(body.Velocity, Is.EqualTo(velocity));
    }

    [Test]
    public void PointOnLine_ChangedLimitDiscardsInadmissibleWarmStart([Values] bool upper, [Values] bool inactive)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JVector anchor = new(1, 1, 1);
        var line = world.CreateConstraint<PointOnLine>(world.NullBody, body);
        line.Initialize(JVector.UnitZ, anchor, anchor, LinearLimit.Fixed);
        line.Bias = line.LimitBias = line.Softness = line.LimitSoftness = 0;
        line.Data.AccumulatedImpulse.Z = upper ? 1 : -1;
        line.Data.Min = inactive ? Real.NegativeInfinity : upper ? -1 : 0;
        line.Data.Max = inactive ? Real.PositiveInfinity : upper ? 0 : 1;
        JVector velocity = JVector.UnitZ * (upper ? -1 : 1);
        body.Velocity = velocity;

        PointOnLine.PrepareForIterationPointOnLine(ref line.Handle.Data, 100);

        Assert.That(line.Impulse.Z, Is.Zero);
        Assert.That(body.Velocity, Is.EqualTo(velocity));
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        PointOnLine.IteratePointOnLine(ref line.Handle.Data, 100);
        Assert.That(JVector.MaxAbs(body.Velocity - velocity), Is.LessThan(Tolerance));
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
    }

    [Test]
    public void PointOnLine_TwoMovingBodiesRetainFreeInwardMotion([Values] bool upper)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody(), body2 = world.CreateRigidBody();
        body1.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        body2.Position = new JVector(1, -2, (Real)0.5);
        body2.Orientation = JQuaternion.CreateRotationY((Real)0.3);
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));
        JVector anchor = new((Real)0.2, (Real)0.4, (Real)0.3);
        var line = world.CreateConstraint<PointOnLine>(body1, body2);
        line.Initialize(axis, anchor, anchor, upper ? new LinearLimit(-1, 0) : new LinearLimit(0, 1));
        Real distance = line.Distance;
        line.Data.Min = upper ? distance - 1 : distance;
        line.Data.Max = upper ? distance : distance + 1;
        line.Bias = line.LimitBias = line.Softness = line.LimitSoftness = 0;
        JVector velocity1 = axis * (Real)0.3;
        JVector velocity2 = velocity1 + axis * (upper ? -1 : 1);
        body1.Velocity = velocity1;
        body2.Velocity = velocity2;

        PointOnLine.PrepareForIterationPointOnLine(ref line.Handle.Data, 100);
        PointOnLine.IteratePointOnLine(ref line.Handle.Data, 100);

        Assert.That(line.Data.Clamp, Is.EqualTo(upper ? 1 : 2));
        Assert.That(JVector.MaxAbs(body1.Velocity - velocity1), Is.LessThan(Tolerance));
        Assert.That(JVector.MaxAbs(body2.Velocity - velocity2), Is.LessThan(Tolerance));
        Assert.That(JVector.MaxAbs(body1.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(JVector.MaxAbs(body2.AngularVelocity), Is.LessThan(Tolerance));
    }

    [Test]
    public void HingeAngle_InwardMotionIsUnaffectedByClampedLimit(
        [Values] bool upper, [Values] bool warmStart, [Values(0, 0.001)] double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(new JSymmetricMatrix(1, 0, (Real)(-0.99), 1, 0, 1), 1, true);
        var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
        hinge.Initialize(JVector.UnitZ, upper ? AngularLimit.FromDegree(-30, 0) : AngularLimit.FromDegree(0, 30));
        hinge.Bias = hinge.LimitBias = 0;
        hinge.Softness = hinge.LimitSoftness = (Real)softness;
        Real direction = upper ? -1 : 1;
        JVector angularVelocity = JVector.UnitZ * direction;
        body.AngularVelocity = angularVelocity;
        if (warmStart) hinge.Data.AccumulatedImpulse = new JVector((Real)0.2, (Real)(-0.3), direction * (Real)0.4);

        HingeAngle.PrepareForIterationHingeAngle(ref hinge.Handle.Data, 100);
        HingeAngle.IterateHingeAngle(ref hinge.Handle.Data, 100);

        Assert.That(hinge.Impulse.Z, Is.Zero);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - angularVelocity), Is.LessThan(Tolerance));
    }

    [Test]
    public void HingeAngle_ChangedLimitDiscardsInadmissibleWarmStart([Values] bool upper, [Values] bool inactive)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(new JSymmetricMatrix(1, 0, (Real)(-0.99), 1, 0, 1), 1, true);
        var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
        hinge.Initialize(JVector.UnitZ, AngularLimit.Fixed);
        hinge.Bias = hinge.LimitBias = hinge.Softness = hinge.LimitSoftness = 0;
        hinge.Data.AccumulatedImpulse.Z = upper ? 1 : -1;
        hinge.Limit = inactive ? AngularLimit.Full : upper ? AngularLimit.FromDegree(-30, 0) : AngularLimit.FromDegree(0, 30);
        JVector velocity = JVector.UnitZ * (upper ? -1 : 1);
        body.AngularVelocity = velocity;

        HingeAngle.PrepareForIterationHingeAngle(ref hinge.Handle.Data, 100);

        Assert.That(hinge.Impulse.Z, Is.Zero);
        Assert.That(body.AngularVelocity, Is.EqualTo(velocity));
        HingeAngle.IterateHingeAngle(ref hinge.Handle.Data, 100);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - velocity), Is.LessThan(Tolerance));
    }

    [Test]
    public void HingeAngle_BlockSolveSatisfiesActiveRowsAndDissipatesEnergy(
        [Values] bool upper, [Values] bool inward, [Values(0, 0.001)] double softness)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JSymmetricMatrix inverseInertia = new(1, 0, (Real)(-0.99), 1, 0, 1);
        body.SetMassInertia(inverseInertia, 1, true);
        Assert.That(JSymmetricMatrix.Inverse(inverseInertia, out JSymmetricMatrix inertia), Is.True);
        var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
        hinge.Initialize(JVector.UnitZ, upper ? AngularLimit.FromDegree(-30, 0) : AngularLimit.FromDegree(0, 30));
        hinge.Bias = hinge.LimitBias = 0;
        hinge.Softness = hinge.LimitSoftness = (Real)softness;
        body.AngularVelocity = new JVector((Real)0.2, (Real)(-0.3), (upper ? -5 : 5) * (inward ? 1 : -1));
        Real initialEnergy = body.AngularVelocity * JVector.Transform(body.AngularVelocity, inertia);

        HingeAngle.PrepareForIterationHingeAngle(ref hinge.Handle.Data, 100);
        HingeAngle.IterateHingeAngle(ref hinge.Handle.Data, 100);

        JVector residual = JVector.TransposedTransform(-body.AngularVelocity, hinge.Data.Jacobian) +
            hinge.Impulse * (100 * (Real)softness);
        if (inward)
        {
            Assert.That(hinge.Impulse.Z, Is.Zero);
        }
        else
        {
            Assert.That(hinge.Impulse.Z * (upper ? -1 : 1), Is.GreaterThan(0));
            Assert.That(MathR.Abs(residual.Z), Is.LessThan(Tolerance));
        }
        Assert.That(MathR.Abs(residual.X), Is.LessThan(Tolerance));
        Assert.That(MathR.Abs(residual.Y), Is.LessThan(Tolerance));
        Real finalEnergy = body.AngularVelocity * JVector.Transform(body.AngularVelocity, inertia);
        Assert.That(finalEnergy, Is.LessThanOrEqualTo(initialEnergy + Tolerance));
    }
}
