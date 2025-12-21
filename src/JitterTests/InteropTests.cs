using System.Numerics;

namespace JitterTests;

[TestFixture]
public sealed class InteropTests
{
    // --- Tuple conversions ----------------------------------------------------

    [Test]
    public void JVector_TupleConversion_AssignsComponents()
    {
        JVector v = (1f, -2f, 3.5f);

        Assert.That((float)v.X, Is.EqualTo(1f));
        Assert.That((float)v.Y, Is.EqualTo(-2f));
        Assert.That((float)v.Z, Is.EqualTo(3.5f));
    }

    [Test]
    public void JQuaternion_TupleConversion_AssignsComponents()
    {
        JQuaternion q = (1f, -2f, 3.5f, -4f);

        Assert.That((float)q.X, Is.EqualTo(1f));
        Assert.That((float)q.Y, Is.EqualTo(-2f));
        Assert.That((float)q.Z, Is.EqualTo(3.5f));
        Assert.That((float)q.W, Is.EqualTo(-4f));
    }

    // --- System.Numerics conversions -----------------------------------------

    [Test]
    public void JVector_SystemNumerics_Vector3_Conversion_Roundtrip()
    {
        var v = new JVector(1.25f, -2.5f, 3.75f);

        Vector3 n = v;       // JVector -> Vector3
        JVector back = n;    // Vector3 -> JVector

        Assert.That(n.X, Is.EqualTo(1.25f));
        Assert.That(n.Y, Is.EqualTo(-2.5f));
        Assert.That(n.Z, Is.EqualTo(3.75f));

        Assert.That((float)back.X, Is.EqualTo(1.25f));
        Assert.That((float)back.Y, Is.EqualTo(-2.5f));
        Assert.That((float)back.Z, Is.EqualTo(3.75f));
    }

    [Test]
    public void JQuaternion_SystemNumerics_Quaternion_Conversion_Roundtrip()
    {
        var q = new JQuaternion(0.1f, -0.2f, 0.3f, 0.4f);

        Quaternion n = q;       // JQuaternion -> Quaternion
        JQuaternion back = n;   // Quaternion -> JQuaternion

        Assert.That(n.X, Is.EqualTo(0.1f));
        Assert.That(n.Y, Is.EqualTo(-0.2f));
        Assert.That(n.Z, Is.EqualTo(0.3f));
        Assert.That(n.W, Is.EqualTo(0.4f));

        Assert.That((float)back.X, Is.EqualTo(0.1f));
        Assert.That((float)back.Y, Is.EqualTo(-0.2f));
        Assert.That((float)back.Z, Is.EqualTo(0.3f));
        Assert.That((float)back.W, Is.EqualTo(0.4f));
    }
}
