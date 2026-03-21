using Jitter2.Dynamics.Constraints;

namespace JitterTests.Constraints;

public class AdditionalConstraintBehaviorTests
{
    [TestCase]
    public void LinearMotor_DrivesBodyTowardTargetVelocity()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4
        };

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));

        var motor = world.CreateConstraint<LinearMotor>(world.NullBody, body);
        motor.Initialize(JVector.UnitX, JVector.UnitX);
        motor.TargetVelocity = (Real)2.0;
        motor.MaximumForce = (Real)100.0;

        Helper.AdvanceWorld(world, 1, 1f / 100f, false);

        Assert.That(body.Velocity.X, Is.EqualTo((Real)2.0).Within((Real)0.2));
        world.Dispose();
    }

    [TestCase]
    public void AngularMotor_DrivesAngularVelocityTowardTarget()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4
        };

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));

        var motor = world.CreateConstraint<AngularMotor>(world.NullBody, body);
        motor.Initialize(JVector.UnitY, JVector.UnitY);
        motor.TargetVelocity = (Real)3.0;
        motor.MaximumForce = (Real)100.0;

        Helper.AdvanceWorld(world, 1, 1f / 100f, false);

        Assert.That(body.AngularVelocity.Y, Is.EqualTo((Real)3.0).Within((Real)0.3));
        world.Dispose();
    }

    [TestCase]
    public void PointOnLine_ReducesPerpendicularDistanceToLine()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4
        };

        var lineBody = world.CreateRigidBody();
        lineBody.AddShape(new SphereShape(1));
        lineBody.MotionType = MotionType.Static;

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(0, 3, 2);

        var pointOnLine = world.CreateConstraint<PointOnLine>(lineBody, body);
        pointOnLine.Initialize(JVector.UnitX, JVector.Zero, body.Position, LinearLimit.Full);

        var before = MathR.Sqrt(body.Position.Y * body.Position.Y + body.Position.Z * body.Position.Z);
        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        var after = MathR.Sqrt(body.Position.Y * body.Position.Y + body.Position.Z * body.Position.Z);

        Assert.That(after, Is.LessThan(before));
        Assert.That(after, Is.LessThan((Real)2.1));
        Assert.That(pointOnLine.Impulse.Length(), Is.GreaterThan((Real)0.0));
        world.Dispose();
    }

    [TestCase]
    public void PointOnPlane_ReducesDistanceToPlane()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4
        };

        var planeBody = world.CreateRigidBody();
        planeBody.AddShape(new SphereShape(1));
        planeBody.MotionType = MotionType.Static;

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(1, 3, 2);

        var pointOnPlane = world.CreateConstraint<PointOnPlane>(planeBody, body);
        pointOnPlane.Initialize(JVector.UnitY, JVector.Zero, body.Position, LinearLimit.Fixed);

        var before = MathR.Abs(body.Position.Y);
        Helper.AdvanceWorld(world, 2, 1f / 100f, false);
        var after = MathR.Abs(body.Position.Y);

        Assert.That(after, Is.LessThan(before));
        Assert.That(after, Is.LessThan((Real)0.2));
        world.Dispose();
    }

    [TestCase]
    public void HingeAngle_LimitReducesRotationComparedToUnconstrained()
    {
        static Real Run(bool constrained)
        {
            var world = new World
            {
                Gravity = JVector.Zero,
                AllowDeactivation = false,
                SubstepCount = 4
            };

            var body = world.CreateRigidBody();
            body.AddShape(new SphereShape(1));

            if (constrained)
            {
                var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
                hinge.Initialize(JVector.UnitY, AngularLimit.FromDegree(-10, 10));
            }

            body.AngularVelocity = new JVector(0, (Real)8.0, 0);
            for (int i = 0; i < 10; i++)
            {
                world.Step(1f / 100f, false);
            }

            var clampedW = Math.Clamp(MathR.Abs(body.Orientation.W), (Real)0.0, (Real)1.0);
            var angle = (Real)2.0 * MathR.Acos(clampedW);
            world.Dispose();
            return angle;
        }

        var unconstrained = Run(false);
        var constrained = Run(true);

        Assert.That(unconstrained, Is.GreaterThan((Real)0.4));
        Assert.That(constrained, Is.LessThan(unconstrained));
        Assert.That(constrained, Is.LessThan((Real)0.4));
    }
}
