using JitterTests;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

BenchmarkRunner.Run<TowerStack>();

public class TowerStack
{
    private World world = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        world = new World(10000, 10000, 10000);
        ThreadPool.Instance.ChangeThreadCount(4);
        world.AllowDeactivation = false;
    }

    [Benchmark]
    public void Test()
    {
        world.SolverIterations = 18;

        Helper.BuildTower(world, JVector.Zero, 30);
        Helper.AdvanceWorld(world, 10, 1.0f / 100.0f, false);

        world.Clear();
    }
}