namespace JitterTests.Regression;

public class CollisionManifoldTests
{
    private static VertexSupportMap CreateRidgeSupport()
    {
        return new([
            new JVector(-2, -1, 0),
            new JVector(2, -1, 0),
            new JVector(-2, 1, -1),
            new JVector(2, 1, -1),
            new JVector(-2, 1, 1),
            new JVector(2, 1, 1)
        ]);
    }

    private static CollisionManifold BuildManifold<Ta, Tb>(in Ta shapeA, in Tb shapeB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        bool hit = NarrowPhase.Collision(shapeA, shapeB, orientationA, orientationB, positionA, positionB,
            out pointA, out pointB, out normal, out penetration);

        Assert.That(hit, Is.True);

        CollisionManifold manifold = default;
        manifold.BuildManifold(shapeA, shapeB, orientationA, orientationB, positionA, positionB, pointA, pointB, normal);

        return manifold;
    }

    private static void AssertUniqueContacts(CollisionManifold manifold, Real epsilonSq)
    {
        for (int i = 0; i < manifold.Count; i++)
        {
            for (int j = i + 1; j < manifold.Count; j++)
            {
                Assert.That((manifold.ManifoldA[i] - manifold.ManifoldA[j]).LengthSquared(), Is.GreaterThan(epsilonSq));
            }
        }
    }

    private static void AssertSamePointSet(IReadOnlyList<JVector> expected, IReadOnlyList<JVector> actual, Real epsilonSq)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count));

        bool[] matched = new bool[actual.Count];

        foreach (JVector expectedPoint in expected)
        {
            int match = -1;

            for (int i = 0; i < actual.Count; i++)
            {
                if (matched[i]) continue;
                if ((actual[i] - expectedPoint).LengthSquared() > epsilonSq) continue;

                match = i;
                break;
            }

            Assert.That(match, Is.GreaterThanOrEqualTo(0));
            matched[match] = true;
        }
    }

    private static void AssertQuadrantCoverage(CollisionManifold manifold, in JVector referencePosition, Real axisEpsilon)
    {
        bool positiveXPositiveZ = false;
        bool positiveXNegativeZ = false;
        bool negativeXPositiveZ = false;
        bool negativeXNegativeZ = false;

        for (int i = 0; i < manifold.Count; i++)
        {
            JVector local = manifold.ManifoldA[i] - referencePosition;

            Assert.That(MathR.Abs(local.X), Is.GreaterThan(axisEpsilon));
            Assert.That(MathR.Abs(local.Z), Is.GreaterThan(axisEpsilon));

            if (local.X > (Real)0.0)
            {
                if (local.Z > (Real)0.0) positiveXPositiveZ = true;
                else positiveXNegativeZ = true;
            }
            else
            {
                if (local.Z > (Real)0.0) negativeXPositiveZ = true;
                else negativeXNegativeZ = true;
            }
        }

        Assert.That(positiveXPositiveZ, Is.True);
        Assert.That(positiveXNegativeZ, Is.True);
        Assert.That(negativeXPositiveZ, Is.True);
        Assert.That(negativeXNegativeZ, Is.True);
    }

    [TestCase(0.5, false)]
    [TestCase(0.5, true)]
    [TestCase(1.0, false)]
    [TestCase(1.0, true)]
    public void SphereAgainstFloor_PreservesOriginalContact(double radius, bool sphereFirst)
    {
        RigidBodyShape floor = new BoxShape(200, 200, 200);
        RigidBodyShape sphere = new SphereShape((Real)radius);
        JVector floorPosition = new(0, -100, 0);
        JVector spherePosition = new(3, (Real)radius - (Real)0.005, -2);
        JVector floorPoint = new(3, 0, -2);
        JVector spherePoint = new(3, -(Real)0.005, -2);

        CollisionManifold manifold = default;
        manifold.BuildManifold(sphereFirst ? sphere : floor, sphereFirst ? floor : sphere,
            JQuaternion.Identity, JQuaternion.Identity,
            sphereFirst ? spherePosition : floorPosition, sphereFirst ? floorPosition : spherePosition,
            sphereFirst ? spherePoint : floorPoint, sphereFirst ? floorPoint : spherePoint,
            sphereFirst ? -JVector.UnitY : JVector.UnitY);

        const Real epsilon = (Real)1e-5;
        Assert.That(manifold.Count, Is.EqualTo(1));
        Assert.That(JVector.MaxAbs(manifold.ManifoldA[0] - (sphereFirst ? spherePoint : floorPoint)),
            Is.LessThan(epsilon));
        Assert.That(JVector.MaxAbs(manifold.ManifoldB[0] - (sphereFirst ? floorPoint : spherePoint)),
            Is.LessThan(epsilon));
    }

    [TestCase(0.5)]
    [TestCase(1.0)]
    public void CylinderAgainstFloor_PreservesEndpointsAndOriginalContact(double radius)
    {
        BoxShape floor = new(200);
        CylinderShape cylinder = new(2, (Real)radius);
        JVector floorPosition = new(0, -100, 0);
        JVector cylinderPosition = new(10, (Real)radius - (Real)0.005, 0);
        JQuaternion cylinderOrientation = JQuaternion.CreateRotationZ(MathR.PI / 2);

        bool hit = NarrowPhase.MprEpa(floor, cylinder,
            JQuaternion.Identity, cylinderOrientation, floorPosition, cylinderPosition,
            out JVector pointA, out JVector pointB, out JVector normal, out _);
        Assert.That(hit, Is.True);

        CollisionManifold manifold = default;
        manifold.BuildManifold(floor, cylinder,
            JQuaternion.Identity, cylinderOrientation, floorPosition, cylinderPosition,
            pointA, pointB, normal);

        const Real epsilon = (Real)1e-5;
        Assert.That(manifold.Count, Is.EqualTo(3));
        Assert.That(MathR.Abs(manifold.ManifoldA[0].X - cylinderPosition.X), Is.EqualTo((Real)1).Within(epsilon));
        Assert.That(MathR.Abs(manifold.ManifoldA[1].X - cylinderPosition.X), Is.EqualTo((Real)1).Within(epsilon));
        Assert.That(manifold.ManifoldA[0].X, Is.Not.EqualTo(manifold.ManifoldA[1].X).Within(epsilon));
        Assert.That(JVector.MaxAbs(manifold.ManifoldA[2] - pointA), Is.LessThan(epsilon));
        Assert.That(JVector.MaxAbs(manifold.ManifoldB[2] - pointB), Is.LessThan(epsilon));
    }

    [TestCase]
    public void EqualFaceBoxes_ProduceFourCornerContacts()
    {
        BoxShape shapeA = new BoxShape(new JVector(2, 2, 2));
        BoxShape shapeB = new BoxShape(new JVector(2, 2, 2));

        CollisionManifold manifold = BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, new JVector(0, (Real)1.9, 0),
            out _, out _, out JVector normal, out Real penetration);

        const Real epsilon = (Real)1e-4;

        Assert.That(manifold.Count, Is.EqualTo(4));
        AssertUniqueContacts(manifold, epsilon * epsilon);

        for (int i = 0; i < manifold.Count; i++)
        {
            JVector mfA = manifold.ManifoldA[i];
            JVector mfB = manifold.ManifoldB[i];

            Assert.That(MathR.Abs(mfA.Y - (Real)1.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfB.Y - (Real)0.9), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(MathR.Abs(mfA.X) - (Real)1.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(MathR.Abs(mfA.Z) - (Real)1.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(JVector.Dot(mfA - mfB, normal) - penetration), Is.LessThan(epsilon));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TriangularFaceAgainstFloor_PreservesTriangleAndOriginalContact(bool prismFirst)
    {
        BoxShape floor = new(4, 2, 4);
        VertexSupportMap prism = new([
            new JVector(-1, -1, -1), new JVector(1, -1, -1), new JVector(0, -1, 1),
            new JVector(-1, 1, -1), new JVector(1, 1, -1), new JVector(0, 1, 1)
        ]);
        JVector floorPosition = new(0, -1, 0);
        JVector prismPosition = new(0, (Real)0.995, 0);
        JVector floorPoint = JVector.Zero;
        JVector prismPoint = new(0, -(Real)0.005, 0);

        CollisionManifold manifold = default;
        manifold.BuildManifold<ISupportMappable, ISupportMappable>(prismFirst ? prism : floor, prismFirst ? floor : prism,
            JQuaternion.Identity, JQuaternion.Identity,
            prismFirst ? prismPosition : floorPosition, prismFirst ? floorPosition : prismPosition,
            prismFirst ? prismPoint : floorPoint, prismFirst ? floorPoint : prismPoint,
            prismFirst ? -JVector.UnitY : JVector.UnitY);

        const Real epsilon = (Real)1e-5;
        Assert.That(manifold.Count, Is.EqualTo(4));
        JVector[] floorContacts = prismFirst ? manifold.ManifoldB.ToArray() : manifold.ManifoldA.ToArray();
        AssertSamePointSet([
            new JVector(-1, 0, -1), new JVector(1, 0, -1), new JVector(0, 0, 1), floorPoint
        ], floorContacts, epsilon * epsilon);
        Assert.That(JVector.MaxAbs(manifold.ManifoldA[3] - (prismFirst ? prismPoint : floorPoint)),
            Is.LessThan(epsilon));
        Assert.That(JVector.MaxAbs(manifold.ManifoldB[3] - (prismFirst ? floorPoint : prismPoint)),
            Is.LessThan(epsilon));
    }

    [TestCase]
    public void RotatedFaceBoxes_IncludeIntersectionVertices()
    {
        BoxShape shapeA = new BoxShape(new JVector(2, 2, 2));
        BoxShape shapeB = new BoxShape(new JVector(2, 2, 2));

        JQuaternion orientationB = JQuaternion.CreateRotationY((Real)(MathR.PI / 4.0));

        CollisionManifold manifold = BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, orientationB,
            JVector.Zero, new JVector(0, (Real)1.9, 0),
            out _, out _, out JVector normal, out Real penetration);

        const Real epsilon = (Real)2e-4;

        Assert.That(manifold.Count, Is.EqualTo(4));
        AssertUniqueContacts(manifold, epsilon * epsilon);

        for (int i = 0; i < manifold.Count; i++)
        {
            JVector mfA = manifold.ManifoldA[i];
            JVector mfB = manifold.ManifoldB[i];

            Assert.That(MathR.Abs(mfA.Y - (Real)1.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(JVector.Dot(mfA - mfB, normal) - penetration), Is.LessThan(epsilon));

            Assert.That(MathR.Abs(mfA.X), Is.LessThanOrEqualTo((Real)1.0 + epsilon));
            Assert.That(MathR.Abs(mfA.Z), Is.LessThanOrEqualTo((Real)1.0 + epsilon));

            JVector localB = mfB - new JVector(0, (Real)1.9, 0);
            JVector.ConjugatedTransform(localB, orientationB, out localB);

            Assert.That(MathR.Abs(localB.Y + (Real)1.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(localB.X), Is.LessThanOrEqualTo((Real)1.0 + epsilon));
            Assert.That(MathR.Abs(localB.Z), Is.LessThanOrEqualTo((Real)1.0 + epsilon));
        }
    }

    [TestCase]
    public void RotatedFaceBoxes_ReductionSpansAllQuadrantsOfOverlapPolygon()
    {
        BoxShape shapeA = new BoxShape(new JVector(2, 2, 2));
        BoxShape shapeB = new BoxShape(new JVector(2, 2, 2));

        JQuaternion orientationB = JQuaternion.CreateRotationY((Real)(MathR.PI / 4.0));

        CollisionManifold manifold = BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, orientationB,
            JVector.Zero, new JVector(0, (Real)1.9, 0),
            out _, out _, out _, out _);

        const Real epsilon = (Real)2e-4;

        Assert.That(manifold.Count, Is.EqualTo(4));
        AssertUniqueContacts(manifold, epsilon * epsilon);
        AssertQuadrantCoverage(manifold, JVector.Zero, (Real)0.05);
    }

    [TestCase]
    public void RotatedFaceBoxes_AtLargeWorldOffset_MatchNearOriginContacts()
    {
        BoxShape shapeA = new BoxShape(new JVector(2, 2, 2));
        BoxShape shapeB = new BoxShape(new JVector(2, 2, 2));

        JQuaternion orientationB = JQuaternion.CreateRotationY((Real)(MathR.PI / 4.0));
        JVector worldOffset = new JVector((Real)10_000.0, (Real)(-20_000.0), (Real)30_000.0);
        bool hit = NarrowPhase.Collision(shapeA, shapeB,
            JQuaternion.Identity, orientationB,
            JVector.Zero, new JVector(0, (Real)1.9, 0),
            out JVector pointA, out JVector pointB, out JVector normal, out _);

        Assert.That(hit, Is.True);

        CollisionManifold nearOrigin = default;
        nearOrigin.BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, orientationB,
            JVector.Zero, new JVector(0, (Real)1.9, 0),
            pointA, pointB, normal);
        CollisionManifold translated = default;
        translated.BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, orientationB,
            worldOffset, worldOffset + new JVector(0, (Real)1.9, 0),
            pointA + worldOffset, pointB + worldOffset, normal);

        const Real epsilon = (Real)1e-3;

        Assert.That(translated.Count, Is.EqualTo(nearOrigin.Count));

        List<JVector> expectedA = [];
        List<JVector> expectedB = [];
        List<JVector> actualA = [];
        List<JVector> actualB = [];

        for (int i = 0; i < nearOrigin.Count; i++)
        {
            expectedA.Add(nearOrigin.ManifoldA[i]);
            expectedB.Add(nearOrigin.ManifoldB[i]);
            actualA.Add(translated.ManifoldA[i] - worldOffset);
            actualB.Add(translated.ManifoldB[i] - worldOffset);
        }

        AssertSamePointSet(expectedA, actualA, epsilon * epsilon);
        AssertSamePointSet(expectedB, actualB, epsilon * epsilon);
    }

    [TestCase]
    public void BoxFaceAndRidge_PreserveLineSegmentEndpointsAndOriginalContact()
    {
        SupportPrimitives.Box shapeA = SupportPrimitives.CreateBox(new JVector(2, 2, 2));
        VertexSupportMap shapeB = CreateRidgeSupport();

        CollisionManifold manifold = default;
        manifold.BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, new JVector(0, (Real)2.9, 0),
            new JVector(0, (Real)2.0, 0), new JVector(0, (Real)1.9, 0), JVector.UnitY);

        const Real epsilon = (Real)1e-4;

        Assert.That(manifold.Count, Is.EqualTo(3));
        AssertUniqueContacts(manifold, epsilon * epsilon);

        for (int i = 0; i < 2; i++)
        {
            JVector mfA = manifold.ManifoldA[i];
            JVector mfB = manifold.ManifoldB[i];

            Assert.That(MathR.Abs(mfA.Y - (Real)2.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfB.Y - (Real)1.9), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(MathR.Abs(mfA.X) - (Real)2.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfA.Z), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfB.Z), Is.LessThan(epsilon));
        }

        Assert.That(manifold.ManifoldA[2], Is.EqualTo(new JVector(0, (Real)2.0, 0)));
        Assert.That(manifold.ManifoldB[2], Is.EqualTo(new JVector(0, (Real)1.9, 0)));
    }

    [TestCase]
    public void RidgeAndBoxFace_PreserveLineSegmentEndpointsAndOriginalContact()
    {
        VertexSupportMap shapeA = CreateRidgeSupport();
        SupportPrimitives.Box shapeB = SupportPrimitives.CreateBox(new JVector(2, 2, 2));

        CollisionManifold manifold = default;
        manifold.BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, JQuaternion.Identity,
            new JVector(0, (Real)2.9, 0), JVector.Zero,
            new JVector(0, (Real)1.9, 0), new JVector(0, (Real)2.0, 0), -JVector.UnitY);

        const Real epsilon = (Real)1e-4;

        Assert.That(manifold.Count, Is.EqualTo(3));
        AssertUniqueContacts(manifold, epsilon * epsilon);

        for (int i = 0; i < 2; i++)
        {
            JVector mfA = manifold.ManifoldA[i];
            JVector mfB = manifold.ManifoldB[i];

            Assert.That(MathR.Abs(mfA.Y - (Real)1.9), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfB.Y - (Real)2.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(MathR.Abs(mfA.X) - (Real)2.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(MathR.Abs(mfB.X) - (Real)2.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfA.Z), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfB.Z), Is.LessThan(epsilon));
        }

        Assert.That(manifold.ManifoldA[2], Is.EqualTo(new JVector(0, (Real)1.9, 0)));
        Assert.That(manifold.ManifoldB[2], Is.EqualTo(new JVector(0, (Real)2.0, 0)));
    }
}
