namespace JitterTests.Robustness;

public class MotionAndPredictionTests
{
    [TestCase]
    public void MotionType_InvalidEnum_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentOutOfRangeException>(() => body.MotionType = (MotionType)1234);
        world.Dispose();
    }

    [TestCase]
    public void MotionType_SetToStatic_ZeroesVelocitiesImmediately()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Velocity = new JVector(1, 2, 3);
        body.AngularVelocity = new JVector(4, 5, 6);

        body.MotionType = MotionType.Static;

        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void MotionType_StaticToDynamic_RestoresConfiguredInverseMass()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var expectedMass = body.Mass;

        body.MotionType = MotionType.Static;
        Assert.That(body.Data.InverseMass, Is.EqualTo((Real)0.0).Within((Real)1e-6));

        body.MotionType = MotionType.Dynamic;

        Assert.That(body.Mass, Is.EqualTo(expectedMass).Within((Real)1e-6));
        Assert.That(body.Data.InverseMass, Is.EqualTo((Real)1.0 / expectedMass).Within((Real)1e-6));
        world.Dispose();
    }

    [TestCase]
    public void PredictPosition_UsesCurrentVelocity()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Position = new JVector(1, 2, 3);
        body.Velocity = new JVector(4, 5, 6);

        var predicted = body.PredictPosition((Real)0.5);

        Assert.That(predicted, Is.EqualTo(new JVector(3, 4.5f, 6)));
        Assert.That(body.Position, Is.EqualTo(new JVector(1, 2, 3)));
        world.Dispose();
    }

    [TestCase]
    public void PredictOrientation_UsesCurrentAngularVelocity()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Orientation = JQuaternion.Identity;
        body.AngularVelocity = new JVector(0, MathF.PI, 0);

        var predicted = body.PredictOrientation((Real)0.5);
        var expected = JQuaternion.Normalize(
            MathHelper.RotationQuaternion(body.AngularVelocity, (Real)0.5) * body.Orientation);

        Assert.That(predicted.X, Is.EqualTo(expected.X).Within(1e-6f));
        Assert.That(predicted.Y, Is.EqualTo(expected.Y).Within(1e-6f));
        Assert.That(predicted.Z, Is.EqualTo(expected.Z).Within(1e-6f));
        Assert.That(predicted.W, Is.EqualTo(expected.W).Within(1e-6f));
        Assert.That(body.Orientation, Is.EqualTo(JQuaternion.Identity));
        world.Dispose();
    }

    [TestCase]
    public void PredictPose_ReturnsPositionAndOrientationWithoutMutatingState()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.Position = new JVector(1, 2, 3);
        body.Velocity = new JVector(4, 0, 0);
        body.AngularVelocity = new JVector(0, MathF.PI, 0);

        body.PredictPose((Real)0.25, out var predictedPosition, out var predictedOrientation);

        var expectedPosition = body.PredictPosition((Real)0.25);
        var expectedOrientation = body.PredictOrientation((Real)0.25);

        Assert.That(predictedPosition, Is.EqualTo(expectedPosition));
        Assert.That(predictedOrientation.X, Is.EqualTo(expectedOrientation.X).Within(1e-6f));
        Assert.That(predictedOrientation.Y, Is.EqualTo(expectedOrientation.Y).Within(1e-6f));
        Assert.That(predictedOrientation.Z, Is.EqualTo(expectedOrientation.Z).Within(1e-6f));
        Assert.That(predictedOrientation.W, Is.EqualTo(expectedOrientation.W).Within(1e-6f));
        Assert.That(body.Position, Is.EqualTo(new JVector(1, 2, 3)));
        Assert.That(body.Velocity, Is.EqualTo(new JVector(4, 0, 0)));
        world.Dispose();
    }
}
