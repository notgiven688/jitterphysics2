namespace JitterTests.Regression;

public class AngularSweepTests
{
    private const Real Tolerance = (Real)1e-4;

    private static JVector AdvancePoint(in JVector point, in JVector origin, in JVector velocity,
        in JVector angularVelocity, Real time) => origin + time * velocity +
        JVector.Transform(point - origin, MathHelper.RotationQuaternion(angularVelocity, time));

    private static void AssertContact(in JVector pointA, in JVector pointB, in JVector normal, Real time,
        in JVector positionA, in JVector positionB, in JVector velocityA, in JVector velocityB,
        in JVector angularA, in JVector angularB)
    {
        JVector a = AdvancePoint(pointA, positionA, velocityA, angularA, time);
        JVector b = AdvancePoint(pointB, positionB, velocityB, angularB, time);
        using (Assert.EnterMultipleScope())
        {
            Assert.That((a - b).Length(), Is.LessThan((Real)2e-4));
            Assert.That(normal.Length(), Is.EqualTo(1).Within((Real)1e-5));
            Assert.That(JVector.Dot(b - a, normal), Is.GreaterThanOrEqualTo((Real)(-2e-5)));
        }
    }

    [TestCase(1)]
    [TestCase(100000)]
    public void SpinningSeparatedSpheresDoNotProduceAnImpact(int angularSpeed)
    {
        SphereShape sphere = new(1);
        JVector angular = angularSpeed * JVector.UnitZ;
        Assert.That(NarrowPhase.Sweep(sphere, sphere, JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, new JVector(3, 0, 0), JVector.Zero, JVector.Zero, angular, JVector.Zero, 1, 1,
            out _, out _, out JVector normal, out Real time), Is.False);
        Assert.That(time, Is.EqualTo(Real.PositiveInfinity));
        Assert.That(normal, Is.EqualTo(JVector.Zero));
    }

    [TestCase(false, 1)]
    [TestCase(true, 1)]
    [TestCase(false, 300)]
    public void OffsetSphereCanHitWhileItsBodyOriginMovesAway(bool swap, int speed)
    {
        ISupportMappable a = new TransformedShape(new SphereShape((Real)0.25), new JVector(-2, 0, 0));
        ISupportMappable b = new SphereShape((Real)0.25);
        JVector pa = JVector.Zero, pb = new((Real)1.1, 0, 0);
        JVector va = new((Real)(-0.2) * speed, 0, 0), vb = JVector.Zero;
        JVector wa = speed * JVector.UnitZ, wb = JVector.Zero;
        Real ra = (Real)2.25, rb = (Real)0.25;
        if (swap)
        {
            (a, b) = (b, a); (pa, pb) = (pb, pa); (va, vb) = (vb, va);
            (wa, wb) = (wb, wa); (ra, rb) = (rb, ra);
        }

        var result = NarrowPhase.Sweep(a, b, JQuaternion.Identity, JQuaternion.Identity, pa, pb,
            va, vb, wa, wb, ra, rb, (Real)3.2 / speed,
            out JVector pointA, out JVector pointB, out JVector normal, out Real time);

        Assert.That(result, Is.EqualTo(NarrowPhase.SweepStatus.Hit));
        // Independent root of (-2*cos(t) - .2*t - 1.1)^2 + 4*sin(t)^2 = .5^2.
        Assert.That(time * speed, Is.EqualTo((Real)2.9293466801522443).Within((Real)2e-4));
        AssertContact(pointA, pointB, normal, time, pa, pb, va, vb, wa, wb);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RotatingRodReturnsAnchorsAtTheInitialPoses(bool transformWorld)
    {
        BoxShape rod = new(4, (Real)0.2, (Real)0.2), wall = new((Real)0.2, 20, 20);
        JQuaternion frame = transformWorld
            ? JQuaternion.CreateRotationX((Real)0.7) * JQuaternion.CreateRotationY((Real)0.35)
            : JQuaternion.Identity;
        JVector pa = transformWorld ? new JVector(3, -2, 4) : JVector.Zero;
        JVector pb = pa + JVector.Transform(new JVector((Real)1.6, 0, 0), frame);
        JQuaternion qa = frame * JQuaternion.CreateRotationZ(MathR.PI / 2);
        JVector angular = JVector.Transform(-JVector.UnitZ, frame);
        JVector commonVelocity = JVector.Transform(new JVector(2, 3, -1), frame);

        var result = NarrowPhase.Sweep(rod, wall, qa, frame, pa, pb, commonVelocity, commonVelocity,
            angular, JVector.Zero, (Real)2.01, (Real)14.15, 2,
            out JVector pointA, out JVector pointB, out JVector normal, out Real time);

        Assert.That(result, Is.EqualTo(NarrowPhase.SweepStatus.Hit));
        Real expected = MathR.Asin((Real)1.5 / MathR.Sqrt((Real)4.01)) - MathR.Atan((Real)0.05);
        Assert.That(time, Is.EqualTo(expected).Within((Real)2e-4));
        AssertContact(pointA, pointB, normal, time, pa, pb, commonVelocity, commonVelocity, angular, JVector.Zero);
        JVector local = JVector.ConjugatedTransform(pointA - pa, qa);
        Assert.That(MathR.Abs(local.X), Is.LessThanOrEqualTo((Real)2.00001));
        Assert.That(MathR.Abs(local.Y), Is.LessThanOrEqualTo((Real)0.10001));
        Assert.That(MathR.Abs(local.Z), Is.LessThanOrEqualTo((Real)0.10001));
    }

    [Test]
    public void RotatingRodThatNeverReachesWallIsAMiss()
    {
        BoxShape rod = new(4, (Real)0.2, (Real)0.2), wall = new((Real)0.2, 20, 20);
        var result = NarrowPhase.Sweep(rod, wall, JQuaternion.CreateRotationZ(MathR.PI / 2), JQuaternion.Identity,
            JVector.Zero, new JVector((Real)2.3, 0, 0), JVector.Zero, JVector.Zero,
            -JVector.UnitZ, JVector.Zero, (Real)2.01, (Real)14.15, 20,
            out JVector a, out JVector b, out JVector normal, out Real time);
        Assert.That(result, Is.EqualTo(NarrowPhase.SweepStatus.Miss));
        Assert.That(time, Is.EqualTo(Real.PositiveInfinity));
        Assert.That(a, Is.EqualTo(JVector.Zero));
        Assert.That(b, Is.EqualTo(JVector.Zero));
        Assert.That(normal, Is.EqualTo(JVector.Zero));
    }

    [TestCase(1)]
    [TestCase(1000)]
    public void FullRevolutionFindsIntermediateImpactWithSeparatedEndpoints(int speed)
    {
        TransformedShape orbit = new(new SphereShape((Real)0.25), new JVector(-2, 0, 0));
        SphereShape target = new((Real)0.25);
        JVector angular = speed * JVector.UnitZ, pb = new(0, -2, 0);
        var result = NarrowPhase.Sweep(orbit, target, JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, pb, JVector.Zero, JVector.Zero, angular, JVector.Zero,
            (Real)2.25, (Real)0.25, 2 * MathR.PI / speed,
            out JVector a, out JVector b, out JVector normal, out Real time);
        Assert.That(result, Is.EqualTo(NarrowPhase.SweepStatus.Hit));
        Real expected = MathR.Asin((Real)(1 - 0.25 / 8));
        Assert.That(time * speed, Is.EqualTo(expected).Within((Real)2e-4));
        AssertContact(a, b, normal, time, JVector.Zero, pb, JVector.Zero, JVector.Zero, angular, JVector.Zero);
    }

    [Test]
    public void TangentialImpactOfRotatingRodIsFound()
    {
        BoxShape rod = new(4, (Real)0.2, (Real)0.2), wall = new((Real)0.2, 20, 20);
        Real wallX = MathR.Sqrt((Real)4.01) + (Real)0.1;
        JVector pb = new(wallX, 0, 0);
        var result = NarrowPhase.Sweep(rod, wall, JQuaternion.CreateRotationZ(MathR.PI / 2), JQuaternion.Identity,
            JVector.Zero, pb, JVector.Zero, JVector.Zero, -JVector.UnitZ, JVector.Zero,
            (Real)2.01, (Real)14.15, MathR.PI,
            out JVector a, out JVector b, out JVector normal, out Real time);
        Assert.That(result, Is.EqualTo(NarrowPhase.SweepStatus.Hit));
        Real expected = MathR.PI / 2 - MathR.Atan((Real)0.05);
        // At a tangent, distance is quadratic in the time error. Check the actual gap as well.
        Assert.That(time, Is.EqualTo(expected).Within((Real)0.011));
        Real rightEdge = 2 * MathR.Sin(time) + (Real)0.1 * MathR.Cos(time);
        Assert.That(wallX - (Real)0.1 - rightEdge, Is.InRange((Real)(-1e-6), (Real)1.1e-4));
        AssertContact(a, b, normal, time, JVector.Zero, pb, JVector.Zero, JVector.Zero, -JVector.UnitZ, JVector.Zero);
    }

    // The rotation origin is the lowest vertex. It stays above the plane while the far vertex
    // eventually hits it. The original distance / maximum-speed advancement stalls on this case.
    private sealed class TrianglePrism : ISupportMappable
    {
        public int Calls;
        public void GetCenter(out JVector point) => point = new((Real)(2.0 / 3), (Real)(2.0 / 3), 0);
        public void SupportMap(in JVector direction, out JVector result)
        {
            Calls++;
            JVector a = JVector.Zero, b = new(2, 1, 0), c = new(0, 1, 0);
            result = JVector.Dot(b, direction) > 0 ? b : a;
            if (JVector.Dot(c, direction) > JVector.Dot(result, direction)) result = c;
            result.Z = direction.Z >= 0 ? (Real)0.1 : (Real)(-0.1);
        }
    }

    [TestCase(0.001)]
    [TestCase(0.0002)]
    public void StationaryClosestVertexDoesNotStallTheSearch(double gap)
    {
        TrianglePrism triangle = new();
        BoxShape floor = new(20, 1, 20);
        JVector pa = new(0, (Real)gap, 0), pb = new(0, (Real)(-0.5), 0);
        var result = NarrowPhase.Sweep(triangle, floor, JQuaternion.Identity, JQuaternion.Identity,
            pa, pb, JVector.Zero, JVector.Zero, -JVector.UnitZ, JVector.Zero,
            (Real)2.24, (Real)14.16, 1,
            out JVector a, out JVector b, out JVector normal, out Real time);
        Assert.That(result, Is.EqualTo(NarrowPhase.SweepStatus.Hit));
        Real expected = MathR.Atan((Real)0.5) + MathR.Asin((Real)gap / MathR.Sqrt(5));
        Assert.That(time, Is.EqualTo(expected).Within((Real)2e-4));
        Assert.That(triangle.Calls, Is.LessThan(1000), "The near vertex should not force hundreds of distance queries.");
        AssertContact(a, b, normal, time, pa, pb, JVector.Zero, JVector.Zero, -JVector.UnitZ, JVector.Zero);
    }

    [Test]
    public void BothShapesRotateIntoContact()
    {
        TransformedShape a = new(new SphereShape((Real)0.25), new JVector(-1, 0, 0));
        TransformedShape b = new(new SphereShape((Real)0.25), new JVector(1, 0, 0));
        JVector pb = new((Real)1.1, 0, 0);
        var result = NarrowPhase.Sweep(a, b, JQuaternion.Identity, JQuaternion.Identity, JVector.Zero, pb,
            JVector.Zero, JVector.Zero, JVector.UnitZ, -JVector.UnitZ, (Real)1.25, (Real)1.25, MathR.PI,
            out JVector pointA, out JVector pointB, out JVector normal, out Real time);
        Assert.That(result, Is.EqualTo(NarrowPhase.SweepStatus.Hit));
        Assert.That(time, Is.EqualTo(MathR.Acos((Real)(-0.3))).Within((Real)2e-4));
        AssertContact(pointA, pointB, normal, time, JVector.Zero, pb, JVector.Zero, JVector.Zero, JVector.UnitZ, -JVector.UnitZ);
    }

    [TestCase(0, NarrowPhase.SweepStatus.Miss)]
    [TestCase(0.9, NarrowPhase.SweepStatus.Miss)]
    [TestCase(1, NarrowPhase.SweepStatus.Hit)]
    [TestCase(2, NarrowPhase.SweepStatus.Hit)]
    public void MaximumTimeBoundsTheSearch(double maximumTime, NarrowPhase.SweepStatus expected)
    {
        SphereShape sphere = new(1);
        var status = NarrowPhase.Sweep(sphere, sphere, JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, new JVector(3, 0, 0), JVector.UnitX, JVector.Zero, JVector.Zero, JVector.Zero,
            1, 1, (Real)maximumTime, out _, out _, out JVector normal, out Real time);
        Assert.That(status, Is.EqualTo(expected));
        if (status == NarrowPhase.SweepStatus.Hit)
        {
            Assert.That(time, Is.LessThanOrEqualTo((Real)maximumTime));
            Assert.That(time, Is.EqualTo(1).Within(Tolerance));
            Assert.That((normal - JVector.UnitX).Length(), Is.LessThan((Real)1e-5));
        }
    }

    [Test]
    public void InitialOverlapReturnsZeroTimeAndNormal()
    {
        SphereShape sphere = new(1);
        var status = NarrowPhase.Sweep(sphere, sphere, JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, JVector.UnitX, JVector.Zero, JVector.Zero, JVector.UnitZ, -JVector.UnitY,
            1, 1, 1, out _, out _, out JVector normal, out Real time);
        Assert.That(status, Is.EqualTo(NarrowPhase.SweepStatus.Hit));
        Assert.That(time, Is.Zero);
        Assert.That(normal, Is.EqualTo(JVector.Zero));
    }

    [Test]
    public void DifficultMissDoesNotBecomeAnImpact()
    {
        BoxShape rod = new(4, (Real)0.2, (Real)0.2), wall = new((Real)0.2, 20, 20);
        // The rod never reaches x = 2.005, but its deliberately loose bounding radius does.
        // Searching thousands of revolutions requires more work than the fixed query budget.
        var status = NarrowPhase.Sweep(rod, wall, JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, new JVector((Real)2.105, 0, 0), JVector.Zero, JVector.Zero,
            JVector.UnitZ, JVector.Zero, (Real)2.01, (Real)14.15, 10000,
            out JVector a, out JVector b, out JVector normal, out Real time);
        Assert.That(status, Is.Not.EqualTo(NarrowPhase.SweepStatus.Hit));
        Assert.That(time, Is.EqualTo(Real.PositiveInfinity));
        Assert.That(a, Is.EqualTo(JVector.Zero));
        Assert.That(b, Is.EqualTo(JVector.Zero));
        Assert.That(normal, Is.EqualTo(JVector.Zero));
    }

    [Test]
    public void MovingRotatingBoxesAgreeWithIndependentSeparationTests()
    {
        Random random = new(83611);
        Real Next(double min, double max) => (Real)(min + (max - min) * random.NextDouble());
        for (int i = 0; i < 2000; i++)
        {
            Real hx = Next(0.05, 2), hy = Next(0.05, 2), jx = Next(0.05, 2), jy = Next(0.05, 2);
            Real aa = Next(-Math.PI, Math.PI), ab = Next(-Math.PI, Math.PI);
            Real wa = Next(-20, 20), wb = Next(-20, 20);
            JVector pa = new(Next(-3, 3), Next(-3, 3), 0), pb = new(Next(-3, 3), Next(-3, 3), 0);
            JVector va = new(Next(-4, 4), Next(-4, 4), 0), vb = new(Next(-4, 4), Next(-4, 4), 0);
            BoxShape a = new(2 * hx, 2 * hy, (Real)0.2), b = new(2 * jx, 2 * jy, (Real)0.2);
            var status = NarrowPhase.Sweep(a, b, JQuaternion.CreateRotationZ(aa), JQuaternion.CreateRotationZ(ab),
                pa, pb, va, vb, wa * JVector.UnitZ, wb * JVector.UnitZ,
                MathR.Sqrt(hx * hx + hy * hy + (Real)0.01), MathR.Sqrt(jx * jx + jy * jy + (Real)0.01), 1,
                out JVector pointA, out JVector pointB, out JVector normal, out Real time);
            Assert.That(status, Is.Not.EqualTo(NarrowPhase.SweepStatus.Failed), $"Case {i}");
            double last = status == NarrowPhase.SweepStatus.Hit ? time : 1;
            if (status == NarrowPhase.SweepStatus.Hit)
            {
                Assert.That(Separation(last), Is.LessThan(3e-4), $"False hit in case {i}");
                if (time > 0)
                    AssertContact(pointA, pointB, normal, time, pa, pb, va, vb, wa * JVector.UnitZ, wb * JVector.UnitZ);
            }
            for (int sample = 0; sample <= 1024; sample++)
            {
                double t = last * sample / 1024;
                if (status == NarrowPhase.SweepStatus.Hit && t >= last - 1e-4) continue;
                Assert.That(Separation(t), Is.GreaterThanOrEqualTo(-2e-4), $"Earlier impact in case {i} at {t}");
            }

            // Independent 2D box SAT, using double-precision BCL trigonometry rather than GJK.
            double Separation(double t)
            {
                double ca = Math.Cos(aa + wa * t), sa = Math.Sin(aa + wa * t);
                double cb = Math.Cos(ab + wb * t), sb = Math.Sin(ab + wb * t);
                double x = pb.X - pa.X + (vb.X - va.X) * t, y = pb.Y - pa.Y + (vb.Y - va.Y) * t;
                double cosine = Math.Abs(ca * cb + sa * sb), sine = Math.Abs(ca * sb - sa * cb);
                return Math.Max(Math.Max(Math.Abs(x * ca + y * sa) - hx - jx * cosine - jy * sine,
                        Math.Abs(-x * sa + y * ca) - hy - jx * sine - jy * cosine),
                    Math.Max(Math.Abs(x * cb + y * sb) - jx - hx * cosine - hy * sine,
                        Math.Abs(-x * sb + y * cb) - jy - hx * sine - hy * cosine));
            }
        }
    }
}
