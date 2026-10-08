namespace JitterTests.Constraints;

public class ConstraintTimeStepTests
{
    [TestCase(SolveMode.Regular)]
    [TestCase(SolveMode.Deterministic)]
    public void SoftConstraint_DeflectionUnderConstantForceDoesNotDependOnSubsteps(SolveMode solveMode)
    {
        foreach (int substeps in new[] { 1, 2, 4, 8 })
        {
            using var world = new World
            {
                Gravity = new JVector(0, -10, 0),
                AllowDeactivation = false,
                SubstepCount = substeps,
                SolveMode = solveMode
            };
            var body = world.CreateRigidBody();
            body.SetMassInertia(JSymmetricMatrix.Identity, 1);
            body.Damping = (0, 0);
            var joint = world.CreateConstraint<BallSocket>(world.NullBody, body);
            joint.Initialize(JVector.Zero);
            joint.Softness = (Real)0.01;
            joint.Bias = (Real)0.2;

            for (int i = 0; i < 200; i++) world.Step((Real)0.01, false);

            // At equilibrium the spring's corrective force balances gravity.
            // With full-step bias scaling its deflection is F * softness * stepDt / bias.
            Assert.That(body.Position.Y, Is.EqualTo((Real)(-0.005)).Within((Real)1e-5),
                $"substeps={substeps}");
            Assert.That(body.Velocity.Length(), Is.LessThan((Real)1e-5));
        }
    }
}
