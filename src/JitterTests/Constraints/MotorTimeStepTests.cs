namespace JitterTests.Constraints;

public class MotorTimeStepTests
{
    // Keep the target unreachable so acceleration is determined by the force/torque cap.
    [TestCase(SolveMode.Regular, false)]
    [TestCase(SolveMode.Regular, true)]
    [TestCase(SolveMode.Deterministic, false)]
    [TestCase(SolveMode.Deterministic, true)]
    public void MotorAcceleration_UsesElapsedTimeRegardlessOfSubsteps(SolveMode solveMode, bool multiThread)
    {
        foreach (bool angular in new[] { false, true })
        foreach (int steps in new[] { 30, 60, 120 })
        foreach (int substeps in new[] { 1, 2, 4, 8 })
        {
            using var world = new World
            {
                Gravity = JVector.Zero,
                AllowDeactivation = false,
                SolveMode = solveMode,
                SubstepCount = substeps
            };
            var body = world.CreateRigidBody();
            body.SetMassInertia(JSymmetricMatrix.Identity, 1);
            body.Damping = (0, 0);

            if (angular)
            {
                var motor = world.CreateConstraint<AngularMotor>(world.NullBody, body);
                motor.Initialize(JVector.UnitX);
                motor.TargetVelocity = 1000;
                motor.MaximumTorque = 2;
            }
            else
            {
                var motor = world.CreateConstraint<LinearMotor>(world.NullBody, body);
                motor.Initialize(JVector.UnitX, JVector.UnitX);
                motor.TargetVelocity = 1000;
                motor.MaximumForce = 2;
            }

            for (int i = 0; i < steps; i++) world.Step((Real)1.0 / steps, multiThread);

            Real velocity = angular ? body.AngularVelocity.X : body.Velocity.X;
            Assert.That(velocity, Is.EqualTo((Real)2.0).Within((Real)1e-4),
                $"angular={angular}, steps={steps}, substeps={substeps}");
        }
    }

    [TestCase(SolveMode.Regular)]
    [TestCase(SolveMode.Deterministic)]
    public void Stabilize_UsesTheWorldSubstepCount(SolveMode solveMode)
    {
        using var world = new World { Gravity = JVector.Zero, SolveMode = solveMode, SubstepCount = 4 };
        var body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Identity, 1);
        var motor = world.CreateConstraint<AngularMotor>(world.NullBody, body);
        motor.Initialize(JVector.UnitX);
        motor.TargetVelocity = 1000;
        motor.MaximumTorque = 2;

        world.Stabilize((Real)0.1, 2, multiThread: false);

        Assert.That(body.AngularVelocity.X, Is.EqualTo((Real)0.2).Within((Real)1e-6));
        Assert.That(body.Orientation, Is.EqualTo(JQuaternion.Identity));
    }
}
