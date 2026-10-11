#if DEBUG

namespace JitterTests.Robustness;

public class DebugConstraintValidationTests
{
    [Test]
    public void InvalidConstraint_DataAccessThrowsButIdentityRemainsInspectable(
        [Values] bool small,
        [Values("removed", "body_removed", "cleared", "disposed", "uncreated")] string invalidation)
    {
        using World world = new();
        RigidBody body1 = world.CreateRigidBody();
        RigidBody body2 = world.CreateRigidBody();
        Constraint constraint;
        (string Name, TestDelegate Access)[] accesses;

        if (small)
        {
            LinearMotor motor = invalidation == "uncreated"
                ? new LinearMotor()
                : world.CreateConstraint<LinearMotor>(body1, body2);
            constraint = motor;
            accesses =
            [
                ("Data", () => _ = motor.Data),
                ("TargetVelocity getter", () => _ = motor.TargetVelocity),
                ("TargetVelocity setter", () => motor.TargetVelocity = (Real)1.0),
                ("Impulse", () => _ = motor.Impulse)
            ];
        }
        else
        {
            BallSocket socket = invalidation == "uncreated"
                ? new BallSocket()
                : world.CreateConstraint<BallSocket>(body1, body2);
            constraint = socket;
            accesses =
            [
                ("Data", () => _ = socket.Data),
                ("Softness getter", () => _ = socket.Softness),
                ("Softness setter", () => socket.Softness = (Real)0.001),
                ("Impulse", () => _ = socket.Impulse)
            ];
        }

        Assert.That(constraint.IsSmallConstraint, Is.EqualTo(small));
        Assert.That(constraint.IsValid, Is.EqualTo(invalidation != "uncreated"));
        RigidBody? expectedBody1 = constraint.Body1;
        RigidBody? expectedBody2 = constraint.Body2;

        switch (invalidation)
        {
            case "removed":
                world.Remove(constraint);
                break;
            case "body_removed":
                world.Remove(body1);
                break;
            case "cleared":
                world.Clear();
                break;
            case "disposed":
                world.Dispose();
                break;
        }

        Assert.That(constraint.IsValid, Is.False);
        Assert.That(constraint.Body1, Is.SameAs(expectedBody1));
        Assert.That(constraint.Body2, Is.SameAs(expectedBody2));
        Assert.DoesNotThrow(() => _ = constraint.Handle);
        Assert.DoesNotThrow(() => _ = constraint.SmallHandle);

        accesses =
        [
            .. accesses,
            ("ResetWarmStart", constraint.ResetWarmStart),
            ("IsEnabled getter", () => _ = constraint.IsEnabled),
            ("IsEnabled setter true", () => constraint.IsEnabled = true),
            ("IsEnabled setter false", () => constraint.IsEnabled = false)
        ];

        foreach (var (name, access) in accesses)
        {
            Assert.Throws<InvalidOperationException>(access, name);
        }

        if (!world.IsDisposed)
        {
            Assert.That(world.RawData.Constraints.Length, Is.Zero);
            Assert.That(world.RawData.SmallConstraints.Length, Is.Zero);
            Assert.DoesNotThrow(() => world.Step((Real)(1.0 / 60.0), false));
        }
    }
}

#endif
