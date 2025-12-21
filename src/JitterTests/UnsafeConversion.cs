using System.Runtime.InteropServices;

namespace JitterTests;

[TestFixture]
public sealed class UnsafeConversion
{
    // Layout-compatible targets (float build)
    [StructLayout(LayoutKind.Sequential)]
    private struct Vec3f { public float X, Y, Z; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Quatf { public float X, Y, Z, W; }

    // Common pitfall: padded/aligned vec3 (16 bytes)
    [StructLayout(LayoutKind.Sequential)]
    private struct Vec3f16 { public float X, Y, Z, Pad; }

    [OneTimeSetUp]
    public void SkipIfDisabled()
    {
        if (Precision.IsDoublePrecision)
        {
            Assert.Ignore("Interop tests disabled for double precision build.");
        }
    }

    [Test]
    public void JVector_UnsafeAs_And_UnsafeFrom_Roundtrip()
    {
        var v = new JVector(1f, -2f, 3f);

        Vec3f asVec = v.UnsafeAs<Vec3f>();
        Assert.That(asVec.X, Is.EqualTo(1f));
        Assert.That(asVec.Y, Is.EqualTo(-2f));
        Assert.That(asVec.Z, Is.EqualTo(3f));

        JVector back = JVector.UnsafeFrom(in asVec);
        Assert.That((float)back.X, Is.EqualTo(1f));
        Assert.That((float)back.Y, Is.EqualTo(-2f));
        Assert.That((float)back.Z, Is.EqualTo(3f));
    }

    [Test]
    public void JQuaternion_UnsafeAs_And_UnsafeFrom_Preserves_XYZW_Order()
    {
        var q = new JQuaternion(1f, 2f, 3f, 4f);

        Quatf asQuat = q.UnsafeAs<Quatf>();
        Assert.That(asQuat.X, Is.EqualTo(1f));
        Assert.That(asQuat.Y, Is.EqualTo(2f));
        Assert.That(asQuat.Z, Is.EqualTo(3f));
        Assert.That(asQuat.W, Is.EqualTo(4f));

        JQuaternion back = JQuaternion.UnsafeFrom(in asQuat);
        Assert.That((float)back.X, Is.EqualTo(1f));
        Assert.That((float)back.Y, Is.EqualTo(2f));
        Assert.That((float)back.Z, Is.EqualTo(3f));
        Assert.That((float)back.W, Is.EqualTo(4f));
    }

    // --- Guard rails ----------------------------------------------------------

    [Test]
    public void JVector_UnsafeAs_SizeMismatch_Throws()
    {
        var v = new JVector(1f, 2f, 3f);
        Assert.Throws<InvalidOperationException>(() => _ = v.UnsafeAs<Vec3f16>());
    }

    [Test]
    public void JVector_UnsafeFrom_SizeMismatch_Throws()
    {
        var bad = new Vec3f16 { X = 1f, Y = 2f, Z = 3f, Pad = 123f };
        Assert.Throws<InvalidOperationException>(() => _ = JVector.UnsafeFrom(in bad));
    }
}
