using NUnit.Framework;

namespace JitterTests.Api;

[TestFixture]
public class RayIntersectTests
{
    private JTriangle tri;

    [SetUp]
    public void Setup()
    {
        // Triangle in XY plane with counter-clockwise winding
        tri = new JTriangle
        {
            V0 = new JVector(0, 0, 0),
            V1 = new JVector(1, 0, 0),
            V2 = new JVector(0, 1, 0)
        };
    }

    [Test]
    public void RayHitsFrontFace_CullNone_ReturnsTrue()
    {
        var origin = new JVector(0.25f, 0.25f, 1);
        var direction = new JVector(0, 0, -1);

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.None, out var normal, out var lambda);

        Assert.That(hit);
        Assert.That(MathHelper.IsZero(lambda - (Real)1.0));
        Assert.That(MathHelper.IsZero(normal - JVector.UnitZ));
    }

    [Test]
    public void RayHitsBackFace_CullNone_ReturnsTrue()
    {
        var origin = new JVector(0.25f, 0.25f, -1);
        var direction = new JVector(0, 0, 1);

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.None, out var normal, out var lambda);

        Assert.That(hit);
        Assert.That(MathHelper.IsZero(lambda - (Real)1.0));
        Assert.That(MathHelper.IsZero(normal + JVector.UnitZ));
    }

    [Test]
    public void RayHitsFrontFace_CullFront_ReturnsFalse()
    {
        var origin = new JVector(0.25f, 0.25f, 1);
        var direction = new JVector(0, 0, -1);

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.FrontFacing, out var normal, out var lambda);

        Assert.That(hit, Is.False);
    }

    [Test]
    public void RayHitsBackFace_CullFront_ReturnsTrue()
    {
        var origin = new JVector(0.25f, 0.25f, -1);
        var direction = new JVector(0, 0, 1);

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.FrontFacing, out var normal, out var lambda);

        Assert.That(hit);
        Assert.That(MathHelper.IsZero(lambda - (Real)1.0));
        Assert.That(MathHelper.IsZero(normal + JVector.UnitZ));
    }

    [Test]
    public void RayHitsFrontFace_CullBack_ReturnsTrue()
    {
        var origin = new JVector(0.25f, 0.25f, 1);
        var direction = new JVector(0, 0, -1);

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.BackFacing, out var normal, out var lambda);

        Assert.That(hit);
        Assert.That(MathHelper.IsZero(lambda - (Real)1.0));
        Assert.That(MathHelper.IsZero(normal - JVector.UnitZ));
    }

    [Test]
    public void RayHitsBackFace_CullBack_ReturnsFalse()
    {
        var origin = new JVector(0.25f, 0.25f, -1);
        var direction = new JVector(0, 0, 1);

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.BackFacing, out var normal, out var lambda);

        Assert.That(hit, Is.False);
    }

    [Test]
    public void RayWithShortDirection_HitsTriangle()
    {
        // The parallel test compares the angle, so the length of the direction does not matter.
        var origin = new JVector(0.25f, 0.25f, 1);
        var direction = new JVector(0, 0, (Real)(-1e-7));

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.None, out var normal, out var lambda);

        Assert.That(hit);
        Assert.That(lambda * direction.Length(), Is.EqualTo((Real)1.0).Within((Real)1e-5));
        Assert.That(MathHelper.IsZero(normal - JVector.UnitZ));
    }

    [Test]
    public void RayAgainstSubMillimetreTriangle_Hits()
    {
        var small = new JTriangle
        {
            V0 = new JVector(0, 0, 0),
            V1 = new JVector((Real)5e-4, 0, 0),
            V2 = new JVector(0, (Real)5e-4, 0)
        };

        var origin = new JVector((Real)1e-4, (Real)1e-4, 1);
        var direction = new JVector(0, 0, -1);

        bool hit = small.RayIntersect(origin, direction, JTriangle.CullMode.None, out var normal, out var lambda);

        Assert.That(hit);
        Assert.That(lambda, Is.EqualTo((Real)1.0).Within((Real)1e-5));
        Assert.That(MathHelper.IsZero(normal - JVector.UnitZ));
    }

    [Test]
    public void RayParallelToTriangle_Misses()
    {
        var origin = new JVector(-1, 0.25f, 0);
        var direction = new JVector(1, 0, 0);

        bool hit = tri.RayIntersect(origin, direction, JTriangle.CullMode.None, out _, out _);

        Assert.That(hit, Is.False);
    }
}

public class TriangleShapeRayCastTests
{
    private static TriangleShape CreateGround()
    {
        var mesh = new TriangleMesh([new JTriangle(new JVector(-5, 0, -5), new JVector(-5, 0, 5), new JVector(5, 0, -5))]);
        return new TriangleShape(mesh, 0);
    }

    [Test]
    public void RayPointingAwayFromTheTriangle_Misses([Values] bool attached)
    {
        using var world = new World();
        var ground = CreateGround();
        if (attached)
        {
            var body = world.CreateRigidBody();
            body.AddShape(ground, MassInertiaUpdateMode.Preserve);
            body.MotionType = MotionType.Static;
        }

        bool hit = ground.RayCast(new JVector(-1, (Real)0.005, -1), JVector.UnitY, out _, out Real lambda);

        Assert.That(hit, Is.False);
        Assert.That(lambda, Is.EqualTo(Real.MaxValue));
    }

    [Test]
    public void RayPointingAtTheTriangle_HitsInFront([Values] bool attached)
    {
        using var world = new World();
        var ground = CreateGround();
        if (attached)
        {
            var body = world.CreateRigidBody();
            body.AddShape(ground, MassInertiaUpdateMode.Preserve);
            body.MotionType = MotionType.Static;
        }

        bool hit = ground.RayCast(new JVector(-1, (Real)0.005, -1), -JVector.UnitY, out JVector normal, out Real lambda);

        Assert.That(hit, Is.True);
        Assert.That(lambda, Is.EqualTo((Real)0.005).Within((Real)1e-5));
        Assert.That(normal.Y, Is.EqualTo((Real)1).Within((Real)1e-5));
    }

}
