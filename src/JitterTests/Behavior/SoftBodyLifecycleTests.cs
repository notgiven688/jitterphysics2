using Jitter2.SoftBodies;

namespace JitterTests.Behavior;

public class SoftBodyLifecycleTests
{
    private static SoftBody CreateSoftBody(World world)
    {
        var softBody = new SoftBody(world);
        var v1 = world.CreateRigidBody();
        var v2 = world.CreateRigidBody();
        var v3 = world.CreateRigidBody();
        v1.Position = JVector.Zero;
        v2.Position = JVector.UnitX;
        v3.Position = JVector.UnitY;

        softBody.Vertices.AddRange([v1, v2, v3]);

        var spring = world.CreateConstraint<SpringConstraint>(v1, v2);
        spring.Initialize(v1.Position, v2.Position);
        softBody.AddSpring(spring);
        softBody.AddShape(new SoftBodyTriangle(softBody, v1, v2, v3));

        return softBody;
    }

    [Test]
    public void DestroyAfterWorldClear_IsSafeAndIdempotent()
    {
        using var world = new World();
        SoftBody softBody = CreateSoftBody(world);

        world.Clear();

        Assert.That(softBody.IsActive, Is.False);
        Assert.DoesNotThrow(softBody.Destroy);
        Assert.DoesNotThrow(softBody.Destroy);
        Assert.That(softBody.Vertices, Is.Empty);
        Assert.That(softBody.Springs, Is.Empty);
        Assert.That(softBody.Shapes, Is.Empty);
    }

    [Test]
    public void StepAfterWorldClear_CleansUpSoftBody()
    {
        using var world = new World();
        SoftBody softBody = CreateSoftBody(world);

        world.Clear();
        world.Step((Real)1.0 / 60, multiThread: false);

        Assert.That(softBody.Vertices, Is.Empty);
        Assert.That(softBody.Springs, Is.Empty);
        Assert.That(softBody.Shapes, Is.Empty);
    }

    [Test]
    public void DestroyAfterWorldDispose_ClearsReferences()
    {
        var world = new World();
        SoftBody softBody = CreateSoftBody(world);

        world.Dispose();

        Assert.That(softBody.IsActive, Is.False);
        Assert.DoesNotThrow(softBody.Destroy);
        Assert.That(softBody.Vertices, Is.Empty);
        Assert.That(softBody.Springs, Is.Empty);
        Assert.That(softBody.Shapes, Is.Empty);
    }

    [Test]
    public void TriangleThickness_UpdatesBoundsAndRejectsNegativeValues()
    {
        using var world = new World();
        SoftBody softBody = CreateSoftBody(world);
        var triangle = (SoftBodyTriangle)softBody.Shapes[0];

        triangle.Thickness = (Real)4.0;

        Assert.That(triangle.WorldBoundingBox.Min.Z, Is.EqualTo((Real)(-2.0)));
        Assert.That(triangle.WorldBoundingBox.Max.Z, Is.EqualTo((Real)2.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => triangle.Thickness = (Real)(-1.0));
        Assert.That(triangle.Thickness, Is.EqualTo((Real)4.0));

        softBody.Destroy();
    }

    [Test]
    public void AddShape_DuplicateRegistrationDoesNotChangeShapeList()
    {
        using var world = new World();
        SoftBody softBody = CreateSoftBody(world);
        SoftBodyShape shape = softBody.Shapes[0];

        Assert.Throws<ArgumentException>(() => softBody.AddShape(shape));
        Assert.That(softBody.Shapes, Has.Count.EqualTo(1));
        Assert.That(softBody.Shapes[0], Is.SameAs(shape));

        softBody.Destroy();
    }
}
