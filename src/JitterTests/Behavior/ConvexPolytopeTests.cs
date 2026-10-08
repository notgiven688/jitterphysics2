using Vertex = Jitter2.Collision.MinkowskiDifference.Vertex;
using Expansion = Jitter2.Collision.ConvexPolytope.AddVertexResult;

namespace JitterTests.Behavior;

public class ConvexPolytopeTests
{
    private static ConvexPolytope Tetrahedron(params JVector[] points)
    {
        var hull = new ConvexPolytope();
        hull.InitHeap();
        for (int i = 0; i < 4; i++) hull.GetVertex(i) = new Vertex(points[i]);
        Assert.That(hull.InitTetrahedron(), Is.True);
        return hull;
    }

    private static void AssertClosed(in ConvexPolytope hull)
    {
        var edges = new Dictionary<(short, short), int>();
        foreach (var face in hull.HullTriangles)
        {
            Assert.That(face.NormalSq, Is.GreaterThan(0));
            foreach (var (a, b) in new[] { (face.A, face.B), (face.B, face.C), (face.C, face.A) })
            {
                var edge = a < b ? (a, b) : (b, a);
                edges.TryGetValue(edge, out int count);
                edges[edge] = count + 1;
            }
        }

        Assert.That(edges.Values, Is.All.EqualTo(2), "Every indexed edge must have two incident triangles.");
    }

    [Test]
    public void ExistingVertexHasNoHorizonAndLeavesSavedTriangleUsable()
    {
        var hull = Tetrahedron(new JVector(-1), JVector.UnitX, JVector.UnitY, JVector.UnitZ);
        var offset = new JVector(2, 3, 4);
        for (int i = 0; i < 4; i++)
        {
            ref var vertex = ref hull.GetVertex(i);
            vertex.A = vertex.V + offset;
            vertex.B = offset;
        }

        var support = new Vertex(new JVector(1)) { A = new JVector(1) + offset, B = offset };
        Assert.That(hull.AddVertexDetailed(support), Is.EqualTo(Expansion.Added));
        var closest = hull.GetClosestTriangle();
        hull.CalculatePoints(closest, out var beforeA, out var beforeB);
        Assert.That((beforeA - beforeB - closest.ClosestToOrigin).Length(), Is.LessThan((Real)1e-6));
        var before = hull.HullTriangles.ToArray();
        Assert.That(hull.AddVertexDetailed(support), Is.EqualTo(Expansion.NoHorizon));
        Assert.That(hull.HullTriangles.ToArray(), Is.EqualTo(before));
        hull.CalculatePoints(closest, out var afterA, out var afterB);
        Assert.That(afterA, Is.EqualTo(beforeA));
        Assert.That(afterB, Is.EqualTo(beforeB));
        AssertClosed(hull);
    }

    [Test]
    public void SupportBeyondInteriorCornerDoesNotUseCollinearHorizonEdge()
    {
        var hull = Tetrahedron(JVector.Zero, new JVector(1, 0, 1), new JVector(-1, 0, 1), new JVector(0, -1, -1));
        var opposite = new Vertex(new JVector(1, 0, -1));
        Assert.That(hull.AddVertexDetailed(opposite), Is.EqualTo(Expansion.Added));
        AssertClosed(hull);
        Assert.That(hull.AddVertexDetailed(opposite), Is.EqualTo(Expansion.NoHorizon));
        AssertClosed(hull);
    }

    [Test]
    public void NonzeroAreaBelowFloatingDegeneracyThresholdIsRetained()
    {
        var hull = Tetrahedron(JVector.Zero, JVector.UnitX, new JVector(0, (Real)1e-9, 0), JVector.UnitZ);
        Assert.That(hull.HullTriangles.Length, Is.EqualTo(4));
        AssertClosed(hull);
    }

    [Test]
    public void InvalidExpansionCanReinitializeAndExpand()
    {
        var hull = Tetrahedron(JVector.Zero, JVector.UnitX, JVector.UnitY, JVector.UnitZ);
        Assert.That(hull.AddVertexDetailed(new Vertex(new JVector(Real.NaN, 0, 0))), Is.EqualTo(Expansion.NumericalFailure));
        Assert.That(hull.InitTetrahedron(), Is.True);
        Assert.That(hull.AddVertexDetailed(new Vertex(new JVector(1))), Is.EqualTo(Expansion.Added));
        AssertClosed(hull);
    }

#if !USE_DOUBLE_PRECISION
    [Test]
    public void NumericalFailureDuringExpansionCanReinitializeAndExpand()
    {
        Real scale = (Real)Math.ScaleB(1, 80);
        var hull = Tetrahedron(new JVector(scale, 0, 0), new JVector(-scale, 0, 0),
            new JVector(0, scale, 0), JVector.UnitZ);

        // The seed has small face distances, but the new fan's squared distances overflow Real.
        Assert.That(hull.AddVertexDetailed(new Vertex(new JVector(0, 0, -scale))), Is.EqualTo(Expansion.NumericalFailure));
        Assert.That(hull.HullTriangles.Length, Is.Zero);
        Assert.Throws<InvalidOperationException>(() => hull.GetClosestTriangle());
        Assert.That(hull.InitTetrahedron(), Is.True);
        Assert.That(hull.AddVertexDetailed(new Vertex(new JVector(0, 0, -1))), Is.EqualTo(Expansion.Added));
        AssertClosed(hull);
    }
#endif

    [Test]
    public void CoplanarSeedIsRejectedWithoutExposingPartialHull()
    {
        var hull = new ConvexPolytope();
        hull.InitHeap();
        hull.GetVertex(0) = new Vertex(JVector.Zero);
        hull.GetVertex(1) = new Vertex(JVector.UnitX);
        hull.GetVertex(2) = new Vertex(JVector.UnitY);
        hull.GetVertex(3) = new Vertex(JVector.UnitX + JVector.UnitY);
        Assert.That(hull.InitTetrahedron(), Is.False);
        Assert.That(hull.HullTriangles.Length, Is.Zero);
    }

#if USE_DOUBLE_PRECISION
    [TestCase(-200)]
    [TestCase(200)]
    public void CoordinatesAtSupportedRangeBoundariesCanExpand(int exponent)
    {
        Real scale = Math.ScaleB(1, exponent);
        var hull = Tetrahedron(JVector.Zero, scale * JVector.UnitX, scale * JVector.UnitY, scale * JVector.UnitZ);
        var support = new Vertex(new JVector(scale));
        Assert.That(hull.AddVertexDetailed(support), Is.EqualTo(Expansion.Added));
        AssertClosed(hull);
        Assert.That(hull.AddVertexDetailed(support), Is.EqualTo(Expansion.NoHorizon));
        AssertClosed(hull);
    }

    [TestCase(-201)]
    [TestCase(201)]
    [TestCase(-1074)]
    public void OutOfRangeSeedIsRejectedWithoutExposingPartialHull(int exponent)
    {
        var hull = new ConvexPolytope();
        hull.InitHeap();
        hull.GetVertex(0) = new Vertex(new JVector(Math.ScaleB(1, exponent), 0, 0));
        hull.GetVertex(1) = new Vertex(JVector.UnitX);
        hull.GetVertex(2) = new Vertex(JVector.UnitY);
        hull.GetVertex(3) = new Vertex(JVector.UnitZ);
        Assert.That(hull.InitTetrahedron(), Is.False);
        Assert.That(hull.HullTriangles.Length, Is.Zero);
    }

    [TestCase(-201)]
    [TestCase(201)]
    [TestCase(-1074)]
    public void OutOfRangeExpansionCanReinitializeAndExpand(int exponent)
    {
        var hull = Tetrahedron(JVector.Zero, JVector.UnitX, JVector.UnitY, JVector.UnitZ);
        var support = new Vertex(new JVector(Math.ScaleB(1, exponent), 1, 1));
        Assert.That(hull.AddVertexDetailed(support), Is.EqualTo(Expansion.NumericalFailure));
        Assert.That(hull.InitTetrahedron(), Is.True);
        Assert.That(hull.AddVertexDetailed(new Vertex(new JVector(1))), Is.EqualTo(Expansion.Added));
        AssertClosed(hull);
    }
#endif

    [Test]
    public void Collision_DuplicateSupportRegressionMatchesBoxSat()
    {
        var a = SupportPrimitives.CreateBox(new JVector(2, (Real)0.25, 1));
        var b = SupportPrimitives.CreateBox(new JVector((Real)0.5));
        var rotation = new JQuaternion((Real)(-0.34783155f), (Real)0.69148487f, (Real)(-0.5953642f), (Real)(-0.21541446f));
        var position = new JVector((Real)0.53918016f, (Real)0.12698002f, (Real)(-1.1122025f));
        Real expected = MinimumBoxDepth(a, b, rotation, position);

        Assert.That(NarrowPhase.Collision(a, b, rotation, position, out _, out _, out var normal, out var depth), Is.True);
        Assert.That(depth, Is.EqualTo(expected).Within((Real)1e-4));
        Assert.That(Real.IsFinite(normal.X) && Real.IsFinite(normal.Y) && Real.IsFinite(normal.Z), Is.True);
    }

    private static Real MinimumBoxDepth(ISupportMappable a, ISupportMappable b, JQuaternion rotation, JVector position)
    {
        JVector[] axesA = [JVector.UnitX, JVector.UnitY, JVector.UnitZ];
        JVector[] axesB = axesA.Select(v => JVector.Transform(v, rotation)).ToArray();
        Real minimum = Real.MaxValue;
        void Axis(JVector axis)
        {
            if (axis.LengthSquared() < (Real)1e-12) return;
            axis = JVector.Normalize(axis);
            MinkowskiDifference.Support(a, b, rotation, position, axis, out var p);
            MinkowskiDifference.Support(a, b, rotation, position, -axis, out var n);
            minimum = MathR.Min(minimum, MathR.Min(JVector.Dot(p.V, axis), JVector.Dot(n.V, -axis)));
        }

        foreach (var axis in axesA) Axis(axis);
        foreach (var axis in axesB) Axis(axis);
        foreach (var axisA in axesA) foreach (var axisB in axesB) Axis(JVector.Cross(axisA, axisB));
        return minimum;
    }

    [Test]
    public void MprEpa_CollinearSupportRegressionMatchesBoxSat()
    {
        var box = SupportPrimitives.CreateBox(new JVector((Real)0.3, (Real)1.2, (Real)0.7));
#if USE_DOUBLE_PRECISION
        var rotation = new JQuaternion(0, 0.999998129169539, 0, -0.0019343364293572605);
        var position = new JVector(0, 0.43866746976909576, 0);
#else
        var rotation = new JQuaternion(0, 0.99999815f, 0, -0.0019342888f);
        var position = new JVector(0, 0.43866748f, 0);
#endif
        AssertMprDepth(box, box, rotation, position);
    }

    [Test]
    public void MprEpa_ObliqueBoxRegressionMatchesBoxSat()
    {
        var box = SupportPrimitives.CreateBox(new JVector(2, (Real)0.25, 1));
#if USE_DOUBLE_PRECISION
        var rotation = new JQuaternion(-0.28864726319747464, -0.15584983931147614, 0.15853538827229016, -0.9312680149669853);
        var position = new JVector(-0.7871254097758174, 0.20686499835933464, -0.014221211987464191);
#else
        var rotation = new JQuaternion(-0.28864738f, -0.1558499f, 0.15853547f, -0.931268f);
        var position = new JVector(-0.7871254f, 0.20686495f, -0.014221248f);
#endif
        AssertMprDepth(box, box, rotation, position);
    }

    private static void AssertMprDepth(ISupportMappable a, ISupportMappable b, JQuaternion rotation, JVector position)
    {
        Real expected = MinimumBoxDepth(a, b, rotation, position);
        Assert.That(NarrowPhase.MprEpa(a, b, rotation, position, out _, out _, out var normal, out var depth), Is.True);
        Assert.That(depth, Is.EqualTo(expected).Within((Real)1e-4));
        MinkowskiDifference.Support(a, b, rotation, position, normal, out var support);
        Assert.That(JVector.Dot(support.V, normal), Is.EqualTo(depth).Within((Real)1e-4));
    }

    [Test]
    public void MprEpa_FloorContactsRemainExact([Values("cube", "cylinder", "flat box")] string shapeName,
        [Values(0f, 0.02f)] float threshold)
    {
        ISupportMappable shape = shapeName switch
        {
            "cube" => SupportPrimitives.CreateBox(new JVector((Real)0.5)),
            "cylinder" => SupportPrimitives.CreateCylinder((Real)0.5, (Real)0.5),
            _ => SupportPrimitives.CreateBox(new JVector((Real)1.5, (Real)0.1, (Real)0.7))
        };
        var floor = SupportPrimitives.CreateBox(new JVector(20, (Real)0.5, 20));
        shape.SupportMap(JVector.UnitY, out var top);
        int wrong = 0;
        string first = "";
        foreach (Real depth in new Real[] { (Real)0.001, (Real)0.005, (Real)0.01, (Real)0.025, (Real)0.05, (Real)0.1 })
        for (int yaw = 0; yaw < 90; yaw += 3)
        for (int x = -2; x <= 2; x++)
        for (int z = -2; z <= 2; z++)
        {
            var rotation = JQuaternion.CreateRotationY(yaw * MathR.PI / 180);
            var position = new JVector(x * (Real)0.37, top.Y - depth, z * (Real)0.53);
            bool hit = NarrowPhase.MprEpa(shape, floor, rotation, JQuaternion.Identity, position, new JVector(0, (Real)(-0.5), 0),
                out _, out _, out var normal, out var actual, (Real)threshold);
            if (hit && Real.IsFinite(actual) && MathR.Abs(actual - depth) <= (Real)1e-4 && MathR.Abs(normal.Y) >= (Real)0.9999) continue;
            if (wrong++ == 0) first = $"depth={depth}, yaw={yaw}, position={position}, result={actual}, normal={normal}";
        }

        Assert.That(wrong, Is.Zero, first);
    }
}
