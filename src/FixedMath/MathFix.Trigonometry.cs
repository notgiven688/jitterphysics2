using System;

namespace FixedMath
{
    public static partial class MathFix
    {
        /// <summary>
        /// Represents the ratio of the circumference of a circle to its diameter, specified by the constant, π.
        /// </summary>
        public static readonly Fix64 PI = new Fix64(_PI);
        public static readonly Fix64 PIOver2 = new Fix64(PI_OVER_2);
        public static readonly Fix64 PITimes2 = new Fix64(PI_TIMES_2);
        public static readonly Fix64 PIInv = (Fix64)0.3183098861837906715377675267M;
        public static readonly Fix64 PIOver2Inv = (Fix64)0.6366197723675813430755350535M;
        public static readonly Fix64 LutInterval = (Fix64)(LUT_SIZE - 1) / PIOver2;

        private const long _PI = 0x3243F6A88;
        private const long PI_OVER_2 = 0x1921FB544;
        private const long PI_TIMES_2 = 0x6487ED511;
        private const int LUT_SIZE = (int)(PI_OVER_2 >> 15);

        private static long[] SinLut { get; } = new long[LUT_SIZE];
        private static long[] AcosLut { get; } = new long[LUT_SIZE];
        private static long[] TanLut { get; } = new long[LUT_SIZE];

        // Returns the arccos of of the specified number, calculated using Atan and Sqrt
        // This function has at least 7 decimals of accuracy.
        /// <summary>
        /// Returns the angle whose cosine is the specified number.
        /// </summary>
        /// <param name="x">A number representing a cosine, where d must be greater than or equal to -1, but less than or equal to 1.</param>
        /// <returns></returns>
        public static Fix64 Acos(Fix64 x)
        {
            if (x < -Fix64.One || x > Fix64.One)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (x.RawValue == 0) return PIOver2;

            var result = Atan(Sqrt(Fix64.One - x * x) / x);
            return x.RawValue < 0 ? result + PI : result;
        }

        public static Fix64 Acosh(Fix64 x)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Returns the angle whose sine is the specified number.
        /// </summary>
        /// <param name="x">A number representing a sine, where d must be greater than or equal to -1, but less than or equal to 1.</param>
        /// <returns></returns>
        public static Fix64 Asin(Fix64 x)
        {
            throw new NotImplementedException();
        }

        public static Fix64 Asinh(Fix64 x)
        {
            throw new NotImplementedException();
        }

        // Returns the arctan of of the specified number, calculated using Euler series
        // This function has at least 7 decimals of accuracy.
        /// <summary>
        /// Returns the angle whose tangent is the specified number.
        /// </summary>
        /// <param name="x">A number representing a tangent.</param>
        /// <returns></returns>
        public static Fix64 Atan(Fix64 x)
        {
            if (x.RawValue == 0) return Fix64.Zero;

            // Force positive values for argument
            // Atan(-z) = -Atan(z).
            var neg = x.RawValue < 0;
            if (neg)
            {
                x = -x;
            }

            Fix64 result;
            var two = (Fix64)2;
            var three = (Fix64)3;

            bool invert = x > Fix64.One;
            if (invert) x = Fix64.One / x;

            result = Fix64.One;
            var term = Fix64.One;

            var zSq = x * x;
            var zSq2 = zSq * two;
            var zSqPlusOne = zSq + Fix64.One;
            var zSq12 = zSqPlusOne * two;
            var dividend = zSq2;
            var divisor = zSqPlusOne * three;

            for (var i = 2; i < 30; ++i)
            {
                term *= dividend / divisor;
                result += term;

                dividend += zSq2;
                divisor += zSq12;

                if (term.RawValue == 0) break;
            }

            result = result * x / zSqPlusOne;

            if (invert)
            {
                result = PIOver2 - result;
            }

            if (neg)
            {
                result = -result;
            }
            return result;
        }

        /// <summary>
        /// Returns the angle whose tangent is the quotient of two specified numbers.
        /// </summary>
        /// <param name="y">The y coordinate of a point.</param>
        /// <param name="x">The x coordinate of a point.</param>
        /// <returns></returns>
        public static Fix64 Atan2(Fix64 y, Fix64 x)
        {
            var yl = y.RawValue;
            var xl = x.RawValue;
            if (xl == 0)
            {
                if (yl > 0)
                {
                    return PIOver2;
                }
                if (yl == 0)
                {
                    return Fix64.Zero;
                }
                return -PIOver2;
            }
            Fix64 atan;
            var z = y / x;

            // Deal with overflow
            if (Fix64.One + (Fix64)0.28M * z * z == Fix64.MaxValue)
            {
                return y < Fix64.Zero ? -PIOver2 : PIOver2;
            }

            if (Abs(z) < Fix64.One)
            {
                atan = z / (Fix64.One + (Fix64)0.28M * z * z);
                if (xl < 0)
                {
                    if (yl < 0)
                    {
                        return atan - PI;
                    }
                    return atan + PI;
                }
            }
            else
            {
                atan = PIOver2 - z / (z * z + (Fix64)0.28M);
                if (yl < 0)
                {
                    return atan - PI;
                }
            }
            return atan;
        }

        public static Fix64 Atanh(Fix64 x)
        {
            throw new NotImplementedException();
        }

        // The relative error is less than 1E-10 for x in [-2PI, 2PI], and less than 1E-7 in the worst case.
        /// <summary>
        /// Returns the cosine of the specified angle.
        /// </summary>
        /// <param name="x">An angle, measured in radians.</param>
        /// <returns></returns>
        public static Fix64 Cos(Fix64 x)
        {
            var xl = x.RawValue;
            var rawAngle = xl + (xl > 0 ? -_PI - PI_OVER_2 : PI_OVER_2);
            return Sin(new Fix64(rawAngle));
        }

        // The relative error is less than 1E-10 for x in [-2PI, 2PI], and less than 1E-7 in the worst case.
        /// <summary>
        /// Returns the sine of the specified angle.
        /// </summary>
        /// <param name="x">An angle, measured in radians.</param>
        /// <returns></returns>
        public static Fix64 Sin(Fix64 x)
        {
            var clampedL = ClampSinValue(x.RawValue, out var flipHorizontal, out var flipVertical);
            var clamped = new Fix64(clampedL);

            // Find the two closest values in the LUT and perform linear interpolation
            // This is what kills the performance of this function on x86 - x64 is fine though
            var rawIndex = Fix64.FastMultiply(clamped, LutInterval);
            var roundedIndex = Round(rawIndex);
            var indexError = Fix64.FastSubtract(rawIndex, roundedIndex);

            var nearestValue = new Fix64(SinLut[flipHorizontal ?
                SinLut.Length - 1 - (int)roundedIndex :
                (int)roundedIndex]);
            var secondNearestValue = new Fix64(SinLut[flipHorizontal ?
                SinLut.Length - 1 - (int)roundedIndex - Sign(indexError) :
                (int)roundedIndex + Sign(indexError)]);

            var delta = Fix64.FastMultiply(indexError, FastAbs(Fix64.FastSubtract(nearestValue, secondNearestValue))).RawValue;
            var interpolatedValue = nearestValue.RawValue + (flipHorizontal ? -delta : delta);
            var finalValue = flipVertical ? -interpolatedValue : interpolatedValue;
            return new Fix64(finalValue);
        }

        /// <summary>
        /// Returns the hyperbolic sine of the specified angle.
        /// </summary>
        /// <param name="value">An angle, measured in radians.</param>
        /// <returns></returns>
        public static Fix64 Sinh(Fix64 value)
        {
            throw new NotImplementedException();
        }

        // This function is not well-tested. It may be wildly inaccurate.
        /// <summary>
        /// Returns the tangent of the specified angle.
        /// </summary>
        /// <param name="x">An angle, measured in radians.</param>
        /// <returns></returns>
        public static Fix64 Tan(Fix64 x)
        {
            var clampedPi = x.RawValue % _PI;
            var flip = false;
            if (clampedPi < 0)
            {
                clampedPi = -clampedPi;
                flip = true;
            }
            if (clampedPi > PI_OVER_2)
            {
                flip = !flip;
                clampedPi = PI_OVER_2 - (clampedPi - PI_OVER_2);
            }

            var clamped = new Fix64(clampedPi);

            // Find the two closest values in the LUT and perform linear interpolation
            var rawIndex = Fix64.FastMultiply(clamped, LutInterval);
            var roundedIndex = Round(rawIndex);
            var indexError = Fix64.FastSubtract(rawIndex, roundedIndex);

            var nearestValue = new Fix64(TanLut[(int)roundedIndex]);
            var secondNearestValue = new Fix64(TanLut[(int)roundedIndex + Sign(indexError)]);

            var delta = Fix64.FastMultiply(indexError, FastAbs(Fix64.FastSubtract(nearestValue, secondNearestValue))).RawValue;
            var interpolatedValue = nearestValue.RawValue + delta;
            var finalValue = flip ? -interpolatedValue : interpolatedValue;
            return new Fix64(finalValue);
        }

        private static long ClampSinValue(long angle, out bool flipHorizontal, out bool flipVertical)
        {
            var largePI = 7244019458077122842;
            // Obtained from ((Fix64)1686629713.065252369824872831112M).m_rawValue
            // This is (2^29)*PI, where 29 is the largest N such that (2^N)*PI < MaxValue.
            // The idea is that this number contains way more precision than PI_TIMES_2,
            // and (((x % (2^29*PI)) % (2^28*PI)) % ... (2^1*PI) = x % (2 * PI)
            // In practice this gives us an error of about 1,25e-9 in the worst case scenario (Sin(MaxValue))
            // Whereas simply doing x % PI_TIMES_2 is the 2e-3 range.

            var clamped2Pi = angle;
            for (int i = 0; i < 29; ++i)
            {
                clamped2Pi %= (largePI >> i);
            }
            if (angle < 0)
            {
                clamped2Pi += PI_TIMES_2;
            }

            // The LUT contains values for 0 - PiOver2; every other value must be obtained by
            // vertical or horizontal mirroring
            flipVertical = clamped2Pi >= _PI;
            // obtain (angle % PI) from (angle % 2PI) - much faster than doing another modulo
            var clampedPi = clamped2Pi;
            while (clampedPi >= _PI)
            {
                clampedPi -= _PI;
            }
            flipHorizontal = clampedPi >= PI_OVER_2;
            // obtain (angle % PI_OVER_2) from (angle % PI) - much faster than doing another modulo
            var clampedPiOver2 = clampedPi;
            if (clampedPiOver2 >= PI_OVER_2)
            {
                clampedPiOver2 -= PI_OVER_2;
            }
            return clampedPiOver2;
        }

        /// <summary>
        /// Returns the hyperbolic tangent of the specified angle.
        /// </summary>
        /// <param name="value">An angle, measured in radians.</param>
        /// <returns></returns>
        public static Fix64 Tanh(Fix64 value)
        {
            throw new NotImplementedException();
        }
    }
}
