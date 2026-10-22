using System.Globalization;
using Jitter2.SoftBodies;

namespace JitterTests.Api;

public class FormattingTests
{
    [Test]
    public void GeneralFormat_PreservesSmallValuesAndFullPrecision()
    {
        JVector vector = new((Real)1e-8, Real.BitIncrement((Real)1), Real.Epsilon);
        string[] components = vector.ToString("G", CultureInfo.InvariantCulture).Split(", ");

        for (int i = 0; i < 3; i++)
        {
            string number = components[i][2..];
            Assert.That(Real.Parse(number, CultureInfo.InvariantCulture), Is.EqualTo(vector[i]));
        }
    }

    [Test]
    [SetCulture("de-DE")]
    public void CompositeFormatting_PropagatesFormatAndCultureToNestedValues()
    {
        JVector point = new((Real)1.25, (Real)2.5, (Real)3.75);
        JBoundingBox box = new(point, point);

        string invariant = string.Format(CultureInfo.InvariantCulture, "{0:F2}", box);
        string localized = string.Format("{0:F2}", box);

        Assert.That(invariant, Is.EqualTo("Min={X=1.25, Y=2.50, Z=3.75}, Max={X=1.25, Y=2.50, Z=3.75}"));
        Assert.That(localized, Is.EqualTo("Min={X=1,25, Y=2,50, Z=3,75}, Max={X=1,25, Y=2,50, Z=3,75}"));
    }

    [Test]
    [SetCulture("de-DE")]
    public void DefaultFormatting_UsesCurrentCultureAndMatchesGeneralFormat()
    {
        JVector point = new((Real)1.25, (Real)2.5, (Real)3.75);
        IFormattable[] values =
        [
            point,
            new JQuaternion(point.X, point.Y, point.Z, (Real)4.5),
            JMatrix.CreateScale(point),
            JSymmetricMatrix.CreateScale(point.X, point.Y, point.Z),
            JAngle.FromRadian(point.X),
            new JBoundingBox(point, point),
            new JTriangle(point, point, point),
            new LinearLimit(point.X, point.Y),
            AngularLimit.FromDegree(point.X, point.Y)
        ];

        foreach (IFormattable value in values)
        {
            string? description = value.ToString();
            Assert.That(description, Is.Not.Null.And.Not.Empty);
            Assert.That(description, Is.EqualTo(value.ToString("G", CultureInfo.CurrentCulture)));
            Assert.That(description, Is.EqualTo(value.ToString(null, null)));
            Assert.That(description, Is.EqualTo(value.ToString("", null)));
            Assert.That(value.ToString("F2", null), Does.Contain("1,25"));
        }
    }

    [Test]
    public void AngularLimitFormatting_LabelsDegrees()
    {
        AngularLimit limit = AngularLimit.FromDegree(-45, 90);
        Assert.That(string.Format(CultureInfo.InvariantCulture, "{0:F1}", limit),
            Is.EqualTo("From=-45.0 deg, To=90.0 deg"));
    }

    [Test]
    public void ConstraintDescriptions_HandleUninitializedConstraints()
    {
        Constraint full = new BallSocket();
        Constraint small = new AngularMotor();
        Constraint spring = new SpringConstraint();

        Assert.That(full.ToString(), Is.EqualTo("BallSocket { Uninitialized }"));
        Assert.That(small.ToString(), Is.EqualTo("AngularMotor { Uninitialized }"));
        Assert.That(spring.ToString(), Is.EqualTo("SpringConstraint { Uninitialized }"));
    }

    [Test]
    public void BodyAndConstraintDescriptions_ShowIdentityAndCurrentState()
    {
        using var world = new World();
        RigidBody body = world.CreateRigidBody();
        body.Position = new JVector((Real)1.25, (Real)2.5, (Real)3.75);
        Constraint constraint = world.CreateConstraint<BallSocket>(world.NullBody, body);
        constraint.IsEnabled = false;

        Assert.That(body.ToString("F2", CultureInfo.InvariantCulture), Is.EqualTo(
            $"RigidBody {{ Id={body.RigidBodyId}, MotionType=Dynamic, IsActive={body.IsActive}, " +
            "Position={X=1.25, Y=2.50, Z=3.75} }"));
        Assert.That(constraint.ToString(), Is.EqualTo(
            $"BallSocket {{ Id={constraint.Handle.Data.ConstraintId}, Body1={world.NullBody.RigidBodyId}, " +
            $"Body2={body.RigidBodyId}, IsEnabled=False }}"));
        Assert.That(body.Position, Is.EqualTo(new JVector((Real)1.25, (Real)2.5, (Real)3.75)));
        Assert.That(constraint.IsEnabled, Is.False);
    }

    [TestCase("Remove")]
    [TestCase("Clear")]
    [TestCase("Dispose")]
    public void Descriptions_AreSafeAfterSimulationDataIsReleased(string operation)
    {
        using var world = new World();
        RigidBody body = world.CreateRigidBody();
        Constraint full = world.CreateConstraint<BallSocket>(world.NullBody, body);
        Constraint small = world.CreateConstraint<AngularMotor>(world.NullBody, body);

        switch (operation)
        {
            case "Remove": world.Remove(body); break;
            case "Clear": world.Clear(); break;
            case "Dispose": world.Dispose(); break;
        }

        Assert.That(body.ToString(), Is.EqualTo($"RigidBody {{ Id={body.RigidBodyId}, Invalid }}"));
        Assert.That(full.ToString(), Does.StartWith("BallSocket {").And.Contain("Invalid"));
        Assert.That(small.ToString(), Does.StartWith("AngularMotor {").And.Contain("Invalid"));
    }
}
