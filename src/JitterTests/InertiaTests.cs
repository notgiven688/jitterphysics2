namespace JitterTests;

[TestFixture]
public class InertiaTests
{
    private static void Check(RigidBodyShape shape, JMatrix inertia, JVector com, double mass)
    {
        shape.CalculateMassInertia(out JMatrix shapeInertia, out JVector shapeCom, out double shapeMass);

        JMatrix dInertia = shapeInertia - inertia;
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(0), 1e-3));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(1), 1e-3));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(2), 1e-3));

        double dmass = shapeMass - mass;
        Assert.That(Math.Abs(dmass), Is.LessThan(1e-3));

        JVector dcom = shapeCom - com;
        Assert.That(MathHelper.IsZero(dcom, 1e-3));
    }

    [TestCase]
    public static void CapsuleInertia()
    {
        var ts = new CapsuleShape(0.429, 1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out double mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void CylinderInertia()
    {
        var ts = new CylinderShape(0.429, 1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out double mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void ConeInertia()
    {
        var ts = new ConeShape(0.429, 1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out double mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void BoxInertia()
    {
        var ts = new BoxShape(0.429, 1.7237, 2.11383);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out double mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void SphereInertia()
    {
        var ts = new SphereShape(0.429);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out double mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedInertia()
    {
        var ss = new SphereShape(0.429);
        var translation = new JVector(2.847, 3.432, 1.234);

        var ts = new TransformedShape(ss, translation);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out double mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void ConvexHullInertia()
    {
        List<JTriangle> cvh = new List<JTriangle>();

        JVector a = new JVector(0.234, 1.23, 3.54);
        JVector b = new JVector(7.788, 0.23, 8.14);
        JVector c = new JVector(2.234, 8.23, 8.14);
        JVector d = new JVector(6.234, 3.23, 9.04);

        cvh.Add(new JTriangle(a, b, c));
        cvh.Add(new JTriangle(a, b, d));
        cvh.Add(new JTriangle(b, c, d));
        cvh.Add(new JTriangle(a, c, d));

        var ts = new ConvexHullShape(cvh);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out double mass, 8);
        Check(ts, inertia, com, mass);
    }
}