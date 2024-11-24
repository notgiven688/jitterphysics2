namespace JitterTests;

[TestFixture]
public class InertiaTests
{
    private static void Check(RigidBodyShape shape, JMatrix inertia, JVector com, Real mass)
    {
        shape.CalculateMassInertia(out JMatrix shapeInertia, out JVector shapeCom, out Real shapeMass);

        JMatrix dInertia = shapeInertia - inertia;
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(0), (Real)1e-3));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(1), (Real)1e-3));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(2), (Real)1e-3));

        Real dmass = shapeMass - mass;
        Assert.That(MathR.Abs(dmass), Is.LessThan((Real)1e-3));

        JVector dcom = shapeCom - com;
        Assert.That(MathHelper.IsZero(dcom, (Real)1e-3));
    }

    [TestCase]
    public static void CapsuleInertia()
    {
        var ts = new CapsuleShape((Real)0.429, (Real)1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void CylinderInertia()
    {
        var ts = new CylinderShape((Real)0.429, (Real)1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void ConeInertia()
    {
        var ts = new ConeShape((Real)0.429, (Real)1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void BoxInertia()
    {
        var ts = new BoxShape((Real)0.429, (Real)1.7237, (Real)2.11383);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void SphereInertia()
    {
        var ts = new SphereShape((Real)0.429);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedInertia()
    {
        var ss = new SphereShape((Real)0.429);
        var translation = new JVector((Real)2.847, (Real)3.432, (Real)1.234);

        var ts = new TransformedShape(ss, translation);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void ConvexHullInertia()
    {
        List<JTriangle> cvh = new List<JTriangle>();

        JVector a = new JVector((Real)0.234, (Real)1.23, (Real)3.54);
        JVector b = new JVector((Real)7.788, (Real)0.23, (Real)8.14);
        JVector c = new JVector((Real)2.234, (Real)8.23, (Real)8.14);
        JVector d = new JVector((Real)6.234, (Real)3.23, (Real)9.04);

        cvh.Add(new JTriangle(a, b, c));
        cvh.Add(new JTriangle(a, b, d));
        cvh.Add(new JTriangle(b, c, d));
        cvh.Add(new JTriangle(a, c, d));

        var ts = new ConvexHullShape(cvh);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }
}