using System.Collections;
using System.Reflection;
using Jitter2.Unmanaged;
using ContactHandle = Jitter2.Unmanaged.JHandle<Jitter2.Dynamics.ContactData>;

namespace JitterTests.Behavior;

[TestFixture]
[NonParallelizable]
public class GraphColoredLocalityTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const int BucketShift = 7;

    private static T Read<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, Fields)!.GetValue(instance)!;

    private static World CreateWorld() => new()
    {
        SolveMode = SolveMode.GraphColored,
        AllowDeactivation = false,
        Gravity = JVector.Zero
    };

    private static ContactHandle AddContact(World world, RigidBody first, RigidBody second,
        ContactData.SolveMode? mode = null)
    {
        ulong id = (ulong)world.RawData.ActiveContacts.Length * 2 + 1000;
        world.GetOrCreateArbiter(id, id + 1, first, second, out var arbiter);
        world.RegisterContact(arbiter, JVector.Zero, JVector.Zero, JVector.UnitY);
        if (mode.HasValue) arbiter.Handle.Data.Mode = mode.Value;
        return arbiter.Handle;
    }

    private static ContactHandle[] ActiveHandles(World world)
    {
        var contacts = Read<PartitionedBuffer<ContactData>>(world, "memContacts");
        var result = new ContactHandle[contacts.Active.Length];
        for (int i = 0; i < result.Length; i++) result[i] = contacts.GetHandle(ref contacts.Active[i]);
        return result;
    }

    private static HashSet<JHandle<RigidBodyData>> WritableBodies(IEnumerable<ContactHandle> contacts)
    {
        var result = new HashSet<JHandle<RigidBodyData>>();
        foreach (var handle in contacts)
        {
            ref var contact = ref handle.Data;
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0) continue;
            if ((contact.Mode & ContactData.SolveMode.FullBody1) != 0) result.Add(contact.Body1);
            if ((contact.Mode & ContactData.SolveMode.FullBody2) != 0) result.Add(contact.Body2);
        }
        return result;
    }

    // Use sets rather than the production bit masks, and retain the original contact
    // order so stable grouping can be checked independently of its scratch storage.
    private static void AssertColoredOrder(World world)
    {
        ContactHandle[] input = ActiveHandles(world);
        var writable = WritableBodies(input);
        var bodies = Read<PartitionedBuffer<RigidBodyData>>(world, "memRigidBodies");
        var expected = new List<List<ContactHandle>>();
        var occupied = new List<HashSet<JHandle<RigidBodyData>>>();
        var overflow = new List<ContactHandle>();
        foreach (var handle in input)
        {
            ref var contact = ref handle.Data;
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0) continue;
            var endpoints = new[] { contact.Body1, contact.Body2 }.Where(writable.Contains).ToArray();
            int color = occupied.FindIndex(set => !endpoints.Any(set.Contains));
            if (color < 0)
            {
                if (expected.Count == 64)
                {
                    overflow.Add(handle);
                    continue;
                }
                color = expected.Count;
                expected.Add(new List<ContactHandle>());
                occupied.Add(new HashSet<JHandle<RigidBodyData>>());
            }
            expected[color].Add(handle);
            occupied[color].UnionWith(endpoints);
        }

        int Bucket(ContactHandle handle)
        {
            ref var contact = ref handle.Data;
            int first = writable.Contains(contact.Body1) ? bodies.GetIndex(contact.Body1) : int.MaxValue;
            int second = writable.Contains(contact.Body2) ? bodies.GetIndex(contact.Body2) : int.MaxValue;
            return Math.Min(first, second) >> BucketShift;
        }

        typeof(World).GetMethod("BuildContactColors", Fields)!.Invoke(world, null);
        Assert.That(Read<int>(world, "contactColorCount"), Is.EqualTo(expected.Count));
        var colors = Read<IList>(world, "contactColors");
        for (int i = 0; i < expected.Count; i++)
        {
            var actual = Read<List<ContactHandle>>(colors[i]!, "Contacts");
            Assert.That(actual, Is.EqualTo(expected[i].OrderBy(Bucket).ToArray()),
                $"Color {i} must preserve greedy membership and the input order within each bucket.");
        }
        Assert.That(Read<List<ContactHandle>>(world, "contactColorOverflow"), Is.EqualTo(overflow),
            "The serial overflow must retain its original order.");
        Assert.That(ActiveHandles(world), Is.EqualTo(input), "Grouping must not move the persistent contact records.");
    }

    // Retained colors need not equal a new greedy coloring after physical moves.
    // The periodic refresh still sorts each retained color stably by current slots.
    private static void AssertRetainedOrderAfterRefresh(World world)
    {
        var input = ActiveHandles(world);
        AdaptiveContactGraphTests.Build(world);
        AdaptiveContactGraphTests.Inspect(world);
        var writable = WritableBodies(input);
        var bodies = Read<PartitionedBuffer<RigidBodyData>>(world, "memRigidBodies");
        int Bucket(ContactHandle h)
        {
            int first = writable.Contains(h.Data.Body1) ? bodies.GetIndex(h.Data.Body1) : int.MaxValue;
            int second = writable.Contains(h.Data.Body2) ? bodies.GetIndex(h.Data.Body2) : int.MaxValue;
            return Math.Min(first, second) >> BucketShift;
        }
        int count = Read<int>(world, "contactColorCount");
        var colors = Read<IList>(world, "contactColors");
        var expected = Enumerable.Range(0, count)
            .Select(c => Read<List<ContactHandle>>(colors[c]!, "Contacts").OrderBy(Bucket).ToArray()).ToArray();
        for (int i = 0; i < 64; i++) AdaptiveContactGraphTests.Build(world);
        AdaptiveContactGraphTests.Inspect(world);
        for (int c = 0; c < count; c++)
            Assert.That(Read<List<ContactHandle>>(colors[c]!, "Contacts"), Is.EqualTo(expected[c]),
                "Periodic locality refresh must stably sort retained membership by physical body slots.");
        Assert.That(ActiveHandles(world), Is.EqualTo(input));
    }

    [Test]
    public void SharedReadOnlyFloor_GroupsByDynamicBodyAndKeepsBucketOrder()
    {
        using var world = CreateWorld();
        var floor = world.CreateRigidBody();
        floor.MotionType = MotionType.Static;
        var bodies = Enumerable.Range(0, 385).Select(_ => world.CreateRigidBody()).ToArray();
        var storage = Read<PartitionedBuffer<RigidBodyData>>(world, "memRigidBodies");
        // Give the floor the smallest physical index. Including it in the key would
        // collapse every contact into the first bucket and leave this permutation intact.
        storage.Swap(storage.GetIndex(floor.Handle), 0);
        for (int i = 0; i < bodies.Length; i++)
        {
            int index = i * 73 % bodies.Length;
            AddContact(world, floor, bodies[index]);
        }
        ContactHandle[] input = ActiveHandles(world);
        Assert.That(input.Select(h => storage.GetIndex(h.Data.Body2) >> BucketShift).Distinct().Count(),
            Is.GreaterThan(2));
        AssertColoredOrder(world);
        var color = Read<IList>(world, "contactColors")[0]!;
        Assert.That(Read<List<ContactHandle>>(color, "Contacts"), Is.Not.EqualTo(input));
        Assert.That(Read<int>(world, "contactColorCount"), Is.EqualTo(1));
    }

    [Test]
    public void GlobalWriteMasksAndOverflow_RetainTheGreedyConflictGraph()
    {
        using var world = CreateWorld();
        var bodies = Enumerable.Range(0, 390).Select(_ => world.CreateRigidBody()).ToArray();
        // Interleave unrelated stars so each color contains distant memory regions.
        // A body read by one manifold but written by another still conflicts globally.
        for (int leaf = 0; leaf < 81; leaf++)
        for (int star = 3; star >= 0; star--)
        {
            var mode = leaf == 0 ? ContactData.SolveMode.FullBody2 : ContactData.SolveMode.Full;
            AddContact(world, bodies[star * 96], bodies[star * 96 + leaf + 1], mode);
        }
        AddContact(world, bodies[388], bodies[389], ContactData.SolveMode.None);
        AddContact(world, bodies[387], bodies[389]).Data.UsageMask = 0;
        AssertColoredOrder(world);
        Assert.That(Read<int>(world, "contactColorCount"), Is.EqualTo(64));
        Assert.That(Read<List<ContactHandle>>(world, "contactColorOverflow"), Has.Count.EqualTo(68));
    }

    [Test]
    public void BodyMovementAndRemoval_UseCurrentPhysicalSlots()
    {
        using var world = CreateWorld();
        var unused = Enumerable.Range(0, 130).Select(_ => world.CreateRigidBody()).ToArray();
        var bodies = Enumerable.Range(0, 385).Select(_ => world.CreateRigidBody()).ToArray();
        for (int i = 0; i < bodies.Length; i++) AddContact(world, world.NullBody, bodies[i]);
        AssertColoredOrder(world);
        foreach (var body in unused) world.Remove(body);
        var storage = Read<PartitionedBuffer<RigidBodyData>>(world, "memRigidBodies");
        for (int i = 0; i < bodies.Length / 2; i++)
            storage.Swap(storage.GetIndex(bodies[i].Handle), storage.GetIndex(bodies[^(i + 1)].Handle));
        storage.MoveToInactive(bodies[0].Handle);
        storage.MoveToActive(bodies[0].Handle);
        Assert.That(bodies.Any(body => (body.Handle.Data._index >> BucketShift) !=
                                      (storage.GetIndex(body.Handle) >> BucketShift)), Is.True,
            "The test must distinguish stable handle IDs from physical slots.");
        AssertRetainedOrderAfterRefresh(world);
    }

    [Test]
    public void GrowthTrimAndClear_RebuildWithoutStaleScratch()
    {
        using var world = CreateWorld();
        AssertColoredOrder(world);
        foreach (int count in new[] { 1, 130, 1025 })
        {
            var bodies = Enumerable.Range(0, count).Select(_ => world.CreateRigidBody()).ToArray();
            for (int i = count - 1; i >= 0; i--) AddContact(world, world.NullBody, bodies[i]);
            AssertColoredOrder(world);
            AssertColoredOrder(world);
        }
        world.Trim();
        AssertColoredOrder(world);
        world.Clear();
        AssertColoredOrder(world);
        world.Trim();
        AssertColoredOrder(world);
        AddContact(world, world.NullBody, world.CreateRigidBody());
        AssertColoredOrder(world);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void IncrementalPhysicalReordering_PreservesHandlesAcrossColoredSteps(bool multiThread)
    {
        const int starCount = 129, leavesPerStar = 9;
        var pool = Jitter2.Parallelization.ThreadPool.Instance;
        int previousThreadCount = pool.ThreadCount;
        pool.ChangeThreadCount(2);
        try
        {
            using var world = CreateWorld();
            world.SolverIterations = (24, 4);
            var hubs = new RigidBody[starCount];
            var leaves = new RigidBody[starCount, leavesPerStar];
            for (int star = 0; star < starCount; star++)
            {
                hubs[star] = world.CreateRigidBody();
                hubs[star].Damping = (0, 0);
                for (int leaf = 0; leaf < leavesPerStar; leaf++)
                {
                    var body = world.CreateRigidBody();
                    body.Damping = (0, 0);
                    body.Velocity = -JVector.UnitY;
                    leaves[star, leaf] = body;
                }
            }

            // Above ReorderContacts' 1024-contact threshold. Interleave independent
            // stars so the first visited manifold must relocate connected records.
            // Each color has 129 manifolds: two worker batches and a SIMD tail.
            for (int leaf = 0; leaf < leavesPerStar; leaf++)
            for (int i = 0; i < starCount; i++)
            {
                int star = i * 37 % starCount;
                AddContact(world, hubs[star], leaves[star, leaf]);
            }
            var handles = ActiveHandles(world);
            var identities = handles.Select(handle =>
                (handle.Data.Key, handle.Data.Body1, handle.Data.Body2)).ToArray();
            var contacts = Read<PartitionedBuffer<ContactData>>(world, "memContacts");
            var originalSlots = handles.Select(contacts.GetIndex).ToArray();

            for (int step = 0; step < 6; step++)
            {
                world.Step((Real)0.01, multiThread);
                Assert.That(ActiveHandles(world), Is.EquivalentTo(handles));
                for (int i = 0; i < handles.Length; i++)
                {
                    ref var contact = ref handles[i].Data;
                    if (!(contact.Key, contact.Body1, contact.Body2).Equals(identities[i]))
                        Assert.Fail($"Handle {i} changed its key/body identity after step {step}.");
                    if ((contact.UsageMask & ContactData.MaskContactAll) == 0)
                        Assert.Fail($"Handle {i} lost its contact points after step {step}.");
                }
                Assert.That(handles.Where((handle, i) => contacts.GetIndex(handle) != originalSlots[i]),
                    Is.Not.Empty, "Actual contact records must move; ordering the color lists alone is insufficient.");
            }

            AdaptiveContactGraphTests.Build(world);
            AdaptiveContactGraphTests.Inspect(world);
            for (int star = 0; star < starCount; star++)
            {
                Real momentum = hubs[star].Velocity.Y;
                Assert.That(hubs[star].Velocity.Y, Is.EqualTo((Real)(-0.9)).Within(0.002));
                for (int leaf = 0; leaf < leavesPerStar; leaf++)
                {
                    var body = leaves[star, leaf];
                    Assert.That(body.Velocity.Y, Is.EqualTo((Real)(-0.9)).Within(0.002));
                    Assert.That(Real.IsFinite(body.Velocity.LengthSquared()), Is.True);
                    momentum += body.Velocity.Y;
                }
                Assert.That(momentum, Is.EqualTo((Real)(-leavesPerStar)).Within(0.0001));
            }
        }
        finally
        {
            pool.ChangeThreadCount(previousThreadCount);
            pool.PauseWorkers();
        }
    }
}
