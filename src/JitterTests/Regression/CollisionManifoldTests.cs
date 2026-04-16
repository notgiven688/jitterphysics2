namespace JitterTests.Regression;

public class CollisionManifoldTests
{
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

        Assert.That(manifold.Count, Is.GreaterThanOrEqualTo(4));
        Assert.That(manifold.Count, Is.LessThanOrEqualTo(6));
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
    public void BoxFaceAndRidge_ProduceLineSegmentEndpoints()
    {
        SupportPrimitives.Box shapeA = SupportPrimitives.CreateBox(new JVector(2, 2, 2));
        VertexSupportMap shapeB =
            new([
                new JVector(-2, -1, 0),
                new JVector(2, -1, 0),
                new JVector(-2, 1, -1),
                new JVector(2, 1, -1),
                new JVector(-2, 1, 1),
                new JVector(2, 1, 1)
            ]);

        CollisionManifold manifold = default;
        manifold.BuildManifold(shapeA, shapeB,
            JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, new JVector(0, (Real)2.9, 0),
            new JVector(0, (Real)2.0, 0), new JVector(0, (Real)1.9, 0), JVector.UnitY);

        const Real epsilon = (Real)1e-4;

        Assert.That(manifold.Count, Is.EqualTo(2));
        AssertUniqueContacts(manifold, epsilon * epsilon);

        for (int i = 0; i < manifold.Count; i++)
        {
            JVector mfA = manifold.ManifoldA[i];
            JVector mfB = manifold.ManifoldB[i];

            Assert.That(MathR.Abs(mfA.Y - (Real)2.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfB.Y - (Real)1.9), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(MathR.Abs(mfA.X) - (Real)2.0), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfA.Z), Is.LessThan(epsilon));
            Assert.That(MathR.Abs(mfB.Z), Is.LessThan(epsilon));
        }
    }
}
