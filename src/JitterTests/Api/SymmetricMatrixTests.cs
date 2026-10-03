using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JitterTests.Api;

[TestFixture]
public class SymmetricMatrixTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    private static readonly JSymmetricMatrix Sample = new(4, 1, -2, 5, 1, 6);

    private static void AssertMatrix(in JMatrix actual, in JMatrix expected)
    {
        for (int i = 0; i < 3; i++)
        {
            Assert.That(JVector.MaxAbs(actual.GetColumn(i) - expected.GetColumn(i)),
                Is.LessThan(Tolerance), $"Column {i}");
        }
    }

    [Test]
    public void Layout_StoresExactlySixScalars()
    {
        Assert.That(Unsafe.SizeOf<JSymmetricMatrix>(), Is.EqualTo(6 * Unsafe.SizeOf<Real>()));
        Assert.That(RuntimeHelpers.IsReferenceOrContainsReferences<JSymmetricMatrix>(), Is.False);

        string[] fields = ["M11", "M12", "M13", "M22", "M23", "M33"];
        for (int i = 0; i < fields.Length; i++)
        {
            Assert.That(Marshal.OffsetOf<JSymmetricMatrix>(fields[i]).ToInt32(),
                Is.EqualTo(i * Unsafe.SizeOf<Real>()));
        }
    }

    [Test]
    public void LowerTriangleAliases_PreserveSymmetryWhenWritten()
    {
        JSymmetricMatrix matrix = Sample;
        matrix.M21 = 7;
        matrix.M31 = -8;
        matrix.M32 = 9;

        Assert.That(matrix.M12, Is.EqualTo((Real)7));
        Assert.That(matrix.M13, Is.EqualTo((Real)(-8)));
        Assert.That(matrix.M23, Is.EqualTo((Real)9));

        matrix.M12 = 10;
        matrix.M13 = 11;
        matrix.M23 = 12;
        Assert.That(matrix.M21, Is.EqualTo((Real)10));
        Assert.That(matrix.M31, Is.EqualTo((Real)11));
        Assert.That(matrix.M32, Is.EqualTo((Real)12));
        AssertMatrix(matrix.ToMatrix(), JMatrix.Transpose(matrix.ToMatrix()));
    }

    [Test]
    public void MatrixExpansion_PreservesBothTriangles()
    {
        JSymmetricMatrix packed = new(1, 2, 3, 4, 5, 6);
        JMatrix expected = new(1, 2, 3, 2, 4, 5, 3, 5, 6);
        AssertMatrix((JMatrix)packed, expected);
        AssertMatrix(packed.ToMatrix(), expected);
    }

    [Test]
    public void VectorTransform_MatchesFullMatrixAndSupportsAliasedOutput()
    {
        JVector vector = new(2, -3, 5);
        JVector expected = JVector.Transform(vector, Sample.ToMatrix());
        Assert.That(JVector.Transform(vector, Sample), Is.EqualTo(expected));

        JVector.Transform(vector, Sample, out vector);
        Assert.That(vector, Is.EqualTo(expected));
        Assert.That(JVector.Transform(vector, JSymmetricMatrix.Identity), Is.EqualTo(vector));
        Assert.That(JVector.Transform(vector, JSymmetricMatrix.Zero), Is.EqualTo(JVector.Zero));
    }

    [Test]
    public void Outer_MatchesFullVectorOuterProduct()
    {
        JVector vector = new(2, -3, 5);
        AssertMatrix(JSymmetricMatrix.Outer(vector).ToMatrix(), JVector.Outer(vector, vector));
        Assert.That(JSymmetricMatrix.Outer(JVector.Zero), Is.EqualTo(JSymmetricMatrix.Zero));
    }

    [Test]
    public void Arithmetic_MatchesFullMatrixAndSupportsAliasedOutput()
    {
        JSymmetricMatrix other = new(-3, 2, 4, 7, -1, 2);
        JMatrix full = Sample.ToMatrix();
        JMatrix fullOther = other.ToMatrix();
        AssertMatrix((Sample + other).ToMatrix(), full + fullOther);
        AssertMatrix((Sample - other).ToMatrix(), full - fullOther);
        AssertMatrix((Sample * (Real)(-2)).ToMatrix(), full * (Real)(-2));
        Assert.That((Real)3 * Sample, Is.EqualTo(Sample * (Real)3));

        JSymmetricMatrix matrix = Sample;
        JSymmetricMatrix.Add(matrix, other, out matrix);
        AssertMatrix(matrix.ToMatrix(), full + fullOther);
        JSymmetricMatrix.Subtract(matrix, other, out matrix);
        Assert.That(matrix, Is.EqualTo(Sample));
        JSymmetricMatrix.Multiply(matrix, (Real)(-2), out matrix);
        AssertMatrix(matrix.ToMatrix(), full * (Real)(-2));
    }

    [Test]
    public void DiagonalTraceAndDeterminant_MatchFullMatrix()
    {
        AssertMatrix(JSymmetricMatrix.Identity.ToMatrix(), JMatrix.Identity);
        AssertMatrix(default(JSymmetricMatrix).ToMatrix(), JMatrix.Zero);
        AssertMatrix(JSymmetricMatrix.CreateScale(2, 3, 4).ToMatrix(), JMatrix.CreateScale(2, 3, 4));
        Assert.That(JSymmetricMatrix.CreateScale(new JVector(2, 3, 4)),
            Is.EqualTo(JSymmetricMatrix.CreateScale(2, 3, 4)));
        Assert.That(Sample.Trace(), Is.EqualTo(Sample.ToMatrix().Trace()));
        Assert.That(Sample.Determinant(), Is.EqualTo(Sample.ToMatrix().Determinant()));
    }

    [Test]
    public void Inverse_MatchesFullMatrixForDefiniteAndIndefiniteMatrices()
    {
        JSymmetricMatrix[] matrices = [Sample, JSymmetricMatrix.Identity, new(0, 1, 0, 0, 0, -2)];
        foreach (JSymmetricMatrix matrix in matrices)
        {
            Assert.That(JSymmetricMatrix.Inverse(matrix, out JSymmetricMatrix inverse), Is.True);
            Assert.That(JMatrix.Inverse(matrix.ToMatrix(), out JMatrix expected), Is.True);
            AssertMatrix(inverse.ToMatrix(), expected);
            AssertMatrix(matrix.ToMatrix() * inverse.ToMatrix(), JMatrix.Identity);

            JSymmetricMatrix inPlace = matrix;
            Assert.That(JSymmetricMatrix.Inverse(inPlace, out inPlace), Is.True);
            Assert.That(inPlace, Is.EqualTo(inverse));
        }
    }

    [Test]
    public void Inverse_RejectsSingularAndNonFiniteDeterminants()
    {
        JSymmetricMatrix[] matrices =
        [
            JSymmetricMatrix.Zero,
            JSymmetricMatrix.CreateScale(1, 1, 0),
            new(1, 1, 1, 1, 1, 1),
            JSymmetricMatrix.CreateScale(Real.NaN, 1, 1),
            JSymmetricMatrix.CreateScale(Real.PositiveInfinity, 1, 1)
        ];

        foreach (JSymmetricMatrix matrix in matrices)
        {
            Assert.That(JSymmetricMatrix.Inverse(matrix, out JSymmetricMatrix result), Is.False);
            Assert.That(result, Is.EqualTo(JSymmetricMatrix.Zero));
        }
    }

    [Test]
    public void TensorTransform_MatchesFullProductForRotationShearAndProjection()
    {
        JMatrix[] transforms =
        [
            JMatrix.Identity,
            JMatrix.CreateRotationX((Real)0.37) * JMatrix.CreateRotationY((Real)(-0.81)),
            new(2, 1, 0, -1, 3, 2, 0, 4, 1),
            JMatrix.CreateScale(1, 0, 1),
            JMatrix.Zero
        ];

        foreach (JMatrix transform in transforms)
        {
            JMatrix expected = JMatrix.MultiplyTransposed(transform * Sample.ToMatrix(), transform);
            JSymmetricMatrix actual = JSymmetricMatrix.Transform(Sample, transform);
            AssertMatrix(actual.ToMatrix(), expected);
            AssertMatrix(actual.ToMatrix(), JMatrix.Transpose(actual.ToMatrix()));

            JSymmetricMatrix inPlace = Sample;
            JSymmetricMatrix.Transform(inPlace, transform, out inPlace);
            Assert.That(inPlace, Is.EqualTo(actual));
        }
    }

    [Test]
    public void TensorTransform_PreservesInertiaInvariantsUnderRotation()
    {
        // A normalized quaternion avoids including StableMath's trigonometric approximation
        // error in the tight double-precision checks for orthogonal-transform invariants.
        JMatrix rotation = JMatrix.CreateFromQuaternion(
            JQuaternion.Normalize(new JQuaternion((Real)0.1, (Real)0.2, (Real)0.3, (Real)0.4)));
        JSymmetricMatrix rotated = JSymmetricMatrix.Transform(Sample, rotation);
        Assert.That(rotated.Trace(), Is.EqualTo(Sample.Trace()).Within(Tolerance));
        Assert.That(rotated.Determinant(), Is.EqualTo(Sample.Determinant()).Within(10 * Tolerance));

        JVector vector = new(2, -3, 5);
        JVector rotatedVector = JVector.Transform(vector, rotation);
        JVector expected = JVector.Transform(JVector.Transform(vector, Sample), rotation);
        Assert.That(JVector.MaxAbs(JVector.Transform(rotatedVector, rotated) - expected),
            Is.LessThan(Tolerance));
    }
}
