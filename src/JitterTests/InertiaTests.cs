namespace JitterTests;

[TestFixture]
public class InertiaTests
{
    private static void Check(RigidBodyShape shape, JMatrix inertia, JVector com, Real mass)
    {
        shape.CalculateMassInertia(out JMatrix shapeInertia, out JVector shapeCom, out Real shapeMass);

        JMatrix dInertia = shapeInertia - inertia;
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(0), 1e-3f));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(1), 1e-3f));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(2), 1e-3f));

        Real dmass = shapeMass - mass;
        Assert.That(MathR.Abs(dmass), Is.LessThan(1e-3f));

        JVector dcom = shapeCom - com;
        Assert.That(MathHelper.IsZero(dcom, 1e-3f));
    }

    [TestCase]
    public static void CapsuleInertia()
    {
        var ts = new CapsuleShape(0.429f, 1.7237f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void CylinderInertia()
    {
        var ts = new CylinderShape(0.429f, 1.7237f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void ConeInertia()
    {
        var ts = new ConeShape(0.429f, 1.7237f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void BoxInertia()
    {
        var ts = new BoxShape(0.429f, 1.7237f, 2.11383f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void SphereInertia()
    {
        var ts = new SphereShape(0.429f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedInertia()
    {
        var ss = new SphereShape(0.429f);
        var translation = new JVector(2.847f, 3.432f, 1.234f);

        var ts = new TransformedShape(ss, translation);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void ConvexHullInertia()
    {
        List<JTriangle> cvh = new List<JTriangle>();

        JVector a = new JVector(0.234f, 1.23f, 3.54f);
        JVector b = new JVector(7.788f, 0.23f, 8.14f);
        JVector c = new JVector(2.234f, 8.23f, 8.14f);
        JVector d = new JVector(6.234f, 3.23f, 9.04f);

        cvh.Add(new JTriangle(a, b, c));
        cvh.Add(new JTriangle(a, b, d));
        cvh.Add(new JTriangle(b, c, d));
        cvh.Add(new JTriangle(a, c, d));

        var ts = new ConvexHullShape(cvh);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }
}