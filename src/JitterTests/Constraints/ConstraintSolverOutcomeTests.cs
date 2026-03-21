using Jitter2.Dynamics.Constraints;

namespace JitterTests.Constraints;

public class ConstraintSolverOutcomeTests
{
    [TestCase]
    public void DistanceLimit_PullsBodiesTowardTargetDistance()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4
        };

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));
        bodyA.Position = new JVector(-3, 0, 0);

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(3, 0, 0);

        var limit = world.CreateConstraint<DistanceLimit>(bodyA, bodyB);
        limit.Initialize(bodyA.Position, bodyB.Position);
        limit.TargetDistance = (Real)2.0;

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);

        Assert.That(limit.Distance, Is.EqualTo((Real)2.0).Within((Real)0.1));
        world.Dispose();
    }

    [TestCase]
    public void BallSocket_AlignsAnchorsAfterStepping()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4
        };

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));
        bodyA.Position = new JVector(0, 0, 0);

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(3, 1, 0);

        var anchor = new JVector(1, 0, 0);
        var socket = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        socket.Initialize(anchor);

        Helper.AdvanceWorld(world, 2, 1f / 100f, false);

        var delta = socket.Anchor2 - socket.Anchor1;
        Assert.That(delta.Length(), Is.LessThan((Real)0.1));
        world.Dispose();
    }

    [TestCase]
    public void FixedAngle_ReducesRelativeOrientationError()
    {
        static Real Run(bool withConstraint)
        {
            var world = new World
            {
                Gravity = JVector.Zero,
                AllowDeactivation = false,
                SubstepCount = 4
            };

            var bodyA = world.CreateRigidBody();
            bodyA.AddShape(new SphereShape(1));

            var bodyB = world.CreateRigidBody();
            bodyB.AddShape(new SphereShape(1));
            bodyB.Orientation = JQuaternion.CreateRotationY((Real)(MathR.PI / 4.0));

            if (withConstraint)
            {
                var fixedAngle = world.CreateConstraint<FixedAngle>(bodyA, bodyB);
                fixedAngle.Initialize();
            }

            bodyB.AngularVelocity = new JVector(0, (Real)8.0, 0);
            Helper.AdvanceWorld(world, 1, 1f / 100f, false);

            var result = MathR.Abs(JQuaternion.Dot(bodyA.Orientation, bodyB.Orientation));
            world.Dispose();
            return result;
        }

        var unconstrainedDot = Run(false);
        var constrainedDot = Run(true);

        Assert.That(constrainedDot, Is.GreaterThan(unconstrainedDot));
    }
}
