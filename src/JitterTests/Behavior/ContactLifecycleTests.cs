namespace JitterTests.Behavior;

public class ContactLifecycleTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    [TestCase(false, 0.1, -0.9)]
    [TestCase(true, 0.1, -0.9)]
    [TestCase(false, 0.005, 10.0)]
    [TestCase(true, 0.005, 10.0)]
    public void SeparatedContact_UsesOriginalSpeculativeThreshold(
        bool accelerated, double gap, double target)
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(0.25f));
        body.SetMassInertia(1);
        body.Position = new JVector((Real)gap, 0, 0);
        body.Velocity = new JVector(-10, 0, 0);

        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.Friction = 0;
        contact.Restitution = 1;
        contact.AddContact(JVector.Zero, body.Position, JVector.UnitX);
        contact.ResetMode();

        Assert.That(contact.Contact0.Bias, Is.EqualTo(10));

        if (accelerated) contact.PrepareForIterationAccelerated(10);
        else contact.PrepareForIterationScalar(10);

        Assert.That(contact.Contact0.Bias, Is.EqualTo((Real)target).Within(Tolerance));
        Assert.That(contact.Contact0.PenaltyBias, Is.Zero);

        if (accelerated) contact.IterateAccelerated(true);
        else contact.IterateScalar(true);

        Assert.That(body.Velocity.X, Is.EqualTo((Real)target).Within(Tolerance));

        if (accelerated) contact.IterateAccelerated(false);
        else contact.IterateScalar(false);

        Assert.That(body.Velocity.X, Is.EqualTo((Real)target).Within(Tolerance));
    }

    [TestCase(false, 0.11, 0.5, 1.0)]
    [TestCase(true, 0.11, 0.5, 1.0)]
    [TestCase(false, 0.61, 0.5, 1.2)]
    [TestCase(true, 0.61, 0.5, 1.2)]
    [TestCase(false, 0.11, 0.0, 0.2)]
    [TestCase(true, 0.11, 0.0, 0.2)]
    public void ContactTargets_PreserveRestitutionAndRemoveOnlyPositionBias(
        bool accelerated, double penetration, double restitution, double solveTarget)
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(0.25f));
        body.SetMassInertia(1);
        body.Position = new JVector(-(Real)penetration, 0, 0);
        body.Velocity = new JVector(-2, 0, 0);

        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.Friction = 0;
        contact.Restitution = (Real)restitution;
        contact.AddContact(JVector.Zero, body.Position, JVector.UnitX);
        contact.ResetMode();

        if (accelerated) contact.PrepareForIterationAccelerated(10);
        else contact.PrepareForIterationScalar(10);

        Assert.That(contact.Contact0.PenaltyBias, Is.GreaterThan(0));

        if (accelerated) contact.IterateAccelerated(true);
        else contact.IterateScalar(true);

        Assert.That(body.Velocity.X, Is.EqualTo((Real)solveTarget).Within(Tolerance));

        if (accelerated) contact.IterateAccelerated(false);
        else contact.IterateScalar(false);

        Assert.That(body.Velocity.X, Is.EqualTo((Real)(2 * restitution)).Within(Tolerance));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SkipWarmStart_AffectsOnlyTheNextPreparation(bool accelerated)
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(0.25f));
        body.SetMassInertia(1);
        body.Position = new JVector(-2, 0, 0);
        body.Velocity = new JVector(400, 0, 0);

        ContactData contact = default;
        contact.Init(body, world.NullBody);
        contact.Friction = contact.Restitution = 0;
        contact.AddContact(new JVector(-1.75f, 0, 0), new JVector(-0.05f, 0, 0), JVector.UnitX);
        contact.ResetMode();

        void Prepare()
        {
            if (accelerated) contact.PrepareForIterationAccelerated(100);
            else contact.PrepareForIterationScalar(100);
        }

        Prepare();
        contact.Iterate(false);
        Assert.That(contact.Contact0.Impulse, Is.GreaterThan(0));
        JVector velocity = body.Velocity;
        Real impulse = contact.Contact0.Impulse;

        contact.SkipWarmStart();
        Prepare();

        Assert.That(body.Velocity, Is.EqualTo(velocity));
        Assert.That(contact.Contact0.Impulse, Is.EqualTo(impulse));
        Assert.That(contact.Contact0.Flag & ContactData.Contact.Flags.SkipWarmStart,
            Is.EqualTo((ContactData.Contact.Flags)0));

        Prepare();
        Assert.That(body.Velocity, Is.Not.EqualTo(velocity));
    }

    [TestCase]
    public void BeginCollide_FiresOnce_WhenBodiesStartTouching()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        int beginA = 0, beginB = 0;
        bodyA.BeginCollide += _ => beginA++;
        bodyB.BeginCollide += _ => beginB++;

        world.Step(1f / 60f, false);

        Assert.That(beginA, Is.EqualTo(1));
        Assert.That(beginB, Is.EqualTo(1));
        Assert.That(bodyA.Contacts, Has.Count.EqualTo(1));
        Assert.That(bodyB.Contacts, Has.Count.EqualTo(1));
        Assert.That(bodyA.Connections, Does.Contain(bodyB));
        Assert.That(bodyB.Connections, Does.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void EndCollide_FiresOnce_WhenBodiesSeparate()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        int endA = 0, endB = 0;
        bodyA.EndCollide += _ => endA++;
        bodyB.EndCollide += _ => endB++;

        world.Step(1f / 60f, false);
        bodyB.Position = new JVector(5, 0, 0);
        world.Step(1f / 60f, false);
        world.Step(1f / 60f, false);

        Assert.That(endA, Is.EqualTo(1));
        Assert.That(endB, Is.EqualTo(1));
        Assert.That(bodyA.Contacts, Is.Empty);
        Assert.That(bodyB.Contacts, Is.Empty);
        Assert.That(bodyA.Connections, Does.Not.Contain(bodyB));
        Assert.That(bodyB.Connections, Does.Not.Contain(bodyA));
        world.Dispose();
    }

    [TestCase]
    public void RemovingBody_InContact_CleansOtherBodyContactsAndConnections()
    {
        var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        world.Step(1f / 60f, false);
        Assert.That(bodyA.Contacts, Has.Count.EqualTo(1));

        world.Remove(bodyB);

        Assert.That(bodyA.Contacts, Is.Empty);
        Assert.That(bodyA.Connections, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void MovingBody_ClearsCachedContactState()
    {
        using var world = new World();
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(1.5f, 0, 0);

        world.Step(1f / 60f, false);

        Arbiter arbiter = bodyA.Contacts.Single();
        Assert.That(arbiter.Handle.Data.UsageMask & ContactData.MaskContactAll, Is.Not.EqualTo(0u));

        bodyB.Position = new JVector(1.4f, 0, 0);

        Assert.That(arbiter.Handle.Data.UsageMask & ContactData.MaskContactAll, Is.EqualTo(0u));

        world.Step(1f / 60f, false);

        Assert.That(bodyA.Contacts, Has.Count.EqualTo(1));
        Assert.That(arbiter.Handle.Data.UsageMask & ContactData.MaskContactAll, Is.Not.EqualTo(0u));
    }
}
