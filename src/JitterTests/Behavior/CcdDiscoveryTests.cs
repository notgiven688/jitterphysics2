namespace JitterTests.Behavior;

public class CcdDiscoveryTests
{
    private static RigidBody Sphere(World world, Real x, Real speed = 0, Real radius = (Real)0.25)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape(radius));
        body.SetMassInertia(1);
        body.Position = new JVector(x, 0, 0);
        body.Velocity = new JVector(speed, 0, 0);
        body.Friction = body.Restitution = 0;
        body.Damping = (0, 0);
        return body;
    }

    private static bool HasContact(World world, RigidBody a, RigidBody b)
    {
        ulong idA = a.Shapes[0].ShapeId, idB = b.Shapes[0].ShapeId;
        if (idB < idA) (idA, idB) = (idB, idA);
        return world.GetArbiter(idA, idB, out _);
    }

    [TestCase(1, SolveMode.Regular, false)]
    [TestCase(4, SolveMode.Regular, false)]
    [TestCase(4, SolveMode.Regular, true)]
    [TestCase(1, SolveMode.Deterministic, false)]
    [TestCase(4, SolveMode.Deterministic, false)]
    [TestCase(4, SolveMode.Deterministic, true)]
    public void FollowsMotionCreatedByContact(int passes, SolveMode mode, bool multiThread)
    {
        using var world = new World
        {
            Gravity = JVector.Zero,
            CcdVelocityThreshold = 0,
            CcdIterations = passes,
            SolveMode = mode
        };
        RigidBody a = Sphere(world, 0, 100);
        RigidBody b = Sphere(world, 1);
        RigidBody c = Sphere(world, 2);
        a.EnableCcd = true;

        bool checkedDiscovery = false;
        world.PreSubStep += _ =>
        {
            Assert.That(HasContact(world, a, b), Is.True);
            Assert.That(HasContact(world, b, c), Is.EqualTo(passes > 1));
            Assert.That(a.Position, Is.EqualTo(JVector.Zero));
            Assert.That(b.Position, Is.EqualTo(JVector.UnitX));
            Assert.That(c.Position, Is.EqualTo(2 * JVector.UnitX));
            Assert.That(c.Velocity.X, passes > 1 ? Is.GreaterThan(1) : Is.EqualTo(0));
            foreach (RigidBody body in new[] { b, c })
            {
                RigidBodyShape shape = body.Shapes[0];
                shape.CalculateBoundingBox(body.Orientation, body.Position, out JBoundingBox tight);
                Assert.That(shape.WorldBoundingBox, Is.EqualTo(tight));
            }
            checkedDiscovery = true;
        };
        world.Step((Real)0.03, multiThread);

        Assert.That(checkedDiscovery, Is.True);
        Assert.That(b.EnableCcd, Is.False);
        Assert.That(c.EnableCcd, Is.False);
        Assert.That(a.Velocity.X + b.Velocity.X + c.Velocity.X, Is.EqualTo(100).Within((Real)1e-4));
        Assert.That(a.Velocity.LengthSquared() + b.Velocity.LengthSquared() + c.Velocity.LengthSquared(),
            Is.LessThanOrEqualTo((Real)10001));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void DiscoversImpactCausedOnlyByRotation(bool multiThread, bool ccdOnRotor)
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody wall = world.CreateRigidBody();
        wall.AddShape(new BoxShape((Real)0.1, 10, 10));
        wall.Position = new JVector((Real)1.6, 0, 0);
        wall.MotionType = MotionType.Static;
        wall.Friction = wall.Restitution = 0;

        RigidBody rotor = world.CreateRigidBody();
        rotor.AddShape(new BoxShape(4, (Real)0.2, (Real)0.2));
        rotor.Orientation = JQuaternion.CreateRotationZ(MathR.PI / 2);
        rotor.AngularVelocity = new JVector(0, 0, -150);
        rotor.Damping = (0, 0);
        rotor.Friction = rotor.Restitution = 0;
        if (ccdOnRotor) rotor.EnableCcd = true;
        else wall.EnableCcd = true;

        bool discovered = false;
        world.PreSubStep += _ =>
        {
            Assert.That(HasContact(world, wall, rotor), Is.EqualTo(ccdOnRotor));
            Assert.That(rotor.AngularVelocity.Z, ccdOnRotor ? Is.GreaterThan(-149) : Is.EqualTo(-150));
            Assert.That(rotor.Position, Is.EqualTo(JVector.Zero));
            discovered = true;
        };
        world.Step((Real)0.01, multiThread);
        Assert.That(discovered, Is.True);
    }

    [TestCase(1, false)]
    [TestCase(4, false)]
    [TestCase(1, true)]
    [TestCase(4, true)]
    public void FollowsRotationCreatedByAnOffCenterImpact(int passes, bool sleepingPanel)
    {
        using var world = new World
        {
            Gravity = JVector.Zero,
            CcdVelocityThreshold = 0,
            CcdIterations = passes
        };
        RigidBody panel = world.CreateRigidBody();
        panel.AddShape(new BoxShape((Real)0.04, (Real)2.16, (Real)0.1));
        panel.SetMassInertia(JSymmetricMatrix.CreateScale((Real)0.039, (Real)0.039, (Real)0.039), (Real)0.1);
        panel.AllowedMotion = MotionAxes.AngularZ;
        panel.Friction = panel.Restitution = 0;
        panel.Damping = (0, 0);
        if (sleepingPanel) world.ForceSleepIsland(panel.Island);

        RigidBody incoming = Sphere(world, (Real)(-0.3), (Real)10.8, (Real)0.14);
        incoming.Position = new JVector((Real)(-0.3), (Real)0.7, 0);
        incoming.EnableCcd = true;

        RigidBody resting = Sphere(world, (Real)(-0.18), 0, (Real)0.14);
        resting.Position = new JVector((Real)(-0.18), (Real)(-0.81), 0);

        bool checkedDiscovery = false;
        world.PreSubStep += _ =>
        {
            Assert.That(HasContact(world, panel, incoming), Is.True);
            Assert.That(HasContact(world, panel, resting), Is.EqualTo(passes > 1));
            Assert.That(panel.Position, Is.EqualTo(JVector.Zero));
            Assert.That(panel.Orientation, Is.EqualTo(JQuaternion.Identity));
            if (passes > 1) Assert.That(resting.Velocity.Length(), Is.GreaterThan((Real)0.1));
            checkedDiscovery = true;
        };
        world.Step((Real)1 / 30, false);
        Assert.That(checkedDiscovery, Is.True);
    }

    [TestCase(1, true)]
    [TestCase(4, true)]
    [TestCase(4, false)]
    public void AddsAnotherCornerAgainstTheSameWall(int passes, bool persistent)
    {
        using var world = new World
        {
            Gravity = JVector.Zero,
            CcdIterations = passes,
            PersistentContactManifold = persistent
        };
        RigidBody wall = world.CreateRigidBody();
        wall.AddShape(new BoxShape((Real)0.2, 10, 10));
        wall.Position = new JVector((Real)(-0.1), 0, 0);
        wall.MotionType = MotionType.Static;
        wall.Friction = wall.Restitution = 0;
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1, 1, (Real)0.5));
        body.SetMassInertia(1);
        body.Position = JVector.UnitX;
        body.Orientation = JQuaternion.CreateRotationZ((Real)0.3);
        body.Velocity = new JVector(-200, 0, 0);
        body.Damping = (0, 0);
        body.Friction = body.Restitution = 0;
        body.EnableCcd = true;

        int points = 0;
        Real minY = Real.PositiveInfinity, maxY = Real.NegativeInfinity;
        world.PreSubStep += _ =>
        {
            Assert.That(world.GetArbiter(wall.Shapes[0].ShapeId, body.Shapes[0].ShapeId, out var arbiter), Is.True);
            ContactData data = arbiter!.Handle.Data;
            points = System.Numerics.BitOperations.PopCount((uint)(data.UsageMask & ContactData.MaskContactAll));
            ContactData.Contact[] contacts = [data.Contact0, data.Contact1, data.Contact2, data.Contact3];
            for (int i = 0; i < contacts.Length; i++)
            {
                if ((data.UsageMask & (1 << i)) == 0) continue;
                minY = MathR.Min(minY, contacts[i].Position2.Y);
                maxY = MathR.Max(maxY, contacts[i].Position2.Y);
            }
        };
        world.Step((Real)0.01, false);
        Assert.That(points, passes > 1 ? Is.GreaterThanOrEqualTo(2) : Is.EqualTo(1));
        if (passes > 1)
        {
            Assert.That(minY, Is.LessThan((Real)(-0.4)), "Find the opposite corner, not another sample near the first.");
            Assert.That(maxY, Is.GreaterThan((Real)0.4));
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void DiscoveryWakesSleepingRecipients(bool multiThread, bool enableRecipients)
    {
        using var world = new World { Gravity = JVector.Zero, CcdVelocityThreshold = 0 };
        RigidBody a = Sphere(world, 0, 100);
        RigidBody b = Sphere(world, 1);
        RigidBody c = Sphere(world, 2);
        world.ForceSleepIsland(b.Island);
        world.ForceSleepIsland(c.Island);
        b.EnableCcd = c.EnableCcd = enableRecipients;
        a.EnableCcd = true;

        world.Step((Real)0.03, multiThread);

        Assert.That(b.IsActive, Is.True);
        Assert.That(c.IsActive, Is.True);
        Assert.That(c.Position.X, Is.GreaterThan(2));
    }

    private sealed class CountingBox() : BoxShape(1)
    {
        public int BoundsCalls;

        public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position,
            out JBoundingBox box)
        {
            BoundsCalls++;
            base.CalculateBoundingBox(orientation, position, out box);
        }
    }

    [TestCase(false, 0)]
    [TestCase(false, 0.1)]
    [TestCase(true, 0)]
    [TestCase(true, 0.1)]
    public void SleepingCcdBodiesSkipDiscoveryGeometry(bool multiThread, double threshold)
    {
        using var world = new World { Gravity = JVector.Zero, CcdVelocityThreshold = (Real)threshold };
        RigidBody a = world.CreateRigidBody(), b = world.CreateRigidBody();
        var shapeA = new CountingBox();
        var shapeB = new CountingBox();
        a.AddShape(shapeA);
        b.AddShape(shapeB);
        b.Position = JVector.UnitX;
        a.EnableCcd = b.EnableCcd = true;
        world.Step((Real)0.01, multiThread);
        world.ForceSleepIsland(a.Island);
        world.ForceSleepIsland(b.Island);
        shapeA.BoundsCalls = shapeB.BoundsCalls = 0;

        for (int i = 0; i < 3; i++) world.Step((Real)0.01, multiThread);

        Assert.That(shapeA.BoundsCalls + shapeB.BoundsCalls, Is.Zero);
        Assert.That(a.IsActive || b.IsActive, Is.False);
        Assert.That(a.EnableCcd && b.EnableCcd, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IncomingBodyNeedsCcdForItsMotionBounds(bool enableIncoming)
    {
        using var world = new World { Gravity = JVector.Zero, CcdVelocityThreshold = 0 };
        RigidBody a = Sphere(world, 0);
        RigidBody b = Sphere(world, 4, -100);
        a.EnableCcd = true;
        b.EnableCcd = enableIncoming;
        world.Step((Real)0.02, false);
        Assert.That(HasContact(world, a, b), Is.False);

        bool discovered = false;
        world.PreSubStep += _ => discovered = HasContact(world, a, b);
        world.Step((Real)0.02, false);
        Assert.That(discovered, Is.EqualTo(enableIncoming));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void CrossingProjectilesNeedCcdOnBothBodies(bool enableA, bool enableB)
    {
        using var world = new World { Gravity = JVector.Zero, CcdVelocityThreshold = 0 };
        world.Step((Real)0.01, false);
        RigidBody a = Sphere(world, -2, 200);
        RigidBody b = Sphere(world, 2, -200);
        a.EnableCcd = enableA;
        b.EnableCcd = enableB;

        bool discovered = false;
        world.PreSubStep += _ => discovered = HasContact(world, a, b);
        world.Step((Real)0.01, false);

        Assert.That(discovered, Is.EqualTo(enableA && enableB));
    }

    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(true, true, true)]
    public void IncomingBodyVelocityChangeRefreshesBounds(bool useImpulse, bool changeInPreStep, bool enableIncoming)
    {
        using var world = new World { Gravity = JVector.Zero, CcdVelocityThreshold = 0 };
        RigidBody a = Sphere(world, 0);
        RigidBody b = Sphere(world, 2);
        a.EnableCcd = true;
        b.EnableCcd = enableIncoming;
        world.Step((Real)0.01, false);

        void Launch(TimeStep _)
        {
            if (useImpulse) b.ApplyImpulse(new JVector(-200, 0, 0));
            else b.Velocity = new JVector(-200, 0, 0);

            List<IDynamicTreeProxy> hits = [];
            world.DynamicTree.Query(hits, a.Shapes[0].WorldBoundingBox);
            Assert.That(hits.Contains(b.Shapes[0]), Is.EqualTo(enableIncoming));
        }

        if (changeInPreStep) world.PreStep += Launch;
        else Launch(default);

        bool discovered = false;
        world.PreSubStep += _ => discovered = HasContact(world, a, b);
        world.Step((Real)0.01, false);
        Assert.That(discovered, Is.EqualTo(enableIncoming));
        Assert.That(b.EnableCcd, Is.EqualTo(enableIncoming));
    }

    [TestCase(1)]
    [TestCase(4)]
    [TestCase(8)]
    public void DoesNotReplayDiscoveryImpulse(int substeps)
    {
        using var world = new World { Gravity = JVector.Zero, SubstepCount = substeps };
        RigidBody wall = Sphere(world, 0);
        wall.MotionType = MotionType.Static;
        RigidBody body = Sphere(world, 1, -100);
        body.EnableCcd = true;

        int substep = 0;
        world.PreSubStep += _ =>
        {
            if (substep != 0) return;
            Assert.That(body.Velocity.X, Is.EqualTo(-45).Within((Real)1e-4));
        };
        world.PostSubStep += _ =>
        {
            if (substep++ != 0) return;
            Assert.That(body.Velocity.X, Is.EqualTo(-45).Within((Real)1e-4));
            world.GetArbiter(wall.Shapes[0].ShapeId, body.Shapes[0].ShapeId, out var arbiter);
            Assert.That(arbiter!.Handle.Data.Contact0.Impulse, Is.EqualTo(55).Within((Real)1e-4));
        };
        world.Step((Real)0.01, false);

        Assert.That(body.Position.X, Is.GreaterThanOrEqualTo((Real)0.5));
        Assert.That(body.Velocity.X, Is.LessThanOrEqualTo(0));
    }

    [TestCase(1, false)]
    [TestCase(1, true)]
    [TestCase(4, false)]
    [TestCase(4, true)]
    public void ColoredBundles_DoNotReplayDiscoveryImpulses(int substeps, bool multiThread)
    {
        using var world = new World
        {
            SolveMode = SolveMode.Regular,
            Gravity = JVector.Zero,
            SubstepCount = substeps,
            AllowDeactivation = false
        };
        // Independent pairs share one color. This count exercises full SIMD bundles,
        // a scalar tail, and more than one parallel contact batch.
        int pairCount = 128 + ContactSolverWide.Width + 1;
        var pairs = new List<(RigidBody Wall, RigidBody Body)>(pairCount);
        for (int row = 0; row < pairCount; row++)
        {
            RigidBody wall = Sphere(world, 0);
            wall.MotionType = MotionType.Static;
            wall.Position = new JVector(0, 0, row * 4);
            RigidBody body = Sphere(world, 1, -100);
            body.Position = new JVector(1, 0, row * 4);
            body.EnableCcd = true;
            pairs.Add((wall, body));
        }

        int substep = 0;
        world.PreSubStep += _ =>
        {
            if (substep != 0) return;
            Assert.That(world.RawData.ActiveContacts.Length, Is.EqualTo(pairCount));
            foreach (var (wall, body) in pairs)
            {
                Assert.That(body.Velocity.X, Is.EqualTo(-45).Within((Real)1e-4));
                Assert.That(world.GetArbiter(wall.Shapes[0].ShapeId, body.Shapes[0].ShapeId,
                    out var arbiter), Is.True);
                Assert.That((arbiter!.Handle.Data.Contact0.Flag & ContactData.Contact.Flags.SkipWarmStart) != 0,
                    Is.True, "Discovery impulses must be marked as already applied.");
            }
        };
        world.PostSubStep += _ =>
        {
            if (substep++ != 0) return;
            foreach (var (wall, body) in pairs)
            {
                Assert.That(body.Velocity.X, Is.EqualTo(-45).Within((Real)1e-4));
                Assert.That(world.GetArbiter(wall.Shapes[0].ShapeId, body.Shapes[0].ShapeId,
                    out var arbiter), Is.True);
                Assert.That(arbiter!.Handle.Data.Contact0.Impulse, Is.EqualTo(55).Within((Real)1e-4));
                Assert.That((arbiter.Handle.Data.Contact0.Flag & ContactData.Contact.Flags.SkipWarmStart) != 0,
                    Is.False, "Colored preparation must consume the one-shot skip flag.");
            }
        };

        world.Step((Real)0.01, multiThread);

        Assert.That(substep, Is.EqualTo(substeps));
        foreach (var (_, body) in pairs)
        {
            Assert.That(body.Position.X, Is.GreaterThanOrEqualTo((Real)0.5));
            Assert.That(body.Velocity.X, Is.LessThanOrEqualTo(0));
        }
    }

    private sealed class RejectContact : INarrowPhaseFilter
    {
        public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB, ref JVector pointA,
            ref JVector pointB, ref JVector normal, ref Real penetration) => false;
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void RespectsCollisionFilters(int filter)
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody a = Sphere(world, 0, 100);
        RigidBody b = Sphere(world, 1);
        a.EnableCcd = true;
        if (filter == 0) world.DynamicTree.Filter = static (_, _) => false;
        if (filter == 1) world.BroadPhaseFilter = new RejectPair();
        if (filter == 2) world.NarrowPhaseFilter = new RejectContact();

        world.Step((Real)0.01, false);

        Assert.That(HasContact(world, a, b), Is.False);
        Assert.That(a.Velocity.X, Is.EqualTo(100));
        Assert.That(b.Velocity, Is.EqualTo(JVector.Zero));
    }

    private sealed class RejectPair : IBroadPhaseFilter
    {
        public bool Filter(IDynamicTreeProxy a, IDynamicTreeProxy b) => false;
    }

    [Test]
    public void DiscoveryIsOptIn()
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody a = Sphere(world, 0, 200);
        RigidBody b = Sphere(world, 1);
        world.Step((Real)0.01, false);
        Assert.That(HasContact(world, a, b), Is.False);
        Assert.That(a.Position.X, Is.GreaterThan(b.Position.X));
        Assert.That(a.Velocity.X, Is.EqualTo(200));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void TogglingCcdUpdatesParticipation(bool enabled, bool inPreStep)
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody body = Sphere(world, 0, 100);
        RigidBody wall = Sphere(world, 1);
        wall.MotionType = MotionType.Static;

        void Toggle(TimeStep _)
        {
            body.EnableCcd = true;
            body.EnableCcd = true;
            body.EnableCcd = false;
            body.EnableCcd = false;
            body.EnableCcd = enabled;
        }

        if (inPreStep) world.PreStep += Toggle;
        else Toggle(default);

        bool checkedDiscovery = false;
        world.PreSubStep += _ =>
        {
            Assert.That(HasContact(world, body, wall), Is.EqualTo(enabled));
            checkedDiscovery = true;
        };
        world.Step((Real)0.02, false);

        Assert.That(checkedDiscovery, Is.True);
        Assert.That(body.Position.X, enabled ? Is.LessThan(1) : Is.GreaterThan(1));
    }

    [Test]
    public void EnabledBodyRemainsRegisteredAcrossSteps()
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody body = Sphere(world, 0, 100);
        RigidBody wall = Sphere(world, 1);
        wall.MotionType = MotionType.Static;
        body.EnableCcd = true;

        world.PreSubStep += _ => Assert.That(HasContact(world, body, wall), Is.True);
        for (int i = 0; i < 3; i++)
        {
            foreach (Arbiter arbiter in body.Contacts.ToArray()) world.Remove(arbiter);
            body.Position = JVector.Zero;
            body.Velocity = new JVector(100, 0, 0);
            world.Step((Real)0.01, false);
            Assert.That(body.Position.X, Is.LessThan(1));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RemovedBodiesDoNotRemainRegistered(bool clearWorld)
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody body = Sphere(world, 0, 100);
        body.EnableCcd = true;
        if (clearWorld) world.Clear();
        else world.Remove(body);

        body.EnableCcd = false;
        body.EnableCcd = true;
        Assert.DoesNotThrow(() => world.Step((Real)0.01, false));
    }

    [Test]
    public void ImpulseRecipientsAreNotRegisteredForTheNextStep()
    {
        using var world = new World { Gravity = JVector.Zero, CcdVelocityThreshold = 0 };
        RigidBody source = Sphere(world, 0, 100);
        RigidBody recipient = Sphere(world, 1);
        source.EnableCcd = true;
        world.Step((Real)0.03, false);
        Assert.That(recipient.Velocity.X, Is.GreaterThan(1));

        foreach (Arbiter arbiter in source.Contacts.ToArray()) world.Remove(arbiter);
        source.EnableCcd = false;
        source.Position = new JVector(-100, 0, 0);
        source.Velocity = JVector.Zero;
        recipient.Position = JVector.Zero;
        recipient.Velocity = new JVector(100, 0, 0);
        RigidBody wall = Sphere(world, 1);
        wall.MotionType = MotionType.Static;

        world.PreSubStep += _ => Assert.That(HasContact(world, recipient, wall), Is.False);
        world.Step((Real)0.03, false);
        Assert.That(recipient.Position.X, Is.GreaterThan(1));
    }

    [TestCase(0, false)]
    [TestCase(0.05, false)]
    [TestCase(0.2, true)]
    public void DefaultVelocityThresholdSkipsNearlyStationaryPairs(double speed, bool expectContact)
    {
        using var world = new World { Gravity = JVector.Zero };
        RigidBody a = Sphere(world, 0, (Real)speed);
        RigidBody b = Sphere(world, (Real)0.501);
        b.MotionType = MotionType.Static;
        a.EnableCcd = true;

        world.PreSubStep += _ => Assert.That(HasContact(world, a, b), Is.EqualTo(expectContact));
        world.Step((Real)0.03, false);
    }

    [Test]
    public void VelocityThresholdCanSkipSlowPairs()
    {
        using var world = new World { Gravity = JVector.Zero, CcdVelocityThreshold = 100000 };
        RigidBody a = Sphere(world, 0, 100);
        RigidBody b = Sphere(world, 1);
        a.EnableCcd = true;
        world.Step((Real)0.01, false);
        Assert.That(HasContact(world, a, b), Is.False);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void RequiresAtLeastOnePass(int passes)
    {
        using var world = new World();
        Assert.Throws<ArgumentOutOfRangeException>(() => world.CcdIterations = passes);
    }

    [Test]
    public void DeterministicDiscoveryMatchesAcrossThreadingModes()
    {
        static JVector[] Run(bool multiThread)
        {
            using var world = new World
            {
                Gravity = JVector.Zero,
                SolveMode = SolveMode.Deterministic,
                CcdVelocityThreshold = 0
            };
            RigidBody[] chain = [Sphere(world, 0, 100), Sphere(world, 1), Sphere(world, 2), Sphere(world, 3)];
            chain[0].EnableCcd = true;
            for (int i = 0; i < 10; i++) world.Step((Real)0.03, multiThread);
            return chain.SelectMany(body => new[] { body.Position, body.Velocity, body.AngularVelocity }).ToArray();
        }

        Assert.That(Run(true), Is.EqualTo(Run(false)));
    }

    [TestCase(false, 1)]
    [TestCase(true, 1)]
    [TestCase(false, 4)]
    [TestCase(true, 4)]
    public void ParallelDiscoveryPreservesSelectionAcrossSharedBodies(bool persistent, int passes)
    {
        JVector[] Run(bool multiThread)
        {
            using var world = new World
            {
                Gravity = JVector.Zero,
                AllowDeactivation = false,
                SolveMode = SolveMode.Deterministic,
                CcdVelocityThreshold = 0,
                CcdIterations = passes,
                PersistentContactManifold = persistent
            };
            world.Step((Real)0.02, false);
            var bodies = new List<RigidBody>();
            for (int row = 0; row < 384; row++)
            {
                RigidBody a = Sphere(world, 0, 100);
                RigidBody b = Sphere(world, 1);
                RigidBody c = Sphere(world, 2, passes == 1 ? -100 : 0);
                foreach (RigidBody body in new[] { a, b, c })
                {
                    body.Position += new JVector(0, 0, row * 8);
                    body.EnableCcd = passes == 1 || body == a;
                    if (!body.EnableCcd) world.ForceSleepIsland(body.Island);
                    bodies.Add(body);
                }
            }

            var states = new List<JVector>();
            bool firstStep = true;
            world.PreSubStep += _ =>
            {
                if (firstStep)
                {
                    for (int i = 0; i < bodies.Count; i += 3)
                    {
                        Assert.That(HasContact(world, bodies[i], bodies[i + 1]), Is.True);
                        // With one pass, B must reject A's already-registered hit and select C.
                        // With four passes, only A opts in and the sleeping recipients follow.
                        Assert.That(HasContact(world, bodies[i + 1], bodies[i + 2]), Is.True);
                    }
                    if (passes == 1) Assert.That(world.RawData.Contacts.Length, Is.GreaterThan(1024));
                    firstStep = false;
                }
                foreach (RigidBody body in bodies)
                {
                    states.Add(body.Velocity);
                    states.Add(body.AngularVelocity);
                    states.Add(new JVector(body.Contacts.Count, 0, 0));
                }
            };
            for (int step = 0; step < 3; step++)
            {
                world.Step((Real)0.02, multiThread);
                foreach (RigidBody body in bodies) states.Add(body.Position);
                if (step == 1) world.Trim();
            }
            return states.ToArray();
        }

        Assert.That(Run(true), Is.EqualTo(Run(false)));
    }

    private sealed class ThrowingBounds() : BoxShape((Real)0.5)
    {
        public bool ThrowOnce;

        public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position,
            out JBoundingBox box)
        {
            if (ThrowOnce)
            {
                ThrowOnce = false;
                throw new InvalidOperationException("CCD bounds failure.");
            }
            base.CalculateBoundingBox(orientation, position, out box);
        }
    }

    [Test]
    public void ParallelDiscoveryRecoversAfterAWorkerThrows()
    {
        using var world = new World { Gravity = JVector.Zero };
        world.Step((Real)0.01, false);
        var pairs = new List<(RigidBody Body, RigidBody Wall)>();
        ThrowingBounds? throwing = null;
        for (int row = 0; row < 64; row++)
        {
            RigidBody body = world.CreateRigidBody();
            var shape = new ThrowingBounds();
            body.AddShape(shape);
            body.Position = new JVector(0, 0, row * 4);
            body.Velocity = new JVector(100, 0, 0);
            body.EnableCcd = true;
            RigidBody wall = world.CreateRigidBody();
            wall.AddShape(new BoxShape((Real)0.1, 1, 1));
            wall.Position = new JVector(1, 0, row * 4);
            wall.MotionType = MotionType.Static;
            pairs.Add((body, wall));
            throwing ??= shape;
        }
        throwing!.ThrowOnce = true;

        Assert.Throws<InvalidOperationException>(() => world.Step((Real)0.01, true));

        world.PreSubStep += _ =>
        {
            foreach (var (body, wall) in pairs) Assert.That(HasContact(world, body, wall), Is.True);
        };
        Assert.DoesNotThrow(() => world.Step((Real)0.01, true));
    }
}
