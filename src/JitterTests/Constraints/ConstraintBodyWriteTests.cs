using System.Runtime.InteropServices;
using Jitter2.SoftBodies;

namespace JitterTests.Constraints;

public class ConstraintBodyWriteTests
{
    [Test]
    public void NonDynamicBodyRemainsBitwiseUnchanged(
        [Values("BallSocket", "DistanceLimit", "PointOnLine", "PointOnPlane", "ConeLimit", "EllipticalConeLimit",
            "FixedAngle", "HingeAngle", "TwistAngle", "AngularMotor", "LinearMotor", "SpringConstraint")] string kind,
        [Values("null", "static", "kinematic")] string supportKind,
        [Values] bool reverseBodies,
        [Values] bool warmStart)
    {
        using World world = new();
        RigidBody support = supportKind == "null" ? world.NullBody : world.CreateRigidBody();
        if (supportKind != "null")
            support.MotionType = supportKind == "static" ? MotionType.Static : MotionType.Kinematic;
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(2, 1, 3);
        body.Velocity = new JVector(1, 2, 3);
        body.AngularVelocity = new JVector(2, -3, 1);
        Constraint constraint = Create(world, reverseBodies ? body : support, reverseBodies ? support : body, kind);
        ref ConstraintData data = ref constraint.Handle.Data;
        var step = new TimeStep((Real)0.01);

        // Produce a cached impulse before checking either warm starting or another solver iteration.
        data.PrepareForIteration(ref data, step);
        data.Iterate(ref data, step);
        // Signed zero exposes zero-valued arithmetic writes that ordinary equality would miss.
        Real negativeZero = MathR.CopySign((Real)0, (Real)(-1));
        support.Data.Velocity = new JVector(negativeZero);
        support.Data.AngularVelocity = new JVector(negativeZero);
        byte[] before = Snapshot(ref support.Data);

        if (warmStart) data.PrepareForIteration(ref data, step);
        else data.Iterate(ref data, step);

        Assert.That(Snapshot(ref support.Data), Is.EqualTo(before));
    }

    [Test]
    public void MotorReadsKinematicVelocityWithoutChangingIt([Values] bool angular, [Values] bool reverseBodies)
    {
        using World world = new();
        RigidBody support = world.CreateRigidBody(), body = world.CreateRigidBody();
        support.MotionType = MotionType.Kinematic;
        support.Velocity = new JVector(2, 3, 4);
        support.AngularVelocity = new JVector(3, 4, 5);
        body.AddShape(new SphereShape(1));
        Constraint constraint = Create(world, reverseBodies ? body : support, reverseBodies ? support : body,
            angular ? "AngularMotor" : "LinearMotor");
        if (constraint is AngularMotor angularMotor)
        {
            angularMotor.Initialize(JVector.UnitX);
            angularMotor.TargetVelocity = 0;
            angularMotor.MaximumTorque = 10000;
        }
        else
        {
            var linearMotor = (LinearMotor)constraint;
            linearMotor.Initialize(JVector.UnitX, JVector.UnitX);
            linearMotor.TargetVelocity = 0;
            linearMotor.MaximumForce = 10000;
        }
        byte[] before = Snapshot(ref support.Data);
        ref ConstraintData data = ref constraint.Handle.Data;
        var step = new TimeStep((Real)0.01);
        data.PrepareForIteration(ref data, step);
        data.Iterate(ref data, step);

        Assert.That(Snapshot(ref support.Data), Is.EqualTo(before));
        Assert.That(angular ? body.AngularVelocity.X : body.Velocity.X,
            Is.EqualTo(angular ? support.AngularVelocity.X : support.Velocity.X).Within((Real)1e-5));
    }

    private static Constraint Create(World world, RigidBody first, RigidBody second, string kind)
    {
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));
        switch (kind)
        {
            case "BallSocket":
                var socket = world.CreateConstraint<BallSocket>(first, second);
                socket.Initialize(first.Position); return socket;
            case "DistanceLimit":
                var distance = world.CreateConstraint<DistanceLimit>(first, second);
                distance.Initialize(first.Position, second.Position, LinearLimit.Fixed); return distance;
            case "PointOnLine":
                var line = world.CreateConstraint<PointOnLine>(first, second);
                line.Initialize(axis, first.Position, second.Position, LinearLimit.Fixed); return line;
            case "PointOnPlane":
                var plane = world.CreateConstraint<PointOnPlane>(first, second);
                plane.Initialize(axis, first.Position, second.Position); return plane;
            case "ConeLimit":
                var cone = world.CreateConstraint<ConeLimit>(first, second);
                cone.Initialize(axis, AngularLimit.FromDegree(45, 45)); return cone;
            case "EllipticalConeLimit":
                var ellipse = world.CreateConstraint<EllipticalConeLimit>(first, second);
                ellipse.Initialize(axis, MathHelper.CreateOrthonormal(axis), AngularLimit.FromDegree(20, 60),
                    AngularLimit.FromDegree(10, 30)); return ellipse;
            case "FixedAngle":
                var fixedAngle = world.CreateConstraint<FixedAngle>(first, second);
                fixedAngle.Initialize(); return fixedAngle;
            case "HingeAngle":
                var hinge = world.CreateConstraint<HingeAngle>(first, second);
                hinge.Initialize(axis, AngularLimit.Fixed); return hinge;
            case "TwistAngle":
                var twist = world.CreateConstraint<TwistAngle>(first, second);
                twist.Initialize(axis, JVector.UnitX, AngularLimit.Fixed); return twist;
            case "AngularMotor":
                var angularMotor = world.CreateConstraint<AngularMotor>(first, second);
                angularMotor.Initialize(axis);
                angularMotor.TargetVelocity = 1; angularMotor.MaximumTorque = 100; return angularMotor;
            case "LinearMotor":
                var linearMotor = world.CreateConstraint<LinearMotor>(first, second);
                linearMotor.Initialize(axis, axis);
                linearMotor.TargetVelocity = 1; linearMotor.MaximumForce = 100; return linearMotor;
            case "SpringConstraint":
                var spring = world.CreateConstraint<SpringConstraint>(first, second);
                spring.Initialize(first.Position, second.Position); return spring;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private static byte[] Snapshot(ref RigidBodyData data) =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)).ToArray();
}
