using System.Diagnostics;
using JVector = Jitter2.LinearMath.JVector;

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
    public void OverlapDistanceTest()
    {
        BoxShape bs = new BoxShape(1);
        SphereShape ss = new SphereShape(1);

        var overlap = NarrowPhase.Overlap(bs, ss,
            JQuaternion.CreateRotationX(0.2f), JVector.UnitY * 3.0f);

        var separated = NarrowPhase.Distance(bs, ss,
            JQuaternion.CreateRotationX(0.2f), JVector.UnitY * 3.0f,
            out JVector pA, out JVector pB, out float dist);

        Assert.That(!overlap);
        Assert.That(separated);

        Assert.That(MathF.Abs(dist - 1.5f) < 1e-4f);
        Assert.That(MathHelper.CloseToZero(pA - new JVector(0, 0.5f, 0), 1e-4f));
        Assert.That(MathHelper.CloseToZero(pB - new JVector(0, 2.0f, 0), 1e-4f));

        overlap = NarrowPhase.Overlap(bs, ss,
            JQuaternion.CreateRotationX(0.2f), JVector.UnitY * 0.5f);

        separated = NarrowPhase.Distance(bs, ss,
            JQuaternion.CreateRotationX(0.2f), JVector.UnitY * 0.5f,
            out pA, out pB, out dist);

        Assert.That(overlap);
        Assert.That(!separated);

        JVector delta = new JVector(10, 13, -22);

        overlap = NarrowPhase.Overlap(ss, ss,
            JQuaternion.CreateRotationX(0.2f), delta);

        separated = NarrowPhase.Distance(ss, ss,
            JQuaternion.CreateRotationX(0.2f), delta,
            out pA, out pB, out dist);

        Assert.That(!overlap);
        Assert.That(separated);

        Assert.That(MathF.Abs(dist - delta.Length() + 2) < 1e-4f);
        Assert.That(MathHelper.CloseToZero(pA - JVector.Normalize(delta), 1e-4f));
        Assert.That(MathHelper.CloseToZero(pB - delta + JVector.Normalize(delta), 1e-4f));
    }

    [TestCase]
    public void SphereRayCast()
    {
        SphereShape ss = new SphereShape(1.2f);

        const float epsilon = 1e-12f;

        bool hit = ss.LocalRayCast(new JVector(0, 1.2f + 0.25f, 0), -JVector.UnitY, out JVector normal, out float lambda);
        Assert.That(hit);
        Assert.That(MathF.Abs(lambda - 0.25f), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = ss.LocalRayCast(new JVector(0, 1.2f + 0.25f, 0), -2.0f * JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathF.Abs(lambda - 0.125f), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = ss.LocalRayCast(new JVector(0, 1.2f - 0.25f, 0), -JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathF.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));

        hit = ss.LocalRayCast(new JVector(0, -1.2f - 0.25f, 0), -JVector.UnitY * 1.1f, out normal, out lambda);
        Assert.That(!hit);
        Assert.That(MathF.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));
    }

    [TestCase]
    public void BoxRayCast()
    {
        BoxShape bs = new BoxShape(1.2f * 2.0f);

        const float epsilon = 1e-12f;

        bool hit = bs.LocalRayCast(new JVector(0, 1.2f + 0.25f, 0), -JVector.UnitY, out JVector normal, out float lambda);
        Assert.That(hit);
        Assert.That(MathF.Abs(lambda - 0.25f), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = bs.LocalRayCast(new JVector(0, 1.2f + 0.25f, 0), -2.0f * JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathF.Abs(lambda - 0.125f), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = bs.LocalRayCast(new JVector(0, 1.2f - 0.25f, 0), -JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathF.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));

        hit = bs.LocalRayCast(new JVector(0, -1.2f - 0.25f, 0), -JVector.UnitY * 1.1f, out normal, out lambda);
        Assert.That(!hit);
        Assert.That(MathF.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));
    }

    [TestCase]
    public void RayCast()
    {
        const float radius = 4;

        JVector sp = new JVector(10, 11, 12);
        JVector op = new JVector(1, 2, 3);

        SphereShape s1 = new(radius);

        bool hit = NarrowPhase.RayCast(s1, JQuaternion.CreateRotationX(0.32f), sp,
            op, sp - op, out float fraction, out JVector normal);

        JVector cn = JVector.Normalize(op - sp); // analytical normal
        JVector hp = op + (sp - op) * fraction; // hit point

        Assert.That(hit);
        Assert.That(MathHelper.CloseToZero(normal - cn, 1e-6f));

        float distance = (hp - sp).Length();
        Assert.That(MathF.Abs(distance - radius), Is.LessThan(1e-4f));
    }

    [TestCase]
    public void SweepTest()
    {
        var s1 = new SphereShape(0.5f);
        var s2 = new BoxShape(1);

        var rot = JQuaternion.CreateRotationZ(MathF.PI / 4.0f);
        var sweep = JVector.Normalize(new JVector(1, 1, 0));

        bool hit = NarrowPhase.Sweep(s1, s2, rot, rot,
            new JVector(1, 1, 3), new JVector(11, 11, 3),
            sweep, -2.0f * sweep,
            out JVector pA, out JVector pB, out JVector normal, out float fraction);

        Assert.That(hit);

        float expectedFraction = (MathF.Sqrt(200.0f) - 1.0f) * (1.0f / 3.0f);
        JVector expectedNormal = JVector.Normalize(new JVector(1, 1, 0));
        JVector expectedPoint = new JVector(1, 1, 3) + expectedNormal * (0.5f + expectedFraction);
        JVector expectedPointA = expectedPoint - sweep * fraction;
        JVector expectedPointB = expectedPoint + 2.0f * sweep * fraction;

        Assert.That((normal - expectedNormal).LengthSquared(), Is.LessThan(1e-4f));
        Assert.That((pA - expectedPointA).LengthSquared(), Is.LessThan(1e-4f));
        Assert.That((pB - expectedPointB).LengthSquared(), Is.LessThan(1e-4f));
        Assert.That(MathF.Abs(fraction - expectedFraction), Is.LessThan(1e-4f));
    }

    [TestCase]
    public void NormalDirection()
    {
        SphereShape s1 = new(0.5f);
        SphereShape s2 = new(0.5f);

        // -----------------------------------------------

        NarrowPhase.MPREPA(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-0.25f, 0, 0), new JVector(+0.25f, 0, 0),
            out JVector pointA, out JVector pointB, out JVector normal, out float penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0f));
        Assert.That(pointB.X, Is.LessThan(0.0f));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0f));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0.0f));

        // -----------------------------------------------

        NarrowPhase.Collision(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-0.25f, 0, 0), new JVector(+0.25f, 0, 0),
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

        NarrowPhase.MPREPA(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-0.25f, 0.1f, 0), new JVector(+0.25f, -0.1f, 0),
            out pointA, out pointB, out normal, out penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0f));
        Assert.That(pointB.X, Is.LessThan(0.0f));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0f));

        // the penetration is positive
        Assert.That(penetration, Is.GreaterThan(0.0f));

        // -----------------------------------------------

        NarrowPhase.Collision(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-2.25f, 0, 0), new JVector(+2.25f, 0, 0),
            out pointA, out pointB, out normal, out penetration);

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0f));

        // the penetration is negative
        Assert.That(penetration, Is.LessThan(0.0f));
    }
}