/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;

namespace Jitter2.LinearMath;

/// <summary>
/// Internal trigonometric helpers for bit-identical results on different platforms (not guaranteed by Math and MathF).
/// </summary>
/// <remarks>
/// Until scalar trig in the BCL is both deterministic and fixed by managed source across targets,
/// this helper keeps its own tiny approximation pipeline so the physics engine owns the behavior.
/// </remarks>
internal static class StableMath
{
    internal const Real Pi = (Real)3.141592653589793238462643383279502884;
    internal const Real HalfPi = (Real)1.570796326794896619231321691639751442;
    internal const Real QuarterPi = (Real)0.785398163397448309615660845819875721;
    internal const Real TwoPi = (Real)6.283185307179586476925286766559005768;

    // Used by atan's angle-addition identity: atan(x) = pi/4 + atan((x - 1) / (x + 1)).
    private const Real TanPiOver8 = (Real)0.414213562373095048801688724209698079;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FloorToInt(Real value)
    {
        // Casting truncates toward zero. The reducers need mathematical floor so that negative
        // angles land in the same buckets on every platform and quadrant boundaries stay symmetric.
        int integer = (int)value;
        return value < integer ? integer - 1 : integer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Real ReduceAngle(Real angle)
    {
        // This is intentionally the "naive" reducer: one modulo by 2*pi into [-pi, pi] in the
        // current Real precision. It is not a Payne-Hanek/Cody-Waite style high-precision reducer.
        // That means enormous inputs can lose low bits before the polynomial ever runs, but for the
        // angle magnitudes seen in the solver this is a good tradeoff: tiny code, deterministic data
        // flow, and no dependency on platform libm internals.
        int periods = FloorToInt((angle + Pi) / TwoPi);
        angle -= periods * TwoPi;

        if (angle > Pi) angle -= TwoPi;
        else if (angle <= -Pi) angle += TwoPi;

        return angle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReduceToQuadrant(Real angle, out int quadrant, out Real reduced)
    {
        angle = ReduceAngle(angle);

        // Fold once more into [-pi/4, pi/4]. The returned quadrant stores the swaps/sign flips needed
        // to reconstruct the original sine/cosine pair after the low-order polynomial is evaluated.
        int nearestQuarterTurn = FloorToInt((angle + QuarterPi) / HalfPi);

        reduced = angle - nearestQuarterTurn * HalfPi;
        quadrant = nearestQuarterTurn & 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Real SinPolynomial(Real x)
    {
        // 13th-order Maclaurin approximation of sin(x), evaluated in Horner form.
        //
        // The reduction above is what makes this usable. In exact arithmetic the truncation remainder
        // on [-pi/4, pi/4] is bounded by |x|^15 / 15!, which is about 2.04e-14 at the endpoint.
        // If we skip reduction and run the same polynomial directly on [-pi, pi], a dense float scan
        // lands around 2.14e-5 max absolute error. The quadrant fold is therefore not optional detail;
        // it is the reason this low-order Taylor polynomial works for the engine.
        Real x2 = x * x;
        Real poly = -(Real)(1.0 / 6227020800.0);
        poly = poly * x2 + (Real)(1.0 / 39916800.0);
        poly = poly * x2 - (Real)(1.0 / 362880.0);
        poly = poly * x2 + (Real)(1.0 / 5040.0);
        poly = poly * x2 - (Real)(1.0 / 120.0);
        poly = poly * x2 + (Real)(1.0 / 6.0);
        poly = poly * x2 - (Real)1.0;
        return -x * poly;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Real CosPolynomial(Real x)
    {
        // 12th-order Maclaurin approximation of cos(x), also in Horner form.
        //
        // The exact-series truncation remainder on [-pi/4, pi/4] is bounded by |x|^14 / 14!, about
        // 3.90e-13 at the endpoint. Without reduction the same coefficients are much less acceptable:
        // a dense float scan over [-pi, pi] lands around 1.01e-4 max absolute error.
        Real x2 = x * x;
        Real poly = -(Real)(1.0 / 479001600.0);
        poly = poly * x2 + (Real)(1.0 / 3628800.0);
        poly = poly * x2 - (Real)(1.0 / 40320.0);
        poly = poly * x2 + (Real)(1.0 / 720.0);
        poly = poly * x2 - (Real)(1.0 / 24.0);
        poly = poly * x2 + (Real)(1.0 / 2.0);
        poly = poly * x2 - (Real)1.0;
        return -poly;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (Real sin, Real cos) ApplyQuadrant(int quadrant, Real sin, Real cos)
    {
        // Undo the octant/quadrant fold from ReduceToQuadrant.
        return quadrant switch
        {
            0 => (sin, cos),
            1 => (cos, -sin),
            2 => (-sin, -cos),
            _ => (-cos, sin)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Real ApplyQuadrantSin(int quadrant, Real sin, Real cos)
    {
        return quadrant switch
        {
            0 => sin,
            1 => cos,
            2 => -sin,
            _ => -cos
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Real ApplyQuadrantCos(int quadrant, Real sin, Real cos)
    {
        return quadrant switch
        {
            0 => cos,
            1 => -sin,
            2 => -cos,
            _ => sin
        };
    }

    internal static (Real sin, Real cos) SinCos(Real angle)
    {
        if (angle >= -QuarterPi && angle <= QuarterPi)
        {
            return (SinPolynomial(angle), CosPolynomial(angle));
        }

        // Everything outside the minimal polynomial interval goes through the normal range reducer.
        ReduceToQuadrant(angle, out int quadrant, out Real reduced);

        Real sin = SinPolynomial(reduced);
        Real cos = CosPolynomial(reduced);

        return ApplyQuadrant(quadrant, sin, cos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Real Sin(Real angle)
    {
        if (angle >= -QuarterPi && angle <= QuarterPi)
        {
            return SinPolynomial(angle);
        }

        ReduceToQuadrant(angle, out int quadrant, out Real reduced);

        // The single-output paths keep the same reduction logic but avoid computing the polynomial
        // that is not needed for the selected quadrant.
        return quadrant switch
        {
            0 => SinPolynomial(reduced),
            1 => CosPolynomial(reduced),
            2 => -SinPolynomial(reduced),
            _ => -CosPolynomial(reduced)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Real Cos(Real angle)
    {
        if (angle >= -QuarterPi && angle <= QuarterPi)
        {
            return CosPolynomial(angle);
        }

        ReduceToQuadrant(angle, out int quadrant, out Real reduced);

        return quadrant switch
        {
            0 => CosPolynomial(reduced),
            1 => -SinPolynomial(reduced),
            2 => -CosPolynomial(reduced),
            _ => SinPolynomial(reduced)
        };
    }

    private static Real AtanTaylor(Real value)
    {
        // 17th-order odd Taylor series for atan(x) in Horner form. The caller keeps |x| small
        // enough that the notoriously slow convergence near x = 1 does not dominate the error.
        Real x2 = value * value;
        Real poly = (Real)(1.0 / 17.0);
        poly = poly * x2 - (Real)(1.0 / 15.0);
        poly = poly * x2 + (Real)(1.0 / 13.0);
        poly = poly * x2 - (Real)(1.0 / 11.0);
        poly = poly * x2 + (Real)(1.0 / 9.0);
        poly = poly * x2 - (Real)(1.0 / 7.0);
        poly = poly * x2 + (Real)(1.0 / 5.0);
        poly = poly * x2 - (Real)(1.0 / 3.0);
        poly = poly * x2 + (Real)1.0;
        return value * poly;
    }

    private static Real Atan(Real value)
    {
        if (value < (Real)0.0) return -Atan(-value);

        if (value > (Real)1.0)
        {
            // atan(x) = pi/2 - atan(1/x)
            return HalfPi - Atan((Real)1.0 / value);
        }

        if (value > TanPiOver8)
        {
            // atan(x) = pi/4 + atan((x - 1) / (x + 1)). After this transform the Taylor series only
            // sees values up to tan(pi/8) ~= 0.4142 instead of fighting the slow x ~= 1 case directly.
            Real reduced = (value - (Real)1.0) / (value + (Real)1.0);
            return QuarterPi + AtanTaylor(reduced);
        }

        return AtanTaylor(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Real AsinTaylor(Real value)
    {
        // 15th-order Maclaurin approximation of asin(x). We only feed it |x| <= 0.5 directly; near
        // the endpoints Asin/Acos switch to a half-angle form so the polynomial still sees a small x.
        Real x2 = value * value;
        Real poly = (Real)(143.0 / 10240.0);
        poly = poly * x2 + (Real)(231.0 / 13312.0);
        poly = poly * x2 + (Real)(63.0 / 2816.0);
        poly = poly * x2 + (Real)(35.0 / 1152.0);
        poly = poly * x2 + (Real)(5.0 / 112.0);
        poly = poly * x2 + (Real)(3.0 / 40.0);
        poly = poly * x2 + (Real)(1.0 / 6.0);
        poly = poly * x2 + (Real)1.0;
        return value * poly;
    }

    internal static Real Atan2(Real y, Real x)
    {
        // Classic quadrant reconstruction around the scalar atan approximation above.
        if (x > (Real)0.0)
        {
            return Atan(y / x);
        }

        if (x < (Real)0.0)
        {
            return y >= (Real)0.0
                ? Atan(y / x) + Pi
                : Atan(y / x) - Pi;
        }

        if (y > (Real)0.0) return HalfPi;
        if (y < (Real)0.0) return -HalfPi;

        return (Real)0.0;
    }

    internal static Real Acos(Real value)
    {
        value = Math.Clamp(value, (Real)(-1.0), (Real)1.0);

        if (value > (Real)0.5)
        {
            // acos(x) = 2 * asin(sqrt((1 - x) / 2))
            Real reduced = MathR.Sqrt(MathR.Max((Real)0.0, ((Real)1.0 - value) * (Real)0.5));
            return (Real)2.0 * AsinTaylor(reduced);
        }

        if (value < (Real)(-0.5))
        {
            // acos(x) = pi - 2 * asin(sqrt((1 + x) / 2))
            Real reduced = MathR.Sqrt(MathR.Max((Real)0.0, ((Real)1.0 + value) * (Real)0.5));
            return Pi - (Real)2.0 * AsinTaylor(reduced);
        }

        return HalfPi - AsinTaylor(value);
    }

    internal static Real Asin(Real value)
    {
        value = Math.Clamp(value, (Real)(-1.0), (Real)1.0);
        Real absValue = MathR.Abs(value);

        if (absValue <= (Real)0.5)
        {
            return AsinTaylor(value);
        }

        // asin(x) = pi/2 - 2 * asin(sqrt((1 - |x|) / 2))
        Real reduced = MathR.Sqrt(MathR.Max((Real)0.0, ((Real)1.0 - absValue) * (Real)0.5));
        Real angle = HalfPi - (Real)2.0 * AsinTaylor(reduced);

        return value < (Real)0.0 ? -angle : angle;
    }
}
