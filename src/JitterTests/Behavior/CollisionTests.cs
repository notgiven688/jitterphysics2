using JVector = Jitter2.LinearMath.JVector;

namespace JitterTests.Behavior;

public class CollisionTests
{
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
            out JVector pA, out JVector pB, out JVector normal, out Real dist);

        Assert.That(!overlap);
        Assert.That(separated, Is.True);

        Assert.That(MathR.Abs(dist - (Real)1.5) < (Real)1e-4);
        Assert.That(MathHelper.CloseToZero(pA - new JVector(0, (Real)0.5, 0), (Real)1e-4));
        Assert.That(MathHelper.CloseToZero(pB - new JVector(0, (Real)2.0, 0), (Real)1e-4));

        overlap = NarrowPhase.Overlap(bs, ss,
            JQuaternion.CreateRotationX((Real)0.2), JVector.UnitY * (Real)0.5);

        separated = NarrowPhase.Distance(bs, ss,
            JQuaternion.CreateRotationX((Real)0.2), JVector.UnitY * (Real)0.5,
            out pA, out pB, out normal, out dist);

        Assert.That(overlap);
        Assert.That(separated, Is.False);

        JVector delta = new JVector(10, 13, -22);

        overlap = NarrowPhase.Overlap(ss, ss,
            JQuaternion.CreateRotationX((Real)0.2), delta);

        separated = NarrowPhase.Distance(ss, ss,
            JQuaternion.CreateRotationX((Real)0.2), delta,
            out pA, out pB, out normal, out dist);

        Assert.That(!overlap);
        Assert.That(separated, Is.True);

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

        Assert.That(hit, Is.True);
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

        Assert.That(hit, Is.True);

        Real expectedlambda = (MathR.Sqrt((Real)200.0) - (Real)1.0) * ((Real)(1.0 / 3.0));
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
    public void SweepCastableMatchesNarrowPhaseSweep()
    {
        var world = new World();
        SupportPrimitives.Sphere query = new((Real)0.5);
        BoxShape target = new BoxShape(1);
        var body = world.CreateRigidBody();
        body.AddShape(target);

        var orientation = JQuaternion.CreateRotationZ(MathR.PI / (Real)4.0);
        var position = new JVector(1, 1, 3);
        var sweep = JVector.Normalize(new JVector(1, 1, 0));
        body.Orientation = JQuaternion.CreateRotationZ(MathR.PI / (Real)4.0);
        body.Position = new JVector(11, 11, 3);

        bool narrowHit = NarrowPhase.Sweep(query, target,
            orientation, body.Orientation,
            position, body.Position,
            sweep, JVector.Zero,
            out JVector narrowA, out JVector narrowB, out JVector narrowNormal, out Real narrowLambda);

        bool castHit = target.Sweep(query,
            orientation, position, sweep,
            out JVector castA, out JVector castB, out JVector castNormal, out Real castLambda);

        Assert.That(castHit, Is.EqualTo(narrowHit));
        Assert.That((castA - narrowA).LengthSquared(), Is.LessThan((Real)1e-6));
        Assert.That((castB - narrowB).LengthSquared(), Is.LessThan((Real)1e-6));
        Assert.That((castNormal - narrowNormal).LengthSquared(), Is.LessThan((Real)1e-6));
        Assert.That(MathR.Abs(castLambda - narrowLambda), Is.LessThan((Real)1e-6));
        world.Dispose();
    }

    [TestCase]
    public void TransformedShapeOverlapDistance()
    {
        var box = new BoxShape(1);
        var rotation = JMatrix.CreateRotationZ(MathR.PI / (Real)4.0);
        var ts = new TransformedShape(box, new JVector(0, 5, 0), rotation);

        var sphere = new SphereShape(1);

        var overlap = NarrowPhase.Overlap(ts, sphere,
            JQuaternion.Identity, JVector.Zero);
        Assert.That(!overlap);

        var separated = NarrowPhase.Distance(ts, sphere,
            JQuaternion.Identity, JVector.Zero,
            out JVector pA, out JVector pB, out JVector normal, out Real dist);
        Assert.That(separated, Is.True);
        Assert.That(dist, Is.GreaterThan((Real)0.0));

        var tsClose = new TransformedShape(box, new JVector(0, (Real)0.5, 0), rotation);

        overlap = NarrowPhase.Overlap(tsClose, sphere,
            JQuaternion.Identity, JVector.Zero);
        Assert.That(overlap);
    }

    [TestCase]
    public void TransformedShapeScaledOverlap()
    {
        var sphere = new SphereShape((Real)0.5);
        var ts = new TransformedShape(sphere, JMatrix.CreateScale((Real)3.0, (Real)1.0, (Real)1.0));

        var probe = new SphereShape((Real)0.1);

        var overlap = NarrowPhase.Overlap(ts, probe,
            JQuaternion.Identity, new JVector((Real)1.2, 0, 0));
        Assert.That(overlap);

        overlap = NarrowPhase.Overlap(ts, probe,
            JQuaternion.Identity, new JVector((Real)1.7, 0, 0));
        Assert.That(!overlap);

        overlap = NarrowPhase.Overlap(ts, probe,
            JQuaternion.Identity, new JVector(0, (Real)0.7, 0));
        Assert.That(!overlap);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void SphereOnFace_ContactRemainsBelowSphere(bool reversed, bool transformed)
    {
        BoxShape floor = new(20, 20, 20);
        SphereShape sphere = new((Real)0.5);
        var frame = transformed
            ? JQuaternion.CreateRotationZ((Real)0.4) * JQuaternion.CreateRotationY((Real)0.7)
            : JQuaternion.Identity;
        var floorPosition = transformed ? new JVector(5, -10, 3) : new JVector(0, -10, 0);
        // This pose returned a contact 0.37 mm off center even with a 1e-8 support-gap tolerance.
        var localPosition = new JVector((Real)3.1203206f, (Real)10.0 + (Real)0.49837333f, (Real)(-0.22746953f));
        JVector spherePosition = floorPosition + JVector.Transform(localPosition, frame);
        var sphereOrientation = frame * new JQuaternion((Real)0.05042054f, (Real)(-0.123349205f),
            (Real)(-0.0064961617f), (Real)(-0.9910604f));
        JVector expectedNormal = JVector.Transform(JVector.UnitY, frame);
        JVector expectedA = floorPosition + JVector.Transform(new JVector(localPosition.X, 10, localPosition.Z), frame);
        JVector expectedB = spherePosition - (Real)0.5 * expectedNormal;

        JVector pointA, pointB, normal;
        Real penetration;
        bool hit = reversed
            ? NarrowPhase.MprEpa(sphere, floor, sphereOrientation, frame, spherePosition, floorPosition,
                out pointB, out pointA, out normal, out penetration)
            : NarrowPhase.MprEpa(floor, sphere, frame, sphereOrientation, floorPosition, spherePosition,
                out pointA, out pointB, out normal, out penetration);

        Assert.That(hit, Is.True);
        Assert.That((pointA - expectedA).Length(), Is.LessThan((Real)5e-5));
        Assert.That((pointB - expectedB).Length(), Is.LessThan((Real)5e-5));
        Assert.That((normal - (reversed ? -expectedNormal : expectedNormal)).Length(), Is.LessThan((Real)5e-5));
        Assert.That(penetration, Is.EqualTo((Real)10.5 - localPosition.Y).Within((Real)5e-5));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Ellipsoids_ContactDoesNotSnapToAnAlmostPlanarPatch(bool reversed)
    {
        var a = new TransformedShape(new SphereShape(1), new JMatrix(
            (Real)1.2622255, 0, 0, 0, (Real)2.177816, 0, 0, 0, (Real)1.6617688));
        var b = new TransformedShape(new SphereShape(1), new JMatrix(
            (Real)0.696555, 0, 0, 0, (Real)2.140087, 0, 0, 0, (Real)1.546733));
        var rotation = new JQuaternion((Real)0.12345854, (Real)(-0.7636755),
            (Real)0.15334113, (Real)0.614853);
        var position = new JVector((Real)(-2.5401924), (Real)(-1.3526405), (Real)(-0.80724305));
        var expectedNormal = new JVector((Real)(-0.9243989), (Real)(-0.2980082), (Real)(-0.23807088));
        a.SupportMap(expectedNormal, out var expectedA);
        var expectedB = expectedA - (Real)0.001216647 * expectedNormal;

        JVector pointA, pointB;
        bool hit = reversed
            ? NarrowPhase.MprEpa(b, a, rotation, JQuaternion.Identity, position, JVector.Zero,
                out pointB, out pointA, out _, out _)
            : NarrowPhase.MprEpa(a, b, rotation, position, out pointA, out pointB, out _, out _);

        Assert.That(hit, Is.True);
        Assert.That((pointA - expectedA).Length(), Is.LessThan((Real)1e-4));
        Assert.That((pointB - expectedB).Length(), Is.LessThan((Real)1e-4));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OffsetBoxFaceContact_RemainsInsideBothBoxes(bool reversed)
    {
        BoxShape a = new(1), b = new(1);
        var positionA = new JVector(0, (Real)(-0.5), 0);
        var positionB = new JVector((Real)0.1, (Real)0.499, (Real)0.13);
        JVector pointA, pointB;
        bool hit = reversed
            ? NarrowPhase.MprEpa(b, a, JQuaternion.Identity, JQuaternion.Identity, positionB, positionA,
                out pointB, out pointA, out _, out _)
            : NarrowPhase.MprEpa(a, b, JQuaternion.Identity, JQuaternion.Identity, positionA, positionB,
                out pointA, out pointB, out _, out _);

        Assert.That(hit, Is.True);
        foreach (var localPoint in new[] { pointA - positionA, pointB - positionB })
        {
            Assert.That(MathR.Abs(localPoint.X), Is.LessThanOrEqualTo((Real)0.5 + (Real)1e-5));
            Assert.That(MathR.Abs(localPoint.Y), Is.LessThanOrEqualTo((Real)0.5 + (Real)1e-5));
            Assert.That(MathR.Abs(localPoint.Z), Is.LessThanOrEqualTo((Real)0.5 + (Real)1e-5));
        }
    }

    [Test]
    public void SphereOutsideFace_ContactUsesBoxEdge()
    {
        BoxShape box = new(2);
        SphereShape sphere = new((Real)0.5);
        Real offset = ((Real)0.5 - (Real)0.002) / MathR.Sqrt((Real)2.0);
        JVector position = new((Real)1.0 + offset, (Real)1.0 + offset, 0);

        Assert.That(NarrowPhase.MprEpa(box, sphere, JQuaternion.Identity, position,
            out JVector pointA, out _, out JVector normal, out Real penetration), Is.True);

        // A projection outside either face must retain the edge contact, rather than snapping
        // the normal to one face and projecting onto that face's unbounded plane.
        Assert.That((pointA - new JVector(1, 1, 0)).Length(), Is.LessThan((Real)5e-5));
        Assert.That(normal.X, Is.GreaterThan((Real)0.6));
        Assert.That(normal.Y, Is.GreaterThan((Real)0.6));
        Assert.That(penetration, Is.EqualTo((Real)0.002).Within((Real)5e-5));
    }

    [TestCase]
    public void NormalDirection()
    {
        SphereShape s1 = new((Real)0.5);
        SphereShape s2 = new((Real)0.5);

        // -----------------------------------------------

        Assert.That(NarrowPhase.MprEpa(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)0.25, 0, 0), new JVector(+(Real)0.25, 0, 0),
            out JVector pointA, out JVector pointB, out JVector normal, out Real penetration), Is.True);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan((Real)0.0));
        Assert.That(pointB.X, Is.LessThan((Real)0.0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0));

        // the separation is negative
        Assert.That(penetration, Is.GreaterThan(0));

        // -----------------------------------------------

        Assert.That(NarrowPhase.Collision(s1, s2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)0.25, 0, 0), new JVector(+(Real)0.25, 0, 0),
            out pointA, out pointB, out normal, out penetration), Is.True);

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

        Assert.That(NarrowPhase.MprEpa(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)0.25, (Real)0.1, 0), new JVector(+(Real)0.25, -(Real)0.1, 0),
            out pointA, out pointB, out normal, out penetration), Is.True);

        // pointA is on s1 and pointB is on s2
        Assert.That(pointA.X, Is.GreaterThan(0));
        Assert.That(pointB.X, Is.LessThan(0));

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0));

        // the penetration is positive
        Assert.That(penetration, Is.GreaterThan(0));

        // -----------------------------------------------

        Assert.That(NarrowPhase.Collision(b1, b2, JQuaternion.Identity, JQuaternion.Identity, new JVector(-(Real)2.25, 0, 0), new JVector(+(Real)2.25, 0, 0),
            out pointA, out pointB, out normal, out penetration), Is.True);

        // the collision normal points from s2 to s1
        Assert.That(normal.X, Is.GreaterThan(0));

        // the penetration is negative
        Assert.That(penetration, Is.LessThan(0));
    }
}
