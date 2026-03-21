namespace JitterTests.Api;

/// <summary>
/// Tests for shape management on a RigidBody (add, remove, clear, mass updates).
/// </summary>
public class ShapeTests
{
    // -------------------------------------------------------------------------
    // Adding shapes
    // -------------------------------------------------------------------------

    [TestCase]
    public void AddShape_AppearsInShapes()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var shape = new SphereShape(1);
        body.AddShape(shape);
        Assert.That(body.Shapes, Does.Contain(shape));
        world.Dispose();
    }

    [TestCase]
    public void AddShape_UpdatesMass()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        Assert.That(body.Mass, Is.GreaterThan(0));
        world.Dispose();
    }

    [TestCase]
    public void AddShape_Preserve_DoesNotChangeMass()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var massBeforeAddition = body.Mass;
        body.AddShape(new SphereShape(1), MassInertiaUpdateMode.Preserve);
        Assert.That(body.Mass, Is.EqualTo(massBeforeAddition));
        world.Dispose();
    }

    [TestCase]
    public void AddShape_SetsMassAfterTwoShapes()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var massOneShape = body.Mass;
        body.AddShape(new SphereShape(1));
        Assert.That(body.Mass, Is.GreaterThan(massOneShape));
        world.Dispose();
    }

    [TestCase]
    public void AddShape_SetsRigidBodyReference()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var shape = new SphereShape(1);
        body.AddShape(shape);
        Assert.That(shape.RigidBody, Is.EqualTo(body));
        world.Dispose();
    }

    [TestCase]
    public void AddShapes_AllAppearInShapes()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var shapes = new RigidBodyShape[] { new SphereShape(1), new BoxShape(1), new CapsuleShape(0.5f, 1f) };
        body.AddShapes(shapes);
        foreach (var shape in shapes)
            Assert.That(body.Shapes, Does.Contain(shape));
        world.Dispose();
    }

    [TestCase]
    public void AddShape_Null_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentNullException>(() => body.AddShape(null!));
        world.Dispose();
    }

    [TestCase]
    public void AddShapes_NullEnumerable_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentNullException>(() => body.AddShapes(null!));
        world.Dispose();
    }

    [TestCase]
    public void AddShapes_NullEntry_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentNullException>(() => body.AddShapes(new RigidBodyShape[] { new SphereShape(1), null! }));
        world.Dispose();
    }

    [TestCase]
    public void AddShape_SameShapeTwice_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var shape = new SphereShape(1);
        body.AddShape(shape);
        Assert.Throws<ArgumentException>(() => body.AddShape(shape));
        world.Dispose();
    }

    [TestCase]
    public void AddShape_FromAnotherBody_Throws()
    {
        var world = new World();
        var first = world.CreateRigidBody();
        var second = world.CreateRigidBody();
        var shape = new SphereShape(1);
        first.AddShape(shape);
        Assert.Throws<ArgumentException>(() => second.AddShape(shape));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Removing shapes
    // -------------------------------------------------------------------------

    [TestCase]
    public void RemoveShape_DisappearsFromShapes()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var shape = new SphereShape(1);
        body.AddShape(shape);
        body.RemoveShape(shape);
        Assert.That(body.Shapes, Does.Not.Contain(shape));
        world.Dispose();
    }

    [TestCase]
    public void RemoveShape_UpdatesMass()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.AddShape(new SphereShape(1));
        var massTwoShapes = body.Mass;
        var shape = body.Shapes[0];
        body.RemoveShape(shape);
        Assert.That(body.Mass, Is.LessThan(massTwoShapes));
        world.Dispose();
    }

    [TestCase]
    public void RemoveShape_Preserve_DoesNotChangeMass()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.AddShape(new SphereShape(1));
        var massBeforeRemoval = body.Mass;
        var shape = body.Shapes[0];
        body.RemoveShape(shape, MassInertiaUpdateMode.Preserve);
        Assert.That(body.Mass, Is.EqualTo(massBeforeRemoval));
        world.Dispose();
    }

    [TestCase]
    public void RemoveShapes_AllDisappearFromShapes()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var s1 = new SphereShape(1);
        var s2 = new BoxShape(1);
        body.AddShapes(new RigidBodyShape[] { s1, s2 });
        body.RemoveShapes(new RigidBodyShape[] { s1, s2 });
        Assert.That(body.Shapes, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void RemoveShape_Null_Throws()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.Throws<ArgumentNullException>(() => body.RemoveShape(null!));
        world.Dispose();
    }

    [TestCase]
    public void RemoveShape_ForeignShape_Throws()
    {
        var world = new World();
        var first = world.CreateRigidBody();
        var second = world.CreateRigidBody();
        var shape = new SphereShape(1);
        first.AddShape(shape);
        Assert.Throws<ArgumentException>(() => second.RemoveShape(shape));
        world.Dispose();
    }

    [TestCase]
    public void RemoveShapes_WithForeignShape_Throws()
    {
        var world = new World();
        var first = world.CreateRigidBody();
        var second = world.CreateRigidBody();
        var s1 = new SphereShape(1);
        var s2 = new SphereShape(1);
        first.AddShape(s1);
        second.AddShape(s2);
        Assert.Throws<ArgumentException>(() => first.RemoveShapes(new[] { s1, s2 }));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // ClearShapes
    // -------------------------------------------------------------------------

    [TestCase]
    public void ClearShapes_RemovesAll()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.AddShape(new BoxShape(1));
        body.AddShape(new CapsuleShape(0.5f, 1f));
        body.ClearShapes();
        Assert.That(body.Shapes, Is.Empty);
        world.Dispose();
    }

    [TestCase]
    public void ClearShapes_LastShape_ResetsMassToDefault()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.ClearShapes();
        Assert.That(body.Mass, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        Assert.That(body.InverseInertia.M11, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        Assert.That(body.InverseInertia.M22, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        Assert.That(body.InverseInertia.M33, Is.EqualTo((Real)1.0).Within((Real)1e-6));
        world.Dispose();
    }

    [TestCase]
    public void ClearShapes_OnEmptyBody_IsNoop()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        Assert.DoesNotThrow(() => body.ClearShapes());
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Shape count
    // -------------------------------------------------------------------------

    [TestCase]
    public void Shapes_CountMatchesAdded()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        for (int i = 0; i < 5; i++)
            body.AddShape(new SphereShape(1));
        Assert.That(body.Shapes.Count, Is.EqualTo(5));
        world.Dispose();
    }
}
