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
        JQuaternion jq1 = new((Real)0.2, (Real)0.3, (Real)0.4, (Real)0.5);
        JQuaternion jq2 = new((Real)0.1, (Real)0.7, (Real)0.1, (Real)0.8);

        var qm1 = QMatrix.CreateLeftMatrix(jq1);
        var qm2 = QMatrix.CreateRightMatrix(jq2);

        JMatrix res1 = QMatrix.Multiply(ref qm1, ref qm2).Projection();
        JMatrix res2 = QMatrix.ProjectMultiplyLeftRight(jq1, jq2);

        JMatrix delta = res1 - res2;
        Assert.That(JVector.MaxAbs(delta.GetColumn(0)), Is.LessThan((Real)1e-06));
        Assert.That(JVector.MaxAbs(delta.GetColumn(1)), Is.LessThan((Real)1e-06));
        Assert.That(JVector.MaxAbs(delta.GetColumn(2)), Is.LessThan((Real)1e-06));
    }

    [TestCase]
    public static void TransformTests()
    {
        JVector a = JVector.UnitX;
        JVector b = JVector.UnitY;
        JVector c = JVector.UnitZ;

        Assert.That((a - b % c).Length(), Is.LessThan((Real)1e-06));
        Assert.That((b - c % a).Length(), Is.LessThan((Real)1e-06));
        Assert.That((c - a % b).Length(), Is.LessThan((Real)1e-06));

        JMatrix ar = JMatrix.CreateRotationX((Real)0.123) *
                     JMatrix.CreateRotationY((Real)0.321) *
                     JMatrix.CreateRotationZ((Real)0.213);

        JVector.Transform(a, ar, out a);
        JVector.Transform(b, ar, out b);
        JVector.Transform(c, ar, out c);

        Assert.That((a - b % c).Length(), Is.LessThan((Real)1e-06));
        Assert.That((b - c % a).Length(), Is.LessThan((Real)1e-06));
        Assert.That((c - a % b).Length(), Is.LessThan((Real)1e-06));

        JMatrix.Inverse(ar, out ar);

        JVector.Transform(a, ar, out a);
        JVector.Transform(b, ar, out b);
        JVector.Transform(c, ar, out c);

        Assert.That((a - JVector.UnitX).Length(), Is.LessThan((Real)1e-06));
        Assert.That((b - JVector.UnitY).Length(), Is.LessThan((Real)1e-06));
        Assert.That((c - JVector.UnitZ).Length(), Is.LessThan((Real)1e-06));

        // ---
        // https://arxiv.org/abs/1801.07478

        float cos = (float)MathR.Cos((Real)(0.321 / 2.0));
        float sin = (float)MathR.Sin((Real)(0.321 / 2.0));
        JQuaternion quat1 = new(sin, 0, 0, cos);
        JQuaternion quat2 = JQuaternion.CreateFromMatrix(JMatrix.CreateRotationZ((Real)0.321));
        JQuaternion quat = JQuaternion.Multiply(quat1, quat2);
        JQuaternion tv = new(1, 2, 3, 0);
        JQuaternion tmp = JQuaternion.Multiply(JQuaternion.Multiply(quat, tv), JQuaternion.Conjugate(quat));
        JVector resQuaternion = new(tmp.X, tmp.Y, tmp.Z);

        JVector.Transform(new JVector(1, 2, 3), JMatrix.CreateFromQuaternion(quat), out JVector resMatrix1);
        Assert.That((resMatrix1 - resQuaternion).Length(), Is.LessThan((Real)1e-06));

        JMatrix rot1 = JMatrix.CreateRotationX((Real)0.321);
        JMatrix rot2 = JMatrix.CreateRotationZ((Real)0.321);
        JMatrix rot = JMatrix.Multiply(rot1, rot2);
        JVector.Transform(new JVector(1, 2, 3), rot, out JVector resMatrix2);

        Assert.That((resMatrix2 - resQuaternion).Length(), Is.LessThan((Real)1e-06));
    }
}