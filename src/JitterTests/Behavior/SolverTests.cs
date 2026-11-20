using Jitter2.Unmanaged;
using Jitter2.Dynamics.Constraints;

namespace JitterTests.Behavior;

// Both solve modes use the same solver and have to pass the same tests. They differ in how the
// contacts get their colors: anew in every step, or kept for as long as a contact is active.
[TestFixture(SolveMode.Regular), TestFixture(SolveMode.Deterministic), NonParallelizable]
public class SolverTests(SolveMode mode)
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

    private World NewWorld(int substeps = 1)
    {
        var world = new World { AllowDeactivation = false, SubstepCount = substeps, SolveMode = mode };
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

    // The constraints are grouped once per step. A constraint disabled between two substeps must still
    // drop out of the solve at once.
    [Test]
    public void ConstraintDisabledInSubstepCallback_IsNoLongerSolved()
    {
        using var world = NewWorld(substeps: 4);
        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.Position = new JVector(0, 5, 0);
        var constraint = world.CreateConstraint<BallSocket>(world.NullBody, body);
        constraint.Initialize(body.Position);

        for (int step = 0; step < 10; step++) world.Step(Dt, multiThread: true);
        Assert.That((body.Position - new JVector(0, 5, 0)).Length(), Is.LessThan((Real)0.01));

        int substep = 0;
        world.PostSubStep += _ =>
        {
            if (++substep == 1) constraint.IsEnabled = false;
        };

        world.Step(Dt, multiThread: true);
        Real fallen = 5 - body.Position.Y;
        for (int step = 0; step < 20; step++) world.Step(Dt, multiThread: true);

        Assert.That(fallen, Is.GreaterThan(0), "the constraint held the body after it was disabled");
        Assert.That(5 - body.Position.Y, Is.GreaterThan((Real)0.1));
    }

    private static ContactColoring ColoringOf(World world) =>
        (ContactColoring)typeof(World).GetField("contactColoring",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;

    // After a solve every active contact is in exactly one list and no two contacts of a color
    // share a dynamic body. Coloring from scratch also leaves every list in the order of the
    // arbiter keys.
    private static void AssertColoringIsValid(World world, string when)
    {
        var coloring = ColoringOf(world);
        var bodies = new HashSet<JHandle<RigidBodyData>>();
        var seen = new HashSet<JHandle<ContactData>>();

        const int overflow = ContactColoring.OverflowColor;
        for (int color = 0; color <= overflow; color++)
        {
            bodies.Clear();
            var contacts = coloring.Contacts(color);
            for (int i = 0; i < contacts.Length; i++)
            {
                Assert.That(seen.Add(contacts[i]), Is.True, $"{when}: contact listed twice");
                ref ContactData contact = ref contacts[i].Data;
                if (i > 0 && world.SolveMode == SolveMode.Deterministic)
                {
                    ArbiterKey key = contact.Key, previous = contacts[i - 1].Data.Key;
                    Assert.That(key.Key1 > previous.Key1 || (key.Key1 == previous.Key1 && key.Key2 > previous.Key2),
                        Is.True, $"{when}, color {color}: key order");
                }

                if (color == overflow) continue;

                if (contact.Body1.Data.MotionType == MotionType.Dynamic)
                    Assert.That(bodies.Add(contact.Body1), Is.True, $"{when}, color {color}: body used twice");
                if (contact.Body2.Data.MotionType == MotionType.Dynamic)
                    Assert.That(bodies.Add(contact.Body2), Is.True, $"{when}, color {color}: body used twice");
            }
        }

        // Nothing adds or removes a record between the solve and the end of the step.
        var buffer = (PartitionedBuffer<ContactData>)typeof(World).GetField("memContacts",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;
        var records = buffer.Active;
        Assert.That(seen, Has.Count.EqualTo(records.Length), $"{when}: contacts in the lists");
        for (int i = 0; i < records.Length; i++)
            Assert.That(seen.Contains(buffer.GetHandle(ref records[i])), Is.True, $"{when}: contact missing");
    }

    [Test]
    public void Contacts_AreColoredWhileTheyComeAndGo()
    {
        using var world = NewWorld();
        world.AllowDeactivation = true;
        AddPyramid(world, 10);
        var bouncing = new List<RigidBody>();
        for (int i = 0; i < 40; i++)
        {
            var body = AddBox(world, new JVector(3 * (i % 8) - 4, 12 + 2 * (i / 8), (Real)0.2 * (i % 3)));
            body.Restitution = (Real)0.6;
            bouncing.Add(body);
        }

        for (int step = 0; step < 400; step++)
        {
            // Take bodies out of the pile and put new ones in while it runs.
            if (step % 40 == 20)
            {
                world.Remove(bouncing[^1]);
                bouncing.RemoveAt(bouncing.Count - 1);
                bouncing.Insert(0, AddBox(world, new JVector(step % 7, 14, 0)));
            }

            world.Step(Dt, multiThread: true);
            AssertColoringIsValid(world, $"step {step}");
        }
    }

    [Test]
    public void Contacts_AreColoredAcrossSleepAndWake()
    {
        using var world = NewWorld();
        world.AllowDeactivation = true;
        var bodies = AddPyramid(world, 6);

        for (int step = 0; step < 600; step++) world.Step(Dt, multiThread: true);
        Assert.That(bodies.TrueForAll(b => !b.IsActive), Is.True, "the pyramid should have gone to sleep");
        AssertColoringIsValid(world, "asleep");

        // A box dropped on top wakes the pile.
        AddBox(world, new JVector((Real)2.5, 9, 0));
        bool woken = false;
        for (int step = 0; step < 200; step++)
        {
            world.Step(Dt, multiThread: true);
            AssertColoringIsValid(world, $"waking, step {step}");
            woken |= bodies.Exists(b => b.IsActive);
        }

        Assert.That(woken, Is.True, "the pile should have woken up");
    }

    // A body that becomes dynamic takes part in the colors from then on; one that becomes static
    // no longer does.
    [Test]
    public void Contacts_AreRecoloredWhenMotionTypesChange()
    {
        using var world = NewWorld();
        var bodies = AddPyramid(world, 6);
        var anchor = bodies[0];
        anchor.MotionType = MotionType.Static;

        for (int step = 0; step < 150; step++)
        {
            if (step == 50) anchor.MotionType = MotionType.Dynamic;
            if (step == 100) bodies[1].MotionType = MotionType.Kinematic;
            world.Step(Dt, multiThread: true);
            AssertColoringIsValid(world, $"step {step}");
        }
    }

    // One body with more contacts than there are colors: the rest is solved without a color.
    [Test]
    public void ContactsBeyondTheColors_AreStillSolved()
    {
        using var world = NewWorld();
        var plate = world.CreateRigidBody();
        plate.AddShape(new BoxShape(30, 1, 30));
        plate.Position = new JVector(0, (Real)0.5, 0);

        var boxes = new List<RigidBody>();
        for (int i = 0; i < 100; i++) boxes.Add(AddBox(world, new JVector(-12 + (Real)2.5 * (i % 10), (Real)1.5, -12 + (Real)2.5 * (i / 10))));

        for (int step = 0; step < 200; step++) world.Step(Dt, multiThread: true);

        AssertColoringIsValid(world, "resting");
        Assert.That(ColoringOf(world).Overflow.Length, Is.GreaterThan(0), "the scene should exceed the colors");
        foreach (var box in boxes)
        {
            Assert.That(box.Position.Y, Is.EqualTo((Real)1.5).Within((Real)0.05));
            Assert.That(box.Velocity.Length(), Is.LessThan((Real)0.1));
        }
    }

    private static T Field<T>(World world, string name) =>
        (T)typeof(World).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;

    // After a solve every active constraint is in exactly one list and no two constraints of a color share
    // a dynamic body. Coloring from scratch also leaves every list in the order of the ids.
    private static void AssertConstraintColoringIsValid(World world, string when)
    {
        var coloring = Field<ConstraintColoring>(world, "constraintColoring");
        var bodies = new HashSet<JHandle<RigidBodyData>>();
        var seen = new HashSet<ulong>();

        const int overflow = ConstraintColoring.OverflowColor;
        for (int color = 0; color <= overflow; color++)
        {
            bodies.Clear();
            var constraints = coloring.Constraints(color);
            for (int i = 0; i < constraints.Length; i++)
            {
                ref ConstraintData constraint = ref constraints[i].Handle.Data;
                Assert.That(seen.Add(constraint.ConstraintId), Is.True, $"{when}: constraint listed twice");
                Assert.That(constraints[i].Constraint.Handle, Is.EqualTo(constraints[i].Handle), $"{when}: stale entry");
                if (i > 0 && world.SolveMode == SolveMode.Deterministic)
                {
                    Assert.That(constraint.ConstraintId, Is.GreaterThan(constraints[i - 1].Handle.Data.ConstraintId),
                        $"{when}, color {color}: id order");
                }

                if (color == overflow) continue;

                if (constraint.Body1.Data.MotionType == MotionType.Dynamic)
                    Assert.That(bodies.Add(constraint.Body1), Is.True, $"{when}, color {color}: body used twice");
                if (constraint.Body2.Data.MotionType == MotionType.Dynamic)
                    Assert.That(bodies.Add(constraint.Body2), Is.True, $"{when}, color {color}: body used twice");
            }
        }

        var full = Field<PartitionedBuffer<ConstraintData>>(world, "memConstraints").Active;
        var small = Field<PartitionedBuffer<SmallConstraintData>>(world, "memSmallConstraints").Active;
        Assert.That(seen, Has.Count.EqualTo(full.Length + small.Length), $"{when}: constraints in the lists");
        foreach (ref ConstraintData constraint in full) Assert.That(seen.Contains(constraint.ConstraintId), Is.True, $"{when}: constraint missing");
        foreach (ref SmallConstraintData constraint in small) Assert.That(seen.Contains(constraint.ConstraintId), Is.True, $"{when}: constraint missing");
    }

    [Test]
    public void Constraints_AreColoredWhileTheyComeAndGo()
    {
        using var world = NewWorld();
        world.AllowDeactivation = true;
        var bodies = BuildMixedScene(world);
        var extra = new List<Constraint>();

        for (int step = 0; step < 400; step++)
        {
            // Tie neighboring bodies together and cut them loose again while the scene runs.
            if (step % 20 == 10)
            {
                int k = step / 20 * 7 % (bodies.Count - 1);
                var constraint = world.CreateConstraint<BallSocket>(bodies[k], bodies[k + 1]);
                constraint.Initialize((bodies[k].Position + bodies[k + 1].Position) * (Real)0.5);
                extra.Add(constraint);
            }

            if (step % 50 == 40 && extra.Count > 0)
            {
                world.Remove(extra[0]);
                extra.RemoveAt(0);
            }

            if (step == 200) bodies[3].MotionType = MotionType.Static;
            if (step == 260) bodies[3].MotionType = MotionType.Dynamic;
            if (step % 90 == 45 && extra.Count > 0) extra[^1].IsEnabled = !extra[^1].IsEnabled;

            world.Step(Dt, multiThread: true);
            AssertConstraintColoringIsValid(world, $"step {step}");
            AssertColoringIsValid(world, $"step {step}");
        }
    }

    [Test]
    public void Constraints_AreColoredAcrossSleepAndWake()
    {
        using var world = NewWorld();
        world.AllowDeactivation = true;
        var bodies = new List<RigidBody>();
        for (int chain = 0; chain < 6; chain++)
        {
            RigidBody? previous = null;
            for (int link = 0; link < 5; link++)
            {
                var body = AddBox(world, new JVector(3 * chain, (Real)0.5 + link, 0));
                if (previous != null)
                    world.CreateConstraint<BallSocket>(previous, body).Initialize(body.Position - new JVector(0, (Real)0.5, 0));
                bodies.Add(body);
                previous = body;
            }
        }

        for (int step = 0; step < 800; step++) world.Step(Dt, multiThread: true);
        Assert.That(bodies.TrueForAll(b => !b.IsActive), Is.True, "the chains should have gone to sleep");
        AssertConstraintColoringIsValid(world, "asleep");

        // Waking one chain brings back its constraints only.
        bodies[2].Velocity = new JVector(1, 0, 0);
        bodies[2].SetActivationState(true);
        bool woken = false;
        for (int step = 0; step < 100; step++)
        {
            world.Step(Dt, multiThread: true);
            AssertConstraintColoringIsValid(world, $"waking, step {step}");
            woken |= bodies[2].IsActive;
        }

        Assert.That(woken, Is.True, "the chain should have woken up");
        Assert.That(bodies[^1].IsActive, Is.False, "the other chains stay asleep");
    }

    // A long rope: every inner body carries two constraints, so the constraints need more than one color.
    [Test]
    public void Rope_IsSolvedInColors()
    {
        using var world = NewWorld();
        RigidBody previous = world.NullBody;
        var constraints = new List<BallSocket>();
        for (int link = 0; link < 150; link++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape((Real)0.5));
            body.Position = new JVector(0, 120 - (Real)0.5 * (link + 1), 30);
            var constraint = world.CreateConstraint<BallSocket>(previous, body);
            constraint.Initialize(body.Position + new JVector(0, (Real)0.25, 0));
            constraints.Add(constraint);
            previous = body;
        }

        for (int step = 0; step < 200; step++) world.Step(Dt, multiThread: true);

        AssertConstraintColoringIsValid(world, "hanging");
        Assert.That(Field<ConstraintColoring>(world, "constraintColoring").ColorCount, Is.GreaterThanOrEqualTo(2));
        // A long rope stretches under its own weight with a few iterations; it must hold
        // together all the same.
        for (int link = 0; link < constraints.Count; link++)
            Assert.That((constraints[link].Anchor1 - constraints[link].Anchor2).Length(), Is.LessThan((Real)0.1), $"constraint {link}");
    }

    [Test]
    public void SolveMode_CanChangeBetweenSteps()
    {
        using var world = NewWorld();
        var bodies = AddPyramid(world, 8);
        var start = bodies.ConvertAll(b => b.Position);

        for (int step = 0; step < 200; step++)
        {
            if (step % 25 == 0)
                world.SolveMode = world.SolveMode == SolveMode.Regular ? SolveMode.Deterministic : SolveMode.Regular;
            world.Step(Dt, multiThread: true);
            AssertColoringIsValid(world, $"step {step}");
        }

        for (int i = 0; i < bodies.Count; i++)
            Assert.That((bodies[i].Position - start[i]).Length(), Is.LessThan((Real)0.1), $"box {i} moved");
    }

    [Test]
    public void ConstraintsAndContacts_AreSolvedTogether()
    {
        using var world = NewWorld();
        // Hanging chains that end on the floor: constraints, body-body contacts and floor contacts.
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

    // Contacts, constraints and a body with more contacts than there are colors: the result must not
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
        // Kept colors depend on the order in which the collision threads report new contacts.
        if (mode == SolveMode.Regular) Assert.Ignore("Only coloring from scratch is independent of thread timing.");

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
        // Kept colors depend on the order in which the collision threads report new contacts.
        if (mode == SolveMode.Regular) Assert.Ignore("Only coloring from scratch is independent of thread timing.");

        using var alone = NewWorld();
        using var crowded = NewWorld();
        var expected = AddPyramid(alone, 9);
        var actual = AddPyramid(crowded, 9);

        // A second, much larger pile far away, with constraints of its own.
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

    [Test]
    public void SolveMode_IsStoredAndValidated()
    {
        using var world = new World();
        Assert.That(world.SolveMode, Is.EqualTo(SolveMode.Regular));
        world.SolveMode = SolveMode.Deterministic;
        Assert.That(world.SolveMode, Is.EqualTo(SolveMode.Deterministic));
        Assert.Throws<ArgumentOutOfRangeException>(() => world.SolveMode = (SolveMode)42);
    }
}
