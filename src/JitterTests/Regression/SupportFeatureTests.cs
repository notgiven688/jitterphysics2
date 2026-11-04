namespace JitterTests.Regression;

/// <summary>
/// Shapes that override <see cref="ISupportFeature.SupportFeature"/> must produce the same manifold
/// as <see cref="SupportFeatureSampler"/> does for their support mapping, which serves as the
/// reference here.
/// </summary>
public class SupportFeatureTests
{
    // Replaces the shape's own feature query with the sampler.
    private sealed class SampledOnly(ISupportMappable shape) : ISupportFeature
    {
        public void SupportMap(in JVector direction, out JVector result) => shape.SupportMap(direction, out result);
        public void GetCenter(out JVector point) => shape.GetCenter(out point);

        public int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices) =>
            SupportFeatureSampler.Sample(this, direction, tolerance, vertices);
    }

    private const Real Depth = (Real)0.005;
    private static readonly JVector FloorPosition = new(0, -100, 0);

    private static readonly JQuaternion Lying = JQuaternion.CreateRotationX(MathR.PI / 2);
    private static readonly JQuaternion OnEdge = JQuaternion.CreateRotationX(MathR.PI / 4);

    private static JQuaternion Yaw(Real radians) => JQuaternion.CreateRotationY(radians);

    // A random orientation in which the floor's normal is clearly off every axis of the shape, so a
    // box touches with a vertex and a capsule or cylinder with a point.
    private static JQuaternion RandomOrientation(Random random)
    {
        while (true)
        {
            var axis = new JVector((Real)random.NextDouble() - (Real)0.5, (Real)random.NextDouble() - (Real)0.5,
                (Real)random.NextDouble() - (Real)0.5);
            JQuaternion orientation = JQuaternion.CreateFromAxisAngle(JVector.Normalize(axis),
                (Real)(random.NextDouble() * Math.PI * 2));

            JVector.ConjugatedTransform(JVector.UnitY, orientation, out JVector up);
            if (MathR.Abs(up.X) > (Real)0.1 && MathR.Abs(up.Y) > (Real)0.1 && MathR.Abs(up.Z) > (Real)0.1) return orientation;
        }
    }

    // Places the shape so that its lowest point is Depth below the floor's top face at y = 0.
    private static JVector RestingPosition(ISupportMappable shape, in JQuaternion orientation, Real x, Real z)
    {
        JVector.ConjugatedTransform(-JVector.UnitY, orientation, out JVector down);
        shape.SupportMap(down, out JVector lowest);
        JVector.Transform(lowest, orientation, out lowest);
        return new JVector(x, -Depth - lowest.Y, z);
    }

    private static CollisionManifold Build(ISupportFeature shape, ISupportFeature floor, in JQuaternion orientation,
        in JVector position, in JVector pointA, in JVector pointB, in JVector normal)
    {
        CollisionManifold manifold = default;
        manifold.BuildManifold(shape, floor, orientation, JQuaternion.Identity, position, FloorPosition,
            pointA, pointB, normal);
        return manifold;
    }

    private static (CollisionManifold Feature, CollisionManifold Sampled) BuildBoth(RigidBodyShape shape,
        in JQuaternion orientation, Real x = 0, Real z = 0)
    {
        BoxShape floor = new(200);
        JVector position = RestingPosition(shape, orientation, x, z);

        bool hit = NarrowPhase.MprEpa(shape, floor, orientation, JQuaternion.Identity, position, FloorPosition,
            out JVector pointA, out JVector pointB, out JVector normal, out _);
        Assert.That(hit, Is.True);

        return (Build(shape, floor, orientation, position, pointA, pointB, normal),
            Build(new SampledOnly(shape), new SampledOnly(floor), orientation, position, pointA, pointB, normal));
    }

    // Every contact of the feature manifold must have a counterpart in the sampled one. Sampling a
    // polytope keeps the narrow phase's pair as an extra contact on an edge, which the feature
    // manifold leaves out for points and lines; any other difference in number is a failure.
    private static void AssertSameManifold(in CollisionManifold feature, in CollisionManifold sampled)
    {
        const Real epsilon = (Real)1e-4;

        int extra = sampled.Count - feature.Count;
        Assert.That(extra == 0 || (extra == 1 && feature.Count <= 2), Is.True,
            $"Number of contacts: feature {feature.Count}, sampled {sampled.Count}");

        for (int j = 0; j < feature.Count; j++)
        {
            Real closest = Real.MaxValue;
            for (int i = 0; i < sampled.Count; i++)
            {
                Real distance = MathR.Max((feature.ManifoldA[j] - sampled.ManifoldA[i]).Length(),
                    (feature.ManifoldB[j] - sampled.ManifoldB[i]).Length());
                closest = MathR.Min(closest, distance);
            }

            Assert.That(closest, Is.LessThan(epsilon), $"Feature contact {j} has no sampled counterpart");
        }
    }

    // A triangle in the XZ plane with one edge along the X axis.
    private static TriangleShape CreateTriangle() => new(new TriangleMesh([
        new JTriangle(new JVector(-(Real)0.5, 0, -(Real)0.3), new JVector((Real)0.5, 0, -(Real)0.3),
            new JVector(0, 0, (Real)0.6))
    ]), 0);

    // Turns the default cone onto the line from its apex to its rim on the +Z side.
    private static JQuaternion ConeOnSide()
    {
        JVector sideNormal = JVector.Normalize(new JVector(0, (Real)0.5, 1));
        return JQuaternion.CreateFromToRotation(sideNormal, -JVector.UnitY);
    }

    // A box as a convex hull, with the extents of the box cases.
    private static ConvexHullShape CreateBoxHull()
    {
        JVector h = new((Real)0.5, (Real)0.3, (Real)0.4);
        var corners = new JVector[8];

        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = new JVector((i & 1) == 0 ? -h.X : h.X, (i & 2) == 0 ? -h.Y : h.Y, (i & 4) == 0 ? -h.Z : h.Z);
        }

        // The six faces as quads, each split into two triangles.
        int[] quads = [0, 2, 6, 4, 1, 3, 7, 5, 0, 1, 5, 4, 2, 3, 7, 6, 0, 1, 3, 2, 4, 5, 7, 6];
        var triangles = new List<JTriangle>();

        for (int q = 0; q < quads.Length; q += 4)
        {
            triangles.Add(new JTriangle(corners[quads[q]], corners[quads[q + 1]], corners[quads[q + 2]]));
            triangles.Add(new JTriangle(corners[quads[q]], corners[quads[q + 2]], corners[quads[q + 3]]));
        }

        return new ConvexHullShape(triangles);
    }

    // A hexagonal prism with its hexagons in the XZ plane, each fanned around its first vertex.
    private static ConvexHullShape CreateHexagonalPrismHull()
    {
        const Real radius = (Real)0.5;
        const Real halfHeight = (Real)0.3;

        var top = new JVector[6];
        var bottom = new JVector[6];

        for (int i = 0; i < 6; i++)
        {
            Real angle = (Real)(i * Math.PI / 3.0);
            top[i] = new JVector(radius * MathR.Cos(angle), halfHeight, radius * MathR.Sin(angle));
            bottom[i] = new JVector(top[i].X, -halfHeight, top[i].Z);
        }

        var triangles = new List<JTriangle>();

        for (int i = 1; i < 5; i++)
        {
            triangles.Add(new JTriangle(top[0], top[i], top[i + 1]));
            triangles.Add(new JTriangle(bottom[0], bottom[i], bottom[i + 1]));
        }

        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6;
            triangles.Add(new JTriangle(bottom[i], bottom[j], top[j]));
            triangles.Add(new JTriangle(bottom[i], top[j], top[i]));
        }

        return new ConvexHullShape(triangles);
    }

    // Turns the hexagonal prism onto the side between its first two hexagon vertices.
    private static JQuaternion PrismOnSide()
    {
        JVector sideNormal = new(MathR.Cos(MathR.PI / 6), 0, MathR.Sin(MathR.PI / 6));
        return JQuaternion.CreateFromToRotation(sideNormal, -JVector.UnitY);
    }

    private static IEnumerable<TestCaseData> RestingPoses()
    {
        var random = new Random(1234);

        for (int i = 0; i < 8; i++)
        {
            Real yaw = (Real)(random.NextDouble() * Math.PI * 2);
            JQuaternion generic = RandomOrientation(random);

            yield return new TestCaseData(new SphereShape((Real)0.5), generic, 1).SetName($"Sphere_{i}");
            yield return new TestCaseData(new CapsuleShape((Real)0.25, 1), Yaw(yaw) * Lying, 2).SetName($"CapsuleLying_{i}");
            yield return new TestCaseData(new CapsuleShape((Real)0.25, 1), generic, 1).SetName($"CapsuleGeneric_{i}");
            yield return new TestCaseData(new CylinderShape(1, (Real)0.25), Yaw(yaw) * Lying, 2).SetName($"CylinderLying_{i}");
            yield return new TestCaseData(new CylinderShape(1, (Real)0.25), generic, 1).SetName($"CylinderGeneric_{i}");
            yield return new TestCaseData(new BoxShape((Real)1.0, (Real)0.6, (Real)0.8), Yaw(yaw), 4).SetName($"BoxFace_{i}");
            yield return new TestCaseData(new BoxShape((Real)1.0, (Real)0.6, (Real)0.8), Yaw(yaw) * OnEdge, 2).SetName($"BoxEdge_{i}");
            yield return new TestCaseData(new BoxShape((Real)1.0, (Real)0.6, (Real)0.8), generic, 1).SetName($"BoxVertex_{i}");
            yield return new TestCaseData(new ConeShape(), Yaw(yaw), 4).SetName($"ConeBase_{i}");
            yield return new TestCaseData(new ConeShape(), Yaw(yaw) * ConeOnSide(), 2).SetName($"ConeSide_{i}");
            yield return new TestCaseData(new ConeShape(), Yaw(yaw) * JQuaternion.CreateRotationX(MathR.PI), 1).SetName($"ConeApex_{i}");
            yield return new TestCaseData(new ConeShape(), generic, 1).SetName($"ConeGeneric_{i}");
            // Lying flat, the face rests on the floor; the narrow phase's pair is kept for a face.
            yield return new TestCaseData(CreateTriangle(), Yaw(yaw), 4).SetName($"TriangleFace_{i}");
            yield return new TestCaseData(CreateTriangle(), Yaw(yaw) * JQuaternion.CreateRotationX(-MathR.PI / 2), 2).SetName($"TriangleEdge_{i}");
            yield return new TestCaseData(CreateTriangle(), generic, 1).SetName($"TriangleVertex_{i}");
            yield return new TestCaseData(CreateBoxHull(), Yaw(yaw), 4).SetName($"HullFace_{i}");
            yield return new TestCaseData(CreateBoxHull(), Yaw(yaw) * OnEdge, 2).SetName($"HullEdge_{i}");
            yield return new TestCaseData(CreateBoxHull(), generic, 1).SetName($"HullVertex_{i}");
            // On a hexagon, the six vertices of the feature and the six probes of the sampling are
            // both reduced to four contacts.
            yield return new TestCaseData(CreateHexagonalPrismHull(), Yaw(yaw), 4).SetName($"HullHexagon_{i}");
            yield return new TestCaseData(CreateHexagonalPrismHull(), Yaw(yaw) * PrismOnSide(), 4).SetName($"HullRectangle_{i}");
        }
    }

    [TestCaseSource(nameof(RestingPoses))]
    public void Feature_MatchesSampledManifold(RigidBodyShape shape, JQuaternion orientation, int expectedCount)
    {
        var (feature, sampled) = BuildBoth(shape, orientation, 3, -2);

        Assert.That(feature.Count, Is.EqualTo(expectedCount));
        AssertSameManifold(feature, sampled);
    }

    // A cylinder standing on a cap rests on a disc. The sampling finds six points of its rim
    // around the contact normal, the feature reports an octagon in the frame of the cylinder;
    // both are reduced to four contacts on the rim.
    [TestCase(0.0)]
    [TestCase(0.3)]
    [TestCase(1.1)]
    public void StandingCylinder_ContactsLieOnTheRim(double yaw)
    {
        const Real radius = (Real)0.25;
        var (feature, _) = BuildBoth(new CylinderShape(1, radius), Yaw((Real)yaw), 2, 1);
        JVector axis = new(2, 0, 1);

        Assert.That(feature.Count, Is.EqualTo(4));

        for (int i = 0; i < feature.Count; i++)
        {
            JVector onCylinder = feature.ManifoldA[i];
            JVector onFloor = feature.ManifoldB[i];
            Real distanceFromAxis = new JVector(onCylinder.X - axis.X, 0, onCylinder.Z - axis.Z).Length();

            Assert.That(distanceFromAxis, Is.EqualTo(radius).Within((Real)1e-4));
            Assert.That(onCylinder.Y, Is.EqualTo(-Depth).Within((Real)1e-4));
            Assert.That(onFloor.Y, Is.EqualTo((Real)0.0).Within((Real)1e-4));
        }
    }

    // Every reported vertex must be within the tolerance of being a support point of the direction.
    [TestCase]
    public void FeatureVertices_AreSupportPoints()
    {
        var random = new Random(4321);
        ISupportFeature[] shapes =
        [
            new SphereShape((Real)0.5), new CapsuleShape((Real)0.25, 1), new CylinderShape(1, (Real)0.25),
            new BoxShape((Real)1.0, (Real)0.6, (Real)0.8),
            new ConeShape(), CreateTriangle(),
            new TransformedShape(new BoxShape(1), new JVector(1, 2, 3), JMatrix.CreateRotationY((Real)0.4)),
            new TransformedShape(new CapsuleShape((Real)0.25, 1), JVector.Zero, JMatrix.CreateScale((Real)0.5, 1, 2)),
            CreateBoxHull(), CreateHexagonalPrismHull()
        ];

        const Real tolerance = (Real)0.01;
        Span<JVector> vertices = stackalloc JVector[ISupportFeature.MaxVertices];

        foreach (ISupportFeature shape in shapes)
        {
            for (int i = 0; i < 200; i++)
            {
                // Bias some directions towards the axes, where edges and faces are reported.
                JVector direction = new((Real)random.NextDouble() - (Real)0.5, (Real)random.NextDouble() - (Real)0.5,
                    (Real)random.NextDouble() - (Real)0.5);
                if (i % 2 == 0) direction.X *= (Real)0.002;
                if (i % 4 == 0) direction.Z *= (Real)0.002;
                direction = JVector.Normalize(direction);

                shape.SupportMap(direction, out JVector support);
                Real best = JVector.Dot(support, direction);

                int count = shape.SupportFeature(direction, tolerance, vertices);
                Assert.That(count, Is.InRange(1, ISupportFeature.MaxVertices));

                for (int k = 0; k < count; k++)
                {
                    // Within the tolerance angle, a vertex may lie below the support plane by at most
                    // the tolerance times the shape's extent (a few units here).
                    Assert.That(JVector.Dot(vertices[k], direction), Is.GreaterThan(best - (Real)5 * tolerance),
                        $"{shape.GetType().Name}, direction {direction}");
                    Assert.That(JVector.Dot(vertices[k], direction), Is.LessThanOrEqualTo(best + (Real)1e-4),
                        $"{shape.GetType().Name}, direction {direction}");
                }
            }
        }
    }

    // A hull reports a face as its convex outline in winding order around the direction, also when
    // the face is triangulated around one of its vertices. Tilted further than the tolerance off
    // the face, it reports the edge or vertex that stays highest.
    [TestCase(0.0, 0.0, 6)]
    [TestCase(0.005, 0.0, 6)]
    [TestCase(0.03, 0.0, 1)]
    [TestCase(0.03, 30.0, 2)]
    public void HullFeature_IsTheOutlineOfTheHexagon(double tilt, double tiltDegrees, int expectedCount)
    {
        ConvexHullShape prism = CreateHexagonalPrismHull();
        Real angle = (Real)(tiltDegrees * Math.PI / 180.0);
        JVector direction = new((Real)tilt * MathR.Cos(angle), -1, (Real)tilt * MathR.Sin(angle));

        Span<JVector> vertices = stackalloc JVector[ISupportFeature.MaxVertices];
        int count = prism.SupportFeature(direction, (Real)0.01, vertices);

        Assert.That(count, Is.EqualTo(expectedCount));

        for (int i = 0; i < count; i++)
        {
            Assert.That(vertices[i].Y, Is.EqualTo(-(Real)0.3).Within((Real)1e-6));
            Assert.That(new JVector(vertices[i].X, 0, vertices[i].Z).Length(), Is.EqualTo((Real)0.5).Within((Real)1e-6));
        }

        if (count < 3) return;

        for (int i = 0; i < count; i++)
        {
            JVector edge = vertices[(i + 1) % count] - vertices[i];
            JVector nextEdge = vertices[(i + 2) % count] - vertices[(i + 1) % count];
            Assert.That(JVector.Dot(edge % nextEdge, direction), Is.GreaterThan((Real)0.0));
        }
    }
}
