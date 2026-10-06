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

// Queries just off the top face and one long edge of a big flat box, on a fixed grid, where the answer is known.
// The box's corners sit far from the origin, which is what used to swamp GJK's closest point with rounding.
public class GjkPrecisionTests
{
    private static readonly Real[] Gaps = [(Real)0.0005, (Real)0.002, (Real)0.005, (Real)0.01, (Real)0.02];
    private static readonly JVector FloorCenter = new(0, (Real)(-0.5), 0);

    private static readonly (string Name, ISupportMappable Shape, Real Bottom)[] Shapes =
    [
        ("sphere", SupportPrimitives.CreateSphere((Real)0.4), (Real)0.4),
        ("capsule", SupportPrimitives.CreateCapsule((Real)0.3, (Real)0.5), (Real)0.8),
        ("box", SupportPrimitives.CreateBox(new JVector((Real)0.3, (Real)0.3, (Real)0.3)), (Real)0.3),
    ];

    private static SupportPrimitives.Box Floor(float halfExtent) => SupportPrimitives.CreateBox(new JVector((Real)halfExtent, (Real)0.5, (Real)halfExtent));

    // Places over the floor's top face, each with a gap to test at.
    private static IEnumerable<(Real X, Real Z, Real Gap)> Grid(float halfExtent)
    {
        Real reach = MathR.Min(3, (Real)halfExtent - 1);
        for (int x = -2; x <= 2; x++)
        for (int z = -2; z <= 2; z++)
        foreach (Real gap in Gaps)
            yield return (x * reach / 2, z * reach / 2, gap);
    }

    // Every shape over every place, upright and turned.
    private static IEnumerable<(string Name, ISupportMappable Shape, JQuaternion Turn, JVector Touching, Real Gap)> Placements(float halfExtent)
    {
        foreach (var (x, z, gap) in Grid(halfExtent))
        foreach (var (name, shape, bottom) in Shapes)
        foreach (int yaw in new[] { 0, 25 })
            yield return (name, shape, JQuaternion.CreateRotationY(yaw * MathR.PI / 180), new JVector(x, bottom, z), gap);
    }

    private static void AssertNoneWrong(List<string> wrong, int total)
        => Assert.That(wrong, Is.Empty, $"{wrong.Count} of {total} wrong, first: {wrong.FirstOrDefault()}");

    [TestCase(40f)]
    [TestCase(400f)]
    public void RayCast_HitsTheFaceAndEdgeExactly(float halfExtent)
    {
        var floor = Floor(halfExtent);
        JVector[] downs = [new(0, -1, 0), JVector.Normalize(new JVector((Real)0.3, -1, 0)), JVector.Normalize(new JVector(0, -1, (Real)0.3)), JVector.Normalize(new JVector((Real)0.2, -1, (Real)(-0.2)))];
        JVector toEdge = JVector.Normalize(new JVector(-1, -1, 0));
        var wrong = new List<string>();
        int total = 0;

        foreach (var (x, z, gap) in Grid(halfExtent))
        {
            foreach (JVector down in downs)
            {
                total++;
                bool hit = NarrowPhase.RayCast(floor, new JVector(x, gap, z) - FloorCenter, down, out Real lambda, out JVector normal);
                if (!hit || MathR.Abs(lambda - gap / -down.Y) > (Real)1e-4 || normal.Y < (Real)0.999)
                    wrong.Add($"ray from {gap} above the face along {down} hit {hit} at {lambda}, normal {normal}");
            }

            total++;
            bool edgeHit = NarrowPhase.RayCast(floor, new JVector((Real)halfExtent + gap, gap, z) - FloorCenter, toEdge, out Real edgeLambda, out _);
            if (!edgeHit || MathR.Abs(edgeLambda - gap * MathR.Sqrt(2)) > (Real)1e-4)
                wrong.Add($"ray from {gap} off the edge hit {edgeHit} at {edgeLambda}");
        }

        AssertNoneWrong(wrong, total);
    }

    [TestCase(40f)]
    [TestCase(400f)]
    public void PointTest_TellsInsideFromOutside(float halfExtent)
    {
        var floor = Floor(halfExtent);
        var wrong = new List<string>();
        int total = 0;

        foreach (var (x, z, gap) in Grid(halfExtent))
        {
            total += 4;
            if (!NarrowPhase.PointTest(floor, new JVector(x, -gap, z) - FloorCenter)) wrong.Add($"{gap} below the face was outside");
            if (NarrowPhase.PointTest(floor, new JVector(x, gap, z) - FloorCenter)) wrong.Add($"{gap} above the face was inside");
            if (!NarrowPhase.PointTest(floor, new JVector((Real)halfExtent - gap, -gap, z) - FloorCenter)) wrong.Add($"{gap} inside the edge was outside");
            if (NarrowPhase.PointTest(floor, new JVector((Real)halfExtent + gap, gap, z) - FloorCenter)) wrong.Add($"{gap} outside the edge was inside");
        }

        AssertNoneWrong(wrong, total);
    }

    [TestCase(4f)]
    [TestCase(40f)]
    [TestCase(400f)]
    public void Overlap_SeesTheGapAboveTheFace(float halfExtent)
    {
        var floor = Floor(halfExtent);
        var wrong = new List<string>();
        int total = 0;

        foreach (var (name, shape, turn, touching, gap) in Placements(halfExtent))
        {
            total += 2;
            if (NarrowPhase.Overlap(shape, floor, turn, JQuaternion.Identity, touching + JVector.UnitY * gap, FloorCenter))
                wrong.Add($"{name} {gap} above the face overlapped");
            if (!NarrowPhase.Overlap(shape, floor, turn, JQuaternion.Identity, touching - JVector.UnitY * gap, FloorCenter))
                wrong.Add($"{name} {gap} into the face did not overlap");
        }

        AssertNoneWrong(wrong, total);
    }

    [TestCase(4f)]
    [TestCase(40f)]
    [TestCase(400f)]
    public void Distance_MeasuresTheGapAboveTheFace(float halfExtent)
    {
        var floor = Floor(halfExtent);
        var wrong = new List<string>();
        int total = 0;

        foreach (var (name, shape, turn, touching, gap) in Placements(halfExtent))
        {
            total++;
            bool separated = NarrowPhase.Distance(shape, floor, turn, JQuaternion.Identity, touching + JVector.UnitY * gap, FloorCenter,
                out _, out _, out _, out Real distance);
            if (!separated || MathR.Abs(distance - gap) > (Real)1e-4)
                wrong.Add($"{name} {gap} above the face gave separated {separated}, distance {distance}");
        }

        AssertNoneWrong(wrong, total);
    }

    // The whole scene turned three ways, so the floor's face and edge run in no particular direction.
    private static readonly JQuaternion[] Turns =
    [
        JQuaternion.Identity,
        JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(1, 2, 3)), (Real)0.4),
        JQuaternion.CreateFromAxisAngle(JVector.Normalize(new JVector(-2, 1, 1)), (Real)0.8),
    ];

    [TestCase(4000f)]
    public void Distance_NeverCallsAnOverlapSeparated(float halfExtent)
    {
        var floor = Floor(halfExtent);
        var wrong = new List<string>();
        int total = 0;

        foreach (JQuaternion scene in Turns)
        foreach (var (name, shape, turn, touching, depth) in Placements(halfExtent))
        {
            JVector floorCenter = JVector.Transform(FloorCenter, scene);
            JVector intoFace = JVector.Transform(touching - JVector.UnitY * depth, scene);
            JVector overEdge = JVector.Transform(new JVector((Real)halfExtent + touching.X * (Real)0.02, touching.Y - depth, touching.Z), scene);

            foreach (var (place, at) in new[] { ("into the face", intoFace), ("over the edge", overEdge) })
            {
                if (!NarrowPhase.Overlap(shape, floor, scene * turn, scene, at, floorCenter)) continue;
                total++;
                if (NarrowPhase.Distance(shape, floor, scene * turn, scene, at, floorCenter, out _, out _, out _, out Real distance))
                    wrong.Add($"{name} {depth} {place} was called separated by {distance}");
            }
        }

        Assert.That(total, Is.GreaterThan(1000));
        AssertNoneWrong(wrong, total);
    }

    [TestCase(4f)]
    [TestCase(400f)]
    [TestCase(4000f)]
    public void Distance_MeasuresTheGapWhicheverWayTheFloorTurns(float halfExtent)
    {
        var floor = Floor(halfExtent);
        var wrong = new List<string>();
        int total = 0;

        foreach (JQuaternion scene in Turns)
        foreach (var (name, shape, turn, touching, gap) in Placements(halfExtent))
        {
            JVector floorCenter = JVector.Transform(FloorCenter, scene);

            // A small gap measured exactly, and a wide one still seen as apart.
            foreach (Real expected in new[] { gap, (Real)0.05 + gap * 20 })
            {
                total++;
                bool separated = NarrowPhase.Distance(shape, floor, scene * turn, scene, JVector.Transform(touching + JVector.UnitY * expected, scene), floorCenter,
                    out _, out _, out _, out Real distance);
                if (!separated || MathR.Abs(distance - expected) > (Real)1e-4)
                    wrong.Add($"{name} {expected} above the face gave separated {separated}, distance {distance}");
            }
        }

        AssertNoneWrong(wrong, total);
    }

    [TestCase(4f)]
    [TestCase(40f)]
    [TestCase(400f)]
    public void Sweep_StopsExactlyOnTheFace(float halfExtent)
    {
        const float Drop = 0.5f;
        var floor = Floor(halfExtent);
        var wrong = new List<string>();
        int total = 0;

        foreach (var (name, shape, turn, touching, gap) in Placements(halfExtent))
        {
            total++;
            bool hit = NarrowPhase.Sweep(shape, floor, turn, JQuaternion.Identity, touching + JVector.UnitY * ((Real)Drop + gap), FloorCenter,
                -JVector.UnitY, JVector.Zero, out _, out _, out _, out Real lambda);
            if (!hit || MathR.Abs(lambda - ((Real)Drop + gap)) > (Real)1e-4)
                wrong.Add($"{name} swept down from {(Real)Drop + gap} above the face hit {hit} after {lambda}");
        }

        AssertNoneWrong(wrong, total);
    }
}
