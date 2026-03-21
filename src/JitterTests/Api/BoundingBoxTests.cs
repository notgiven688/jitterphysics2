using Jitter2.SoftBodies;

namespace JitterTests.Api;

public class BoundingBoxTests
{
    private static void CheckBoundingBox(RigidBodyShape shape)
    {
        JQuaternion ori = new JQuaternion(1, 2, 3, 4);
        JQuaternion.NormalizeInPlace(ref ori);

        JVector pos = new JVector(12, -11, 17);

        ShapeHelper.CalculateBoundingBox(shape, ori, pos, out JBoundingBox shr);
        shape.CalculateBoundingBox(ori, pos, out JBoundingBox sbb);

        Real fraction = shr.GetVolume() / sbb.GetVolume();

        Assert.That(fraction - (Real)1e-4, Is.LessThan((Real)1.0));
        Assert.That(fraction, Is.GreaterThan((Real)0.2));
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

        CheckBoundingBox(new TransformedShape(new BoxShape(1, 2, 3),
            new JVector(1, 2, 3)));
        CheckBoundingBox(new TransformedShape(new BoxShape(1, 2, 3),
            JMatrix.CreateRotationX((Real)0.7) * JMatrix.CreateRotationY((Real)1.1)));
        CheckBoundingBox(new TransformedShape(new BoxShape(1, 2, 3),
            new JVector(1, 2, 3),
            JMatrix.CreateRotationZ((Real)0.5) * JMatrix.CreateRotationX((Real)1.3)));

        CheckBoundingBox(new TransformedShape(new SphereShape(1),
            new JVector(1, -2, 3), JMatrix.CreateScale((Real)2.0, (Real)1.5, (Real)3.0)));

        var shear = JMatrix.Identity;
        shear.M12 = (Real)0.5;
        shear.M31 = (Real)0.3;
        CheckBoundingBox(new TransformedShape(new BoxShape(1, 2, 3),
            new JVector(1, -2, 3), shear));
    }
}
