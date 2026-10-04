using Jitter2.Collision;
using Jitter2.SoftBodies;

namespace JitterTests.Api;

public class BoundingBoxTests
{
    [Test]
    public void DetachedHullShapesAndClonesCanBeFoundAtTheirGeometry()
    {
        JVector a = new(10, 10, 10);
        JVector b = new(11, 10, 10);
        JVector c = new(10, 11, 10);
        JVector d = new(10, 10, 11);

        PointCloudShape cloud = new([a, b, c, d]);
        ConvexHullShape hull = new([
            new JTriangle(a, b, c), new JTriangle(a, b, d),
            new JTriangle(a, c, d), new JTriangle(b, c, d)
        ]);

        Assert.That(cloud.WorldBoundingBox.Min, Is.EqualTo(a));
        Assert.That(hull.WorldBoundingBox.Min, Is.EqualTo(a));
        Assert.That(cloud.WorldBoundingBox.Max, Is.EqualTo(new JVector(11, 11, 11)));
        Assert.That(hull.WorldBoundingBox.Max, Is.EqualTo(new JVector(11, 11, 11)));

        cloud.Shift = new JVector(3, 0, 0);
        hull.Shift = new JVector(3, 0, 0);
        Assert.That(cloud.WorldBoundingBox.Min.X, Is.EqualTo((Real)13));
        Assert.That(hull.WorldBoundingBox.Min.X, Is.EqualTo((Real)13));

        var tree = new DynamicTree((_, _) => true);
        tree.AddProxy(cloud.Clone());
        tree.AddProxy(hull.Clone());

        List<IDynamicTreeProxy> hits = [];
        tree.Query(hits, new JBoundingBox(new JVector(12, 9, 9), new JVector(14, 12, 12)));
        Assert.That(hits, Has.Count.EqualTo(2));

        hits.Clear();
        tree.Query(hits, new JBoundingBox(new JVector(-1), new JVector(1)));
        Assert.That(hits, Is.Empty);
    }

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
