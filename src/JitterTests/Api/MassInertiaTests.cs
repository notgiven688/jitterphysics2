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
    // SetMassInertia(JMatrix, Real, bool) — fully manual
    // -------------------------------------------------------------------------

    [TestCase]
    public void SetMassInertia_Manual_SetsMassAndInertia()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var inertia = JMatrix.Identity * 2f;
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
        var inverseInertia = JMatrix.Identity * 4f;
        // inverseMass = 0.1 → mass = 10
        body.SetMassInertia(inverseInertia, 0.1f, setAsInverse: true);
        Assert.That(body.Mass, Is.EqualTo((Real)10.0).Within((Real)1e-4));
        Assert.That(body.InverseInertia.M11, Is.EqualTo((Real)4.0).Within((Real)1e-5));
        world.Dispose();
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
        Assert.Throws<ArgumentException>(() => body.SetMassInertia(JMatrix.Zero, (Real)1.0));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_AsInverse_WithNegativeInverseMass_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentException>(() => body.SetMassInertia(JMatrix.Identity, (Real)(-1.0), true));
        world.Dispose();
    }

    [TestCase]
    public void SetMassInertia_AsInverse_WithInfiniteInverseMass_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentException>(() => body.SetMassInertia(JMatrix.Identity, Real.PositiveInfinity, true));
        world.Dispose();
    }
}
