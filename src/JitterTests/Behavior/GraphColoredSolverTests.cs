using Jitter2.Dynamics.Constraints;

namespace JitterTests.Behavior;

[TestFixture, NonParallelizable]
public class GraphColoredSolverTests
{
    private const Real Dt = (Real)0.01;
    private const int Threads = 4;
    private int previousThreadCount;

    [OneTimeSetUp]
    public void EnableWorkers()
    {
        var pool = Jitter2.Parallelization.ThreadPool.Instance;
        previousThreadCount = pool.ThreadCount;
        pool.ChangeThreadCount(Threads);
    }

    [OneTimeTearDown]
    public void RestoreWorkers()
    {
        var pool = Jitter2.Parallelization.ThreadPool.Instance;
        pool.ChangeThreadCount(previousThreadCount);
        pool.PauseWorkers();
    }

    private static World NewWorld(SolveMode mode, int substeps = 1)
    {
        var world = new World { SolveMode = mode, AllowDeactivation = false, SubstepCount = substeps };
        var floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(200, 2, 200));
        floor.Position = new JVector(0, -1, 0);
        floor.MotionType = MotionType.Static;
        return world;
    }

    private static RigidBody AddBox(World world, JVector position)
    {
        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.Position = position;
        return body;
    }

    // Boxes that touch nothing but the static floor: their contacts are independent of each
    // other, so the order in which a solver visits them cannot change the result.
    private static List<RigidBody> AddSeparateBoxes(World world, int count)
    {
        var bodies = new List<RigidBody>();
        for (int i = 0; i < count; i++)
        {
            var body = AddBox(world, new JVector(3 * (i % 10), (Real)0.6 + (Real)0.01 * (i % 7), 3 * (i / 10)));
            body.Orientation = JQuaternion.CreateRotationY((Real)0.1 * i);
            body.Velocity = new JVector((Real)0.3 * (i % 3 - 1), 0, (Real)0.2 * (i % 5 - 2));
            bodies.Add(body);
        }

        return bodies;
    }

    private static List<RigidBody> AddPyramid(World world, int size)
    {
        var bodies = new List<RigidBody>();
        for (int row = 0; row < size; row++)
        {
            for (int k = 0; k < size - row; k++)
                bodies.Add(AddBox(world, new JVector(k * (Real)1.01 + row * (Real)0.5, (Real)0.5 + row, 0)));
        }

        return bodies;
    }

    [Test]
    public void IndependentContacts_MatchTheRegularSolver()
    {
        using var regular = NewWorld(SolveMode.Regular);
        using var colored = NewWorld(SolveMode.GraphColored);
        var expected = AddSeparateBoxes(regular, 37);
        var actual = AddSeparateBoxes(colored, 37);

        for (int step = 0; step < 120; step++)
        {
            regular.Step(Dt, multiThread: false);
            colored.Step(Dt, multiThread: false);
        }

        const double tolerance = 1e-3;
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.That((actual[i].Position - expected[i].Position).Length(), Is.LessThan(tolerance), $"position {i}");
            Assert.That((actual[i].Velocity - expected[i].Velocity).Length(), Is.LessThan(tolerance), $"velocity {i}");
            Assert.That((actual[i].AngularVelocity - expected[i].AngularVelocity).Length(), Is.LessThan(tolerance),
                $"angular velocity {i}");
        }
    }

    [TestCase(false, 1)]
    [TestCase(true, 1)]
    [TestCase(true, 3)]
    public void Pyramid_StaysStanding(bool multiThread, int substeps)
    {
        using var world = NewWorld(SolveMode.GraphColored, substeps);
        var bodies = AddPyramid(world, 12);
        var start = bodies.ConvertAll(b => b.Position);

        for (int step = 0; step < 300; step++) world.Step(Dt, multiThread);

        for (int i = 0; i < bodies.Count; i++)
        {
            Assert.That((bodies[i].Position - start[i]).Length(), Is.LessThan((Real)0.1), $"box {i} moved");
            Assert.That(bodies[i].Velocity.Length(), Is.LessThan((Real)0.1), $"box {i} is not at rest");
        }
    }

    [Test]
    public void SubstepCallbacks_KeepTheSolveValid()
    {
        using var world = NewWorld(SolveMode.GraphColored, substeps: 2);
        var bodies = AddPyramid(world, 6);
        var start = bodies.ConvertAll(b => b.Position);
        int calls = 0;
        world.PostSubStep += _ => calls++;

        for (int step = 0; step < 100; step++) world.Step(Dt, multiThread: true);

        Assert.That(calls, Is.EqualTo(200));
        for (int i = 0; i < bodies.Count; i++)
            Assert.That((bodies[i].Position - start[i]).Length(), Is.LessThan((Real)0.1), $"box {i} moved");
    }

    // Arbiters must be created and retired in the same steps as with the regular solver, although
    // the graph-colored solver refreshes the contacts before the broken arbiters are removed.
    [TestCase(false)]
    [TestCase(true)]
    public void ArbiterLifetime_MatchesTheRegularSolver(bool multiThread)
    {
        using var regular = NewWorld(SolveMode.Regular);
        using var colored = NewWorld(SolveMode.GraphColored);

        foreach (var world in new[] { regular, colored })
        {
            // Boxes dropped from different heights bounce, so contacts break and form again.
            for (int i = 0; i < 20; i++)
            {
                var body = AddBox(world, new JVector(3 * i, 1 + (Real)0.3 * i, 0));
                body.Restitution = (Real)0.8;
            }
        }

        for (int step = 0; step < 400; step++)
        {
            regular.Step(Dt, multiThread);
            colored.Step(Dt, multiThread);
            Assert.That(colored.RawData.ActiveContacts.Length, Is.EqualTo(regular.RawData.ActiveContacts.Length),
                $"arbiter count at step {step}");
        }
    }

    [Test]
    public void JointsAndContacts_AreSolvedTogether()
    {
        using var world = NewWorld(SolveMode.GraphColored);
        // Hanging chains that end on the floor: joints, body-body contacts and floor contacts.
        var last = new List<RigidBody>();
        for (int chain = 0; chain < 30; chain++)
        {
            RigidBody? previous = null;
            for (int link = 0; link < 6; link++)
            {
                var body = AddBox(world, new JVector(3 * chain, (Real)0.5 + (Real)1.2 * link, 0));
                if (previous != null)
                {
                    world.CreateConstraint<BallSocket>(previous, body)
                        .Initialize(body.Position - new JVector(0, (Real)0.6, 0));
                }

                previous = body;
            }

            last.Add(previous!);
        }

        for (int step = 0; step < 300; step++) world.Step(Dt, multiThread: true);

        foreach (var body in last)
        {
            Assert.That(body.Position.Y, Is.InRange((Real)0.4, (Real)7.0));
            Assert.That(body.Velocity.Length(), Is.LessThan((Real)5.0));
        }
    }

    [Test]
    public void SleepingBodies_AreLeftAlone()
    {
        using var world = NewWorld(SolveMode.GraphColored);
        world.AllowDeactivation = true;
        var bodies = AddPyramid(world, 5);

        for (int step = 0; step < 600; step++) world.Step(Dt, multiThread: true);
        Assert.That(bodies.TrueForAll(b => !b.IsActive), Is.True, "the pyramid should have gone to sleep");

        var falling = AddBox(world, new JVector(30, 5, 0));
        for (int step = 0; step < 100; step++) world.Step(Dt, multiThread: true);

        Assert.That(bodies.TrueForAll(b => !b.IsActive), Is.True);
        Assert.That(falling.Position.Y, Is.LessThan((Real)1.0));
    }

    [Test]
    public void Stabilize_RemovesApproachVelocity()
    {
        using var world = NewWorld(SolveMode.GraphColored);
        var bodies = new List<RigidBody>();
        for (int i = 0; i < 12; i++) bodies.Add(AddBox(world, new JVector(3 * i, (Real)0.5, 0)));
        for (int step = 0; step < 20; step++) world.Step(Dt, multiThread: false);
        Assert.That(world.RawData.ActiveContacts.Length, Is.EqualTo(12));
        foreach (var body in bodies) body.Velocity = new JVector(0, -3, 0);

        world.Stabilize(Dt, 8, 0, multiThread: true);

        foreach (var body in bodies) Assert.That(body.Velocity.Y, Is.GreaterThan((Real)(-0.5)));
    }

    [Test]
    public void SolveMode_CanChangeBetweenSteps()
    {
        using var world = NewWorld(SolveMode.Regular);
        var bodies = AddPyramid(world, 8);
        var start = bodies.ConvertAll(b => b.Position);

        for (int step = 0; step < 300; step++)
        {
            world.SolveMode = (step / 10 % 3) switch
            {
                0 => SolveMode.Regular, 1 => SolveMode.GraphColored, _ => SolveMode.Deterministic
            };
            world.Step(Dt, multiThread: true);
        }

        for (int i = 0; i < bodies.Count; i++)
            Assert.That((bodies[i].Position - start[i]).Length(), Is.LessThan((Real)0.1), $"box {i} moved");
    }
}
