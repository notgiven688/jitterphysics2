namespace JitterTests.Regression;

/// <summary>
/// The support mapping of <see cref="ConvexHullShape"/> climbs over neighbouring vertices. A flat
/// region of the tessellation facing away from the direction ties in exact arithmetic, which the
/// climb escapes, but with roundoff in the vertices every neighbour is a little higher or lower and
/// the climb could stop in the middle of that region, at the far side of the hull. The climb now
/// starts from the axis extreme farthest along the direction, which such a region never contains.
/// </summary>
public class ConvexHullSupportTests
{
    private const int GridCells = 4;

    // A unit box whose bottom face is a grid of triangles with roundoff-sized noise on its
    // vertices. The first triangle starts at the centre of the grid, so the climb's former start,
    // vertex 0, lies in the interior of the noisy face.
    private static List<JTriangle> NoisyGridBottomBox()
    {
        var random = new Random(11);
        var noise = new Dictionary<(int, int), Real>();

        JVector Bottom(int i, int j)
        {
            if (!noise.TryGetValue((i, j), out Real dy))
            {
                dy = (Real)(random.Next(-2, 3) * 1e-7);
                noise[(i, j)] = dy;
            }

            return new JVector(-(Real)0.5 + i / (Real)GridCells, -(Real)0.5 + dy, -(Real)0.5 + j / (Real)GridCells);
        }

        JVector Top(int i, int j) => new(-(Real)0.5 + i, (Real)0.5, -(Real)0.5 + j);

        var triangles = new List<JTriangle>();
        int c = GridCells / 2;
        triangles.Add(new JTriangle(Bottom(c, c), Bottom(c - 1, c - 1), Bottom(c, c - 1)));
        triangles.Add(new JTriangle(Bottom(c, c), Bottom(c - 1, c), Bottom(c - 1, c - 1)));

        for (int i = 0; i < GridCells; i++)
        {
            for (int j = 0; j < GridCells; j++)
            {
                if (i == c - 1 && j == c - 1) continue;
                triangles.Add(new JTriangle(Bottom(i, j), Bottom(i + 1, j), Bottom(i + 1, j + 1)));
                triangles.Add(new JTriangle(Bottom(i, j), Bottom(i + 1, j + 1), Bottom(i, j + 1)));
            }
        }

        triangles.Add(new JTriangle(Top(0, 0), Top(1, 0), Top(1, 1)));
        triangles.Add(new JTriangle(Top(0, 0), Top(1, 1), Top(0, 1)));

        void Side(JVector t0, JVector t1, Func<int, JVector> bottom)
        {
            for (int k = 0; k < GridCells; k++) triangles.Add(new JTriangle(t0, bottom(k), bottom(k + 1)));
            triangles.Add(new JTriangle(t0, bottom(GridCells), t1));
        }

        Side(Top(0, 0), Top(1, 0), k => Bottom(k, 0));
        Side(Top(1, 1), Top(0, 1), k => Bottom(GridCells - k, GridCells));
        Side(Top(0, 1), Top(0, 0), k => Bottom(0, GridCells - k));
        Side(Top(1, 0), Top(1, 1), k => Bottom(GridCells, k));

        return triangles;
    }

    private static Real BruteForceSupport(List<JTriangle> triangles, in JVector direction)
    {
        Real best = Real.MinValue;
        foreach (JTriangle t in triangles)
        {
            best = MathR.Max(best, MathR.Max(JVector.Dot(t.V0, direction),
                MathR.Max(JVector.Dot(t.V1, direction), JVector.Dot(t.V2, direction))));
        }

        return best;
    }

    [TestCase]
    public void NoisyFlatFaceTurnedAway_DoesNotStopTheClimb()
    {
        List<JTriangle> triangles = NoisyGridBottomBox();
        ConvexHullShape hull = new(triangles);

        hull.SupportMap(JVector.UnitY, out JVector top);
        Assert.That(top.Y, Is.EqualTo((Real)0.5).Within((Real)1e-5));

        hull.SupportMap(-JVector.UnitY, out JVector bottom);
        Assert.That(bottom.Y, Is.EqualTo(-(Real)0.5).Within((Real)1e-5));
    }

    [TestCase]
    public void SupportMap_MatchesBruteForce()
    {
        List<JTriangle> triangles = NoisyGridBottomBox();
        ConvexHullShape hull = new(triangles);
        var random = new Random(7);

        for (int i = 0; i < 2000; i++)
        {
            JVector direction = new((Real)random.NextDouble() - (Real)0.5, (Real)random.NextDouble() - (Real)0.5,
                (Real)random.NextDouble() - (Real)0.5);
            // Many directions close to the axes, where flat faces are nearly turned away.
            if (i % 2 == 0) direction.X *= (Real)1e-3;
            if (i % 4 == 0) direction.Z *= (Real)1e-3;
            direction = JVector.Normalize(direction);

            hull.SupportMap(direction, out JVector support);
            Assert.That(JVector.Dot(support, direction), Is.EqualTo(BruteForceSupport(triangles, direction)).Within((Real)1e-5),
                $"direction {direction}");
        }
    }

    [TestCase]
    public void BoundingBox_IsTheExtentOfTheVertices()
    {
        ConvexHullShape hull = new(NoisyGridBottomBox());
        hull.CalculateBoundingBox(JQuaternion.Identity, JVector.Zero, out JBoundingBox box);

        Assert.That(JVector.MaxAbs(box.Min + new JVector((Real)0.5)), Is.LessThan((Real)1e-5));
        Assert.That(JVector.MaxAbs(box.Max - new JVector((Real)0.5)), Is.LessThan((Real)1e-5));
    }
}
