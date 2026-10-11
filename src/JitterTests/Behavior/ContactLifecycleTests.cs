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

        if (accelerated) contact.PrepareForIterationAccelerated(new TimeStep((Real)1.0 / 10));
        else contact.PrepareForIterationScalar(new TimeStep((Real)1.0 / 10));

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

        if (accelerated) contact.PrepareForIterationAccelerated(new TimeStep((Real)1.0 / 10));
        else contact.PrepareForIterationScalar(new TimeStep((Real)1.0 / 10));

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
            if (accelerated) contact.PrepareForIterationAccelerated(new TimeStep((Real)1.0 / 100));
            else contact.PrepareForIterationScalar(new TimeStep((Real)1.0 / 100));
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

    private static (World world, RigidBody floor) CreateFloorWorld()
    {
        var world = new World { Gravity = JVector.Zero };
        var floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(20, 1, 20));
        floor.Position = new JVector(0, -0.5f, 0);
        floor.MotionType = MotionType.Static;
        return (world, floor);
    }

    private static RigidBody AddSphereOnFloor(World world, Real x)
    {
        var sphere = world.CreateRigidBody();
        sphere.AddShape(new SphereShape(0.5f));
        sphere.Position = new JVector(x, 0.45f, 0);
        return sphere;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BeginCollide_RemovingTheBodyInTheHandler_LeavesTheWorldConsistent(bool multiThread)
    {
        var (world, floor) = CreateFloorWorld();
        using (world)
        {
            var sphere = AddSphereOnFloor(world, 0);
            sphere.BeginCollide += _ => world.Remove(sphere);

            Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
            Assert.That(sphere.IsValid, Is.False);
            Assert.That(floor.Contacts, Is.Empty);
            Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void BeginCollide_RemovingTheArbiterInEitherHandler_LeavesTheWorldConsistent(
        bool multiThread, bool inFirstHandler)
    {
        var (world, floor) = CreateFloorWorld();
        using (world)
        {
            var sphere = AddSphereOnFloor(world, 0);
            int begins = 0;
            void Handler(RigidBody self, Arbiter arbiter)
            {
                begins++;
                if ((inFirstHandler ? arbiter.Body1 : arbiter.Body2) == self) world.Remove(arbiter);
            }

            floor.BeginCollide += arbiter => Handler(floor, arbiter);
            sphere.BeginCollide += arbiter => Handler(sphere, arbiter);

            Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
            Assert.That(floor.Contacts, Is.Empty);
            Assert.That(sphere.Contacts, Is.Empty);
            Assert.That(begins, Is.EqualTo(inFirstHandler ? 1 : 2));

            Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
            Assert.DoesNotThrow(() => world.Remove(sphere));
            Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BeginCollide_RemovingBody2InBody1Handler_SkipsItsOtherQueuedCollisions(bool multiThread)
    {
        using var world = new World { Gravity = JVector.Zero };
        var first = world.CreateRigidBody();
        var second = world.CreateRigidBody();
        var third = world.CreateRigidBody();
        world.GetOrCreateArbiter(100, 101, first, second, out var current);
        world.RegisterContact(current, JVector.Zero, JVector.Zero, JVector.UnitY);
        world.GetOrCreateArbiter(200, 201, second, third, out var later);
        world.RegisterContact(later, JVector.Zero, JVector.Zero, JVector.UnitY);
        world.GetOrCreateArbiter(300, 301, first, third, out var survivor);
        world.RegisterContact(survivor, JVector.Zero, JVector.Zero, JVector.UnitY);

        int firstBegins = 0, secondBegins = 0, thirdBegins = 0;
        first.BeginCollide += arbiter =>
        {
            firstBegins++;
            if (arbiter != current) return;
            Assert.That(arbiter.Body1, Is.SameAs(first));
            Assert.That(arbiter.Body2, Is.SameAs(second));
            world.Remove(arbiter.Body2);
        };
        second.BeginCollide += _ => secondBegins++;
        third.BeginCollide += _ => thirdBegins++;

        world.Step(1f / 60f, multiThread);

        Assert.That(firstBegins, Is.EqualTo(2));
        Assert.That(secondBegins, Is.Zero);
        Assert.That(thirdBegins, Is.EqualTo(1));
        Assert.That(first.IsValid, Is.True);
        Assert.That(second.IsValid, Is.False);
        Assert.That(third.IsValid, Is.True);
        Assert.That(first.Contacts.Single(), Is.SameAs(survivor));
        Assert.That(third.Contacts.Single(), Is.SameAs(survivor));
        Assert.That(first.Connections, Does.Not.Contain(second));
        Assert.That(third.Connections, Does.Not.Contain(second));
        Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BeginCollide_RemovingOneArbiter_StillLinksAndReportsTheOthers(bool multiThread)
    {
        var (world, floor) = CreateFloorWorld();
        using (world)
        {
            var first = AddSphereOnFloor(world, -3);
            var second = AddSphereOnFloor(world, 3);
            bool removed = false;
            int survivorBegins = 0;
            floor.BeginCollide += arbiter =>
            {
                if (removed) return;
                removed = true;
                world.Remove(arbiter);
            };
            first.BeginCollide += _ => survivorBegins++;
            second.BeginCollide += _ => survivorBegins++;

            world.Step(1f / 60f, multiThread);

            RigidBody survivor = first.Contacts.Count == 1 ? first : second;
            RigidBody other = survivor == first ? second : first;
            Assert.That(survivor.Contacts, Has.Count.EqualTo(1));
            Assert.That(other.Contacts, Is.Empty);
            Assert.That(floor.Contacts, Has.Count.EqualTo(1));
            Assert.That(survivorBegins, Is.EqualTo(1));

            Assert.DoesNotThrow(() => world.Remove(survivor));
            Assert.That(floor.Contacts, Is.Empty);
            Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BeginCollide_RecreatingTheSameContact_SkipsTheRemovedNotification(bool multiThread)
    {
        using var world = new World { Gravity = JVector.Zero };
        var first = world.CreateRigidBody();
        var second = world.CreateRigidBody();
        world.GetOrCreateArbiter(100, 101, first, second, out var original);
        world.RegisterContact(original, JVector.Zero, JVector.Zero, JVector.UnitY);

        Arbiter? replacement = null;
        int firstBegins = 0, secondBegins = 0;
        first.BeginCollide += arbiter =>
        {
            firstBegins++;
            if (firstBegins != 1) return;
            world.Remove(arbiter);
            world.GetOrCreateArbiter(100, 101, first, second, out replacement);
            world.RegisterContact(replacement, JVector.Zero, JVector.Zero, JVector.UnitY);
        };
        second.BeginCollide += _ => secondBegins++;

        world.Step(1f / 60f, multiThread);

        Assert.That(firstBegins, Is.EqualTo(2));
        Assert.That(secondBegins, Is.EqualTo(1));
        Assert.That(first.Contacts.Single(), Is.SameAs(replacement));
        Assert.That(second.Contacts.Single(), Is.SameAs(replacement));

        world.Step(1f / 60f, multiThread);
        Assert.That(firstBegins, Is.EqualTo(2));
        Assert.That(secondBegins, Is.EqualTo(1));
        world.Remove(first);
        Assert.That(second.Contacts, Is.Empty);
        Assert.That(second.Connections, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BeginCollide_RecreatingALaterContact_ReportsTheReplacementOnlyOnce(bool multiThread)
    {
        using var world = new World { Gravity = JVector.Zero };
        var first = world.CreateRigidBody();
        var second = world.CreateRigidBody();
        var third = world.CreateRigidBody();
        world.GetOrCreateArbiter(100, 101, first, second, out var current);
        world.RegisterContact(current, JVector.Zero, JVector.Zero, JVector.UnitY);
        world.GetOrCreateArbiter(200, 201, first, third, out var later);
        world.RegisterContact(later, JVector.Zero, JVector.Zero, JVector.UnitY);

        Arbiter? replacement = null;
        int firstBegins = 0, secondBegins = 0, thirdBegins = 0;
        first.BeginCollide += arbiter =>
        {
            firstBegins++;
            if (arbiter != current) return;
            world.Remove(later);
            world.GetOrCreateArbiter(200, 201, first, third, out replacement);
            world.RegisterContact(replacement, JVector.Zero, JVector.Zero, JVector.UnitY);
        };
        second.BeginCollide += _ => secondBegins++;
        third.BeginCollide += _ => thirdBegins++;

        world.Step(1f / 60f, multiThread);

        Assert.That(firstBegins, Is.EqualTo(2));
        Assert.That(secondBegins, Is.EqualTo(1));
        Assert.That(thirdBegins, Is.EqualTo(1));
        Assert.That(first.Contacts, Has.Count.EqualTo(2));
        Assert.That(third.Contacts.Single(), Is.SameAs(replacement));
        world.Remove(first);
        Assert.That(second.Contacts, Is.Empty);
        Assert.That(third.Contacts, Is.Empty);
        Assert.DoesNotThrow(() => world.Step(1f / 60f, multiThread));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BeginCollide_ThrowingAfterRemoval_StillReturnsTheArbiterToThePool(bool multiThread)
    {
        using var world = new World { Gravity = JVector.Zero };
        var first = world.CreateRigidBody();
        var second = world.CreateRigidBody();
        world.GetOrCreateArbiter(100, 101, first, second, out var original);
        world.RegisterContact(original, JVector.Zero, JVector.Zero, JVector.UnitY);

        Action<Arbiter> handler = arbiter =>
        {
            world.Remove(arbiter);
            throw new InvalidOperationException("Callback failed.");
        };
        first.BeginCollide += handler;
        Assert.Throws<InvalidOperationException>(() => world.Step(1f / 60f, multiThread));
        first.BeginCollide -= handler;

        Assert.That(first.Contacts, Is.Empty);
        Assert.That(second.Contacts, Is.Empty);
        world.GetOrCreateArbiter(200, 201, first, second, out var replacement);
        Assert.That(replacement, Is.SameAs(original));
        world.RegisterContact(replacement, JVector.Zero, JVector.Zero, JVector.UnitY);

        int begins = 0;
        first.BeginCollide += _ => begins++;
        second.BeginCollide += _ => begins++;
        world.Step(1f / 60f, multiThread);
        Assert.That(begins, Is.EqualTo(2));
        Assert.That(first.Contacts.Single(), Is.SameAs(replacement));
        Assert.That(second.Contacts.Single(), Is.SameAs(replacement));
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
