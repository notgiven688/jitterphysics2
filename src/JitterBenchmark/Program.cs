using JitterTests;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

BenchmarkRunner.Run<TowerStack>();

public class TowerStack
{
    private World world = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        World.Capacity capacity = new World.Capacity
        {
            BodyCount = 10_000,
            ConstraintCount = 10_000,
            ContactCount = 10_000,
            SmallConstraintCount = 10_000
        };

        world = new World(capacity);
        ThreadPool.Instance.ChangeThreadCount(4);
        world.AllowDeactivation = false;
    }

    [Benchmark]
    public void Test()
    {
        world.SolverIterations = (14, 4);

        Helper.BuildTower(world, JVector.Zero, 30);
        Helper.AdvanceWorld(world, 10, 1.0 / 100.0, false);

        world.Clear();
    }
}