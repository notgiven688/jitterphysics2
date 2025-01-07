namespace JitterTests;

public class AddRemoveTests
{
    private World world = null!;

    [SetUp]
    public void Setup()
    {
        world = new World();
    }

    [TearDown]
    public void TearDown()
    {
        world.Dispose();
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

        world.Step((Real)0.01);
        Assert.That(world.DynamicTree.HashSetInfo.Count == 1);

        world.Clear();
        Assert.That(world.DynamicTree.HashSetInfo.Count == 0);
    }

    [TestCase]
    public void RemoveStaticShape0()
    {
        var staticShape = new BoxShape(1000);
        world.DynamicTree.AddProxy(staticShape);

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape());

        world.BroadPhaseFilter = new FilterOut(staticShape);

        world.Step((Real)0.01);
        Assert.That(world.DynamicTree.HashSetInfo.Count == 1);

        world.Remove(body);
        Assert.That(world.DynamicTree.HashSetInfo.Count == 0);
    }

    [TestCase]
    public void AddRemoveBodies()
    {
        void TinyStep() => world.Step((Real)1e-12);

        var bA = world.CreateRigidBody();
        bA.AddShape(new SphereShape());
        var bB = world.CreateRigidBody();
        bB.AddShape(new SphereShape());
        Assert.That(world.DynamicTree.HashSetInfo.Count == 1);
        var bC = world.CreateRigidBody();
        bC.AddShape(new SphereShape());
        Assert.That(world.DynamicTree.HashSetInfo.Count == 3);
        var bD = world.CreateRigidBody();
        bD.AddShape(new SphereShape());
        bD.AddShape(new SphereShape());
        Assert.That(world.DynamicTree.HashSetInfo.Count == 9);
        TinyStep();
        Assert.That(world.DynamicTree.HashSetInfo.Count == 9);
        world.Remove(bB);
        Assert.That(world.DynamicTree.HashSetInfo.Count == 5);
        TinyStep();
        bD.RemoveShape(bD.Shapes[0]);
        Assert.That(world.DynamicTree.HashSetInfo.Count == 3);
        TinyStep();
        world.Remove(bD);
        Assert.That(world.DynamicTree.HashSetInfo.Count == 1);
        TinyStep();
        world.NullBody.AddShape(new SphereShape());
        TinyStep();
        Assert.That(world.DynamicTree.HashSetInfo.Count == 3);
        TinyStep();
        world.Remove(world.NullBody);
        Assert.That(world.DynamicTree.HashSetInfo.Count == 1);
        TinyStep();
    }
}