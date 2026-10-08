namespace JitterTests.Constraints;

[TestFixture]
public class DistanceLimitRegressionTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    [TestCase(1e-3, MotionAxes.All)]
    [TestCase(1e-7, MotionAxes.All)]
    [TestCase(1e-10, MotionAxes.All)]
    [TestCase(1e-7, MotionAxes.LinearY)]
    public void TinySeparation_KeepsImpulseAndWarmStartIndependentOfDistance(double separation, MotionAxes axes)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Zero, 1, true);
        body.AllowedMotion = axes;
        var limit = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
        Real distance = (Real)separation;
        limit.Initialize(JVector.Zero, JVector.UnitY * distance, LinearLimit.Fixed);
        limit.Bias = limit.Softness = 0;
        body.Velocity = JVector.UnitY;

        DistanceLimit.PrepareForIterationDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        DistanceLimit.IterateDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));

        Assert.That(limit.Impulse, Is.EqualTo((Real)(-1)).Within(Tolerance));
        Assert.That(body.Velocity.Length(), Is.LessThan(Tolerance));

        // Updated anchors must not amplify the cached impulse when their separation grows.
        limit.Anchor2 = JVector.UnitY * (10 * distance);
        DistanceLimit.PrepareForIterationDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        Assert.That(body.Velocity.Y, Is.EqualTo((Real)(-1)).Within(Tolerance));
        DistanceLimit.IterateDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        Assert.That(body.Velocity.Length(), Is.LessThan(Tolerance));
    }

    [Test]
    public void CoincidentAnchors_ClearWarmStartUntilDirectionIsDefined()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Zero, 1, true);
        var limit = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
        limit.Initialize(JVector.Zero, JVector.UnitY, LinearLimit.Fixed);
        limit.Bias = limit.Softness = 0;
        body.Velocity = JVector.UnitY;
        DistanceLimit.PrepareForIterationDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        DistanceLimit.IterateDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        Assert.That(limit.Impulse, Is.EqualTo((Real)(-1)).Within(Tolerance));

        limit.Anchor2 = JVector.Zero;
        body.Velocity = JVector.UnitY;
        DistanceLimit.PrepareForIterationDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        DistanceLimit.IterateDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        Assert.That(limit.Impulse, Is.Zero);
        Assert.That(body.Velocity, Is.EqualTo(JVector.UnitY));

        limit.Anchor2 = JVector.UnitY;
        DistanceLimit.PrepareForIterationDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        DistanceLimit.IterateDistanceLimit(ref limit.Handle.Data, new TimeStep((Real)1.0 / 100));
        Assert.That(body.Velocity.Length(), Is.LessThan(Tolerance));
    }
}
