namespace JitterTests.Behavior;

[TestFixture]
public class AllowedMotionRegressionTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    [Test]
    public void AngularLocks_PreserveSingularInverseInertia()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.CreateScale(1, 0, 1), 1, true);
        body.Orientation = JQuaternion.CreateRotationZ(MathR.PI / 4);
        body.AllowedMotion = MotionAxes.Linear | MotionAxes.AngularY | MotionAxes.AngularZ;
        body.ApplyImpulse(new JVector(1, 0, -1), JVector.UnitY);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - new JVector(0, 0, -1)), Is.LessThan(Tolerance));
    }
}
