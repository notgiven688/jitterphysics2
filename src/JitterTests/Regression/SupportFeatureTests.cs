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
            new TransformedShape(new CapsuleShape((Real)0.25, 1), JVector.Zero, JMatrix.CreateScale((Real)0.5, 1, 2))
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
}
