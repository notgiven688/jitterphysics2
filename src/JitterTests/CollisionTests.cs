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
        const Real boxSize = (Real)10.0;
        BoxShape shape = new BoxShape(boxSize);
        Assert.That(MathHelper.CloseToZero(shape.WorldBoundingBox.Max - shape.Size * (Real)0.5));
        Assert.That(MathHelper.CloseToZero(shape.WorldBoundingBox.Min + shape.Size * (Real)0.5));
    }

    [TestCase]
    public void OverlapDistanceTest()
    {
        BoxShape bs = new BoxShape(1);
        SphereShape ss = new SphereShape(1);

        var overlap = NarrowPhase.Overlap(bs, ss,
            JQuaternion.CreateRotationX((Real)0.2), JVector.UnitY * (Real)3.0);

        var separated = NarrowPhase.Distance(bs, ss,
            JQuaternion.CreateRotationX((Real)0.2), JVector.UnitY * (Real)3.0,
            out JVector pA, out JVector pB, out Real dist);

        Assert.That(!overlap);
        Assert.That(separated);

        Assert.That(MathR.Abs(dist - (Real)1.5) < (Real)1e-4);
        Assert.That(MathHelper.CloseToZero(pA - new JVector(0, (Real)0.5, 0), (Real)1e-4));
        Assert.That(MathHelper.CloseToZero(pB - new JVector(0, (Real)2.0, 0), (Real)1e-4));

        overlap = NarrowPhase.Overlap(bs, ss,
            JQuaternion.CreateRotationX((Real)0.2), JVector.UnitY * (Real)0.5);

        separated = NarrowPhase.Distance(bs, ss,
            JQuaternion.CreateRotationX((Real)0.2), JVector.UnitY * (Real)0.5,
            out pA, out pB, out dist);

        Assert.That(overlap);
        Assert.That(!separated);

        JVector delta = new JVector(10, 13, -22);

        overlap = NarrowPhase.Overlap(ss, ss,
            JQuaternion.CreateRotationX((Real)0.2), delta);

        separated = NarrowPhase.Distance(ss, ss,
            JQuaternion.CreateRotationX((Real)0.2), delta,
            out pA, out pB, out dist);

        Assert.That(!overlap);
        Assert.That(separated);

        Assert.That(MathR.Abs(dist - delta.Length() + 2) < (Real)1e-4);
        Assert.That(MathHelper.CloseToZero(pA - JVector.Normalize(delta), (Real)1e-4));
        Assert.That(MathHelper.CloseToZero(pB - delta + JVector.Normalize(delta), (Real)1e-4));
    }

    [TestCase]
    public void SphereRayCast()
    {
        SphereShape ss = new SphereShape((Real)1.2);

        const Real epsilon = (Real)1e-12;

        bool hit = ss.LocalRayCast(new JVector(0, (Real)1.2 + (Real)0.25, 0), -JVector.UnitY, out JVector normal, out Real lambda);
        Assert.That(hit);
        Assert.That(MathR.Abs(lambda - (Real)0.25), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = ss.LocalRayCast(new JVector(0, (Real)1.2 + (Real)0.25, 0), -(Real)2.0 * JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathR.Abs(lambda - (Real)0.125), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = ss.LocalRayCast(new JVector(0, (Real)1.2 - (Real)0.25, 0), -JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathR.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));

        hit = ss.LocalRayCast(new JVector(0, -(Real)1.2 - (Real)0.25, 0), -JVector.UnitY * (Real)1.1, out normal, out lambda);
        Assert.That(!hit);
        Assert.That(MathR.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));
    }

    [TestCase]
    public void BoxRayCast()
    {
        BoxShape bs = new BoxShape((Real)1.2 * (Real)2.0);

        const Real epsilon = (Real)1e-12;

        bool hit = bs.LocalRayCast(new JVector(0, (Real)1.2 + (Real)0.25, 0), -JVector.UnitY, out JVector normal, out Real lambda);
        Assert.That(hit);
        Assert.That(MathR.Abs(lambda - (Real)0.25), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = bs.LocalRayCast(new JVector(0, (Real)1.2 + (Real)0.25, 0), -(Real)2.0 * JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathR.Abs(lambda - (Real)0.125), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal - JVector.UnitY));

        hit = bs.LocalRayCast(new JVector(0, (Real)1.2 - (Real)0.25, 0), -JVector.UnitY, out normal, out lambda);
        Assert.That(hit);
        Assert.That(MathR.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));

        hit = bs.LocalRayCast(new JVector(0, -(Real)1.2 - (Real)0.25, 0), -JVector.UnitY * (Real)1.1, out normal, out lambda);
        Assert.That(!hit);
        Assert.That(MathR.Abs(lambda), Is.LessThan(epsilon));
        Assert.That(MathHelper.CloseToZero(normal));
    }

    [TestCase]
    public void RayCast()
    {
        const Real radius = 4;

        JVector sp = new JVector(10, 11, 12);
        JVector op = new JVector(1, 2, 3);

        SphereShape s1 = new(radius);

        bool hit = NarrowPhase.RayCast(s1, JQuaternion.CreateRotationX((Real)0.32), sp,
            op, sp - op, out Real lambda, out JVector normal);

        JVector cn = JVector.Normalize(op - sp); // analytical normal
        JVector hp = op + (sp - op) * lambda; // hit point

        Assert.That(hit);
        Assert.That(MathHelper.CloseToZero(normal - cn, (Real)1e-6));

        Real distance = (hp - sp).Length();
        Assert.That(MathR.Abs(distance - radius), Is.LessThan((Real)1e-4));
    }

    [TestCase]
    public void SweepTest()
    {
        var s1 = new SphereShape((Real)0.5);
        var s2 = new BoxShape(1);

        var rot = JQuaternion.CreateRotationZ(MathR.PI / (Real)4.0);
        var sweep = JVector.Normalize(new JVector(1, 1, 0));

        bool hit = NarrowPhase.Sweep(s1, s2, rot, rot,
            new JVector(1, 1, 3), new JVector(11, 11, 3),
            sweep, -(Real)2.0 * sweep,
            out JVector pA, out JVector pB, out JVector normal, out Real lambda);

        Assert.That(hit);

        Real expectedlambda = (MathR.Sqrt((Real)200.0) - (Real)1.0) * ((Real)1.0 / (Real)3.0);
        JVector expectedNormal = JVector.Normalize(new JVector(1, 1, 0));
        JVector expectedPoint = new JVector(1, 1, 3) + expectedNormal * ((Real)0.5 + expectedlambda);
        JVector expectedPointA = expectedPoint - sweep * lambda;
        JVector expectedPointB = expectedPoint + (Real)2.0 * sweep * lambda;

        Assert.That((normal - expectedNormal).LengthSquared(), Is.LessThan((Real)1e-4));
        Assert.That((pA - expectedPointA).LengthSquared(), Is.LessThan((Real)1e-4));
        Assert.That((pB - expectedPointB).LengthSquared(), Is.LessThan((Real)1e-4));
        Assert.That(MathR.Abs(lambda - expectedlambda), Is.LessThan((Real)1e-4));
    }

    [TestCase]
    public void NormalDirection()
    {
        SphereShape s1 = new((Real)0.5);
        SphereShape s2 = new((Real)0.5);

        // -----------------------------------------------

        NarrowPhase.MPREPA(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)0.25, 0, 0), new JVector(+(Real)0.25, 0, 0),
            out JVector pointA, out JVector pointB, out JVector normal, out Real penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan((Real)0.0));
        Assert.That(pointB.X, Is.LessThan((Real)0.0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0));

        // -----------------------------------------------

        NarrowPhase.Collision(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)0.25, 0, 0), new JVector(+(Real)0.25, 0, 0),
            out pointA, out pointB, out normal, out penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0));
        Assert.That(pointB.X, Is.LessThan(0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0));

        // -----------------------------------------------

        BoxShape b1 = new(1);
        BoxShape b2 = new(1);

        NarrowPhase.MPREPA(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)0.25, (Real)0.1, 0), new JVector(+(Real)0.25, -(Real)0.1, 0),
            out pointA, out pointB, out normal, out penetration);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0));
        Assert.That(pointB.X, Is.LessThan(0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0));

        // the penetration is positive
        Assert.That(penetration, Is.GreaterThan(0));

        // -----------------------------------------------

        NarrowPhase.Collision(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)2.25, 0, 0), new JVector(+(Real)2.25, 0, 0),
            out pointA, out pointB, out normal, out penetration);

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0));

        // the penetration is negative
        Assert.That(penetration, Is.LessThan(0));
    }
}