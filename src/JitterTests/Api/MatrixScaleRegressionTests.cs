namespace JitterTests.Api;

[TestFixture]
public class MatrixScaleRegressionTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    [Test]
    public void MatrixInverse_UniformExtremeScalePreservesInverse([Values] bool large, [Values] bool inPlace)
    {
        Real scale = ExtremeScale(large);
        JMatrix matrix = new(4, 1, (Real)(-0.5), 0, 3, (Real)0.75, (Real)0.5, (Real)0.25, 2);
        matrix *= scale;
        JMatrix inverse;
        if (inPlace)
        {
            inverse = matrix;
            Assert.That(JMatrix.Inverse(inverse, out inverse), Is.True);
        }
        else Assert.That(JMatrix.Inverse(matrix, out inverse), Is.True);

        AssertIdentity(matrix * inverse);
        AssertIdentity(inverse * matrix);
    }

    [Test]
    public void SymmetricMatrixInverse_UniformExtremeScalePreservesInverse([Values] bool large, [Values] bool inPlace)
    {
        Real scale = ExtremeScale(large);
        JSymmetricMatrix matrix = new(4, 1, (Real)(-0.5), 3, (Real)0.75, 2);
        matrix *= scale;
        JSymmetricMatrix inverse;
        if (inPlace)
        {
            inverse = matrix;
            Assert.That(JSymmetricMatrix.Inverse(inverse, out inverse), Is.True);
        }
        else Assert.That(JSymmetricMatrix.Inverse(matrix, out inverse), Is.True);

        AssertIdentity(matrix.ToMatrix() * inverse.ToMatrix());
        AssertIdentity(inverse.ToMatrix() * matrix.ToMatrix());
    }

    private static void AssertIdentity(JMatrix matrix)
    {
        for (int i = 0; i < 3; i++)
        {
            Assert.That(JVector.MaxAbs(matrix.GetColumn(i) - JMatrix.Identity.GetColumn(i)),
                Is.LessThan(Tolerance));
        }
    }

    private static Real ExtremeScale(bool large) => Precision.IsDoublePrecision
        ? (large ? (Real)1e110 : (Real)1e-110)
        : (large ? (Real)1e20 : (Real)1e-13);
}
