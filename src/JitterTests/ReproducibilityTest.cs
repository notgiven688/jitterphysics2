using Jitter2.DataStructures;

namespace JitterTests;

public class ReproducibilityTest
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase]
    public static void BasicReproducibilityTest()
    {
        var worldA = new World();
        worldA.SolverIterations = (2, 2);
        Helper.BuildSimpleStack(worldA);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);
        worldA.Remove(worldA.RigidBodies.Last());
        Helper.BuildSimpleStack(worldA);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);
        worldA.Clear();
        Helper.BuildSimpleStack(worldA);
        worldA.RigidBodies.Last().Velocity = new JVector(10);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);

        var worldB = new World();
        worldB.SolverIterations = (2, 2);
        Helper.BuildSimpleStack(worldB);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);
        worldB.Remove(worldB.RigidBodies.Last());
        Helper.BuildSimpleStack(worldB);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);
        worldB.Clear();
        Helper.BuildSimpleStack(worldB);
        worldB.RigidBodies.Last().Velocity = new JVector(10);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);

        var positionsA = worldA.RigidBodies.Select(body => body.Position);
        var positionsB = worldB.RigidBodies.Select(body => body.Position);

        Assert.That(positionsA.SequenceEqual(positionsB));

        worldA.Dispose();
        worldB.Dispose();
    }
}