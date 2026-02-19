using Jitter2.DataStructures;

namespace JitterTests;

public class ReproducibilityTest
{
    [TestCase]
    public static void BasicReproducibilityTest()
    {
        var worldA = new World();
        worldA.SolverIterations = (2, 2);
        var last = Helper.BuildSimpleStack(worldA);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);
        worldA.Remove(last);
        Helper.BuildSimpleStack(worldA);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);
        worldA.Clear();
        last = Helper.BuildSimpleStack(worldA);
        last.Velocity = new JVector(10);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);

        var worldB = new World();
        worldB.SolverIterations = (2, 2);
        last = Helper.BuildSimpleStack(worldB);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);
        worldB.Remove(last);
        Helper.BuildSimpleStack(worldB);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);
        worldB.Clear();
        last = Helper.BuildSimpleStack(worldB);
        last.Velocity = new JVector(10);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);

        var positionsA = worldA.RigidBodies.Select(body => body.Position);
        var positionsB = worldB.RigidBodies.Select(body => body.Position);

        Assert.That(positionsA.SequenceEqual(positionsB));

        worldA.Dispose();
        worldB.Dispose();
    }
}