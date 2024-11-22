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
        const double boxSize = 10.0;
        BoxShape shape = new BoxShape(boxSize);
        Assert.That(MathHelper.CloseToZero(shape.WorldBoundingBox.Max - shape.Size * 0.5));
        Assert.That(MathHelper.CloseToZero(shape.WorldBoundingBox.Min + shape.Size * 0.5));
    }

    [TestCase]
    public void OverlapDistanceTest()
    {
        BoxShape bs = new BoxShape(1);
        SphereShape ss = new SphereShape(1);

        var overlap = NarrowPhase.Overlap(bs, ss,
            JQuaternion.CreateRotationX(0.2), JVector.UnitY * 3.0);

        var separated = NarrowPhase.Distance(bs, ss,
            JQuaternion.CreateRotationX(0.2), JVector.UnitY * 3.0,
            out JVector pA, out JVector pB, out double dist);

        Assert.That(!overlap);
        Assert.That(separated);

        Assert.That(Math.Abs(dist - 1.5) < 1e-4);
        Assert.That(MathHelper.CloseToZero(pA - new JVector(0, 0.5, 0), 1e-4));
        Assert.That(MathHelper.CloseToZero(pB - new JVector(0, 2.0, 0), 1e-4));

        overlap = NarrowPhase.Overlap(bs, ss,
            JQuaternion.CreateRotationX(0.2), JVector.UnitY * 0.5);

        separated = NarrowPhase.Distance(bs, ss,
            JQuaternion.CreateRotationX(0.2), JVector.UnitY * 0.5,
            out pA, out pB, out dist);

        Assert.That(overlap);
        Assert.That(!separated);

        JVector delta = new JVector(10, 13, -22);

        overlap = NarrowPhase.Overlap(ss, ss,
            JQuaternion.CreateRotationX(0.2), delta);

        separated = NarrowPhase.Distance(ss, ss,
            JQuaternion.CreateRotationX(0.2), delta,
            out pA, out pB, out dist);

        Assert.That(!overlap);
        Assert.That(separated);

        Assert.That(Math.Abs(dist - delta.Length() + 2) < 1e-4);
        Assert.That(MathHelper.CloseToZero(pA - JVector.Normalize(delta), 1e-4));
        Assert.That(MathHelper.CloseToZero(pB - delta + JVector.Normalize(delta), 1e-4));
    }

    [TestCase]
    public void SphereRayCast()
    {
        SphereShape ss = new SphereShape(1.2);

        const double epsilon = 1e-12;

        bool hit = ss.LocalRayCast(new JVector(0, 1.2 + 0.25, 0), -JVector.UnitY, out JVector normal, out double lambda);
        Assert.That(hit);
        Assert.That(Math.Abs(lambda - 0.25), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = ss.LocalRayCast(new JVector(0, 1.2 + 0.25, 0), -2.0 * JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(Math.Abs(lambda - 0.125), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = ss.LocalRayCast(new JVector(0, 1.2 - 0.25, 0), -JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(Math.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));

        hit = ss.LocalRayCast(new JVector(0, -1.2 - 0.25, 0), -JVector.UnitY * 1.1, out normal, out lambda);
        Assert.That(!hit);
        Assert.That(Math.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));
    }

    [TestCase]
    public void BoxRayCast()
    {
        BoxShape bs = new BoxShape(1.2 * 2.0);

        const double epsilon = 1e-12;

        bool hit = bs.LocalRayCast(new JVector(0, 1.2 + 0.25, 0), -JVector.UnitY, out JVector normal, out double lambda);
        Assert.That(hit);
        Assert.That(Math.Abs(lambda - 0.25), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = bs.LocalRayCast(new JVector(0, 1.2 + 0.25, 0), -2.0 * JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(Math.Abs(lambda - 0.125), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = bs.LocalRayCast(new JVector(0, 1.2 - 0.25, 0), -JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(Math.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));

        hit = bs.LocalRayCast(new JVector(0, -1.2 - 0.25, 0), -JVector.UnitY * 1.1, out normal, out lambda);
        Assert.That(!hit);
        Assert.That(Math.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));
    }

    [TestCase]
    public void RayCast()
    {
        const double radius = 4;

        JVector sp = new JVector(10, 11, 12);
        JVector op = new JVector(1, 2, 3);

        SphereShape s1 = new(radius);

        bool hit = NarrowPhase.RayCast(s1, JQuaternion.CreateRotationX(0.32), sp,
            op, sp - op, out double lambda, out JVector normal);

        JVector cn = JVector.Normalize(op - sp); // analytical normal
        JVector hp = op + (sp - op) * lambda; // hit point

        Assert.That(hit);
        Assert.That(MathHelper.CloseToZero(normal - cn, 1e-6));

        double distance = (hp - sp).Length();
        Assert.That(Math.Abs(distance - radius), Is.LessThan(1e-4));
    }

    [TestCase]
    public void SweepTest()
    {
        var s1 = new SphereShape(0.5);
        var s2 = new BoxShape(1);

        var rot = JQuaternion.CreateRotationZ(Math.PI / 4.0);
        var sweep = JVector.Normalize(new JVector(1, 1, 0));

        bool hit = NarrowPhase.Sweep(s1, s2, rot, rot,
            new JVector(1, 1, 3), new JVector(11, 11, 3),
            sweep, -2.0 * sweep,
            out JVector pA, out JVector pB, out JVector normal, out double lambda);

        Assert.That(hit);

        double expectedlambda = (Math.Sqrt(200.0) - 1.0) * (1.0 / 3.0);
        JVector expectedNormal = JVector.Normalize(new JVector(1, 1, 0));
        JVector expectedPoint = new JVector(1, 1, 3) + expectedNormal * (0.5 + expectedlambda);
        JVector expectedPointA = expectedPoint - sweep * lambda;
        JVector expectedPointB = expectedPoint + 2.0 * sweep * lambda;

        Assert.That((normal - expectedNormal).LengthSquared(), Is.LessThan(1e-4));
        Assert.That((pA - expectedPointA).LengthSquared(), Is.LessThan(1e-4));
        Assert.That((pB - expectedPointB).LengthSquared(), Is.LessThan(1e-4));
        Assert.That(Math.Abs(lambda - expectedlambda), Is.LessThan(1e-4));
    }

    [TestCase]
    public void NormalDirection()
    {
        SphereShape s1 = new(0.5);
        SphereShape s2 = new(0.5);

        // -----------------------------------------------

        NarrowPhase.MPREPA(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-0.25, 0, 0), new JVector(+0.25, 0, 0),
            out JVector pointA, out JVector pointB, out JVector normal, out double penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0));
        Assert.That(pointB.X, Is.LessThan(0.0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0.0));

        // -----------------------------------------------

        NarrowPhase.Collision(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-0.25, 0, 0), new JVector(+0.25, 0, 0),
            out pointA, out pointB, out normal, out penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0));
        Assert.That(pointB.X, Is.LessThan(0.0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0.0));

        // -----------------------------------------------

        BoxShape b1 = new(1);
        BoxShape b2 = new(1);

        NarrowPhase.MPREPA(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-0.25, 0.1, 0), new JVector(+0.25, -0.1, 0),
            out pointA, out pointB, out normal, out penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0.0));
        Assert.That(pointB.X, Is.LessThan(0.0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0));

        // the penetration is positive
        Assert.That(penetration, Is.GreaterThan(0.0));

        // -----------------------------------------------

        NarrowPhase.Collision(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-2.25, 0, 0), new JVector(+2.25, 0, 0),
            out pointA, out pointB, out normal, out penetration);

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0.0));

        // the penetration is negative
        Assert.That(penetration, Is.LessThan(0.0));
    }
}