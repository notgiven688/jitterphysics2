using System.Runtime.InteropServices;

namespace JitterTests.Constraints;

[TestFixture]
public class ConeLimitRangeTests
{
    [Test]
    public void DataFitsSmallConstraintStorage()
    {
        Assert.That(Marshal.SizeOf<ConeLimit.ConeLimitData>(), Is.LessThanOrEqualTo(Precision.ConstraintSizeSmall));
    }

    [Test]
    public void FixedEndpointsAreRejectedWithoutChangingExistingConfiguration(
        [Values("SingleAxisInitialize", "TwoAxisInitialize", "Limit")] string assignment,
        [Values(0, 180)] int endpoint)
    {
        using World world = new();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, world.CreateRigidBody());
        AngularLimit initial = AngularLimit.FromDegree(15, 60);
        cone.Initialize(JVector.UnitX, JVector.UnitY, initial);
        cone.Softness = (Real)0.002;
        cone.Bias = (Real)0.3;
        AngularLimit rejected = AngularLimit.FromDegree(endpoint, endpoint);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => AssignLimit(cone, assignment, rejected));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain(nameof(HingeAngle)));
            Assert.That(cone.Limit.From, Is.EqualTo(initial.From));
            Assert.That(cone.Limit.To, Is.EqualTo(initial.To));
            Assert.That(cone.AxisBody1, Is.EqualTo(JVector.UnitX));
            Assert.That(cone.AxisBody2, Is.EqualTo(JVector.UnitY));
            Assert.That(cone.Softness, Is.EqualTo((Real)0.002));
            Assert.That(cone.Bias, Is.EqualTo((Real)0.3));
        });
    }

    [TestCase(0, 45, "SingleAxisInitialize")]
    [TestCase(45, 180, "SingleAxisInitialize")]
    [TestCase(0, 180, "SingleAxisInitialize")]
    [TestCase(45, 45, "SingleAxisInitialize")]
    [TestCase(0, 45, "TwoAxisInitialize")]
    [TestCase(45, 180, "TwoAxisInitialize")]
    [TestCase(0, 180, "TwoAxisInitialize")]
    [TestCase(45, 45, "TwoAxisInitialize")]
    [TestCase(0, 45, "Limit")]
    [TestCase(45, 180, "Limit")]
    [TestCase(0, 180, "Limit")]
    [TestCase(45, 45, "Limit")]
    public void NaturalEndpointBoundsAndFixedInteriorTiltRemainAllowed(int lower, int upper, string assignment)
    {
        using World world = new();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, world.CreateRigidBody());
        cone.Initialize(JVector.UnitX, JVector.UnitY, AngularLimit.FromDegree(15, 60));
        AngularLimit expected = AngularLimit.FromDegree(lower, upper);

        AssignLimit(cone, assignment, expected);

        Assert.Multiple(() =>
        {
            Assert.That(cone.Limit.From, Is.EqualTo(expected.From));
            Assert.That(cone.Limit.To, Is.EqualTo(expected.To));
        });
    }

    private static void AssignLimit(ConeLimit cone, string assignment, AngularLimit limit)
    {
        switch (assignment)
        {
            case "SingleAxisInitialize":
                cone.Initialize(JVector.UnitZ, limit);
                break;
            case "TwoAxisInitialize":
                cone.Initialize(JVector.UnitZ, -JVector.UnitZ, limit);
                break;
            case "Limit":
                cone.Limit = limit;
                break;
        }
    }

    [Test]
    public void ResetWarmStartDiscardsImpulseBeforeNextPreparation()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
        cone.Initialize(JVector.UnitZ, JVector.UnitZ, AngularLimit.FromDegree(0, 30));
        body.Orientation = JQuaternion.CreateRotationX(MathR.PI / 3);
        cone.Softness = 0;
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, new TimeStep((Real)1.0 / 100));
        ConeLimit.IterateConeLimit(ref cone.Handle.Data, new TimeStep((Real)1.0 / 100));
        Assert.That(cone.Impulse, Is.Not.Zero);

        cone.ResetWarmStart();
        body.AngularVelocity = JVector.Zero;
        ConeLimit.PrepareForIterationConeLimit(ref cone.Handle.Data, new TimeStep((Real)1.0 / 100));

        Assert.That(cone.Impulse, Is.Zero);
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
    }
}
