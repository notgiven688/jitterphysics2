using Jitter2.Dynamics.Constraints;

namespace JitterTests;

public class MathTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase]
    public static void QMatrixProjectMultiplyLeftRight()
    {
        JQuaternion jq1 = new(0.2f, 0.3f, 0.4f, 0.5f);
        JQuaternion jq2 = new(0.1f, 0.7f, 0.1f, 0.8f);

        var qm1 = QMatrix.CreateLM(jq1);
        var qm2 = QMatrix.CreateRM(jq2);

        JMatrix res1 = QMatrix.Multiply(ref qm1, ref qm2).Projection();
        JMatrix res2 = QMatrix.ProjectMultiplyLeftRight(jq1, jq2);

        JMatrix delta = res1 - res2;
        Assert.That(JVector.MaxAbs(delta.GetColumn(0)), Is.LessThan(1e-06f));
        Assert.That(JVector.MaxAbs(delta.GetColumn(1)), Is.LessThan(1e-06f));
        Assert.That(JVector.MaxAbs(delta.GetColumn(2)), Is.LessThan(1e-06f));
    }

    [TestCase]
    public static void TransformTests()
    {
        JVector a = JVector.UnitX;
        JVector b = JVector.UnitY;
        JVector c = JVector.UnitZ;

        Assert.That((a - b % c).Length(), Is.LessThan(1e-06f));
        Assert.That((b - c % a).Length(), Is.LessThan(1e-06f));
        Assert.That((c - a % b).Length(), Is.LessThan(1e-06f));

        JMatrix ar = JMatrix.CreateRotationX(0.123f) *
                     JMatrix.CreateRotationY(0.321f) *
                     JMatrix.CreateRotationZ(0.213f);

        JVector.Transform(a, ar, out a);
        JVector.Transform(b, ar, out b);
        JVector.Transform(c, ar, out c);

        Assert.That((a - b % c).Length(), Is.LessThan(1e-06f));
        Assert.That((b - c % a).Length(), Is.LessThan(1e-06f));
        Assert.That((c - a % b).Length(), Is.LessThan(1e-06f));

        JMatrix.Inverse(ar, out ar);

        JVector.Transform(a, ar, out a);
        JVector.Transform(b, ar, out b);
        JVector.Transform(c, ar, out c);

        Assert.That((a - JVector.UnitX).Length(), Is.LessThan(1e-06f));
        Assert.That((b - JVector.UnitY).Length(), Is.LessThan(1e-06f));
        Assert.That((c - JVector.UnitZ).Length(), Is.LessThan(1e-06f));

        // ---
        // https://arxiv.org/abs/1801.07478

        float cos = (float)Math.Cos(0.321f / 2.0f);
        float sin = (float)Math.Sin(0.321f / 2.0f);
        JQuaternion quat1 = new(sin, 0, 0, cos);
        JQuaternion quat2 = JQuaternion.CreateFromMatrix(JMatrix.CreateRotationZ(0.321f));
        JQuaternion quat = JQuaternion.Multiply(quat1, quat2);
        JQuaternion tv = new(1, 2, 3, 0);
        JQuaternion tmp = JQuaternion.Multiply(JQuaternion.Multiply(quat, tv), JQuaternion.Conjugate(quat));
        JVector resQuaternion = new(tmp.X, tmp.Y, tmp.Z);

        JVector.Transform(new JVector(1, 2, 3), JMatrix.CreateFromQuaternion(quat), out JVector resMatrix1);
        Assert.That((resMatrix1 - resQuaternion).Length(), Is.LessThan(1e-06f));

        JMatrix rot1 = JMatrix.CreateRotationX(0.321f);
        JMatrix rot2 = JMatrix.CreateRotationZ(0.321f);
        JMatrix rot = JMatrix.Multiply(rot1, rot2);
        JVector.Transform(new JVector(1, 2, 3), rot, out JVector resMatrix2);

        Assert.That((resMatrix2 - resQuaternion).Length(), Is.LessThan(1e-06f));
    }
}