namespace JitterTests;

[TestFixture]
public class InertiaTests
{
    private static void Check(Shape shape, JMatrix inertia, JVector com, float mass)
    {
        JMatrix dInertia = shape.Inertia - inertia;
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(0), 1e-3f));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(1), 1e-3f));
        Assert.That(MathHelper.IsZero(dInertia.UnsafeGet(2), 1e-3f));

        float dmass = shape.Mass - mass;
        Assert.That(MathF.Abs(dmass), Is.LessThan(1e-3f));

        JVector dcom = com;
        Assert.That(MathHelper.IsZero(dcom, 1e-3f));
    }

    [TestCase]
    public static void CapsuleInertia()
    {
        var ts = new CapsuleShape(0.429f, 1.7237f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out float mass, 8);
        Check(ts, inertia, com, mass);
    }
    
    [TestCase]
    public static void CylinderInertia()
    {
        var ts = new CylinderShape(0.429f, 1.7237f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out float mass, 8);
        Check(ts, inertia, com, mass);
    }
    
    [TestCase]
    public static void ConeInertia()
    {
        var ts = new ConeShape(0.429f, 1.7237f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out float mass, 8);
        Check(ts, inertia, com, mass);
    }
    
    [TestCase]
    public static void BoxInertia()
    {
        var ts = new BoxShape(0.429f, 1.7237f, 2.11383f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out float mass, 8);
        Check(ts, inertia, com, mass);
    }
    
    [TestCase]
    public static void SphereInertia()
    {
        var ts = new SphereShape(0.429f);
        ShapeHelper.CalculateMassInertia(ts, out JMatrix inertia, out JVector com, out float mass, 8);
        Check(ts, inertia, com, mass);
    }
    
}