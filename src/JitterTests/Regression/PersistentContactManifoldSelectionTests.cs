namespace JitterTests.Regression;

public class PersistentContactManifoldSelectionTests
{
    private static void CreateRotatedFaceContactScenario(World world,
        out RigidBody bodyA, out RigidBody bodyB, out BoxShape shapeA, out BoxShape shapeB,
        out CollisionManifold manifold, out JVector normal)
    {
        bodyA = world.CreateRigidBody();
        bodyA.Position = JVector.Zero;
        shapeA = new BoxShape(new JVector(2, 2, 2));
        bodyA.AddShape(shapeA);

        bodyB = world.CreateRigidBody();
        bodyB.Position = new JVector(0, (Real)1.9, 0);
        bodyB.Orientation = JQuaternion.CreateRotationY((Real)(MathR.PI / 4.0));
        shapeB = new BoxShape(new JVector(2, 2, 2));
        bodyB.AddShape(shapeB);

        bool hit = NarrowPhase.Collision(shapeA, shapeB, bodyA.Orientation, bodyB.Orientation, bodyA.Position, bodyB.Position,
            out JVector pointA, out JVector pointB, out normal, out _);

        Assert.That(hit, Is.True);

        manifold = default;
        manifold.BuildManifold(shapeA, shapeB, pointA, pointB, normal);
    }

    private static CollisionManifold RotateManifold(CollisionManifold manifold, int offset)
    {
        int count = manifold.Count;

        CollisionManifold rotated = manifold;
        JVector[] pointsA = new JVector[count];
        JVector[] pointsB = new JVector[count];

        for (int i = 0; i < count; i++)
        {
            pointsA[i] = manifold.ManifoldA[i];
            pointsB[i] = manifold.ManifoldB[i];
        }

        for (int i = 0; i < count; i++)
        {
            rotated.ManifoldA[i] = pointsA[(i + offset) % count];
            rotated.ManifoldB[i] = pointsB[(i + offset) % count];
        }

        return rotated;
    }

    private static List<JVector> GetWorldContactPositions(Arbiter arbiter)
    {
        return GetWorldContactPositions(arbiter.Handle.Data, arbiter.Body1.Position);
    }

    private static List<JVector> GetWorldContactPositions(ContactData data, in JVector bodyPosition)
    {
        List<JVector> contacts = [];

        if ((data.UsageMask & ContactData.MaskContact0) != 0)
        {
            contacts.Add(data.Contact0.RelativePosition1 + bodyPosition);
        }

        if ((data.UsageMask & ContactData.MaskContact1) != 0)
        {
            contacts.Add(data.Contact1.RelativePosition1 + bodyPosition);
        }

        if ((data.UsageMask & ContactData.MaskContact2) != 0)
        {
            contacts.Add(data.Contact2.RelativePosition1 + bodyPosition);
        }

        if ((data.UsageMask & ContactData.MaskContact3) != 0)
        {
            contacts.Add(data.Contact3.RelativePosition1 + bodyPosition);
        }

        return contacts;
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

    private static Real CalculateQuadrilateralArea(in JVector p0, in JVector p1, in JVector p2, in JVector p3, in JVector normal)
    {
        JVector area = p0 % p1;
        area += p1 % p2;
        area += p2 % p3;
        area += p3 % p0;

        return MathR.Abs(JVector.Dot(area, normal));
    }

    private static int[] SelectLargestQuadrilateralIndices(CollisionManifold manifold, in JVector normal)
    {
        Assert.That(manifold.Count, Is.GreaterThanOrEqualTo(4));

        if (manifold.Count == 4)
        {
            return [0, 1, 2, 3];
        }

        Real bestArea = Real.MinValue;
        int best0 = 0, best1 = 1, best2 = 2, best3 = 3;

        for (int i0 = 0; i0 < manifold.Count - 3; i0++)
        {
            for (int i1 = i0 + 1; i1 < manifold.Count - 2; i1++)
            {
                for (int i2 = i1 + 1; i2 < manifold.Count - 1; i2++)
                {
                    for (int i3 = i2 + 1; i3 < manifold.Count; i3++)
                    {
                        Real area = CalculateQuadrilateralArea(
                            manifold.ManifoldA[i0], manifold.ManifoldA[i1],
                            manifold.ManifoldA[i2], manifold.ManifoldA[i3], normal);

                        if (area <= bestArea) continue;

                        bestArea = area;
                        best0 = i0;
                        best1 = i1;
                        best2 = i2;
                        best3 = i3;
                    }
                }
            }
        }

        return [best0, best1, best2, best3];
    }

    private static List<JVector> SelectLargestQuadrilateral(CollisionManifold manifold, in JVector normal)
    {
        int[] indices = SelectLargestQuadrilateralIndices(manifold, normal);
        return [manifold.ManifoldA[indices[0]], manifold.ManifoldA[indices[1]], manifold.ManifoldA[indices[2]], manifold.ManifoldA[indices[3]]];
    }

    [Test]
    public void RegisterContact_SelectsSameFourContactsRegardlessOfIncomingManifoldOrder()
    {
        using var worldA = new World();
        using var worldB = new World();

        CreateRotatedFaceContactScenario(worldA, out RigidBody bodyA0, out RigidBody bodyB0, out BoxShape shapeA0, out BoxShape shapeB0,
            out CollisionManifold manifoldA, out JVector normalA);
        CreateRotatedFaceContactScenario(worldB, out RigidBody bodyA1, out RigidBody bodyB1, out BoxShape shapeA1, out BoxShape shapeB1,
            out CollisionManifold manifoldB, out JVector normalB);

        Assert.That(manifoldA.Count, Is.GreaterThan(4));

        CollisionManifold rotated = RotateManifold(manifoldB, 2);

        worldA.RegisterContact(shapeA0.ShapeId, shapeB0.ShapeId, bodyA0, bodyB0, normalA, ref manifoldA);
        worldB.RegisterContact(shapeA1.ShapeId, shapeB1.ShapeId, bodyA1, bodyB1, normalB, ref rotated);

        Assert.That(worldA.GetArbiter(shapeA0.ShapeId, shapeB0.ShapeId, out Arbiter? arbiterA), Is.True);
        Assert.That(worldB.GetArbiter(shapeA1.ShapeId, shapeB1.ShapeId, out Arbiter? arbiterB), Is.True);

        List<JVector> contactsA = GetWorldContactPositions(arbiterA!);
        List<JVector> contactsB = GetWorldContactPositions(arbiterB!);

        Assert.That(contactsA, Has.Count.EqualTo(4));
        Assert.That(contactsB, Has.Count.EqualTo(4));
        AssertSamePointSet(contactsA, contactsB, (Real)1e-8);
    }

    [Test]
    public void RegisterContact_SelectsLargestQuadrilateralFromOrderedManifold()
    {
        using var world = new World();

        CreateRotatedFaceContactScenario(world, out RigidBody bodyA, out RigidBody bodyB, out BoxShape shapeA, out BoxShape shapeB,
            out CollisionManifold manifold, out JVector normal);

        Assert.That(manifold.Count, Is.GreaterThan(4));

        List<JVector> expected = SelectLargestQuadrilateral(manifold, normal);

        world.RegisterContact(shapeA.ShapeId, shapeB.ShapeId, bodyA, bodyB, normal, ref manifold);

        Assert.That(world.GetArbiter(shapeA.ShapeId, shapeB.ShapeId, out Arbiter? arbiter), Is.True);

        List<JVector> actual = GetWorldContactPositions(arbiter!);

        Assert.That(actual, Has.Count.EqualTo(4));
        AssertSamePointSet(expected, actual, (Real)1e-8);
    }

    [Test]
    public void AddContact_WhenCacheIsFull_ReusesMatchingContactInsteadOfReplacingAnotherSlot()
    {
        using var world = new World();

        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();

        ContactData data = default;
        data.Init(bodyA, bodyB);

        JVector normal = new JVector(0, 1, 0);
        JVector[] points =
        [
            new JVector(-1, 0, -1),
            new JVector(1, 0, -1),
            new JVector(1, 0, 1),
            new JVector(-1, 0, 1)
        ];

        foreach (JVector point in points)
        {
            data.AddContact(point, point - (Real)0.1 * normal, normal);
        }

        List<JVector> original = GetWorldContactPositions(data, bodyA.Position);

        data.AddContact(points[1], points[1] - (Real)0.1 * normal, normal);

        List<JVector> updated = GetWorldContactPositions(data, bodyA.Position);

        Assert.That(updated, Has.Count.EqualTo(4));
        AssertSamePointSet(original, updated, (Real)1e-10);
    }
}
