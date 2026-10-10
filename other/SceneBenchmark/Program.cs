using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using Jitter2;
using Jitter2.Collision;
using SceneBenchmark;
using PhysicsThreads = Jitter2.Parallelization.ThreadPool;

if (args.Length != 9)
    throw new ArgumentException("scene threads warmupSteps steps dt iterations relaxations substeps output.json");
string sceneName = args[0];
int threads = int.Parse(args[1]), warmup = int.Parse(args[2]), steps = int.Parse(args[3]);
float dt = float.Parse(args[4], CultureInfo.InvariantCulture);
int iterations = int.Parse(args[5]), relaxations = int.Parse(args[6]), substeps = int.Parse(args[7]);
if (threads < 1 || warmup < 0 || steps < 1 || !float.IsFinite(dt) || dt <= 0 || dt > 1f / 60 ||
    iterations < 1 || relaxations < 0 || substeps < 1) throw new ArgumentException("Invalid run settings.");
if (File.Exists(args[8])) throw new IOException("Refusing to overwrite trial data.");
bool multiThread = threads > 1;
PhysicsThreads.Instance.ChangeThreadCount(threads);

try
{
    // Warm the same code paths, then measure a fresh scene from simulation time zero.
    if (warmup > 0)
    {
        using var warmScene = new Scene(sceneName, iterations, relaxations, substeps);
        for (int i = 0; i < warmup; i++)
        {
            warmScene.BeforeStep();
            warmScene.World.Step(dt, multiThread);
        }
    }
    using var scene = new Scene(sceneName, iterations, relaxations, substeps);
    var world = scene.World;
    var initial = world.RawData;
    int bodyCount = initial.RigidBodies.Length;
    int constraintCount = initial.Constraints.Length + initial.SmallConstraints.Length;
    var stepMs = new double[steps];
    var activeBodies = new int[steps];
    var contacts = new int[steps];
    var activeConstraints = new int[steps];
    var candidatePairs = new int[steps];
    var unmanagedBytes = new long[steps];
    var anchorRms = new double[steps];
    var anchorMax = new double[steps];
    var worldNames = Enum.GetNames<World.Timings>().Where(n => n != "Last").ToArray();
    var treeNames = Enum.GetNames<DynamicTree.Timings>().Where(n => n != "Last").ToArray();
    var worldStages = worldNames.Select(_ => new double[steps]).ToArray();
    var treeStages = treeNames.Select(_ => new double[steps]).ToArray();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    int[] gcBefore = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
    long allocatedBefore = GC.GetTotalAllocatedBytes(true);
    (double rms, double max) error = (0, 0);
    for (int i = 0; i < steps; i++)
    {
        scene.BeforeStep();
        long start = Stopwatch.GetTimestamp();
        world.Step(dt, multiThread);
        stepMs[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        // Preallocated observations, outside the timed region; serialize after measurement.
        var data = world.RawData;
        activeBodies[i] = data.ActiveRigidBodies.Length;
        contacts[i] = data.ActiveContacts.Length;
        activeConstraints[i] = data.ActiveConstraints.Length + data.ActiveSmallConstraints.Length;
        candidatePairs[i] = world.DynamicTree.HashSetInfo.Count;
        unmanagedBytes[i] = data.TotalBytesAllocated;
        for (int j = 0; j < worldStages.Length; j++) worldStages[j][i] = world.DebugTimings[j];
        for (int j = 0; j < treeStages.Length; j++) treeStages[j][i] = world.DynamicTree.DebugTimings[j];
        if (i % 10 == 0 || i == steps - 1) error = scene.AnchorError();
        anchorRms[i] = error.rms;
        anchorMax[i] = error.max;
    }
    long allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
    int[] collections = [GC.CollectionCount(0) - gcBefore[0], GC.CollectionCount(1) - gcBefore[1],
        GC.CollectionCount(2) - gcBefore[2]];
    PhysicsThreads.Instance.PauseWorkers();
    foreach (ref var body in world.RawData.RigidBodies)
    {
        if (!float.IsFinite(body.Position.LengthSquared()) || !float.IsFinite(body.Velocity.LengthSquared()) ||
            !float.IsFinite(body.AngularVelocity.LengthSquared()) || !float.IsFinite(body.Orientation.X) ||
            !float.IsFinite(body.Orientation.Y) || !float.IsFinite(body.Orientation.Z) ||
            !float.IsFinite(body.Orientation.W)) throw new InvalidOperationException("Nonfinite body state.");
    }
    if (world.RawData.RigidBodies.Length != bodyCount || activeBodies.Any(n => n != activeBodies[0]))
        throw new InvalidOperationException("Body count changed with sleeping disabled.");
    var result = new
    {
        schemaVersion = 1, scene = sceneName, threads, multiThread, warmupSteps = warmup,
        measuredSteps = steps, dt, iterations, relaxations, substeps, bodyCount, constraintCount,
        anchorCount = scene.Anchors.Count, precision = "single", finiteFinalState = true,
        runtime = RuntimeInformation.FrameworkDescription, serverGC = GCSettings.IsServerGC,
        allocatedBytes = allocated, gcCollections = collections,
        managedHeapBytes = GC.GetTotalMemory(false), peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64,
        stepMs, activeBodies, contacts, activeConstraints, candidatePairs, unmanagedBytes, anchorRms, anchorMax,
        stagesMs = worldNames.Select((name, i) => (name, values: worldStages[i])).ToDictionary(p => p.name, p => p.values),
        treeStagesMs = treeNames.Select((name, i) => (name, values: treeStages[i])).ToDictionary(p => p.name, p => p.values)
    };
    using var output = new FileStream(args[8], FileMode.CreateNew);
    JsonSerializer.Serialize(output, result);
    Console.WriteLine($"{sceneName}, {threads} threads: {stepMs.Average():F3} ms/step; " +
        $"{bodyCount} bodies, {constraintCount} constraints; GC {string.Join('/', collections)}");
}
finally
{
    PhysicsThreads.Instance.PauseWorkers();
}
