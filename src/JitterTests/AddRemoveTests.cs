namespace JitterTests;

public class AddRemoveTests
{
    private World world = null!;

    [SetUp]
    public void Setup()
    {
        world = new World(100, 100, 100);
    }

    class FilterOut : IBroadPhaseFilter
    {
        private readonly Shape shape;
        public FilterOut(Shape shape)
        {
            this.shape = shape;
        }

        public bool Filter(Shape shapeA, Shape shapeB)
        {
            return shapeA != shape && shapeB != shape;
        }
    }

    [TestCase]
    public void RemoveStaticShape1()
    {
        Shape staticShape = new BoxShape(1000);
        world.AddShape(staticShape);

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));

        world.BroadPhaseFilter = new FilterOut(staticShape);

        world.Step(0.01f);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);

        world.Clear();
        Assert.That(world.DynamicTree.PotentialPairs.Count == 0);
    }

    [TestCase]
    public void RemoveStaticShape0()
    {
        Shape staticShape = new BoxShape(1000);
        world.AddShape(staticShape);

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));

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
        bA.AddShape(new SphereShape(1));
        var bB = world.CreateRigidBody();
        bB.AddShape(new SphereShape(1));
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);
        var bC = world.CreateRigidBody();
        bC.AddShape(new SphereShape(1));
        Assert.That(world.DynamicTree.PotentialPairs.Count == 3);
        var bD = world.CreateRigidBody();
        bD.AddShape(new SphereShape(1));
        bD.AddShape(new SphereShape(1));
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
        world.NullBody.AddShape(new SphereShape(1));
        world.Step(1e-12f);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 3);
        world.Step(1e-12f);
        world.Remove(world.NullBody);
        Assert.That(world.DynamicTree.PotentialPairs.Count == 1);
        world.Step(1e-12f);
    }

    [TestCase]
    public void AddRemoveShapes()
    {
        RigidBody rb = world.CreateRigidBody();
        Shape sh1 = new SphereShape();
        Shape sh2 = new SphereShape();

        RigidBody rb2 = world.CreateRigidBody();
        Shape sh3 = new SphereShape();
        rb2.AddShape(sh3);

        // add shape to world
        world.AddShape(sh2);

        // adding the same shape again should fail
        Assert.That(() => world.AddShape(sh2), Throws.TypeOf<ArgumentException>());

        // adding the same shape to a body should fail
        Assert.That(() => rb.AddShape(sh2), Throws.TypeOf<ArgumentException>());

        // removing from body should fail
        Assert.That(() => rb.RemoveShape(sh2), Throws.TypeOf<ArgumentException>());

        // removing from world should work
        world.Remove(sh2);

        // add shape to rigid body
        rb.AddShape(sh2);

        // add shape to rigid body
        rb.AddShape(sh1);

        // contact is added to deferredArbiters
        world.Step(1.0f / 100, false);

        // adding again to rigid body should fail
        Assert.That(() => rb.AddShape(sh1), Throws.TypeOf<ArgumentException>());

        // adding to world should fail
        Assert.That(() => world.AddShape(sh1), Throws.TypeOf<ArgumentException>());

        // removing shape from world should fail, since it is owned by the body
        Assert.That(() => world.Remove(sh1), Throws.TypeOf<ArgumentException>());

        // removing the shape here should work.
        rb.RemoveShape(sh1);

        // there should be only one shape registered.
        Assert.That(rb.Shapes.Count, Is.EqualTo(1));

        Assert.That(world.Shapes.Count, Is.EqualTo(2));

        world.Clear();

        Assert.That(world.Shapes.Count, Is.EqualTo(0));
    }
}