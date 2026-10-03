namespace JitterTests.Api;

[TestFixture]
public class InertiaTests
{
    private static void Check(RigidBodyShape shape, JSymmetricMatrix inertia, JVector com, Real mass)
    {
        shape.CalculateMassInertia(out JSymmetricMatrix shapeInertia, out JVector shapeCom, out Real shapeMass);

        JMatrix dInertia = (shapeInertia - inertia).ToMatrix();
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
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void CylinderInertia()
    {
        var ts = new CylinderShape((Real)0.429, (Real)1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void ConeInertia()
    {
        var ts = new ConeShape((Real)0.429, (Real)1.7237);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void BoxInertia()
    {
        var ts = new BoxShape((Real)0.429, (Real)1.7237, (Real)2.11383);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void SphereInertia()
    {
        var ts = new SphereShape((Real)0.429);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedInertia()
    {
        var ss = new SphereShape((Real)0.429);
        var translation = new JVector((Real)2.847, (Real)3.432, (Real)1.234);

        var ts = new TransformedShape(ss, translation);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedRotationInertia()
    {
        var box = new BoxShape((Real)1.0, (Real)2.0, (Real)3.0);
        var rotation = JMatrix.CreateRotationX((Real)0.7) * JMatrix.CreateRotationY((Real)1.1);

        var ts = new TransformedShape(box, rotation);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedRotationTranslationInertia()
    {
        var box = new BoxShape((Real)1.0, (Real)2.0, (Real)3.0);
        var translation = new JVector((Real)2.847, (Real)3.432, (Real)1.234);
        var rotation = JMatrix.CreateRotationZ((Real)0.5) * JMatrix.CreateRotationX((Real)1.3);

        var ts = new TransformedShape(box, translation, rotation);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedScaleInertia()
    {
        var box = new BoxShape((Real)1.0, (Real)2.0, (Real)3.0);
        var scale = JMatrix.CreateScale((Real)2.0, (Real)1.5, (Real)3.0);
        var translation = new JVector((Real)1.0, (Real)2.0, (Real)3.0);

        var ts = new TransformedShape(box, translation, scale);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase]
    public static void TransformedShearInertia()
    {
        var box = new BoxShape((Real)1.0, (Real)2.0, (Real)3.0);
        var shear = JMatrix.Identity;
        shear.M12 = (Real)0.5;
        shear.M31 = (Real)0.3;
        var translation = new JVector((Real)1.5, (Real)(-0.7), (Real)2.3);

        var ts = new TransformedShape(box, translation, shear);
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
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
        ShapeHelper.CalculateMassInertia(ts, out JSymmetricMatrix inertia, out JVector com, out Real mass, 8);
        Check(ts, inertia, com, mass);
    }

    [TestCase(false)]
    [TestCase(true)]
    public static void TransformedInertia_MatchesAnalyticalShearedBox(bool reflected)
    {
        // The box has mass 48 and second moments diag(16, 64, 144).
        // Shearing produces all three independent off-diagonal inertia components after translation.
        var box = new BoxShape(2, 4, 6);
        JMatrix transform = new(1, (Real)0.5, 0, 0, 1, 0, (Real)0.25, 0, 1);
        if (reflected)
        {
            transform.M11 = -transform.M11;
            transform.M12 = -transform.M12;
        }

        JVector translation = new(1, -2, 3);
        var shape = new TransformedShape(box, translation, transform);
        JSymmetricMatrix expected = new(833, reflected ? 128 : 64, reflected ? -140 : -148, 657, 288, 336);
        Check(shape, expected, translation, 48);
    }
}
