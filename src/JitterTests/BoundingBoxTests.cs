using Jitter2.SoftBodies;

namespace JitterTests;

public class BoundingBoxTests
{
    private static void CheckBoundingBox(RigidBodyShape shape)
    {
        JQuaternion ori = new JQuaternion(1, 2, 3, 4);
        ori.Normalize();

        JVector pos = new JVector(12, -11, 17);

        ShapeHelper.CalculateBoundingBox(shape, ori, pos, out JBBox shr);
        shape.CalculateBoundingBox(ori, pos, out JBBox sbb);

        double fraction = shr.GetVolume() / sbb.GetVolume();

        Assert.That(fraction - 1e-7f, Is.LessThan(1.0f));
        Assert.That(fraction, Is.GreaterThan(0.2f));
    }

    [TestCase]
    public static void RigidBodyShapeTests()
    {
        CheckBoundingBox(new BoxShape(1, 2, 3));
        CheckBoundingBox(new CapsuleShape(1, 2));
        CheckBoundingBox(new ConeShape(1, 2));
        CheckBoundingBox(new CylinderShape(1, 2));
        CheckBoundingBox(new SphereShape(1));

        JVector a = new JVector(1, 2, 3);
        JVector b = new JVector(2, 2, 3);
        JVector c = new JVector(1, -2, 4);
        JVector d = new JVector(3, 3, 3);

        List<JVector> vertices =
        [
            a, b, c, d
        ];

        List<JTriangle> triangles =
        [
            new JTriangle(a, b, c),
            new JTriangle(a, b, d),
            new JTriangle(a, d, c),
            new JTriangle(d, b, c)
        ];

        TriangleMesh tm = new TriangleMesh(triangles);

        CheckBoundingBox(new ConvexHullShape(triangles));
        CheckBoundingBox(new PointCloudShape(vertices));
        CheckBoundingBox(new TriangleShape(tm, 0));
    }
}