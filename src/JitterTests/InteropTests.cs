using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitter2.Tests.LinearMath;

[TestFixture]
public sealed class InteropTests
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
    public void EnsureFloatBuild()
    {
        Assert.That(Unsafe.SizeOf<JVector>(), Is.EqualTo(12), "These tests expect the float build (sizeof(JVector) == 12).");
        Assert.That(Unsafe.SizeOf<JQuaternion>(), Is.EqualTo(16), "These tests expect the float build (sizeof(JQuaternion) == 16).");
    }

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

    // --- Unsafe bit reinterpretation -----------------------------------------

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
        var bad = new Vec3f16 { X = 1, Y = 2, Z = 3, Pad = 123 };
        Assert.Throws<InvalidOperationException>(() => _ = JVector.UnsafeFrom(in bad));
    }
}
