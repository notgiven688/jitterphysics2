using System.Runtime.InteropServices;

namespace JitterTests;

[TestFixture]
public sealed class UnsafeConversion
{
    // Layout-compatible targets (float build)
    [StructLayout(LayoutKind.Sequential)]
    private struct Vec3R { public Real X, Y, Z; }

    [StructLayout(LayoutKind.Sequential)]
    private struct QuatR { public Real X, Y, Z, W; }

    [Test]
    public void JVector_UnsafeAs_And_UnsafeFrom_Roundtrip()
    {
        var v = new JVector(1, -2, 3);

        Vec3R asVec = v.UnsafeAs<Vec3R>();
        Assert.That(asVec.X, Is.EqualTo(1));
        Assert.That(asVec.Y, Is.EqualTo(-2));
        Assert.That(asVec.Z, Is.EqualTo(3));

        JVector back = JVector.UnsafeFrom(in asVec);
        Assert.That((float)back.X, Is.EqualTo(1));
        Assert.That((float)back.Y, Is.EqualTo(-2));
        Assert.That((float)back.Z, Is.EqualTo(3));
    }

    [Test]
    public void JQuaternion_UnsafeAs_And_UnsafeFrom_Preserves_XYZW_Order()
    {
        var q = new JQuaternion(1, 2, 3, 4);

        QuatR asQuat = q.UnsafeAs<QuatR>();
        Assert.That(asQuat.X, Is.EqualTo(1));
        Assert.That(asQuat.Y, Is.EqualTo(2));
        Assert.That(asQuat.Z, Is.EqualTo(3));
        Assert.That(asQuat.W, Is.EqualTo(4));

        JQuaternion back = JQuaternion.UnsafeFrom(in asQuat);
        Assert.That((float)back.X, Is.EqualTo(1));
        Assert.That((float)back.Y, Is.EqualTo(2));
        Assert.That((float)back.Z, Is.EqualTo(3));
        Assert.That((float)back.W, Is.EqualTo(4));
    }

    // --- Guard rails ----------------------------------------------------------

    [Test]
    public void JVector_UnsafeAs_SizeMismatch_Throws()
    {
        var v = new JVector(1, 2, 3);
        Assert.Throws<InvalidOperationException>(() => _ = v.UnsafeAs<QuatR>());
    }

    [Test]
    public void JVector_UnsafeFrom_SizeMismatch_Throws()
    {
        var bad = new QuatR { X = 1, Y = 2, Z = 3, W = 123 };
        Assert.Throws<InvalidOperationException>(() => _ = JVector.UnsafeFrom(in bad));
    }
}
