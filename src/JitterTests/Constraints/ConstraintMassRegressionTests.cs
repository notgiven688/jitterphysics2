using Jitter2.SoftBodies;

namespace JitterTests.Constraints;

[TestFixture]
public class ConstraintMassRegressionTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    [TestCase(false, 1e-7)]
    [TestCase(true, 1e-7)]
    [TestCase(false, 1e-13)]
    [TestCase(true, 1e-13)]
    public void PointOnLine_InactiveLimitPreservesSmallResponsiveRows(bool locked, double inverseMass)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Zero, (Real)inverseMass, true);
        if (locked) body.AllowedMotion = MotionAxes.Linear;
        var line = world.CreateConstraint<PointOnLine>(world.NullBody, body);
        line.Initialize(JVector.UnitZ, JVector.Zero, JVector.Zero, LinearLimit.Full);
        line.Softness = line.LimitSoftness = line.Bias = line.LimitBias = 0;
        body.Velocity = new JVector(1, 0, 2);

        PointOnLine.PrepareForIterationPointOnLine(ref line.Handle.Data, 100);
        PointOnLine.IteratePointOnLine(ref line.Handle.Data, 100);
        Assert.That(JVector.MaxAbs(body.Velocity - new JVector(0, 0, 2)), Is.LessThan(Tolerance));
    }

    [TestCase(false, 1e-7)]
    [TestCase(true, 1e-7)]
    [TestCase(false, 1e-13)]
    [TestCase(true, 1e-13)]
    public void HingeAngle_InactiveLimitPreservesSmallResponsiveRows(bool locked, double inverseInertia)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Identity * (Real)inverseInertia, 1, true);
        if (locked) body.AllowedMotion = MotionAxes.Linear | MotionAxes.AngularX;
        var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
        hinge.Initialize(JVector.UnitZ, AngularLimit.Full);
        hinge.Softness = hinge.LimitSoftness = hinge.Bias = hinge.LimitBias = 0;
        body.AngularVelocity = new JVector(1, 0, 2);

        HingeAngle.PrepareForIterationHingeAngle(ref hinge.Handle.Data, 100);
        HingeAngle.IterateHingeAngle(ref hinge.Handle.Data, 100);
        JVector expected = locked ? JVector.Zero : new JVector(0, 0, 2);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - expected), Is.LessThan(Tolerance));
    }

    [TestCase(0, MotionAxes.All)]
    [TestCase(1e-7, MotionAxes.All)]
    [TestCase(0, MotionAxes.LinearY)]
    [TestCase(1e-7, MotionAxes.LinearY)]
    public void SpringParameters_KeepFiniteComplianceAtCoincidentAnchors(double distance, MotionAxes axes)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody(), body2 = world.CreateRigidBody();
        body1.AllowedMotion = body2.AllowedMotion = axes;
        body2.Position = JVector.UnitY * (Real)distance;
        var spring = world.CreateConstraint<SpringConstraint>(body1, body2);
        spring.Initialize(body1.Position, body2.Position);
        spring.SetSpringParameters(2, 1, (Real)0.01);

        Real omega = 4 * MathR.PI;
        Real expectedSoftness = 1 / (omega + (Real)0.005 * omega * omega);
        Assert.That(spring.Softness, Is.EqualTo(expectedSoftness).Within(Tolerance));
        Assert.That(spring.Bias, Is.EqualTo((Real)0.005 * omega * omega * expectedSoftness).Within(Tolerance));
    }

}
