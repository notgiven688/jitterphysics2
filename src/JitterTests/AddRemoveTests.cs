namespace JitterTests;

public class AddRemoveTests
{
    private World world = null!;

    [SetUp]
    public void Setup()
    {
        world = new World(100, 100, 100);
    }

    private class FilterOut(IDynamicTreeProxy shape) : IBroadPhaseFilter
    {
        public bool Filter(IDynamicTreeProxy shapeA, IDynamicTreeProxy shapeB)
        {
            return shapeA != shape && shapeB != shape;
        }
    }

    [TestCase]
    public void RemoveStaticShape1()
    {
        Shape staticShape = new BoxShape(1000);
        world.DynamicTree.AddProxy(staticShape);

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape());

        world.BroadPhaseFilter = new FilterOut(staticShape);

        world.Step(0.01f);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);

        world.Clear();
        Assert.That(world.DynamicTree.PotentialPairs.Count == 0);
    }

    [TestCase]
    public void RemoveStaticShape0()
    {
        var staticShape = new BoxShape(1000);
        world.DynamicTree.AddProxy(staticShape);

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape());

        world.BroadPhaseFilter = new FilterOut(staticShape);

        world.Step(0.01f);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);

        world.Remove(body);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 0);
    }

    [TestCase]
    public void AddRemoveBodies()
    {
        var bA = world.CreateRigidBody();
        bA.AddShape(new SphereShape());
        var bB = world.CreateRigidBody();
        bB.AddShape(new SphereShape());
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);
        var bC = world.CreateRigidBody();
        bC.AddShape(new SphereShape());
        Assert.That(world.DynamicTree.PotentialPairs.Count == 3);
        var bD = world.CreateRigidBody();
        bD.AddShape(new SphereShape());
        bD.AddShape(new SphereShape());
        Assert.That(world.DynamicTree.PotentialPairs.Count == 9);
        world.Step(1e-12f);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 9);
        world.Remove(bB);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 5);
        world.Step(1e-12f);
        bD.RemoveShape(bD.Shapes[0]);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 3);
        world.Step(1e-12f);
        world.Remove(bD);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);
        world.Step(1e-12f);
        world.NullBody.AddShape(new SphereShape());
        world.Step(1e-12f);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 3);
        world.Step(1e-12f);
        world.Remove(world.NullBody);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);
        world.Step(1e-12f);
    }
}