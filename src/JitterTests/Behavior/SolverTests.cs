using Jitter2.Unmanaged;
using Jitter2.Dynamics.Constraints;

namespace JitterTests.Behavior;

[TestFixture, NonParallelizable]
public class SolverTests
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

    private static World NewWorld(int substeps = 1)
    {
        var world = new World { AllowDeactivation = false, SubstepCount = substeps };
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

    [TestCase(false, 1)]
    [TestCase(true, 1)]
    [TestCase(true, 3)]
    public void Pyramid_StaysStanding(bool multiThread, int substeps)
    {
        using var world = NewWorld(substeps);
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
        using var world = NewWorld(substeps: 2);
        var bodies = AddPyramid(world, 6);
        var start = bodies.ConvertAll(b => b.Position);
        int calls = 0;
        world.PostSubStep += _ => calls++;

        for (int step = 0; step < 100; step++) world.Step(Dt, multiThread: true);

        Assert.That(calls, Is.EqualTo(200));
        for (int i = 0; i < bodies.Count; i++)
            Assert.That((bodies[i].Position - start[i]).Length(), Is.LessThan((Real)0.1), $"box {i} moved");
    }

    // Whatever the engine did to the contact buffer between solves, after a solve every color
    // is one run of records, and no two records of a color share a dynamic body.
    [Test]
    public void ContactRecords_AreGroupedByColor()
    {
        using var world = NewWorld();
        world.AllowDeactivation = true;
        AddPyramid(world, 10);
        for (int i = 0; i < 40; i++)
        {
            var body = AddBox(world, new JVector(3 * (i % 8) - 4, 12 + 2 * (i / 8), (Real)0.2 * (i % 3)));
            body.Restitution = (Real)0.6;
        }

        var segmentStart = (int[])typeof(World).GetField("segmentStart",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;
        var bodies = new HashSet<JHandle<RigidBodyData>>();

        for (int step = 0; step < 400; step++)
        {
            world.Step(Dt, multiThread: true);

            // The contact update of the step has run, so points may have broken since the solve;
            // endpoints and positions of the records are still those of the solve.
            var records = world.RawData.ActiveContacts;
            if (segmentStart[^1] != records.Length) continue; // arbiters were removed after the solve

            for (int color = 0; color < 64; color++)
            {
                bodies.Clear();
                for (int i = segmentStart[color]; i < segmentStart[color + 1]; i++)
                {
                    ref ContactData contact = ref records[i];
                    if (contact.Body1.Data.MotionType == MotionType.Dynamic)
                        Assert.That(bodies.Add(contact.Body1), Is.True, $"step {step}, color {color}");
                    if (contact.Body2.Data.MotionType == MotionType.Dynamic)
                        Assert.That(bodies.Add(contact.Body2), Is.True, $"step {step}, color {color}");
                }
            }
        }
    }

    [Test]
    public void JointsAndContacts_AreSolvedTogether()
    {
        using var world = NewWorld();
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

    // Contacts, joints and a body with more contacts than there are colors: the result must not
    // depend on whether the pool is used, nor on how many workers it has.
    private static List<RigidBody> BuildMixedScene(World world)
    {
        var bodies = AddPyramid(world, 8);

        // A wide plate carrying a hundred boxes has more contacts than colors.
        var plate = world.CreateRigidBody();
        plate.AddShape(new BoxShape(30, 1, 6));
        plate.Position = new JVector(0, (Real)0.5, 12);
        bodies.Add(plate);
        for (int i = 0; i < 100; i++)
            bodies.Add(AddBox(world, new JVector((Real)(-12.5) + (Real)1.25 * (i % 20), (Real)1.5, (Real)10.4 + (Real)0.8 * (i / 20))));

        for (int chain = 0; chain < 12; chain++)
        {
            RigidBody? previous = null;
            for (int link = 0; link < 5; link++)
            {
                var body = AddBox(world, new JVector(3 * chain - 16, (Real)0.5 + (Real)1.2 * link, -8));
                if (previous != null)
                {
                    world.CreateConstraint<BallSocket>(previous, body)
                        .Initialize(body.Position - new JVector(0, (Real)0.6, 0));
                }

                bodies.Add(body);
                previous = body;
            }
        }

        return bodies;
    }

    [Test]
    public void Result_IsIndependentOfThreads()
    {
        var pool = Jitter2.Parallelization.ThreadPool.Instance;
        using var serial = NewWorld(substeps: 2);
        using var parallel = NewWorld(substeps: 2);
        var expected = BuildMixedScene(serial);
        var actual = BuildMixedScene(parallel);

        try
        {
            for (int step = 0; step < 300; step++)
            {
                // Change the worker count as the scene runs.
                if (step % 50 == 0) pool.ChangeThreadCount(2 + step / 50 % 3 * 3);
                serial.Step(Dt, multiThread: false);
                parallel.Step(Dt, multiThread: true);

                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.That(actual[i].Position, Is.EqualTo(expected[i].Position), $"position of body {i} at step {step}");
                    Assert.That(actual[i].Orientation, Is.EqualTo(expected[i].Orientation), $"orientation of body {i} at step {step}");
                    Assert.That(actual[i].Velocity, Is.EqualTo(expected[i].Velocity), $"velocity of body {i} at step {step}");
                }
            }
        }
        finally
        {
            pool.ChangeThreadCount(Threads);
        }
    }

    // Bodies that never touch must not influence each other: how many contacts the rest of the
    // world has changes the sizes of the colors, but not the order in which a pile is solved.
    [Test]
    public void Result_IsIndependentOfUnrelatedBodies()
    {
        using var alone = NewWorld();
        using var crowded = NewWorld();
        var expected = AddPyramid(alone, 9);
        var actual = AddPyramid(crowded, 9);

        // A second, much larger pile far away, with joints of its own.
        for (int x = 0; x < 12; x++)
        {
            for (int z = 0; z < 12; z++)
            {
                RigidBody? below = null;
                for (int y = 0; y < 4; y++)
                {
                    var body = AddBox(crowded, new JVector(60 + (Real)1.02 * x, (Real)0.5 + y, (Real)1.02 * z));
                    if (below != null && (x + z) % 5 == 0)
                    {
                        crowded.CreateConstraint<BallSocket>(below, body)
                            .Initialize(body.Position - new JVector(0, (Real)0.5, 0));
                    }

                    below = body;
                }
            }
        }

        for (int step = 0; step < 200; step++)
        {
            alone.Step(Dt, multiThread: true);
            crowded.Step(Dt, multiThread: true);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.That(actual[i].Position, Is.EqualTo(expected[i].Position), $"position of body {i} at step {step}");
                Assert.That(actual[i].Velocity, Is.EqualTo(expected[i].Velocity), $"velocity of body {i} at step {step}");
            }
        }
    }

    [Test]
    public void SleepingBodies_AreLeftAlone()
    {
        using var world = NewWorld();
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
        using var world = NewWorld();
        var bodies = new List<RigidBody>();
        for (int i = 0; i < 12; i++) bodies.Add(AddBox(world, new JVector(3 * i, (Real)0.5, 0)));
        for (int step = 0; step < 20; step++) world.Step(Dt, multiThread: false);
        Assert.That(world.RawData.ActiveContacts.Length, Is.EqualTo(12));
        foreach (var body in bodies) body.Velocity = new JVector(0, -3, 0);

        world.Stabilize(Dt, 8, 0, multiThread: true);

        foreach (var body in bodies) Assert.That(body.Velocity.Y, Is.GreaterThan((Real)(-0.5)));
    }
}
