using System.Reflection;

namespace JitterTests.Constraints;

public class JointDriveTests
{
    public enum JointKind { Hinge, Prismatic, Universal }

    [Test]
    public void HingeCreatesOnlySelectedDrive([Values] JointDrive drive, [Values] bool limited)
    {
        using var world = new World();
        var body = CreateBody(world);
        body.Position = new JVector(2, 3, 4);
        body.Orientation = JQuaternion.CreateRotationY((Real)0.7);
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));
        JVector position = body.Position;
        JQuaternion orientation = body.Orientation;
        var joint = limited
            ? new HingeJoint(world, world.NullBody, body, position, axis, AngularLimit.FromDegree(-60, 60), drive)
            : new HingeJoint(world, world.NullBody, body, position, axis, drive);

        Assert.That(joint.Motor is not null, Is.EqualTo(drive == JointDrive.Motor));
        Assert.That(joint.Servo is not null, Is.EqualTo(drive == JointDrive.Servo));
        Assert.That(joint.Constraints.Count, Is.EqualTo(drive == JointDrive.None ? 2 : 3));
        if (joint.Motor is { } motor) Assert.That(motor.MaximumTorque, Is.Zero);
        if (joint.Servo is { } servo)
        {
            Assert.That(joint.Constraints, Does.Contain(servo));
            Assert.That(servo.Angle.Radian, Is.EqualTo((Real)0).Within((Real)1e-5));
            Assert.That(servo.TargetAngle.Radian, Is.Zero);
            Assert.That(servo.MaximumTorque, Is.Zero);
        }
        Assert.That(body.Position, Is.EqualTo(position));
        Assert.That(body.Orientation, Is.EqualTo(orientation));
    }

    [Test]
    public void PrismaticCreatesOnlySelectedDrive([Values] JointDrive drive, [Values] bool limited, [Values] bool pinned)
    {
        using var world = new World();
        var body = CreateBody(world);
        body.Position = new JVector(2, 3, 4);
        body.Orientation = JQuaternion.CreateRotationY((Real)0.7);
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));
        var joint = limited
            ? new PrismaticJoint(world, world.NullBody, body, body.Position, axis, new LinearLimit(-2, 2), drive, pinned)
            : new PrismaticJoint(world, world.NullBody, body, body.Position, axis, drive, pinned);

        Assert.That(joint.Motor is not null, Is.EqualTo(drive == JointDrive.Motor));
        Assert.That(joint.Servo is not null, Is.EqualTo(drive == JointDrive.Servo));
        Assert.That(joint.FixedAngle is not null, Is.EqualTo(pinned));
        Assert.That(joint.HingeAngle is not null, Is.EqualTo(!pinned));
        Assert.That(joint.Constraints.Count, Is.EqualTo(drive == JointDrive.None ? 2 : 3));
        if (joint.Motor is { } motor) Assert.That(motor.MaximumForce, Is.Zero);
        if (joint.Servo is { } servo)
        {
            Assert.That(joint.Constraints, Does.Contain(servo));
            Assert.That(servo.Position, Is.EqualTo(joint.Slider.Distance).Within((Real)1e-5));
            Assert.That(servo.TargetPosition, Is.Zero);
            Assert.That(servo.MaximumForce, Is.Zero);
            Assert.That((servo.Axis - axis).Length(), Is.LessThan((Real)1e-5));
        }
    }

    [Test]
    public void ServoIsIncludedInJointLifecycle([Values] JointKind kind)
    {
        using var world = new World();
        var body = CreateBody(world);
        Joint joint = kind switch
        {
            JointKind.Hinge => new HingeJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, JointDrive.Servo),
            JointKind.Prismatic => new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, JointDrive.Servo),
            _ => new UniversalJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, JVector.UnitX, JointDrive.Servo)
        };
        Constraint servo = joint switch
        {
            HingeJoint hinge => hinge.Servo!,
            PrismaticJoint prismatic => prismatic.Servo!,
            _ => ((UniversalJoint)joint).Servo!
        };
        joint.Disable();
        Assert.That(servo.IsEnabled, Is.False);
        joint.Enable();
        Assert.That(servo.IsEnabled, Is.True);
        var drawer = new CountingDrawer();
        joint.DebugDraw(drawer, new DebugDrawSettings { SolidArrows = false });
        Assert.That(drawer.Count, Is.GreaterThan(0));
        joint.Remove();
        Assert.That(servo.IsValid, Is.False);
        Assert.That(body.Constraints, Is.Empty);
        Assert.That(joint.Constraints.Count, Is.Zero);
    }

    [Test]
    public void ServoTargetUsesJointCoordinateAndRespectsLimits([Values] bool prismatic, [Values] SolveMode mode)
    {
        using var world = new World { Gravity = JVector.Zero, AllowDeactivation = false, SolveMode = mode, SubstepCount = 4 };
        var body = CreateBody(world);
        Func<Real> coordinate;
        Action<Real> setTarget;
        if (prismatic)
        {
            var joint = new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX,
                new LinearLimit((Real)(-0.5), (Real)0.5), JointDrive.Servo);
            joint.Servo!.MaximumForce = 5;
            coordinate = () => joint.Slider.Distance;
            setTarget = target => joint.Servo.TargetPosition = target;
        }
        else
        {
            var joint = new HingeJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX,
                new AngularLimit((JAngle)(Real)(-0.5), (JAngle)(Real)0.5), JointDrive.Servo);
            joint.Servo!.MaximumTorque = 5;
            coordinate = () => joint.HingeAngle.Angle.Radian;
            setTarget = target => joint.Servo.TargetAngle = (JAngle)target;
        }
        setTarget((Real)0.9);
        for (int i = 0; i < 300; i++) world.Step((Real)0.01, false);
        Assert.That(coordinate(), Is.EqualTo((Real)0.5).Within((Real)0.01));
        setTarget((Real)(-0.25));
        for (int i = 0; i < 300; i++) world.Step((Real)0.01, false);
        Assert.That(coordinate(), Is.EqualTo((Real)(-0.25)).Within((Real)0.001));
    }

    [Test]
    public void HingeServoTravelsThroughItsAllowedAngularInterval(
        [Values(-1, 1)] int direction, [Values(1, 4)] int substeps, [Values] SolveMode mode)
    {
        using var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = substeps,
            SolveMode = mode
        };
        var reference = CreateBody(world);
        reference.MotionType = MotionType.Kinematic;
        reference.Position = new JVector(3, 2, 1);
        reference.Orientation = JQuaternion.CreateRotationY((Real)0.4);
        var body = CreateBody(world);
        body.Position = reference.Position;
        body.Orientation = reference.Orientation;
        JVector axis = JVector.Transform(JVector.UnitX, reference.Orientation);
        var joint = new HingeJoint(world, reference, body, reference.Position, axis,
            AngularLimit.FromDegree(-175, 175), JointDrive.Servo);
        var servo = joint.Servo!;
        servo.MaximumTorque = 100;
        servo.Bias = (Real)0.02;
        servo.Softness = (Real)0.00001;
        body.Orientation = reference.Orientation * JQuaternion.CreateRotationX(JAngle.FromDegree(-170 * direction).Radian);

        // Jump the target across zero in both directions, keeping the physical stops in place.
        foreach (int targetDegrees in new[] { 170 * direction, -170 * direction })
        {
            servo.TargetAngle = JAngle.FromDegree(targetDegrees);
            bool passedThroughZero = false;
            for (int i = 0; i < 600; i++)
            {
                world.Step((Real)0.01, false);
                Real angle = joint.HingeAngle.Angle.Degree;
                passedThroughZero |= MathR.Abs(angle) < (Real)5;
                Assert.That(MathR.Abs(angle), Is.LessThan((Real)175));
                Assert.That(MathR.Abs(servo.Impulse), Is.LessThanOrEqualTo((Real)100 * (Real)0.01 / substeps + (Real)1e-5));
            }
            Assert.That(passedThroughZero, Is.True);
            Assert.That(servo.Angle.Degree, Is.EqualTo((Real)targetDegrees).Within((Real)0.01));
        }
    }

    [Test]
    public void InvalidDriveDoesNotCreatePartialJoint([Values] JointKind kind)
    {
        using var world = new World();
        var body = CreateBody(world);
        var drive = (JointDrive)123;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            switch (kind)
            {
                case JointKind.Prismatic:
                    _ = new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, drive);
                    break;
                case JointKind.Hinge:
                    _ = new HingeJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, drive);
                    break;
                default:
                    _ = new UniversalJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, JVector.UnitX, drive);
                    break;
            }
        });
        Assert.That(body.Constraints, Is.Empty);
        Assert.That(world.NullBody.Constraints, Is.Empty);
    }

    [Test]
    public void ExistingMotorConstructorsForwardToDriveSelection([Values] bool prismatic, [Values] bool motor, [Values] bool limited)
    {
        using var world = new World();
        var body = CreateBody(world);
#pragma warning disable CS0618 // Check compatibility of the deprecated public constructors.
        if (prismatic)
        {
            var joint = limited
                ? new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, LinearLimit.Full, hasMotor: motor)
                : new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, hasMotor: motor);
            Assert.That(joint.Motor is not null, Is.EqualTo(motor));
            Assert.That(joint.Servo, Is.Null);
            Assert.That(joint.FixedAngle, Is.Not.Null);
        }
        else
        {
            var joint = limited
                ? new HingeJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, AngularLimit.Full, motor)
                : new HingeJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, hasMotor: motor);
            Assert.That(joint.Motor is not null, Is.EqualTo(motor));
            Assert.That(joint.Servo, Is.Null);
        }
#pragma warning restore CS0618
    }

    [TestCase(typeof(HingeJoint), 2)]
    [TestCase(typeof(PrismaticJoint), 2)]
    [TestCase(typeof(UniversalJoint), 1)]
    public void MotorFlagConstructorsAreObsolete(Type type, int count)
    {
        var constructors = type.GetConstructors().Where(c => c.GetParameters().Any(p => p.Name == "hasMotor")).ToArray();
        Assert.That(constructors, Has.Length.EqualTo(count));
        foreach (var constructor in constructors)
        {
            var obsolete = constructor.GetCustomAttribute<ObsoleteAttribute>();
            Assert.That(obsolete, Is.Not.Null);
            Assert.That(obsolete!.IsError, Is.False);
            Assert.That(obsolete.Message, Does.Contain(nameof(JointDrive)));
        }
    }

    [Test]
    public void NoDriveConstructionIsNotObsolete()
    {
        using var world = new World();
        var body = CreateBody(world);
        Joint[] joints =
        [
            new HingeJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX),
            new HingeJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, AngularLimit.Full),
            new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX),
            new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, pinned: false),
            new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, LinearLimit.Full),
            new PrismaticJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, LinearLimit.Full, pinned: false),
            new UniversalJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, JVector.UnitX)
        ];
        foreach (var joint in joints) Assert.That(joint.Constraints.Count, Is.EqualTo(2));
    }

    [Test]
    public void UniversalCreatesOnlySelectedDrive([Values] JointDrive drive)
    {
        using var world = new World();
        var first = CreateBody(world);
        var second = CreateBody(world);
        first.Position = new JVector(-2, 3, 4);
        second.Position = new JVector(1, 2, -3);
        first.Orientation = JQuaternion.CreateRotationX((Real)0.4);
        second.Orientation = JQuaternion.CreateRotationY((Real)(-0.7));
        JQuaternion expected = first.Orientation.Conjugate() * second.Orientation;
        JVector axis1 = JVector.UnitX, axis2 = JVector.Normalize(new JVector(1, 2, 3));
        var joint = new UniversalJoint(world, first, second, JVector.Zero, axis1, axis2, drive);

        Assert.That(joint.Motor is not null, Is.EqualTo(drive == JointDrive.Motor));
        Assert.That(joint.Servo is not null, Is.EqualTo(drive == JointDrive.Servo));
        Assert.That(joint.Constraints.Count, Is.EqualTo(drive == JointDrive.None ? 2 : 3));
        if (joint.Motor is { } motor)
        {
            Assert.That(motor.MaximumTorque, Is.Zero);
            Assert.That((JVector.Transform(motor.LocalAxis1, first.Orientation) - axis1).Length(), Is.LessThan((Real)1e-5));
            Assert.That((JVector.Transform(motor.LocalAxis2, second.Orientation) - axis2).Length(), Is.LessThan((Real)1e-5));
        }
        if (joint.Servo is { } servo)
        {
            Assert.That(joint.Constraints, Does.Contain(servo));
            Assert.That(servo.MaximumTorque, Is.Zero);
            Assert.That(servo.TargetRelativeOrientation, Is.EqualTo(expected));
        }
        Assert.That(first.Position, Is.EqualTo(new JVector(-2, 3, 4)));
        Assert.That(second.Position, Is.EqualTo(new JVector(1, 2, -3)));
    }

    [Test]
    public void UniversalMotorFlagConstructorStillWorks([Values] bool hasMotor)
    {
        using var world = new World();
        var body = CreateBody(world);
#pragma warning disable CS0618 // Check the deprecated constructor's compatibility.
        var joint = new UniversalJoint(world, world.NullBody, body, JVector.Zero, JVector.UnitX, JVector.UnitY, hasMotor: hasMotor);
#pragma warning restore CS0618
        Assert.That(joint.Motor is not null, Is.EqualTo(hasMotor));
        Assert.That(joint.Servo, Is.Null);
    }

    [Test]
    public void UniversalServoDrivesBothFreeRotationsAndPreservesTwistLock([Values] SolveMode mode, [Values] bool multiThread)
    {
        using var world = new World { Gravity = JVector.Zero, AllowDeactivation = false, SolveMode = mode, SubstepCount = 4 };
        var first = CreateBody(world);
        first.MotionType = MotionType.Kinematic;
        first.Position = new JVector(3, 2, 1);
        first.Orientation = JQuaternion.CreateRotationZ((Real)0.4) * JQuaternion.CreateRotationY((Real)0.3);
        JVector anchor = first.Position + JVector.Transform(JVector.UnitX * (Real)0.5, first.Orientation);
        var second = CreateBody(world);
        second.Orientation = first.Orientation * JQuaternion.CreateRotationX((Real)0.2);
        second.Position = anchor - JVector.Transform(JVector.UnitY, second.Orientation);
        JVector axis1 = JVector.Transform(JVector.UnitZ, first.Orientation);
        JVector axis2 = JVector.Transform(JVector.UnitZ, second.Orientation);
        var joint = new UniversalJoint(world, first, second, anchor, axis1, axis2, drive: JointDrive.Servo);
        joint.Servo!.MaximumTorque = 20;

        foreach (JQuaternion target in new[]
        {
            JQuaternion.CreateRotationX((Real)0.6),
            JQuaternion.CreateRotationY((Real)(-0.5)),
            JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(1, 2, 0)), (Real)0.7)
        })
        {
            joint.Servo.TargetRelativeOrientation = target;
            for (int i = 0; i < 300; i++)
            {
                world.Step((Real)0.01, multiThread);
                Assert.That(joint.Servo.Impulse.Length(), Is.LessThanOrEqualTo((Real)0.05 + (Real)1e-6));
            }
            JQuaternion expected = first.Orientation * target;
            Assert.That(MathR.Abs(JQuaternion.Dot(second.Orientation, expected)), Is.EqualTo((Real)1).Within((Real)1e-5));
            Assert.That(MathR.Abs(joint.TwistAngle.Angle.Radian), Is.LessThan((Real)0.001));
            Assert.That((joint.BallSocket.Anchor1 - joint.BallSocket.Anchor2).Length(), Is.LessThan((Real)0.001));
        }

        // A target in the forbidden direction must not unlock the mechanical joint.
        joint.Servo.TargetRelativeOrientation = JQuaternion.CreateRotationZ((Real)0.7);
        for (int i = 0; i < 300; i++) world.Step((Real)0.01, multiThread);
        Assert.That(MathR.Abs(joint.TwistAngle.Angle.Radian), Is.LessThan((Real)0.003));
        Assert.That((joint.BallSocket.Anchor1 - joint.BallSocket.Anchor2).Length(), Is.LessThan((Real)0.001));
    }

    private static RigidBody CreateBody(World world)
    {
        var body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Identity, 1);
        body.Damping = (0, 0);
        return body;
    }

    private sealed class CountingDrawer : IDebugDrawer
    {
        public int Count;
        public void DrawSegment(in JVector start, in JVector end, uint color) => Count++;
        public void DrawTriangle(in JVector a, in JVector b, in JVector c, uint color) => Count++;
    }
}
