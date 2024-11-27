namespace JitterTests;

public class StackingTests
{
    private World world = null!;

    [SetUp]
    public void Setup()
    {
        world = new World()
        {
            AllowDeactivation = false
        };
    }

    [TearDown]
    public void TearDown()
    {
        world.Dispose();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void SimpleStack(bool fullEPA)
    {
        world.SolverIterations = (4, 4);

        RigidBody last = Helper.BuildSimpleStack(world);

        world.UseFullEPASolver = fullEPA;

        Real stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, (Real)(1.0 / 100.0), true);
        Real delta = MathR.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1));
    }

    [TestCase(0, 0, 0, true)]
    [TestCase(0, 0, 0, false)]
    [TestCase(1, 0, 0, true)]
    public void PyramidStack(int x, int y, int z, bool multiThread)
    {
        world.SolverIterations = (4, 4);

        RigidBody last = Helper.BuildPyramidBox(world, new JVector(x, y, z));

        Real stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, (Real)(1.0 / 100.0), multiThread);
        Real delta = MathR.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1f));
    }

    [TestCase(0, 0, 0, true, true)]
    [TestCase(0, 0, 0, false, false)]
    [TestCase(0, 0, 0, false, true)]
    [TestCase(1000, 1000, 1000, false, true)]
    public void PyramidStackCylinder(int x, int y, int z, bool fullEPA, bool multiThread)
    {
        world.UseFullEPASolver = fullEPA;
        world.SolverIterations = (4, 4);

        RigidBody last = Helper.BuildPyramidCylinder(world, new JVector(x, y, z));

        Real stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, (Real)(1.0 / 100.0), multiThread);
        Real delta = MathR.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1f));
    }

    [TestCase(false, true)]
    [TestCase(true, true)]
    public void TowerStack(bool fullEPA, bool multiThread)
    {
        world.UseFullEPASolver = fullEPA;
        world.SolverIterations = (14, 4);

        RigidBody last = Helper.BuildTower(world, JVector.Zero, 30);

        Real stackHeight = last.Position.Y;
        Helper.AdvanceWorld(world, 10, (Real)(1.0 / 100.0), multiThread);
        Real delta = MathR.Abs(stackHeight - last.Position.Y);

        Assert.That(delta, Is.LessThan(1f));
    }
}