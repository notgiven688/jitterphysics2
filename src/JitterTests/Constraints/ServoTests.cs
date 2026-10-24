using System.Runtime.InteropServices;

namespace JitterTests.Constraints;

public class ServoTests
{
    public enum ServoKind { Angular, Twist, Linear, LinearAxis }

    private static Constraint CreateServo(World world, RigidBody first, RigidBody second, ServoKind kind, Real maximum = 2)
    {
        Constraint servo;
        switch (kind)
        {
            case ServoKind.Angular:
                var angular = world.CreateConstraint<AngularServo>(first, second);
                angular.Initialize(JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(1, 2, 3)), (Real)2.8));
                servo = angular;
                break;
            case ServoKind.Twist:
                var twist = world.CreateConstraint<TwistServo>(first, second);
                twist.Initialize(JVector.UnitX, (JAngle)(Real)1.5);
                servo = twist;
                break;
            case ServoKind.Linear:
                var linear = world.CreateConstraint<LinearServo>(first, second);
                linear.Initialize(first.Position, second.Position);
                linear.TargetPosition = new JVector(1000, 2000, 3000);
                servo = linear;
                break;
            default:
                var axis = world.CreateConstraint<LinearAxisServo>(first, second);
                axis.Initialize(JVector.UnitX, first.Position, second.Position, 1000);
                servo = axis;
                break;
        }
        SetMaximum(servo, maximum);
        return servo;
    }

    private static void SetMaximum(Constraint servo, Real maximum)
    {
        switch (servo)
        {
            case AngularServo angular: angular.MaximumTorque = maximum; break;
            case TwistServo twist: twist.MaximumTorque = maximum; break;
            case LinearServo linear: linear.MaximumForce = maximum; break;
            case LinearAxisServo axis: axis.MaximumForce = maximum; break;
        }
    }

    private static void Solve(Constraint servo, TimeStep step, int iterations = 12)
    {
        ref var data = ref servo.Handle.Data;
        data.PrepareForIteration(ref data, step);
        for (int i = 0; i < iterations; i++) data.Iterate(ref data, step);
    }

    private static RigidBody CreateBody(World world)
    {
        var body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Identity, 1);
        body.Damping = (0, 0);
        return body;
    }

    [Test]
    public void CappedAccelerationUsesElapsedTime([Values] ServoKind kind,
        [Values] SolveMode mode, [Values] bool multiThread)
    {
        foreach (int steps in new[] { 30, 120 })
            foreach (int substeps in new[] { 1, 4 })
                foreach (int iterations in new[] { 1, 12 })
                {
                    using var world = new World
                    {
                        Gravity = JVector.Zero,
                        AllowDeactivation = false,
                        SolveMode = mode,
                        SubstepCount = substeps,
                        SolverIterations = (iterations, 0)
                    };
                    var body = CreateBody(world);
                    CreateServo(world, world.NullBody, body, kind);
                    for (int i = 0; i < steps; i++) world.Step((Real)1 / steps, multiThread);
                    Real speed = kind is ServoKind.Angular or ServoKind.Twist
                        ? body.AngularVelocity.Length() : body.Velocity.Length();
                    Assert.That(speed, Is.EqualTo((Real)2).Within((Real)1e-4),
                        $"steps={steps}, substeps={substeps}, iterations={iterations}");
                }
    }

    [Test]
    public void WarmStartRespectsReducedMaximumAndChangedTimestep([Values] ServoKind kind)
    {
        using var world = new World();
        var body = CreateBody(world);
        var servo = CreateServo(world, world.NullBody, body, kind);
        Solve(servo, new TimeStep((Real)0.1));
        foreach (Real maximum in new[] { (Real)0.1, (Real)0 })
        {
            body.Velocity = body.AngularVelocity = JVector.Zero;
            SetMaximum(servo, maximum);
            ref var data = ref servo.Handle.Data;
            var step = new TimeStep((Real)0.01, 4);
            data.PrepareForIteration(ref data, step);
            Real speed = kind is ServoKind.Angular or ServoKind.Twist
                ? body.AngularVelocity.Length() : body.Velocity.Length();
            Assert.That(speed, Is.EqualTo(maximum * step.SubstepDt).Within((Real)1e-7));
            for (int i = 0; i < 12; i++) data.Iterate(ref data, step);
            speed = kind is ServoKind.Angular or ServoKind.Twist ? body.AngularVelocity.Length() : body.Velocity.Length();
            Assert.That(speed, Is.LessThanOrEqualTo(maximum * step.SubstepDt + (Real)1e-7));
        }
    }

    [Test]
    public void AngularServoTracksTargetsInBody1Frame([Values] SolveMode mode)
    {
        using var world = new World { Gravity = JVector.Zero, AllowDeactivation = false, SolveMode = mode, SubstepCount = 4 };
        var reference = CreateBody(world);
        reference.MotionType = MotionType.Kinematic;
        reference.Orientation = JQuaternion.CreateRotationY((Real)0.7) * JQuaternion.CreateRotationX((Real)0.3);
        var body = CreateBody(world);
        body.Orientation = JQuaternion.CreateRotationZ((Real)(-0.8));
        var before = body.Orientation;
        var servo = world.CreateConstraint<AngularServo>(reference, body);
        JQuaternion target = JQuaternion.CreateRotationX((Real)0.9) * JQuaternion.CreateRotationZ((Real)0.5);
        servo.Initialize(target);
        servo.MaximumTorque = 20;
        Assert.That(body.Orientation, Is.EqualTo(before));
        for (int change = 0; change < 2; change++)
        {
            for (int i = 0; i < 200; i++) world.Step((Real)0.01, false);
            var expected = reference.Orientation * target;
            Assert.That(MathR.Abs(JQuaternion.Dot(expected, body.Orientation)), Is.EqualTo((Real)1).Within((Real)1e-5));
            target = JQuaternion.CreateRotationZ((Real)(-0.6)) * JQuaternion.CreateRotationY((Real)0.4);
            servo.TargetRelativeOrientation = target;
        }
    }

    [Test]
    public void LinearServoTracksOffsetsInBody1Frame([Values] SolveMode mode)
    {
        using var world = new World { Gravity = JVector.Zero, AllowDeactivation = false, SolveMode = mode, SubstepCount = 4 };
        var reference = CreateBody(world);
        reference.MotionType = MotionType.Kinematic;
        reference.Position = new JVector(4, 3, 2);
        reference.Orientation = JQuaternion.CreateRotationZ((Real)0.7);
        var body = CreateBody(world);
        body.Position = new JVector(-1, 2, 3);
        var before = body.Position;
        var servo = world.CreateConstraint<LinearServo>(reference, body);
        servo.Initialize(reference.Position, body.Position);
        servo.MaximumForce = 100;
        Assert.That(body.Position, Is.EqualTo(before));
        for (int change = 0; change < 2; change++)
        {
            var target = change == 0 ? JVector.Zero : new JVector(1, 2, -1);
            servo.TargetPosition = target;
            for (int i = 0; i < 300; i++) world.Step((Real)0.01, false);
            var expected = reference.Position + JVector.Transform(target, reference.Orientation);
            Assert.That((body.Position - expected).Length(), Is.LessThan((Real)0.001));
        }
    }

    [Test]
    public void TwistServoCoexistsWithHingeLimits([Values] SolveMode mode)
    {
        using var world = new World { Gravity = JVector.Zero, AllowDeactivation = false, SolveMode = mode, SubstepCount = 4 };
        var body = CreateBody(world);
        var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
        hinge.Initialize(JVector.UnitX, new AngularLimit((JAngle)(Real)(-0.5), (JAngle)(Real)0.5));
        var servo = world.CreateConstraint<TwistServo>(world.NullBody, body);
        servo.Initialize(JVector.UnitX, (JAngle)(Real)0.9);
        servo.MaximumTorque = 5;
        for (int i = 0; i < 300; i++) world.Step((Real)0.01, false);
        Assert.That(hinge.Angle.Radian, Is.EqualTo((Real)0.5).Within((Real)0.01));
        servo.TargetAngle = (JAngle)(Real)(-0.25);
        for (int i = 0; i < 300; i++) world.Step((Real)0.01, false);
        Assert.That(hinge.Angle.Radian, Is.EqualTo((Real)(-0.25)).Within((Real)0.001));
    }

    [Test]
    public void AxisServoLeavesUncontrolledMotionFree([Values] bool angular)
    {
        using var world = new World();
        var body = CreateBody(world);
        body.Velocity = new JVector(0, 2, 3);
        body.AngularVelocity = new JVector(0, 4, 5);
        var servo = CreateServo(world, world.NullBody, body, angular ? ServoKind.Twist : ServoKind.LinearAxis);
        Solve(servo, new TimeStep((Real)0.01));
        Assert.That(body.Velocity.Y, Is.EqualTo((Real)2));
        Assert.That(body.Velocity.Z, Is.EqualTo((Real)3));
        Assert.That(body.AngularVelocity.Y, Is.EqualTo((Real)4));
        Assert.That(body.AngularVelocity.Z, Is.EqualTo((Real)5));
        Assert.That(angular ? body.Velocity.X : body.AngularVelocity.X, Is.Zero);
        Assert.That(angular ? body.AngularVelocity.X : body.Velocity.X, Is.GreaterThan((Real)0));
    }

    [Test]
    public void NonDynamicBodiesRemainBitwiseUnchanged([Values] ServoKind kind,
        [Values("null", "static", "kinematic")] string supportKind, [Values] bool reverseBodies)
    {
        using var world = new World();
        var support = supportKind == "null" ? world.NullBody : CreateBody(world);
        if (supportKind != "null") support.MotionType = supportKind == "static" ? MotionType.Static : MotionType.Kinematic;
        var body = CreateBody(world);
        var servo = CreateServo(world, reverseBodies ? body : support, reverseBodies ? support : body, kind);
        Solve(servo, new TimeStep((Real)0.01));
        Real negativeZero = MathR.CopySign((Real)0, -1);
        support.Data.Velocity = support.Data.AngularVelocity = new JVector(negativeZero);
        byte[] before = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref support.Data, 1)).ToArray();
        Solve(servo, new TimeStep((Real)0.01));
        byte[] after = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref support.Data, 1)).ToArray();
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void AngularTorqueCapAppliesToPhysicalImpulseAtLargeErrors()
    {
        using var world = new World();
        var body = CreateBody(world);
        body.Orientation = JQuaternion.CreateRotationY((Real)2.8) * JQuaternion.CreateRotationZ((Real)0.4);
        body.AngularVelocity = new JVector(20, -30, 40);
        var servo = world.CreateConstraint<AngularServo>(world.NullBody, body);
        servo.Initialize(JQuaternion.Identity);
        servo.MaximumTorque = 3;
        JVector previous = body.AngularVelocity;
        Solve(servo, new TimeStep((Real)0.01));
        Assert.That((body.AngularVelocity - previous).Length(), Is.EqualTo((Real)0.03).Within((Real)1e-5));
        Assert.That(servo.Impulse.Length(), Is.EqualTo((Real)0.03).Within((Real)1e-6));
    }

    [Test]
    public void MaximumRejectsInvalidValuesAndAllowsInfinity([Values] ServoKind kind)
    {
        using var world = new World();
        var servo = CreateServo(world, world.NullBody, CreateBody(world), kind);
        foreach (Real invalid in new[] { (Real)(-1), Real.NaN, Real.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => SetMaximum(servo, invalid));
        SetMaximum(servo, Real.PositiveInfinity);
        Solve(servo, new TimeStep((Real)0.01));
    }
    [Test]
    public void TargetChangesWakeSleepingBodies([Values] ServoKind kind, [Values] SolveMode mode)
    {
        using var world = new World { Gravity = JVector.Zero, SolveMode = mode };
        var body = CreateBody(world);
        var servo = CreateServo(world, world.NullBody, body, kind, 0);
        world.Step((Real)0.01, false);
        world.ForceSleepIsland(body.Island);
        Assert.That(body.IsActive, Is.False);
        switch (servo)
        {
            case AngularServo angular: angular.TargetRelativeOrientation = JQuaternion.CreateRotationY((Real)0.8); break;
            case TwistServo twist: twist.TargetAngle = (JAngle)(Real)0.8; break;
            case LinearServo linear: linear.TargetPosition = JVector.UnitX; break;
            case LinearAxisServo axis: axis.TargetPosition = 1; break;
        }
        // Target setters should wake the body even while the actuator is disabled.
        world.Step((Real)0.01, false);
        Assert.That(body.IsActive, Is.True);
        SetMaximum(servo, 2);
        world.Step((Real)0.01, false);
        Real speed = kind is ServoKind.Angular or ServoKind.Twist ? body.AngularVelocity.Length() : body.Velocity.Length();
        Assert.That(speed, Is.GreaterThan((Real)0));
    }

    [Test]
    public void LinearAxisServoTracksSignedPositionWithoutCorrectingPerpendicularOffset()
    {
        using var world = new World { Gravity = JVector.Zero, AllowDeactivation = false, SubstepCount = 4 };
        var body = CreateBody(world);
        body.Position = new JVector(3, 4, 5);
        var servo = world.CreateConstraint<LinearAxisServo>(world.NullBody, body);
        servo.Initialize(JVector.UnitX, JVector.Zero, body.Position, -1);
        servo.MaximumForce = 100;
        for (int i = 0; i < 300; i++) world.Step((Real)0.01, false);
        Assert.That(body.Position.X, Is.EqualTo((Real)(-1)).Within((Real)0.001));
        Assert.That(body.Position.Y, Is.EqualTo((Real)4));
        Assert.That(body.Position.Z, Is.EqualTo((Real)5));
    }

    [TestCase(-179, 179)]
    [TestCase(179, -179)]
    public void TwistServoPreservesSignedAngleDifference(int currentDegrees, int targetDegrees)
    {
        using var world = new World();
        var body = CreateBody(world);
        var servo = world.CreateConstraint<TwistServo>(world.NullBody, body);
        servo.Initialize(JVector.UnitX, JAngle.FromDegree(targetDegrees));
        servo.MaximumTorque = Real.PositiveInfinity;
        body.Orientation = JQuaternion.CreateRotationX(JAngle.FromDegree(currentDegrees).Radian);
        Solve(servo, new TimeStep((Real)0.01));
        Assert.That(body.AngularVelocity.X * Math.Sign(targetDegrees - currentDegrees), Is.GreaterThan((Real)1));
    }

    [TestCase(-179, 179)]
    [TestCase(179, -179)]
    public void AngularServoTakesShortestPathBetweenOrientations(int currentDegrees, int targetDegrees)
    {
        using var world = new World();
        var body = CreateBody(world);
        body.Orientation = JQuaternion.CreateRotationX(JAngle.FromDegree(currentDegrees).Radian);
        var servo = world.CreateConstraint<AngularServo>(world.NullBody, body);
        servo.Initialize(JQuaternion.CreateRotationX(JAngle.FromDegree(targetDegrees).Radian));
        servo.MaximumTorque = Real.PositiveInfinity;
        Solve(servo, new TimeStep((Real)0.01));
        Assert.That(body.AngularVelocity.X * Math.Sign(currentDegrees - targetDegrees), Is.GreaterThan((Real)0));
        Assert.That(MathR.Abs(body.AngularVelocity.X), Is.LessThan((Real)1));
    }

    [Test]
    public void TwistServoRejectsTargetsOutsideItsAngularInterval()
    {
        using var world = new World();
        var body = CreateBody(world);
        var servo = world.CreateConstraint<TwistServo>(world.NullBody, body);
        servo.Initialize(JVector.UnitX);
        foreach (Real angle in new[] { -MathR.PI - (Real)0.01, MathR.PI + (Real)0.01, Real.NaN, Real.PositiveInfinity, Real.NegativeInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => servo.TargetAngle = (JAngle)angle);
            Assert.That(servo.TargetAngle.Radian, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => servo.Initialize(JVector.UnitX, (JAngle)angle));
            Assert.That(servo.TargetAngle.Radian, Is.Zero);
        }
        foreach (Real angle in new[] { -MathR.PI, MathR.PI })
        {
            servo.Initialize(JVector.UnitX, (JAngle)angle);
            Assert.That(servo.TargetAngle.Radian, Is.EqualTo(angle));
            servo.TargetAngle = (JAngle)(-angle);
            Assert.That(servo.TargetAngle.Radian, Is.EqualTo(-angle));
        }
    }

    [Test]
    public void ZeroDurationWithUnlimitedForceDoesNotApplyImpulse([Values] ServoKind kind)
    {
        using var world = new World();
        var body = CreateBody(world);
        var servo = CreateServo(world, world.NullBody, body, kind, Real.PositiveInfinity);
        body.Velocity = new JVector(1, 2, 3);
        body.AngularVelocity = new JVector(3, 2, 1);
        Solve(servo, new TimeStep(0));
        Assert.That(body.Velocity, Is.EqualTo(new JVector(1, 2, 3)));
        Assert.That(body.AngularVelocity, Is.EqualTo(new JVector(3, 2, 1)));
    }

    [Test]
    public void ZeroMaximumDoesNotApplyImpulse([Values] ServoKind kind)
    {
        using var world = new World();
        var body = CreateBody(world);
        var servo = CreateServo(world, world.NullBody, body, kind, 0);
        body.Velocity = new JVector(1, 2, 3);
        body.AngularVelocity = new JVector(3, 2, 1);
        Solve(servo, new TimeStep((Real)0.01));
        Assert.That(body.Velocity, Is.EqualTo(new JVector(1, 2, 3)));
        Assert.That(body.AngularVelocity, Is.EqualTo(new JVector(3, 2, 1)));
    }

    [Test]
    public void UnlimitedServoMatchesExistingConstraintResponse([Values] ServoKind kind)
    {
        using var servoWorld = new World();
        using var constraintWorld = new World();
        var servoBody = CreateBody(servoWorld);
        var constraintBody = CreateBody(constraintWorld);
        var servo = CreateServo(servoWorld, servoWorld.NullBody, servoBody, kind, Real.PositiveInfinity);
        Constraint constraint;
        switch (servo)
        {
            case AngularServo angular:
                constraintBody.Orientation = angular.TargetRelativeOrientation;
                var fixedAngle = constraintWorld.CreateConstraint<FixedAngle>(constraintWorld.NullBody, constraintBody);
                fixedAngle.Initialize();
                constraintBody.Orientation = JQuaternion.Identity;
                constraint = fixedAngle;
                break;
            case TwistServo twist:
                var twistAngle = constraintWorld.CreateConstraint<TwistAngle>(constraintWorld.NullBody, constraintBody);
                twistAngle.Initialize(JVector.UnitX, JVector.UnitX, new AngularLimit(twist.TargetAngle, twist.TargetAngle));
                constraint = twistAngle;
                break;
            case LinearServo linear:
                var socket = constraintWorld.CreateConstraint<BallSocket>(constraintWorld.NullBody, constraintBody);
                socket.Initialize(linear.TargetPosition);
                socket.Anchor2 = JVector.Zero;
                constraint = socket;
                break;
            default:
                var axis = (LinearAxisServo)servo;
                var plane = constraintWorld.CreateConstraint<PointOnPlane>(constraintWorld.NullBody, constraintBody);
                plane.Initialize(JVector.UnitX, JVector.Zero, JVector.Zero,
                    new LinearLimit(axis.TargetPosition, axis.TargetPosition));
                constraint = plane;
                break;
        }
        servoBody.Velocity = constraintBody.Velocity = new JVector(2, 3, -4);
        servoBody.AngularVelocity = constraintBody.AngularVelocity = new JVector(-3, 1, 2);
        var step = new TimeStep((Real)0.01, 4);
        Solve(servo, step);
        Solve(constraint, step);
        Assert.That((servoBody.Velocity - constraintBody.Velocity).Length(), Is.LessThan((Real)1e-4));
        Assert.That((servoBody.AngularVelocity - constraintBody.AngularVelocity).Length(), Is.LessThan((Real)1e-4));
    }

    [Test]
    public void AngularServoCorrectsAnInitialHalfTurn()
    {
        using var world = new World { Gravity = JVector.Zero, AllowDeactivation = false, SubstepCount = 4 };
        var body = CreateBody(world);
        body.Orientation = new JQuaternion(0, 1, 0, 0);
        var servo = world.CreateConstraint<AngularServo>(world.NullBody, body);
        servo.Initialize(JQuaternion.Identity);
        servo.MaximumTorque = 20;
        for (int i = 0; i < 300; i++) world.Step((Real)0.01, false);
        Assert.That(MathR.Abs(body.Orientation.W), Is.EqualTo((Real)1).Within((Real)1e-5));
    }

}
