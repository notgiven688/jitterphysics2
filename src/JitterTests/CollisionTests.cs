namespace JitterTests;

public class CollisionTests
{
    [SetUp]
    public void Setup()
    {
    }
    
    [TestCase]
    public void NoBodyWorldBoundingBox()
    {
        const float boxSize = 10.0f;
        BoxShape shape = new BoxShape(boxSize);
        Assert.That(MathHelper.CloseToZero(shape.WorldBoundingBox.Max - shape.Size * 0.5f));
        Assert.That(MathHelper.CloseToZero(shape.WorldBoundingBox.Min + shape.Size * 0.5f));
    }

    [TestCase]
    public void RayCast()
    {
        const float radius = 4;

        JVector sp = new JVector(10, 11, 12);
        JVector op = new JVector(1, 2, 3);

        SphereShape s1 = new(radius);

        bool hit = NarrowPhase.RayCast(s1, JMatrix.CreateRotationX(0.32f), sp, 
            op, sp - op, out float fraction, out JVector normal);

        JVector cn = JVector.Normalize(op - sp);   // analytical normal
        JVector hp = op + (sp - op) * fraction;    // hit point

        Assert.That(hit);
        Assert.That(MathHelper.CloseToZero(normal - cn, 1e-6f));
        Assert.That(MathF.Abs((hp - sp).Length() - radius) < 1e-6f);
    }

    [TestCase]
    public void SweepTest()
    {
        SphereShape s1 = new SphereShape(1);
        SphereShape s2 = new SphereShape(1);

        NarrowPhase.SweepTest(s1, s2, JMatrix.Identity, JMatrix.Identity,
            new JVector(-10, 0, 0), new JVector(0, 0, 0),
            new JVector(1, 0, 0), new JVector(0, 0, 0),
            out JVector pA, out JVector pB, out JVector normal);
    }

    [TestCase]
    public void NormalDirection()
    {
        SphereShape s1 = new(0.5f);
        SphereShape s2 = new(0.5f);

        // -----------------------------------------------

        NarrowPhase.MPREPA(s1, s2, JMatrix.Identity, JMatrix.Identity, new JVector(-0.25f, 0, 0), new JVector(+0.25f, 0, 0),
            out JVector pointA, out JVector pointB, out JVector normal, out float penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0f));
        Assert.That(pointB.X, Is.LessThan(0.0f));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0f));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0.0f));

        // -----------------------------------------------

        NarrowPhase.GJKEPA(s1, s2, JMatrix.Identity, JMatrix.Identity, new JVector(-0.25f, 0, 0), new JVector(+0.25f, 0, 0),
            out pointA, out pointB, out normal, out penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0f));
        Assert.That(pointB.X, Is.LessThan(0.0f));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0f));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0.0f));

        // -----------------------------------------------

        BoxShape b1 = new(1);
        BoxShape b2 = new(1);

        NarrowPhase.MPREPA(b1, b2, JMatrix.Identity, JMatrix.Identity, new JVector(-0.25f, 0.1f, 0), new JVector(+0.25f, -0.1f, 0),
            out pointA, out pointB, out normal, out penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0f));
        Assert.That(pointB.X, Is.LessThan(0.0f));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0f));

        // the penetration is positive
        Assert.That(penetration, Is.GreaterThan(0.0f));

        // -----------------------------------------------

        NarrowPhase.GJKEPA(b1, b2, JMatrix.Identity, JMatrix.Identity, new JVector(-2.25f, 0, 0), new JVector(+2.25f, 0, 0),
            out pointA, out pointB, out normal, out penetration);

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0f));

        // the penetration is negative
        Assert.That(penetration, Is.LessThan(0.0f));
    }
}