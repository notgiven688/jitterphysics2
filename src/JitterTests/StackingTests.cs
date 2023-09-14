namespace JitterTests;

public class StackingTests
{
    private World world = null!;

    [SetUp]
    public void Setup()
    {
        world = new World(2000, 10000, 100)
        {
            AllowDeactivation = false
        };
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SimpleStack(bool fullEPA)
    {
        world.SolverIterations = 8;

        RigidBody last = Helper.BuildSimpleStack(world);

        world.UseFullEPASolver = fullEPA;

        float stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, 1.0f / 100.0f, true);
        float delta = Math.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1f));
    }

    [TestCase(0, 0, 0, true)]
    [TestCase(0, 0, 0, false)]
    [TestCase(1, 0, 0, true)]
    public void PyramidStack(int x, int y, int z, bool multiThread)
    {
        world.SolverIterations = 8;

        RigidBody last = Helper.BuildPyramidBox(world, new JVector(x, y, z));

        float stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, 1.0f / 100.0f, multiThread);
        float delta = Math.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1f));
    }

    [TestCase(0, 0, 0, true, true)]
    [TestCase(0, 0, 0, false, false)]
    [TestCase(0, 0, 0, false, true)]
    [TestCase(1000, 1000, 1000, false, true)]
    public void PyramidStackCylinder(int x, int y, int z, bool fullEPA, bool multiThread)
    {
        world.UseFullEPASolver = fullEPA;
        world.SolverIterations = 8;

        RigidBody last = Helper.BuildPyramidCylinder(world, new JVector(x, y, z));

        float stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, 1.0f / 100.0f, multiThread);
        float delta = Math.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1f));
    }

    [TestCase(false, true)]
    [TestCase(true, true)]
    public void TowerStack(bool fullEPA, bool multiThread)
    {
        world.UseFullEPASolver = fullEPA;
        world.SolverIterations = 18;

        RigidBody last = Helper.BuildTower(world, JVector.Zero, 30);

        float stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, 1.0f / 100.0f, multiThread);
        float delta = Math.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1f));
    }
}