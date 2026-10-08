using Jitter2.Collision.ExactArithmetic;

namespace JitterTests.Behavior;

public class ExactPredicateTests
{
    [Test]
    public void FilterAndExpansionsAgreeWithDyadicsIncludingAlmostCoplanarPoints()
    {
        var random = new Random(76541);
        int accepted = 0, uncertain = 0, tested = 0;
        int exponentRange = Precision.IsDoublePrecision ? 190 : 100;
        JVector Vector(int exponent) => new((Real)Math.ScaleB(random.NextDouble() - 0.5, exponent),
            (Real)Math.ScaleB(random.NextDouble() - 0.5, exponent), (Real)Math.ScaleB(random.NextDouble() - 0.5, exponent));

        for (int i = 0; i < 20000; i++)
        {
            int exponent = random.Next(-exponentRange, exponentRange);
            var a = Vector(exponent);
            var b = Vector(i % 7 == 0 ? random.Next(-exponentRange, exponentRange) : exponent);
            var c = Vector(i % 7 == 0 ? random.Next(-exponentRange, exponentRange) : exponent);
            var point = i % 4 == 0 ? a + b - c : i % 4 == 1 ? a : Vector(exponent);
            if (i % 8 == 0) point.X = MathR.BitIncrement(point.X);
            // Cancellation or BitIncrement(0) can produce a coordinate below
            // the supported range; the polytope rejects those inputs.
            if (!(PlaneFilter.IsSafe(a) && PlaneFilter.IsSafe(b) && PlaneFilter.IsSafe(c) && PlaneFilter.IsSafe(point))) continue;
            tested++;

            var ea = Dyadic3.FromVector(a);
            var normal = Dyadic3.Cross(ea - Dyadic3.FromVector(b), ea - Dyadic3.FromVector(c));
            int expected = Dyadic3.Dot(normal, Dyadic3.FromVector(point) - ea).Sign;
            var filter = new PlaneFilter(a, b, c);
            if (filter.TrySide(a, point, out int side))
            {
                accepted++;
                Assert.That(side, Is.EqualTo(expected), $"Filtered sign at sample {i}");
                filter.Negate();
                if (filter.TrySide(b, point, out side))
                    Assert.That(side, Is.EqualTo(-expected), $"Reoriented sign at sample {i}");
            }
            else uncertain++;

            Assert.That(ExpansionArithmetic.Side(a, b, c, point), Is.EqualTo(expected), $"Expansion sign at sample {i}");
        }

        Assert.That(accepted, Is.GreaterThan(0));
        Assert.That(uncertain, Is.GreaterThan(0));
        Assert.That(tested, Is.GreaterThan(15000));
    }

    [Test]
    public void ExactNormalAndSideRetainSignLostByRealCrossProduct()
    {
        Real delta = (Real)Math.ScaleB(1, Precision.IsDoublePrecision ? -27 : -13);
        var a = JVector.Zero;
        var b = new JVector(1, 1 + delta, 0);
        var c = new JVector(1 - delta, 1, 0);
        Assert.That(JVector.Cross(b, c).Z, Is.Zero);
        Assert.That(ExpansionArithmetic.Side(a, b, c, JVector.UnitZ), Is.EqualTo(1));
        ExpansionArithmetic.Normal(a, b, c, out var x, out var y, out var z);
        Assert.That(x, Is.Zero);
        Assert.That(y, Is.Zero);
        Assert.That(z, Is.EqualTo((double)delta * delta));
    }

    [Test]
    public void SupportedCoordinateRangeIncludesZeroAndRejectsNonfiniteValues()
    {
        Assert.That(PlaneFilter.IsSafe(JVector.Zero), Is.True);
        Assert.That(PlaneFilter.IsSafe(new JVector(Real.NaN, 0, 0)), Is.False);
        Assert.That(PlaneFilter.IsSafe(new JVector(0, Real.PositiveInfinity, 0)), Is.False);
        Assert.That(PlaneFilter.IsSafe(new JVector(0, 0, Real.NegativeInfinity)), Is.False);
#if USE_DOUBLE_PRECISION
        Assert.That(PlaneFilter.IsSafe(new JVector(Math.ScaleB(1, -201), 0, 0)), Is.False);
        Assert.That(PlaneFilter.IsSafe(new JVector(Math.ScaleB(1, 201), 0, 0)), Is.False);
        Assert.That(PlaneFilter.IsSafe(new JVector(double.Epsilon, 0, 0)), Is.False);
        foreach (int exponent in new[] { -200, 200 })
            Assert.That(PlaneFilter.IsSafe(new JVector(Math.ScaleB(1, exponent), 0, 0)), Is.True);
#else
        Assert.That(PlaneFilter.IsSafe(new JVector(float.Epsilon, float.MaxValue, float.MinValue)), Is.True);
#endif
    }
}
