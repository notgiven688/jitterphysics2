using System.Runtime.InteropServices;

namespace JitterTests.Constraints;

[TestFixture]
public class EllipticalConeLimitTests
{
    private static readonly TimeStep Step = new((Real)0.01);

    [Test]
    public void DataFitsSmallConstraintStorage() =>
        Assert.That(Marshal.SizeOf<EllipticalConeLimit.EllipticalConeLimitData>(),
            Is.LessThanOrEqualTo(Precision.ConstraintSizeSmall));

    [TestCase(0, 60, 0, 25)]
    [TestCase(15, 60, 5, 25)]
    [TestCase(60, 60, 25, 25)]
    [TestCase(100, 160, 30, 120)]
    public void InitializationStoresWorldFrameOnCorrespondingBodies(int min1, int max1, int min2, int max2)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        second.Orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 5));
        var cone = world.CreateConstraint<EllipticalConeLimit>(first, second);
        JVector axis = JVector.Normalize(new JVector(1, 3, 2));
        JVector reference = MathHelper.CreateOrthonormal(axis);
        JVector target = JVector.Normalize(axis + reference);
        cone.Initialize(axis, target, reference + 2 * axis,
            AngularLimit.FromDegree(min1, max1), AngularLimit.FromDegree(min2, max2));
        Assert.That((cone.AxisBody1 - axis).Length(), Is.LessThan((Real)1e-6));
        Assert.That((cone.AxisBody2 - target).Length(), Is.LessThan((Real)1e-6));
        Assert.That((cone.ReferenceDirectionBody1 - reference).Length(), Is.LessThan((Real)1e-6));
        cone.GetSwing(out JAngle x, out JAngle y);
        Assert.That(x.Degree, Is.EqualTo(45).Within((Real)1e-4));
        Assert.That(y.Degree, Is.EqualTo(0).Within((Real)1e-4));

        JQuaternion rotation = JQuaternion.CreateRotationY((Real)0.7);
        first.Orientation = rotation * first.Orientation;
        Assert.That((cone.ReferenceDirectionBody1 - JVector.Transform(reference, rotation)).Length(), Is.LessThan((Real)1e-6));
        Assert.That((cone.AxisBody2 - target).Length(), Is.LessThan((Real)1e-6));
    }

    [TestCase(-1, 60, 5, 25)]
    [TestCase(61, 60, 5, 25)]
    [TestCase(0, 0, 0, 25)]
    [TestCase(0, 180, 0, 25)]
    [TestCase(0, 60, 5, 25)]
    [TestCase(15, 60, 0, 25)]
    [TestCase(double.NaN, 60, 5, 25)]
    [TestCase(15, double.PositiveInfinity, 5, 25)]
    public void InvalidLimitsLeaveExistingConfigurationUnchanged(double min1, double max1, double min2, double max2)
    {
        using World world = new();
        var cone = CreateCone(world);
        cone.Data.AccumulatedImpulse = 2;
        AngularLimit first = AngularLimit.FromDegree((Real)min1, (Real)max1);
        AngularLimit second = AngularLimit.FromDegree((Real)min2, (Real)max2);
        Assert.Throws<ArgumentOutOfRangeException>(() => cone.SetLimits(first, second));
        Assert.Throws<ArgumentOutOfRangeException>(() => cone.Initialize(JVector.UnitX, JVector.UnitY, first, second));
        Assert.That(cone.LimitAlongReference.To.Degree, Is.EqualTo(60).Within((Real)1e-4));
        Assert.That(cone.LimitAlongPerpendicular.To.Degree, Is.EqualTo(25).Within((Real)1e-4));
        Assert.That(cone.AxisBody1, Is.EqualTo(JVector.UnitZ));
        Assert.That(cone.Impulse, Is.EqualTo(2));
    }

    [Test]
    public void InvalidDirectionsAreRejectedBeforeChangingTheFrame()
    {
        using World world = new();
        var cone = CreateCone(world);
        var limit = AngularLimit.FromDegree(0, 45);
        foreach (JVector invalid in new[] { JVector.Zero, new JVector(Real.NaN, 0, 0) })
        {
            Assert.Throws<ArgumentException>(() => cone.Initialize(invalid, JVector.UnitX, limit, limit));
            Assert.Throws<ArgumentException>(() => cone.Initialize(JVector.UnitZ, invalid, JVector.UnitX, limit, limit));
            Assert.Throws<ArgumentException>(() => cone.Initialize(JVector.UnitZ, invalid, limit, limit));
        }
        Assert.Throws<ArgumentException>(() => cone.Initialize(JVector.UnitX, JVector.UnitX, limit, limit));
        Assert.That(cone.AxisBody1, Is.EqualTo(JVector.UnitZ));
        Assert.That(cone.ReferenceDirectionBody1, Is.EqualTo(JVector.UnitX));
    }

    [Test]
    public void JacobianMatchesImplicitEllipseDerivativeOnBothBodies(
        [Values(0.2, 0.8, 2.8, 3.13)] double angle, [Values] bool perturbBody2)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        second.Orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 5));
        var cone = world.CreateConstraint<EllipticalConeLimit>(first, second);
        JVector axis = JVector.Normalize(new JVector(1, 3, 2));
        JVector reference = MathHelper.CreateOrthonormal(axis);
        JVector direction = JVector.Normalize(reference + (axis % reference) * (Real)0.6);
        cone.Initialize(axis, axis, reference, AngularLimit.FromDegree(60, 60), AngularLimit.FromDegree(25, 25));
        second.Orientation = JQuaternion.CreateFromAxisAngle(axis % direction, (Real)angle) * second.Orientation;
        EllipticalConeLimit.PrepareForIterationEllipticalConeLimit(ref cone.Handle.Data, Step);

        RigidBody body = perturbBody2 ? second : first;
        JQuaternion original = body.Orientation;
#if USE_DOUBLE_PRECISION
        const Real delta = 1e-6;
        const Real tolerance = 1e-6;
#else
        const Real delta = 1e-4f;
        const Real tolerance = 0.006f;
#endif
        JVector numerical = JVector.Zero;
        int component = 0;
        foreach (JVector directionWorld in new[] { JVector.UnitX, JVector.UnitY, JVector.UnitZ })
        {
            body.Orientation = JQuaternion.CreateFromAxisAngle(directionWorld, delta) * original;
            Real plus = EllipseValue(cone, 60, 25) - 1;
            body.Orientation = JQuaternion.CreateFromAxisAngle(directionWorld, -delta) * original;
            Real minus = EllipseValue(cone, 60, 25) - 1;
            body.Orientation = original;
            numerical[component++] = (plus - minus) / (2 * delta);
        }
        JVector expected = cone.Data.Jacobian * (perturbBody2 ? -1 : 1);
        Assert.That((JVector.Normalize(numerical) - expected).Length(), Is.LessThan(tolerance));
        Real error = (EllipseValue(cone, 60, 25) - 1) / numerical.Length();
        Assert.That(cone.Data.Bias, Is.EqualTo(error * cone.Bias * Step.InverseStepDt)
            .Within(MathR.Max((Real)0.002, MathR.Abs(error) * (Real)0.02)));
        Assert.That(MathR.Abs(cone.Data.Jacobian * cone.AxisBody2), Is.LessThan((Real)1e-5));
    }

    [TestCase(0, 0.6, "Maximum")]
    [TestCase(0.15, 0.6, "Band")]
    [TestCase(1.8, 0.6, "Maximum")]
    [TestCase(1.8, 0.6, "Band")]
    [TestCase(0, 0.6, "Fixed")]
    [TestCase(1.8, 0.6, "Fixed")]
    [TestCase(3.141592653589793, 0, "Band")]
    [TestCase(3.14158, 0.6, "Band")]
    public void SimulationRecoversIntoAllowedRegion(double angle, double azimuth, string kind)
    {
        foreach (SolveMode mode in new[] { SolveMode.Regular, SolveMode.Deterministic })
        {
            using World world = new()
            {
                Gravity = JVector.Zero,
                AllowDeactivation = false,
                SolveMode = mode,
                SubstepCount = 1,
                SolverIterations = (8, 0)
            };
            var cone = CreateCone(world);
            if (kind == "Band") cone.SetLimits(AngularLimit.FromDegree(20, 60), AngularLimit.FromDegree(10, 25));
            if (kind == "Fixed") cone.SetLimits(AngularLimit.FromDegree(60, 60), AngularLimit.FromDegree(25, 25));
            cone.Softness = 0;
            RigidBody body = cone.Body2;
            body.Damping = (0, (Real)0.01);
            body.EnableGyroscopicForces = false;
            JVector rotationAxis = new(-(Real)Math.Sin(azimuth), (Real)Math.Cos(azimuth), 0);
            body.Orientation = angle == Math.PI ? new JQuaternion(0, rotationAxis) :
                JQuaternion.CreateFromAxisAngle(rotationAxis, (Real)angle);
            for (int i = 0; i < 400; i++)
            {
                world.Step((Real)0.01, false);
                Assert.That(Real.IsFinite(body.Orientation.LengthSquared()), Is.True);
                Assert.That(body.AngularVelocity.Length(), Is.LessThan((Real)100));
            }
            Assert.That(EllipseValue(cone, 60, 25), Is.LessThanOrEqualTo((Real)1.003));
            if (kind == "Band") Assert.That(EllipseValue(cone, 20, 10), Is.GreaterThanOrEqualTo((Real)0.997));
            if (kind == "Fixed") Assert.That(EllipseValue(cone, 60, 25), Is.EqualTo(1).Within((Real)0.003));
        }
    }

    [Test]
    public void AlignedAndOppositeAxesRecoverWithRestrictedRotation(
        [Values] bool opposite, [Values(MotionAxes.AngularX, MotionAxes.AngularY)] MotionAxes motion)
    {
        using World world = new() { Gravity = JVector.Zero, AllowDeactivation = false };
        var cone = CreateCone(world);
        cone.SetLimits(AngularLimit.FromDegree(20, 60), AngularLimit.FromDegree(10, 25));
        cone.Softness = 0;
        cone.Body2.Damping = (0, 0);
        cone.Body2.AllowedMotion = motion;
        if (opposite) cone.Body2.Orientation = new JQuaternion(1, 0, 0, 0);
        for (int i = 0; i < 200; i++) world.Step((Real)0.01, false);
        Assert.That(EllipseValue(cone, 60, 25), Is.LessThanOrEqualTo((Real)1.003));
        Assert.That(EllipseValue(cone, 20, 10), Is.GreaterThanOrEqualTo((Real)0.997));
    }

    [TestCase(0.1)]
    [TestCase(0.7)]
    [TestCase(1.7)]
    public void CircularRangesMatchConeLimit(double angle)
    {
        using World world = new();
        var cone = CreateCone(world);
        var circular = world.CreateConstraint<ConeLimit>(world.NullBody, world.CreateRigidBody());
        var limit = AngularLimit.FromDegree(20, 60);
        cone.SetLimits(limit, limit);
        circular.Initialize(JVector.UnitZ, limit);
        JQuaternion orientation = JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(1, 2, 0)), (Real)angle);
        cone.Body2.Orientation = circular.Body2.Orientation = orientation;
        cone.Body2.AngularVelocity = circular.Body2.AngularVelocity = new JVector(1, -2, 3);
        EllipticalConeLimit.PrepareForIterationEllipticalConeLimit(ref cone.Handle.Data, Step);
        ConeLimit.PrepareForIterationConeLimit(ref circular.Handle.Data, Step);
        EllipticalConeLimit.IterateEllipticalConeLimit(ref cone.Handle.Data, Step);
        ConeLimit.IterateConeLimit(ref circular.Handle.Data, Step);
        Assert.That((cone.Body2.AngularVelocity - circular.Body2.AngularVelocity).Length(), Is.LessThan((Real)1e-5));
        Assert.That(cone.Impulse, Is.EqualTo(circular.Impulse).Within((Real)1e-5));
    }

    [Test]
    public void NearAntipodeJacobianNormalizationDoesNotOverflow()
    {
        using World world = new();
        var cone = CreateCone(world);
        cone.Body2.Orientation = JQuaternion.Normalize(new JQuaternion((Real)0.6, (Real)0.8, 0, (Real)1e-20));
        EllipticalConeLimit.PrepareForIterationEllipticalConeLimit(ref cone.Handle.Data, Step);
        Assert.That(cone.Data.Jacobian.Length(), Is.EqualTo(1).Within((Real)1e-5));
        Assert.That(Real.IsFinite(cone.Data.Bias), Is.True);
        Assert.That(cone.Data.EffectiveMass, Is.GreaterThan(0));
    }

    [Test]
    public void InteriorSwingAndTwistAreUnconstrainedAndWarmStartCanBeReset()
    {
        using World world = new();
        var cone = CreateCone(world);
        cone.Softness = 0;
        cone.Body2.Orientation = JQuaternion.CreateRotationY((Real)1.3);
        EllipticalConeLimit.PrepareForIterationEllipticalConeLimit(ref cone.Handle.Data, Step);
        EllipticalConeLimit.IterateEllipticalConeLimit(ref cone.Handle.Data, Step);
        Assert.That(cone.Impulse, Is.Not.Zero);
        cone.ResetWarmStart();
        cone.Body2.AngularVelocity = JVector.Zero;
        EllipticalConeLimit.PrepareForIterationEllipticalConeLimit(ref cone.Handle.Data, Step);
        Assert.That(cone.Body2.AngularVelocity, Is.EqualTo(JVector.Zero));

        cone.Body2.Orientation = JQuaternion.CreateRotationY((Real)0.3);
        cone.Body2.AngularVelocity = cone.AxisBody2 * 2;
        JVector velocity = cone.Body2.AngularVelocity;
        cone.Data.AccumulatedImpulse = 2;
        EllipticalConeLimit.PrepareForIterationEllipticalConeLimit(ref cone.Handle.Data, Step);
        EllipticalConeLimit.IterateEllipticalConeLimit(ref cone.Handle.Data, Step);
        Assert.That(cone.Body2.AngularVelocity, Is.EqualTo(velocity));
        Assert.That(cone.Impulse, Is.Zero);
        cone.Data.AccumulatedImpulse = 1;
        cone.SetLimits(AngularLimit.FromDegree(10, 50), AngularLimit.FromDegree(5, 30));
        Assert.That(cone.Impulse, Is.Zero);
    }

    [Test]
    public void DebugDrawBoundariesMatchSwingEllipses()
    {
        using World world = new();
        var cone = CreateCone(world);
        cone.SetLimits(AngularLimit.FromDegree(20, 60), AngularLimit.FromDegree(10, 25));
        var drawer = new BoundaryDrawer();
        cone.DebugDraw(drawer, new DebugDrawSettings { Segments = 64, ReferenceColor = 0x0000FFFF });
        Assert.That(drawer.Segments.Count, Is.GreaterThan(128));
        // The drawer selects the boundary colors. Each endpoint lies on an ellipse on
        // the unit sphere, or at the origin of a spoke.
        foreach (var (from, to) in drawer.Segments)
        {
            foreach (JVector point in new[] { from, to })
            {
                if (point == JVector.Zero) continue;
                Assert.That(point.Length(), Is.EqualTo(1).Within((Real)1e-6));
                Real angle = MathR.Atan2(MathR.Sqrt(point.X * point.X + point.Y * point.Y), point.Z);
                Real factor = angle / MathR.Sqrt(point.X * point.X + point.Y * point.Y);
                Real x = point.X * factor, y = point.Y * factor;
                Real outer = EllipseValue(x, y, 60, 25), inner = EllipseValue(x, y, 20, 10);
                Assert.That(MathR.Min(MathR.Abs(outer - 1), MathR.Abs(inner - 1)), Is.LessThan((Real)1e-5));
            }
        }
    }

    private static EllipticalConeLimit CreateCone(World world)
    {
        var cone = world.CreateConstraint<EllipticalConeLimit>(world.NullBody, world.CreateRigidBody());
        cone.Initialize(JVector.UnitZ, JVector.UnitX, AngularLimit.FromDegree(0, 60), AngularLimit.FromDegree(0, 25));
        return cone;
    }

    private static Real EllipseValue(EllipticalConeLimit cone, Real a, Real b)
    {
        cone.GetSwing(out JAngle x, out JAngle y);
        return EllipseValue((Real)x, (Real)y, a, b);
    }

    private static Real EllipseValue(Real x, Real y, Real a, Real b)
    {
        x /= a * MathR.PI / 180;
        y /= b * MathR.PI / 180;
        return MathR.Sqrt(x * x + y * y);
    }

    private sealed class BoundaryDrawer : IDebugDrawer
    {
        public readonly List<(JVector, JVector)> Segments = [];
        public void DrawSegment(in JVector from, in JVector to, uint color)
        {
            if (color == DebugDrawColors.Green || color == DebugDrawColors.Red) Segments.Add((from, to));
        }
    }
}
