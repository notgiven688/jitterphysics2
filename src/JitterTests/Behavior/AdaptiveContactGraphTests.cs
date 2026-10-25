using System.Reflection;
using Jitter2.Unmanaged;
using ContactHandle = Jitter2.Unmanaged.JHandle<Jitter2.Dynamics.ContactData>;

namespace JitterTests.Behavior;

[TestFixture, NonParallelizable]
public class AdaptiveContactGraphTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static World NewWorld() => new() { SolveMode = SolveMode.Regular,
        Gravity = JVector.Zero, AllowDeactivation = false, SolverIterations = (8, 4) };
    private static T Read<T>(object o, string name) => (T)o.GetType().GetField(name, Fields)!.GetValue(o)!;
    private static int Count(World world, string name) => Read<int>(world, name);
    internal static void Build(World world) => typeof(World).GetMethod("BuildContactColors", Fields)!.Invoke(world, null);

    private static Arbiter Add(World world, ulong id, RigidBody first, RigidBody second,
        ContactData.SolveMode remove = ContactData.SolveMode.None)
    {
        world.GetOrCreateArbiter(id, id + 1, first, second, out var arbiter);
        world.RegisterContact(arbiter, JVector.Zero, JVector.Zero, JVector.UnitY, remove);
        return arbiter;
    }

    // Independent set oracle plus cached-record consistency. Conservative writable
    // flags are allowed, but body reference counts and occupied bits must be exact.
    internal static Dictionary<ContactHandle, int> Inspect(World world)
    {
        var memory = Read<PartitionedBuffer<ContactData>>(world, "memContacts");
        var active = new HashSet<ContactHandle>();
        var writers = new HashSet<JHandle<RigidBodyData>>();
        foreach (ref var contact in world.RawData.ActiveContacts)
        {
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0) continue;
            active.Add(memory.GetHandle(ref contact));
            if ((contact.Mode & ContactData.SolveMode.FullBody1) != 0) writers.Add(contact.Body1);
            if ((contact.Mode & ContactData.SolveMode.FullBody2) != 0) writers.Add(contact.Body2);
        }
        var graph = Read<Array>(world, "contactGraph");
        var masks = Read<ulong[]>(world, "bodyContactColors");
        var writable = Read<bool[]>(world, "contactBodyWritable");
        var references = Read<int[]>(world, "contactBodyReferences");
        var expectedMasks = new ulong[masks.Length];
        var expectedReferences = new int[references.Length];
        foreach (var handle in active)
        {
            expectedReferences[handle.Data.Body1.Data._index]++;
            expectedReferences[handle.Data.Body2.Data._index]++;
        }
        var result = new Dictionary<ContactHandle, int>();
        var segmentStart = Read<int[]>(world, "segmentStart");
        int count = Read<int>(world, "contactColorCount");
        var records = world.RawData.ActiveContacts;
        Assert.That(segmentStart[0], Is.Zero);
        for (int segment = 0; segment < segmentStart.Length; segment++)
        {
            int start = segmentStart[segment];
            int end = segment + 1 < segmentStart.Length ? segmentStart[segment + 1] : records.Length;
            Assert.That(end, Is.GreaterThanOrEqualTo(start), "Segment bounds must be ordered.");
            // Storage holds colors in reverse order, then the overflow, then uncolored records.
            int colorIndex = segment == 65 ? -1 : segment == 64 ? 64 : 63 - segment;
            if (colorIndex >= count && colorIndex < 64)
                Assert.That(end - start, Is.Zero, $"Color {colorIndex} lies beyond the color count but is not empty.");
            var occupied = new HashSet<JHandle<RigidBodyData>>();
            for (int i = start; i < end; i++)
            {
                var handle = memory.GetHandle(ref records[i]);
                if (colorIndex < 0)
                {
                    Assert.That(active.Contains(handle), Is.False, "A used contact is stored outside its color segment.");
                    if (handle.Data._internal < graph.Length)
                        Assert.That(Read<ContactHandle>(graph.GetValue(handle.Data._internal)!, "Handle").IsZero, Is.True,
                            "An uncolored record retains a graph entry.");
                    continue;
                }
                Assert.That(active.Contains(handle), Is.True,
                    $"Color segment {segment} holds an inactive/empty contact at index {i}.");
                Assert.That(result.TryAdd(handle, colorIndex), Is.True, "Contact appears twice in graph.");
                object entry = graph.GetValue(handle.Data._internal)!;
                Assert.That(Read<ContactHandle>(entry, "Handle"), Is.EqualTo(handle));
                Assert.That(Read<int>(entry, "Color"), Is.EqualTo(colorIndex));
                Assert.That(Read<ContactData.SolveMode>(entry, "Mode"), Is.EqualTo(handle.Data.Mode));
                Assert.That(Read<JHandle<RigidBodyData>>(entry, "Body1"), Is.EqualTo(handle.Data.Body1));
                Assert.That(Read<JHandle<RigidBodyData>>(entry, "Body2"), Is.EqualTo(handle.Data.Body2));
                Assert.That(Read<int>(entry, "BodyId1"), Is.EqualTo(handle.Data.Body1.Data._index));
                Assert.That(Read<int>(entry, "BodyId2"), Is.EqualTo(handle.Data.Body2.Data._index));
                if (colorIndex == 64) continue;
                foreach (var body in new[] { handle.Data.Body1, handle.Data.Body2 })
                {
                    if (writers.Contains(body)) Assert.That(occupied.Add(body), Is.True, "Global writable endpoint conflicts within a color.");
                    if (writable[body.Data._index]) expectedMasks[body.Data._index] |= 1UL << colorIndex;
                }
            }
        }
        Assert.That(result.Keys, Is.EquivalentTo(active), "Graph does not exactly cover active used contacts.");
        foreach (object entry in graph)
        {
            var handle = Read<ContactHandle>(entry, "Handle");
            if (!handle.IsZero) Assert.That(active.Contains(handle), Is.True, "Side cache retained a removed/sleeping contact.");
        }
        Assert.That(references, Is.EqualTo(expectedReferences), "Stable-body reference counts are stale.");
        Assert.That(masks, Is.EqualTo(expectedMasks), "Stable-body color occupancy is stale.");
        foreach (ref var body in world.RawData.RigidBodies)
            Assert.That(Real.IsFinite(body.Position.LengthSquared()) && Real.IsFinite(body.Velocity.LengthSquared()) &&
                Real.IsFinite(body.AngularVelocity.LengthSquared()), Is.True, "Body became non-finite.");
        return result;
    }

    [Test]
    public void UnchangedGraph_ReusesAssignmentsAndStableBodyMasks()
    {
        using var world = NewWorld();
        var pool = Jitter2.Parallelization.ThreadPool.Instance;
        int previous = pool.ThreadCount;
        pool.ChangeThreadCount(2);
        try
        {
            var hubs = Enumerable.Range(0, 129).Select(_ => world.CreateRigidBody()).ToArray();
            ulong id = 10000;
            for (int edge = 0; edge < 3; edge++)
            for (int i = 0; i < hubs.Length; i++) Add(world, id += 2, hubs[(i * 37) % hubs.Length], world.CreateRigidBody());
            world.Step((Real).01, false);
            var original = Inspect(world);
            Assert.That(original.Count, Is.EqualTo(387));
            Assert.That(original.Values.Distinct().Count(), Is.EqualTo(3));
            int rebuilds = Count(world, "contactGraphRebuildCount"), reuses = Count(world, "contactGraphReuseCount");
            // Stable IDs survive deliberate physical storage swaps too.
            var bodies = Read<PartitionedBuffer<RigidBodyData>>(world, "memRigidBodies");
            bodies.Swap(bodies.GetIndex(hubs[0].Handle), bodies.GetIndex(hubs[^1].Handle));
            for (int i = 0; i < 8; i++)
            {
                world.Step((Real).01, i % 2 == 1);
                var current = Inspect(world);
                foreach (var pair in original) Assert.That(current[pair.Key], Is.EqualTo(pair.Value));
            }
            Assert.That(Count(world, "contactGraphRebuildCount"), Is.EqualTo(rebuilds));
            Assert.That(Count(world, "contactGraphReuseCount"), Is.GreaterThanOrEqualTo(reuses + 8));
            TestContext.Out.WriteLine($"Unchanged graph: 387 contacts, 3 colors, {Count(world, "contactGraphReuseCount") - reuses} reuses, no new rebuilds.");
        }
        finally { pool.ChangeThreadCount(previous); pool.PauseWorkers(); }
    }

    [Test]
    public void HighContactChurn_UpdatesColorsWithoutRebuildingTheGraph()
    {
        using var world = NewWorld();
        var bodies = Enumerable.Range(0, 300).Select(_ => world.CreateRigidBody()).ToArray();
        ulong id = 40000;
        foreach (var body in bodies) Add(world, id += 2, world.NullBody, body);
        Build(world);
        Inspect(world);
        int rebuilds = Count(world, "contactGraphRebuildCount");
        int increments = Count(world, "contactGraphIncrementalCount");

        for (int i = 0; i < 150; i++)
        {
            world.Remove(bodies[i]);
            bodies[i] = world.CreateRigidBody();
            Add(world, id += 2, world.NullBody, bodies[i]);
        }

        Build(world);
        Assert.That(Inspect(world).Count, Is.EqualTo(300));
        Assert.That(Count(world, "contactGraphRebuildCount"), Is.EqualTo(rebuilds));
        Assert.That(Count(world, "contactGraphIncrementalCount"), Is.EqualTo(increments + 1));
    }

    [Test]
    public void EqualCountChurn_SleepWakeAndSolverSwitch_DoNotRetainGhostEdges()
    {
        using var world = NewWorld();
        var bodies = Enumerable.Range(0, 129).Select(_ => world.CreateRigidBody()).ToList();
        ulong id = 20000;
        foreach (var body in bodies) Add(world, id += 2, world.NullBody, body);
        world.Step((Real).01, false);
        Inspect(world);
        int increments = Count(world, "contactGraphIncrementalCount");
        world.AllowDeactivation = true;
        world.ForceSleepIsland(bodies[0].Island);
        world.Step((Real).01, false);
        int sleepingCount = Inspect(world).Count;
        world.ForceSleepIsland(bodies[1].Island);
        bodies[0].SetActivationState(true);
        world.Step((Real).01, false);
        Assert.That(Inspect(world).Count, Is.EqualTo(sleepingCount));
        Assert.That(bodies[0].IsActive && !bodies[1].IsActive, Is.True);

        // Same contact/body count, different identities; allocators can reuse IDs.
        for (int i = 2; i < 10; i++)
        {
            int count = Inspect(world).Count;
            world.Remove(bodies[i]);
            bodies[i] = world.CreateRigidBody();
            Add(world, id += 2, world.NullBody, bodies[i]);
            world.Step((Real).01, false);
            Assert.That(Inspect(world).Count, Is.EqualTo(count));
        }
        Assert.That(Count(world, "contactGraphIncrementalCount"), Is.GreaterThan(increments));
        world.SolveMode = SolveMode.Deterministic;
        world.Step((Real).01, false);
        world.SolveMode = SolveMode.Regular;
        world.Step((Real).01, false);
        Inspect(world);
        world.Trim();
        world.Stabilize((Real).01, 2, 1, false);
        Inspect(world);
        world.Clear();
        world.Stabilize((Real).01, 1, 0, false);
        Assert.That(Inspect(world), Is.Empty);
        Add(world, id += 2, world.NullBody, world.CreateRigidBody());
        world.Step((Real).01, false);
        Assert.That(Inspect(world).Count, Is.EqualTo(1));
    }

    [Test]
    public void PreSubStepRegistrations_ReclassifySharedReadersAndRefillEmptiedManifolds()
    {
        using var world = NewWorld();
        var hub = world.CreateRigidBody();
        var leaves = Enumerable.Range(0, 17).Select(_ => world.CreateRigidBody()).ToArray();
        var arbiters = leaves.Select((body, i) => Add(world, (ulong)(30000 + i * 2), hub, body,
            ContactData.SolveMode.FullBody1)).ToArray();
        world.Step((Real).01, false);
        Assert.That(Inspect(world).Values.Distinct().Count(), Is.EqualTo(1), "Read-only hub should be shareable.");
        int rebuilds = Count(world, "contactGraphRebuildCount");
        int stage = 0, observations = 0;
        world.SubstepCount = 3;
        World.WorldStep before = _ =>
        {
            // A registration without remove flags promotes the hub to a writer of this manifold.
            if (stage == 0) world.RegisterContact(arbiters[0], JVector.Zero, JVector.Zero, JVector.UnitY);
            // Discarding a body's contact cache empties its manifolds through the engine API.
            if (stage == 1) leaves[1].ClearContactCache();
            // Refilling the emptied manifold returns it to the graph as a shared reader.
            if (stage == 2) world.RegisterContact(arbiters[1], JVector.Zero, JVector.Zero, JVector.UnitY,
                ContactData.SolveMode.FullBody1);
            stage++;
        };
        World.WorldStep after = _ =>
        {
            var graph = Inspect(world);
            int expected = stage == 2 ? 16 : 17;
            Assert.That(graph.Count, Is.EqualTo(expected));
            Assert.That(graph.Values.Distinct().Count(), Is.EqualTo(expected), "All shared-hub readers must exclude its writer in the same substep.");
            observations++;
        };
        world.PreSubStep += before;
        world.PostSubStep += after;
        world.Step((Real).01, false);
        world.PreSubStep -= before;
        world.PostSubStep -= after;
        Assert.That(observations, Is.EqualTo(3));
        Assert.That(Count(world, "contactGraphRebuildCount"), Is.GreaterThan(rebuilds), "Read-to-write upgrade must rebuild old reader assignments.");

        // Removing the write mode again through a registration downgrades the hub for this manifold.
        world.RegisterContact(arbiters[0], JVector.Zero, JVector.Zero, JVector.UnitY, ContactData.SolveMode.FullBody1);
        Build(world);
        Inspect(world); // Conservative reservations after the downgrade are allowed.
        world.SubstepCount = 1;
        world.Step((Real).01, false);
        Inspect(world);
    }
}
