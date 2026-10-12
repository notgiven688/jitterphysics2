using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using PhysicsThreads = Jitter2.Parallelization.ThreadPool;

// Positional arguments keep this standalone harness dependency-free.
if (args.Length > 5 || args.Any(a => a is "--help" or "-h"))
{
    Console.WriteLine("[regular|graphcolored|both] [ground|pile|both] [threads=4] [warmup=80] [steps=120]");
    return;
}

string modeArgument = args.ElementAtOrDefault(0)?.ToLowerInvariant() ?? "both";
string sceneArgument = args.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "both";
int threads = ParsePositive(args.ElementAtOrDefault(2), 4, "threads");
int warmupSteps = ParsePositive(args.ElementAtOrDefault(3), 80, "warmup");
int measuredSteps = ParsePositive(args.ElementAtOrDefault(4), 120, "steps");
SolveMode[] modes = modeArgument switch
{
    "regular" => [SolveMode.Regular],
    "graphcolored" => [SolveMode.GraphColored],
    "both" => [SolveMode.Regular, SolveMode.GraphColored],
    _ => throw new ArgumentException("Mode must be regular, graphcolored, or both.")
};
string[] scenes = sceneArgument switch
{
    "ground" => ["ground"],
    "pile" => ["pile"],
    "both" => ["ground", "pile"],
    _ => throw new ArgumentException("Scene must be ground, pile, or both.")
};

const float dt = 0.01f;
PhysicsThreads.Instance.ChangeThreadCount(threads);
Console.WriteLine($"# {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}; " +
    $"SIMD accelerated={Vector.IsHardwareAccelerated}; lanes={Vector<float>.Count}; " +
    $"dt={dt.ToString(CultureInfo.InvariantCulture)}; warmup={warmupSteps}; steps={measuredSteps}");
Console.WriteLine("mode,scene,threads,bodies,mean_contacts,step_ms,solve_ms,reorder_ms,allocated_bytes,gc0,gc1,gc2,min_y,max_y,finite");

try
{
    foreach (string scene in scenes)
    foreach (SolveMode mode in modes)
    {
        using World world = Build(scene, mode);
        for (int i = 0; i < warmupSteps; i++) world.Step(dt, threads > 1);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        long allocatedBefore = GC.GetTotalAllocatedBytes(true);
        long elapsedTicks = 0, contactSum = 0;
        double solveMilliseconds = 0, reorderMilliseconds = 0;

        for (int i = 0; i < measuredSteps; i++)
        {
            long start = Stopwatch.GetTimestamp();
            world.Step(dt, threads > 1);
            elapsedTicks += Stopwatch.GetTimestamp() - start;
            solveMilliseconds += world.DebugTimings[(int)World.Timings.Solve];
            reorderMilliseconds += world.DebugTimings[(int)World.Timings.ReorderContacts];
            contactSum += world.RawData.ActiveContacts.Length;
        }

        long allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        gc0 = GC.CollectionCount(0) - gc0;
        gc1 = GC.CollectionCount(1) - gc1;
        gc2 = GC.CollectionCount(2) - gc2;
        float minimumY = float.PositiveInfinity, maximumY = float.NegativeInfinity;
        int dynamicBodyCount = 0;
        foreach (ref var body in world.RawData.RigidBodies)
        {
            if (!float.IsFinite(body.Position.LengthSquared()) ||
                !float.IsFinite(body.Velocity.LengthSquared()) ||
                !float.IsFinite(body.AngularVelocity.LengthSquared()) ||
                !float.IsFinite(body.Orientation.X) || !float.IsFinite(body.Orientation.Y) ||
                !float.IsFinite(body.Orientation.Z) || !float.IsFinite(body.Orientation.W))
                throw new InvalidOperationException($"Non-finite final state for {mode}/{scene}.");
            if (body.MotionType != MotionType.Dynamic) continue;
            dynamicBodyCount++;
            minimumY = Math.Min(minimumY, body.Position.Y);
            maximumY = Math.Max(maximumY, body.Position.Y);
        }
        // The large floor encloses both scenes. A center below its top indicates a major failure.
        if (minimumY < 0)
            throw new InvalidOperationException($"Body fell through the floor for {mode}/{scene}: y={minimumY}.");

        double stepMilliseconds = elapsedTicks * 1000.0 / Stopwatch.Frequency / measuredSteps;
        Console.WriteLine(FormattableString.Invariant(
            $"{mode},{scene},{threads},{dynamicBodyCount},{(double)contactSum / measuredSteps:F1},{stepMilliseconds:F6},{solveMilliseconds / measuredSteps:F6},{reorderMilliseconds / measuredSteps:F6},{allocated},{gc0},{gc1},{gc2},{minimumY:F6},{maximumY:F6},true"));
    }
}
finally
{
    PhysicsThreads.Instance.PauseWorkers();
}

static int ParsePositive(string? value, int fallback, string name)
{
    int result = value == null ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    return result > 0 ? result : throw new ArgumentException($"{name} must be positive.");
}

static World Build(string scene, SolveMode mode)
{
    World world = new()
    {
        SolveMode = mode,
        AllowDeactivation = false,
        SolverIterations = (8, 4),
        SubstepCount = 1
    };
    var ground = world.CreateRigidBody();
    ground.AddShape(new BoxShape(100, 2, 100));
    ground.Position = new JVector(0, -1, 0);
    ground.MotionType = MotionType.Static;

    int width = scene == "ground" ? 32 : 8;
    int height = scene == "ground" ? 1 : 12;
    float spacing = scene == "ground" ? 1.5f : 1.0f;
    for (int y = 0; y < height; y++)
    for (int z = 0; z < width; z++)
    for (int x = 0; x < width; x++)
    {
        var body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        // Offset alternate layers so the pile bridges neighboring columns.
        float offset = (y & 1) * 0.25f;
        body.Position = new JVector((x - (width - 1) * 0.5f) * spacing + offset,
            0.495f + y * 0.99f, (z - (width - 1) * 0.5f) * spacing + offset);
    }
    return world;
}
