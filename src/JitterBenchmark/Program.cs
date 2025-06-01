using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using JitterTests;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

BenchmarkRunner.Run<TowerStack>();

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.HostProcess, launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class TowerStack
{
    private World world = null!;

    [Params(2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 36, 38, 40)]
    public int ThreadCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        world = new World();
        ThreadPool.Instance.ChangeThreadCount(ThreadCount);
        world.AllowDeactivation = false;
    }

    [Benchmark]
    public void Test()
    {
        world.SolverIterations = (14, 4);
        world.SubstepCount = 4;

        Helper.BuildTower(world, JVector.Zero, 400);
        Helper.AdvanceWorld(world, 20, 1.0f / 100.0f, true);

        world.Clear();
    }
}