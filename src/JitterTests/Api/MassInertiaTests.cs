namespace JitterTests.Api;

public class MassInertiaTests
{
    // -------------------------------------------------------------------------
    // SetMassInertia() — auto-compute from shapes
    // -------------------------------------------------------------------------

    [TestCase]
    public void SetMassInertia_NoShapes_SetsUnitMassAndIdentityInertia()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.SetMassInertia();
        Assert.That(body.Mass, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        Assert.That(body.InverseInertia.M11, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        Assert.That(body.InverseInertia.M22, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        Assert.That(body.InverseInertia.M33, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_WithShape_MatchesMassFromAddShape()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var massFromAddShape = body.Mass;
        body.SetMassInertia();
        Assert.That(body.Mass, Is.EqualTo(massFromAddShape).Within((Real)1e-5));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // SetMassInertia(Real mass) — scale inertia to a specific mass
    // -------------------------------------------------------------------------

    [TestCase]
    public void SetMassInertia_SpecificMass_SetsMassCorrectly()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.SetMassInertia(5f);
        Assert.That(body.Mass, Is.EqualTo((Real)5.0).Within((Real)1e-5));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // SetMassInertia(JSymmetricMatrix, Real, bool) — fully manual
    // -------------------------------------------------------------------------

    [TestCase]
    public void SetMassInertia_Manual_SetsMassAndInertia()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var inertia = JSymmetricMatrix.Identity * 2f;
        body.SetMassInertia(inertia, 10f);
        Assert.That(body.Mass, Is.EqualTo((Real)10.0).Within((Real)1e-5));
        // InverseInertia should be inverse of 2*I = 0.5*I
        Assert.That(body.InverseInertia.M11, Is.EqualTo((Real)0.5).Within((Real)1e-5));
        Assert.That(body.InverseInertia.M22, Is.EqualTo((Real)0.5).Within((Real)1e-5));
        Assert.That(body.InverseInertia.M33, Is.EqualTo((Real)0.5).Within((Real)1e-5));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_ManualInverse_SetsMassAndInertiaDirectly()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var inverseInertia = JSymmetricMatrix.Identity * 4f;
        // inverseMass = 0.1 → mass = 10
        body.SetMassInertia(inverseInertia, 0.1f, setAsInverse: true);
        Assert.That(body.Mass, Is.EqualTo((Real)10.0).Within((Real)1e-4));
        Assert.That(body.InverseInertia.M11, Is.EqualTo((Real)4.0).Within((Real)1e-5));
        world.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SetMassInertia_CoupledTensor_MatchesFullMatrixResponse(bool setAsInverse)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JSymmetricMatrix inertia = new(4, 1, 2, 5, 1, 6);
        JMatrix expected = inertia.ToMatrix();
        if (!setAsInverse)
        {
            Assert.That(JMatrix.Inverse(expected, out expected), Is.True);
        }

        body.SetMassInertia(inertia, 1, setAsInverse);
        JMatrix actual = body.InverseInertia.ToMatrix();
        for (int i = 0; i < 3; i++)
        {
            Assert.That(JVector.MaxAbs(actual.GetColumn(i) - expected.GetColumn(i)), Is.LessThan((Real)1e-6));
        }

        body.Orientation = JQuaternion.CreateRotationY((Real)0.3) *
                           JQuaternion.CreateRotationX((Real)0.5) *
                           JQuaternion.CreateRotationZ((Real)(-0.2));
        JMatrix rotation = JMatrix.CreateFromQuaternion(body.Orientation);
        JMatrix expectedWorld = rotation * expected * JMatrix.Transpose(rotation);
        JVector impulse = new(2, -1, 1);
        JVector position = new(1, 2, 3);
        body.ApplyImpulse(impulse, position);
        JVector expectedVelocity = JVector.Transform(position % impulse, expectedWorld);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - expectedVelocity), Is.LessThan((Real)1e-6));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SetMassInertia_NonFiniteTensor_ThrowsWithoutChangingMassProperties(bool setAsInverse)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JSymmetricMatrix original = body.InverseInertia;
        foreach (Real nonFinite in new[] { Real.NaN, Real.PositiveInfinity, Real.NegativeInfinity })
        {
            for (int i = 0; i < 6; i++)
            {
                JSymmetricMatrix inertia = JSymmetricMatrix.Identity;
                switch (i)
                {
                    case 0: inertia.M11 = nonFinite; break;
                    case 1: inertia.M12 = nonFinite; break;
                    case 2: inertia.M13 = nonFinite; break;
                    case 3: inertia.M22 = nonFinite; break;
                    case 4: inertia.M23 = nonFinite; break;
                    case 5: inertia.M33 = nonFinite; break;
                }

                Assert.Throws<ArgumentException>(() => body.SetMassInertia(inertia, 2, setAsInverse));
                Assert.That(body.InverseInertia, Is.EqualTo(original));
                Assert.That(body.Mass, Is.EqualTo((Real)1));
            }
        }
    }

    [TestCase]
    public void SetMassInertia_PreservedAcrossMotionTypeChanges()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.SetMassInertia(7f);
        var mass = body.Mass;

        body.MotionType = MotionType.Static;
        Assert.That(body.Mass, Is.EqualTo(mass).Within((Real)1e-5));

        body.MotionType = MotionType.Kinematic;
        Assert.That(body.Mass, Is.EqualTo(mass).Within((Real)1e-5));

        body.MotionType = MotionType.Dynamic;
        Assert.That(body.Mass, Is.EqualTo(mass).Within((Real)1e-5));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Throws
    // -------------------------------------------------------------------------

    [TestCase]
    public void SetMassInertia_WithMass_Zero_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentException>(() => body.SetMassInertia((Real)0.0));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_WithMass_Negative_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentException>(() => body.SetMassInertia((Real)(-1.0)));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_WithSingularMatrix_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Real originalMass = body.Mass;
        JSymmetricMatrix originalInertia = body.InverseInertia;
        Assert.Throws<ArgumentException>(() => body.SetMassInertia(JSymmetricMatrix.Zero, (Real)1.0));
        Assert.That(body.Mass, Is.EqualTo(originalMass));
        Assert.That(body.InverseInertia, Is.EqualTo(originalInertia));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_AsInverse_WithNegativeInverseMass_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentException>(() => body.SetMassInertia(JSymmetricMatrix.Identity, (Real)(-1.0), true));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_AsInverse_WithInfiniteInverseMass_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentException>(() => body.SetMassInertia(JSymmetricMatrix.Identity, Real.PositiveInfinity, true));
        world.Dispose();
    }
}
