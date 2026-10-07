namespace JitterTests.Regression;

public class SimplexProjectionTests
{
    private static JVector Solve(bool withWitnesses, params JVector[] vertices)
    {
        JVector closest = default;

        if (withWitnesses)
        {
            SimplexSolverAB solver = default;
            foreach (JVector vertex in vertices)
            {
                Assert.That(solver.AddVertex(new MinkowskiDifference.Vertex(vertex), out closest), Is.True);
            }
        }
        else
        {
            SimplexSolver solver = default;
            foreach (JVector vertex in vertices)
            {
                Assert.That(solver.AddVertex(vertex, out closest), Is.True);
            }
        }

        return closest;
    }

    private static void AssertVector(JVector actual, JVector expected, Real tolerance)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(tolerance));
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(tolerance));
            Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(tolerance));
        }
    }

    private static MinkowskiDifference.Vertex Vertex(JVector difference, JVector pointA)
    {
        return new MinkowskiDifference.Vertex { V = difference, A = pointA, B = pointA - difference };
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Segment_LargeVerticesPreserveSmallPerpendicularDistance(bool withWitnesses)
    {
        Real offset = (Real)0.001;
        JVector closest = Solve(withWitnesses,
            new JVector(100000, offset, 0),
            new JVector(-130000, offset, 0));

        // The segment is parallel to X and straddles the origin. Interpolating its
        // large endpoint coordinates leaves an X residual greater than the gap.
        AssertVector(closest, new JVector(0, offset, 0), (Real)1e-9);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Triangle_LargeVerticesPreserveSmallPlaneDistance(bool withWitnesses)
    {
        Real offset = (Real)0.001;
        JVector closest = Solve(withWitnesses,
            new JVector(100000, 100000, offset),
            new JVector(-130000, 100000, offset),
            new JVector(100000, -130000, offset));

        // The XY projection lies inside this triangle; its plane is Z = offset.
        AssertVector(closest, new JVector(0, 0, offset), (Real)1e-9);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Segment_ProjectionOutsideSegmentReturnsNearestEndpoint(bool withWitnesses, bool reverse)
    {
        JVector near = new JVector(2, 1, -3);
        JVector far = new JVector(5, 1, -3);
        JVector closest = reverse ? Solve(withWitnesses, far, near) : Solve(withWitnesses, near, far);

        Assert.That(closest, Is.EqualTo(near));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Segment_ProjectionOnEndpointReturnsThatEndpoint(bool withWitnesses, bool reverse)
    {
        JVector near = new JVector(0, 2, 3);
        JVector far = new JVector(4, 2, 3);
        JVector closest = reverse ? Solve(withWitnesses, far, near) : Solve(withWitnesses, near, far);

        Assert.That(closest, Is.EqualTo(near));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Segment_TinyEdgeRetainsSecondVertexFallback(bool withWitnesses)
    {
        JVector first = new JVector(2, 1, 3);
        JVector second = new JVector(2 + (Real)1e-5, 1, 3);

        Assert.That(Solve(withWitnesses, first, second), Is.EqualTo(second));
        Assert.That(Solve(withWitnesses, second, first), Is.EqualTo(first));
        Assert.That(Solve(withWitnesses, first, first), Is.EqualTo(first));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Triangle_ProjectionOutsideFaceFallsBackToEdge(bool withWitnesses)
    {
        JVector closest = Solve(withWitnesses,
            new JVector(3, -2, 1),
            new JVector(3, 2, 1),
            new JVector(5, 0, 1));

        AssertVector(closest, new JVector(3, 0, 1), (Real)1e-6);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Triangle_CollinearVerticesFallBackToSegment(bool withWitnesses)
    {
        JVector closest = Solve(withWitnesses,
            new JVector(3, -2, 1),
            new JVector(3, 2, 1),
            new JVector(3, 0, 1));

        AssertVector(closest, new JVector(3, 0, 1), (Real)1e-6);
    }

    [TestCase(40)]
    [TestCase(400)]
    public void LargeBox_NearFaceQueriesPreserveGapAndNormal(int extent)
    {
        BoxShape box = new(new JVector(2 * extent, 2, 2 * extent));
        var point = SupportPrimitives.CreatePoint();
        JVector origin = new JVector(-(Real)0.83 * extent, (Real)1.001, -(Real)0.31 * extent);
        Real gap = origin.Y - 1;

        Assert.That(NarrowPhase.PointTest(box, new JVector(origin.X, (Real)0.999, origin.Z)), Is.True);
        Assert.That(NarrowPhase.PointTest(box, origin), Is.False);

        Assert.That(NarrowPhase.RayCast(box, origin, -JVector.UnitY, out Real lambda, out JVector normal), Is.True);
        Assert.That(lambda, Is.EqualTo(gap).Within((Real)1e-5));
        AssertVector(normal, JVector.UnitY, (Real)1e-4);

        Assert.That(NarrowPhase.Distance(box, point, JQuaternion.Identity, origin,
            out _, out _, out normal, out Real distance), Is.True);
        Assert.That(distance, Is.EqualTo(gap).Within((Real)1e-5));
        AssertVector(normal, JVector.UnitY, (Real)1e-4);

        Assert.That(NarrowPhase.Sweep(box, point, JQuaternion.Identity, origin, -JVector.UnitY,
            out _, out _, out normal, out lambda), Is.True);
        Assert.That(lambda, Is.EqualTo(gap).Within((Real)1e-5));
        AssertVector(normal, JVector.UnitY, (Real)1e-4);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Segment_WitnessesUseClampedEndpoint(bool reverse)
    {
        MinkowskiDifference.Vertex near = Vertex(new JVector(2, 1, -3), new JVector(7, -3, 2));
        MinkowskiDifference.Vertex far = Vertex(new JVector(5, 1, -3), new JVector(-1, 5, 6));
        SimplexSolverAB solver = default;
        solver.AddVertex(reverse ? far : near, out _);
        solver.AddVertex(reverse ? near : far, out JVector closest);
        solver.GetClosest(out JVector pointA, out JVector pointB);

        Assert.That(closest, Is.EqualTo(near.V));
        Assert.That(pointA, Is.EqualTo(near.A));
        Assert.That(pointB, Is.EqualTo(near.B));
    }

    [Test]
    public void Segment_WitnessesPreserveInteriorBarycentricWeights()
    {
        SimplexSolverAB solver = default;
        solver.AddVertex(Vertex(new JVector(3, 2, 0), new JVector(7, -3, 2)), out _);
        solver.AddVertex(Vertex(new JVector(-1, 2, 0), new JVector(-1, 5, 6)), out JVector closest);
        solver.GetClosest(out JVector pointA, out JVector pointB);

        AssertVector(closest, new JVector(0, 2, 0), (Real)1e-6);
        AssertVector(pointA, new JVector(1, 3, 5), (Real)1e-6);
        AssertVector(pointB, new JVector(1, 1, 5), (Real)1e-6);
        AssertVector(pointA - pointB, closest, (Real)1e-6);
    }

    [Test]
    public void Triangle_WitnessesPreserveInteriorBarycentricWeights()
    {
        SimplexSolverAB solver = default;
        solver.AddVertex(Vertex(new JVector(2, 2, 3), new JVector(5, 8, 4)), out _);
        solver.AddVertex(Vertex(new JVector(-4, 2, 3), new JVector(-1, 2, 7)), out _);
        solver.AddVertex(Vertex(new JVector(2, -4, 3), new JVector(8, -1, 1)), out JVector closest);
        solver.GetClosest(out JVector pointA, out JVector pointB);

        AssertVector(closest, new JVector(0, 0, 3), (Real)1e-6);
        AssertVector(pointA, new JVector(4, 3, 4), (Real)1e-6);
        AssertVector(pointB, new JVector(4, 3, 1), (Real)1e-6);
        AssertVector(pointA - pointB, closest, (Real)1e-6);
    }

    [Test]
    public void Triangle_WitnessesUseOnlySelectedEdge()
    {
        SimplexSolverAB solver = default;
        solver.AddVertex(Vertex(new JVector(3, -2, 1), new JVector(7, -3, 2)), out _);
        solver.AddVertex(Vertex(new JVector(3, 2, 1), new JVector(-1, 5, 6)), out _);
        solver.AddVertex(Vertex(new JVector(5, 0, 1), new JVector(20, 30, 40)), out JVector closest);
        solver.GetClosest(out JVector pointA, out JVector pointB);

        AssertVector(closest, new JVector(3, 0, 1), (Real)1e-6);
        AssertVector(pointA, new JVector(3, 1, 4), (Real)1e-6);
        AssertVector(pointB, new JVector(0, 1, 3), (Real)1e-6);
        AssertVector(pointA - pointB, closest, (Real)1e-6);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Segment_DegenerateEdgeWitnessesUseSecondVertex(bool duplicate)
    {
        JVector first = new JVector(2, 1, 3);
        JVector second = duplicate ? first : new JVector(2 + (Real)1e-5, 1, 3);
        MinkowskiDifference.Vertex selected = Vertex(second, new JVector(-7, 4, 6));

        SimplexSolverAB solver = default;
        solver.AddVertex(Vertex(first, new JVector(7, 8, 9)), out _);
        solver.AddVertex(selected, out JVector closest);
        solver.GetClosest(out JVector pointA, out JVector pointB);

        Assert.That(closest, Is.EqualTo(second));
        Assert.That(pointA, Is.EqualTo(selected.A));
        Assert.That(pointB, Is.EqualTo(selected.B));
    }
}
