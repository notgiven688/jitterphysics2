namespace JitterTests.Behavior;

[TestFixture]
[NonParallelizable]
public class GraphColoredSolverTests
{
    private const Real Dt = (Real)0.01;
    private int previousThreadCount;

    [OneTimeSetUp]
    public void EnableWorkers()
    {
        var pool = Jitter2.Parallelization.ThreadPool.Instance;
        previousThreadCount = pool.ThreadCount;
        pool.ChangeThreadCount(2);
    }

    [OneTimeTearDown]
    public void RestoreWorkers()
    {
        var pool = Jitter2.Parallelization.ThreadPool.Instance;
        pool.ChangeThreadCount(previousThreadCount);
        pool.PauseWorkers();
    }

    private static World CreateWorld(int substeps = 1) => new()
    {
        SolveMode = SolveMode.GraphColored,
        AllowDeactivation = false,
        SubstepCount = substeps,
        SolverIterations = (8, 4)
    };

    private static RigidBody CreateFloor(World world, MotionType motionType = MotionType.Static)
    {
        var floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(100, 2, 100));
        floor.Position = new JVector(0, -1, 0);
        floor.Friction = 1;
        floor.MotionType = motionType;
        return floor;
    }

    private static RigidBody CreateBox(World world, JVector position)
    {
        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.Position = position;
        body.Friction = 1;
        body.Damping = (0, 0);
        return body;
    }

    [TestCase(false, MotionType.Static)]
    [TestCase(true, MotionType.Static)]
    [TestCase(false, MotionType.Kinematic)]
    [TestCase(true, MotionType.Kinematic)]
    public void SharedGround_SupportsBatchesAndTransfersFriction(bool multiThread, MotionType groundType)
    {
        using var world = CreateWorld(2);
        var floor = CreateFloor(world, groundType);
        if (groundType == MotionType.Kinematic) floor.Velocity = new JVector((Real)0.75, 0, 0);
        JVector prescribedVelocity = floor.Velocity;

        // Enough contacts to dispatch multiple worker batches, with a partial SIMD tail.
        var boxes = new List<RigidBody>();
        for (int i = 0; i < 261; i++)
        {
            var box = CreateBox(world, new JVector(2 * (i % 16), (Real)0.495, 2 * (i / 16)));
            box.Velocity = new JVector(2, 0, 1);
            boxes.Add(box);
        }

        for (int i = 0; i < 180; i++) world.Step(Dt, multiThread);

        Assert.That(floor.Velocity, Is.EqualTo(prescribedVelocity));
        Assert.That(floor.AngularVelocity, Is.EqualTo(JVector.Zero));
        foreach (var box in boxes)
        {
            Assert.That(box.Position.Y, Is.EqualTo((Real)0.495).Within(0.02));
            Assert.That((box.Velocity - prescribedVelocity).Length(), Is.LessThan((Real)0.04));
            Assert.That(box.Contacts, Is.Not.Empty);
        }
    }

    [TestCase(false, 1)]
    [TestCase(true, 1)]
    [TestCase(false, 3)]
    [TestCase(true, 3)]
    public void Stacks_PropagateSupportAcrossColors(bool multiThread, int substeps)
    {
        using var world = CreateWorld(substeps);
        CreateFloor(world);
        var boxes = new List<(RigidBody body, Real height)>();
        for (int column = 0; column < 5; column++)
        for (int row = 0; row < 5; row++)
        {
            Real height = (Real)(0.495 + row * 0.99);
            boxes.Add((CreateBox(world, new JVector(column * 3, height, 0)), height));
        }

        for (int i = 0; i < 300; i++) world.Step(Dt, multiThread);

        foreach (var (body, height) in boxes)
        {
            Assert.That(body.Position.Y, Is.EqualTo(height).Within(0.12));
            Assert.That(body.Velocity.Length(), Is.LessThan((Real)0.15));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Stabilize_RelaxesPenaltyWithoutChangingTransformsOrApplyingForces(bool multiThread)
    {
        using var world = CreateWorld(3);
        world.Gravity = new JVector(10, -20, 30);
        var bodies = new List<RigidBody>();
        for (int i = 0; i < 13; i++)
        {
            var body = world.CreateRigidBody();
            body.AddForce(new JVector(100, 200, 300));
            body.Orientation = JQuaternion.CreateRotationY((Real)0.2);
            world.RegisterContact(1000, (ulong)(1001 + i), world.NullBody, body,
                new JVector(0, (Real)0.1, 0), JVector.Zero, JVector.UnitY);
            bodies.Add(body);
        }

        var orientations = bodies.Select(body => body.Orientation).ToArray();
        world.Stabilize(Dt, 2, 0, multiThread);
        Assert.That(bodies.All(body => body.Velocity.Y > (Real)0.1), Is.True,
            "The initial velocity solve must apply penetration bias.");

        // Repeat from rest: unilateral contacts cannot retract a pre-existing
        // separating velocity once its associated impulse has been released.
        foreach (var body in bodies) body.Velocity = JVector.Zero;
        foreach (ref var contact in world.RawData.ActiveContacts) contact.ResetImpulses();
        world.Stabilize(Dt, 2, 3, multiThread);
        for (int i = 0; i < bodies.Count; i++)
        {
            Assert.That(bodies[i].Position, Is.EqualTo(JVector.Zero));
            Assert.That(bodies[i].Orientation, Is.EqualTo(orientations[i]));
            Assert.That(bodies[i].Velocity.Length(), Is.LessThan((Real)1e-5));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ObliqueContacts_RespectLinearAndAngularLocks(bool multiThread)
    {
        using var world = CreateWorld();
        world.Gravity = JVector.Zero;
        var bodies = new List<RigidBody>();
        JVector normal = JVector.Normalize(new JVector(1, 1, 1));
        for (int i = 0; i < 17; i++)
        {
            var body = world.CreateRigidBody();
            body.SetMassInertia(JSymmetricMatrix.Identity, 1);
            body.AllowedMotion = i % 2 == 0 ? MotionAxes.LinearY : MotionAxes.None;
            body.Velocity = new JVector(0, -3, 0);
            world.RegisterContact(2000, (ulong)(2001 + i), world.NullBody, body,
                JVector.UnitX, JVector.UnitX, normal);
            bodies.Add(body);
        }

        world.Stabilize(Dt, 4, 2, multiThread);
        foreach (var body in bodies)
        {
            Assert.That(body.Velocity.Length(), Is.LessThan((Real)1e-5));
            Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HighDegreeDynamicBody_SolvesMoreThan64ConflictingManifolds(bool multiThread)
    {
        const int count = 81;
        using var world = CreateWorld();
        world.Gravity = JVector.Zero;
        var hub = world.CreateRigidBody();
        hub.SetMassInertia(JSymmetricMatrix.Identity, count);
        var leaves = new List<RigidBody>();
        for (int i = 0; i < count; i++)
        {
            var leaf = world.CreateRigidBody();
            leaf.Velocity = -JVector.UnitX;
            leaf.Restitution = 0;
            world.RegisterContact(3000, (ulong)(3001 + i), hub, leaf,
                JVector.Zero, JVector.Zero, JVector.UnitX);
            leaves.Add(leaf);
        }

        Assert.That(world.RawData.ActiveContacts.Length, Is.EqualTo(count));
        world.Stabilize(Dt, 100, 2, multiThread);

        Real momentum = count * hub.Velocity.X;
        Assert.That(hub.Velocity.X, Is.EqualTo((Real)(-0.5)).Within(1e-4));
        foreach (var leaf in leaves)
        {
            Assert.That(leaf.Velocity.X, Is.EqualTo((Real)(-0.5)).Within(1e-4));
            momentum += leaf.Velocity.X;
        }
        Assert.That(momentum, Is.EqualTo((Real)(-count)).Within(0.001));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ContactsAndJoints_SolveTogetherAcrossSubsteps(bool multiThread)
    {
        using var world = CreateWorld(3);
        CreateFloor(world);
        var pairs = new List<(RigidBody first, RigidBody second, BallSocket socket)>();
        for (int i = 0; i < 16; i++)
        {
            JVector offset = new(6 * (i % 4), 0, 6 * (i / 4));
            var first = CreateBox(world, offset + new JVector(0, (Real)0.495, 0));
            var second = CreateBox(world, offset + new JVector(2, (Real)0.495, 0));
            var socket = world.CreateConstraint<BallSocket>(first, second);
            socket.Initialize(offset + new JVector(1, (Real)0.495, 0));
            var motor = world.CreateConstraint<LinearMotor>(world.NullBody, first);
            motor.Initialize(JVector.UnitX, JVector.UnitX);
            motor.TargetVelocity = 1;
            motor.MaximumForce = 1000;
            pairs.Add((first, second, socket));
        }

        for (int i = 0; i < 200; i++) world.Step(Dt, multiThread);

        foreach (var (first, second, socket) in pairs)
        {
            Assert.That((socket.Anchor2 - socket.Anchor1).Length(), Is.LessThan((Real)0.03));
            Assert.That(first.Velocity.X, Is.EqualTo((Real)1).Within(0.05));
            Assert.That(second.Velocity.X, Is.EqualTo((Real)1).Within(0.05));
            Assert.That(first.Position.Y, Is.EqualTo((Real)0.495).Within(0.03));
            Assert.That(second.Position.Y, Is.EqualTo((Real)0.495).Within(0.03));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PreSubStepContacts_AreIncludedInTheCurrentSolve(bool multiThread)
    {
        using var world = CreateWorld(2);
        world.Gravity = JVector.Zero;
        var bodies = new List<RigidBody>();
        for (int i = 0; i < 17; i++) bodies.Add(world.CreateRigidBody());
        int substep = 0;
        world.PreSubStep += _ =>
        {
            int start = substep++ == 0 ? 0 : 8;
            int end = start == 0 ? 8 : bodies.Count;
            for (int i = start; i < end; i++)
            {
                bodies[i].Velocity = -JVector.UnitY;
                world.RegisterContact(4000, (ulong)(4001 + i), world.NullBody, bodies[i],
                    JVector.Zero, JVector.Zero, JVector.UnitY);
            }
        };

        world.Step(Dt, multiThread);

        Assert.That(substep, Is.EqualTo(2));
        Assert.That(world.RawData.ActiveContacts.Length, Is.EqualTo(bodies.Count));
        foreach (var body in bodies)
        {
            Assert.That(body.Velocity.Length(), Is.LessThan((Real)1e-5));
            Assert.That(body.Position.Length(), Is.LessThan((Real)1e-5));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SleepingRemovalTrimAndClear_RebuildTheActiveContactGraph(bool multiThread)
    {
        using var world = CreateWorld();
        CreateFloor(world);
        var boxes = new List<RigidBody>();
        for (int i = 0; i < 19; i++)
            boxes.Add(CreateBox(world, new JVector(i * 2, (Real)0.495, 0)));
        for (int i = 0; i < 80; i++) world.Step(Dt, multiThread);

        world.AllowDeactivation = true;
        world.ForceSleepIsland(boxes[0].Island);
        JVector sleepingPosition = boxes[0].Position;
        Assert.That(boxes[0].IsActive, Is.False);
        for (int i = 1; i < boxes.Count; i += 2) world.Remove(boxes[i]);
        world.Trim();
        world.Stabilize(Dt, 3, 2, multiThread);
        Assert.That(boxes[0].Position, Is.EqualTo(sleepingPosition));
        Assert.That(boxes[0].IsActive, Is.False);

        boxes[0].SetActivationState(true);
        boxes[0].Velocity = JVector.UnitY;
        world.Step(Dt, multiThread);
        Assert.That(boxes[0].IsActive, Is.True);
        Assert.That(boxes[0].Position.Y, Is.GreaterThan(sleepingPosition.Y));

        world.Clear();
        world.Trim();
        world.Stabilize(Dt, 2, 2, multiThread);
        Assert.That(world.RawData.ActiveContacts.Length, Is.Zero);
        CreateFloor(world);
        var replacement = CreateBox(world, new JVector(0, 1, 0));
        for (int i = 0; i < 100; i++) world.Step(Dt, multiThread);
        Assert.That(replacement.Position.Y, Is.EqualTo((Real)0.495).Within(0.03));
    }
}
