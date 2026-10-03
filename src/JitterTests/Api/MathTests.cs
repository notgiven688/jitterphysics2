using Jitter2.Dynamics.Constraints;

namespace JitterTests.Api;

public class MathTests
{
#if USE_DOUBLE_PRECISION
    private const Real PseudoInverseTolerance = 1e-12;
#else
    private const Real PseudoInverseTolerance = 1e-5f;
#endif

    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 1)]
    [TestCase(1, 0.001)]
    [TestCase(2, 0.001)]
    [TestCase(3, 0.001)]
    [TestCase(1, 1000)]
    [TestCase(2, 1000)]
    [TestCase(3, 1000)]
    public static void PseudoInverseSymmetric_InvertsResponsiveSubspace(int rank, double scaleValue)
    {
        Real scale = (Real)scaleValue;
        JMatrix rotation = JMatrix.CreateFromQuaternion(
            JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4)));
        JSymmetricMatrix diagonal = JSymmetricMatrix.CreateScale(
            rank >= 1 ? 2 : 0, rank >= 2 ? 3 : 0, rank == 3 ? 4 : 0);
        JMatrix matrix = JSymmetricMatrix.Transform(diagonal, rotation).ToMatrix() * scale;
        JMatrix inverse = MathHelper.PseudoInverseSymmetric(matrix);
        JMatrix expected = JSymmetricMatrix.Transform(JSymmetricMatrix.CreateScale(
            rank >= 1 ? (Real)0.5 : 0, rank >= 2 ? (Real)(1.0 / 3.0) : 0,
            rank == 3 ? (Real)0.25 : 0), rotation).ToMatrix() * ((Real)1 / scale);

        JMatrix matrixProjection = matrix * inverse;
        JMatrix inverseProjection = inverse * matrix;
        for (int i = 0; i < 3; i++)
        {
            Assert.That(JVector.MaxAbs(inverse.GetColumn(i) - expected.GetColumn(i)) * scale,
                Is.LessThan(PseudoInverseTolerance));
            Assert.That(JVector.MaxAbs((matrix * inverse * matrix).GetColumn(i) - matrix.GetColumn(i)) / scale,
                Is.LessThan(PseudoInverseTolerance));
            Assert.That(JVector.MaxAbs((inverse * matrix * inverse).GetColumn(i) - inverse.GetColumn(i)) * scale,
                Is.LessThan(PseudoInverseTolerance));
            Assert.That(JVector.MaxAbs(matrixProjection.GetColumn(i) - JMatrix.Transpose(matrixProjection).GetColumn(i)),
                Is.LessThan(PseudoInverseTolerance));
            Assert.That(JVector.MaxAbs(inverseProjection.GetColumn(i) - JMatrix.Transpose(inverseProjection).GetColumn(i)),
                Is.LessThan(PseudoInverseTolerance));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public static void PseudoInverseSymmetric_FallbackAppliesEigenvalueCutoff(bool retained)
    {
        Real cutoff = Precision.IsDoublePrecision ? (Real)1e-12 : (Real)1e-6;
        Real eigenvalue = cutoff * (retained ? (Real)10 : (Real)0.1);
        JMatrix matrix = JMatrix.CreateScale(1, eigenvalue, 0);
        JMatrix inverse = MathHelper.PseudoInverseSymmetric(matrix);
        Assert.That(inverse.M11, Is.EqualTo((Real)1));
        Assert.That(inverse.M33, Is.Zero);
        Assert.That((matrix * inverse).M22, Is.EqualTo(retained ? (Real)1 : 0).Within(PseudoInverseTolerance));
        if (!retained) Assert.That(inverse.M22, Is.Zero);
    }

    [TestCase]
    public static void RotationQuaternion_LongSweepWithTinyAngularVelocityRemainsFinite()
    {
        Real dt = (Real)1e13;

        JQuaternion stationary = MathHelper.RotationQuaternion(JVector.Zero, dt);
        Assert.That(stationary, Is.EqualTo(JQuaternion.Identity));

        JQuaternion slowlyRotating = MathHelper.RotationQuaternion(new JVector((Real)1e-17, 0, 0), dt);
        Assert.That(Real.IsFinite(slowlyRotating.X), Is.True);
        Assert.That(slowlyRotating.X, Is.EqualTo((Real)5e-5).Within((Real)1e-7));
        Assert.That(slowlyRotating.Length(), Is.EqualTo((Real)1).Within((Real)1e-6));
    }

    [TestCase]
    public static void StableMath_MatchesMathROnRepresentativeInputs()
    {
        Real[] angles =
        [
            (Real)(-16.0) * MathR.PI,
            (Real)(-3.0) * MathR.PI,
            -MathR.PI,
            (Real)(-0.75) * MathR.PI,
            (Real)(-0.5) * MathR.PI,
            (Real)(-0.25) * MathR.PI,
            (Real)(-0.1),
            (Real)0.0,
            (Real)0.1,
            (Real)0.25 * MathR.PI,
            (Real)0.5 * MathR.PI,
            (Real)0.75 * MathR.PI,
            MathR.PI,
            (Real)3.0 * MathR.PI,
            (Real)16.0 * MathR.PI
        ];

        foreach (Real angle in angles)
        {
            var (sinDet, cosDet) = StableMath.SinCos(angle);

            Assert.That(MathR.Abs(sinDet - MathR.Sin(angle)), Is.LessThan((Real)2e-6), $"sin({angle})");
            Assert.That(MathR.Abs(cosDet - MathR.Cos(angle)), Is.LessThan((Real)2e-6), $"cos({angle})");
            Assert.That(MathR.Abs(StableMath.Sin(angle) - MathR.Sin(angle)), Is.LessThan((Real)2e-6), $"single sin({angle})");
            Assert.That(MathR.Abs(StableMath.Cos(angle) - MathR.Cos(angle)), Is.LessThan((Real)2e-6), $"single cos({angle})");
        }

        (Real y, Real x)[] atanInputs =
        [
            ((Real)1.0, (Real)1.0),
            ((Real)1.0, (Real)(-1.0)),
            ((Real)(-1.0), (Real)1.0),
            ((Real)(-1.0), (Real)(-1.0)),
            ((Real)0.2, (Real)3.0),
            ((Real)3.0, (Real)0.2),
            ((Real)(-0.2), (Real)3.0),
            ((Real)3.0, (Real)(-0.2)),
            ((Real)0.0, (Real)1.0),
            ((Real)1.0, (Real)0.0)
        ];

        foreach (var (y, x) in atanInputs)
        {
            Assert.That(MathR.Abs(StableMath.Atan2(y, x) - MathR.Atan2(y, x)),
                Is.LessThan((Real)2e-6), $"atan2({y}, {x})");
        }

        Real[] unitInputs = [(Real)(-1.0), (Real)(-0.75), (Real)(-0.25), (Real)0.0, (Real)0.25, (Real)0.75, (Real)1.0];

        foreach (Real value in unitInputs)
        {
            Assert.That(MathR.Abs(StableMath.Asin(value) - MathR.Asin(value)),
                Is.LessThan((Real)2e-6), $"asin({value})");
            Assert.That(MathR.Abs(StableMath.Acos(value) - MathR.Acos(value)),
                Is.LessThan((Real)2e-6), $"acos({value})");
        }
    }

    [TestCase]
    public static void StableMath_MatchesMathROnNonReducedBoundaryInputs()
    {
        Real epsilon = (Real)1e-4;
        Real[] angles =
        [
            -StableMath.Pi,
            -StableMath.Pi + epsilon,
            -StableMath.HalfPi,
            -StableMath.QuarterPi,
            (Real)0.0,
            StableMath.QuarterPi,
            StableMath.HalfPi,
            StableMath.Pi - epsilon,
            StableMath.Pi
        ];

        foreach (Real angle in angles)
        {
            var (sinDet, cosDet) = StableMath.SinCos(angle);

            Assert.That(MathR.Abs(sinDet - MathR.Sin(angle)), Is.LessThan((Real)2e-6), $"sin({angle})");
            Assert.That(MathR.Abs(cosDet - MathR.Cos(angle)), Is.LessThan((Real)2e-6), $"cos({angle})");
            Assert.That(MathR.Abs(StableMath.Sin(angle) - MathR.Sin(angle)), Is.LessThan((Real)2e-6), $"single sin({angle})");
            Assert.That(MathR.Abs(StableMath.Cos(angle) - MathR.Cos(angle)), Is.LessThan((Real)2e-6), $"single cos({angle})");
        }
    }

    [TestCase]
    public static void DeterministicInverseTrig_MatchesMathROnUnitInterval()
    {
        for (int i = -2000; i <= 2000; i++)
        {
            Real value = i / (Real)2000.0;

            Assert.That(MathR.Abs(StableMath.Asin(value) - MathR.Asin(value)),
                Is.LessThan((Real)5e-6), $"asin({value})");
            Assert.That(MathR.Abs(StableMath.Acos(value) - MathR.Acos(value)),
                Is.LessThan((Real)5e-6), $"acos({value})");
        }
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
