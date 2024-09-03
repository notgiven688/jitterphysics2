using System;
using System.Linq;

namespace FixedMath
{
    public static partial class MathFix
    {
        /// <summary>
        /// Represents the natural logarithmic base, specified by the constant, e.
        /// </summary>
        public static readonly Fix64 E = new Fix64(_E);
        private static readonly Fix64 Log2Max = new Fix64(LOG_2_MAX);
        private static readonly Fix64 Log2Min = new Fix64(LOG_2_MIN);

        private const long _E = 0xB17217F7;
        private const long LOG_2_MAX = 0x1F00000000;
        private const long LOG_2_MIN = -0x2000000000;

        static MathFix()
        {
            SinLut = Enumerable.Range(0, LUT_SIZE)
                .Select(i =>
                {
                    var angle = i * Math.PI * 0.5 / (LUT_SIZE - 1);
                    var sin = Math.Sin(angle);
                    return ((Fix64)sin).RawValue;
                })
                .ToArray();

            AcosLut = Enumerable.Range(0, LUT_SIZE)
                .Select(i =>
                {
                    var angle = i / (double)(LUT_SIZE - 1);
                    var acos = Math.Acos(angle);
                    return ((Fix64)acos).RawValue;
                })
                .ToArray();

            TanLut = Enumerable.Range(0, LUT_SIZE)
                .Select(i =>
                {
                    var angle = i * Math.PI * 0.5 / (LUT_SIZE - 1);
                    var tan = Math.Tan(angle);
                    if (tan > (double)Fix64.MaxValue || tan < 0.0)
                    {
                        tan = (double)Fix64.MaxValue;
                    }
                    return (((decimal)tan > (decimal)Fix64.MaxValue || tan < 0.0) ? Fix64.MaxValue : (Fix64)tan).RawValue;
                })
                .ToArray();
        }

        // Note: Abs(Fix64.MinValue) == Fix64.MaxValue.
        /// <summary>
        /// Returns the absolute value of a <see cref="Fix64"/> number.
        /// </summary>
        /// <param name="value">A number that is greater than or equal to <see cref="Fix64.MinValue"/>, but less than or equal to <see cref="Fix64.MaxValue"/>.</param>
        /// <returns></returns>
        public static Fix64 Abs(Fix64 value)
        {
            if (value.RawValue == Fix64.MIN_VALUE)
            {
                return Fix64.MaxValue;
            }

            // branchless implementation, see http://www.strchr.com/optimized_abs_function
            var mask = value.RawValue >> 63;
            return new Fix64((value.RawValue + mask) ^ mask);
        }

        public static Fix64 Cbrt(Fix64 x)
        {
            throw new NotImplementedException();
        }

        // Returns the smallest integral value that is greater than or equal to the specified number.
        /// <summary>
        /// Returns the smallest integral value that is greater than or equal to the specified <see cref="Fix64"/> number.
        /// </summary>
        /// <param name="value">A <see cref="Fix64"/> number.</param>
        /// <returns></returns>
        public static Fix64 Ceiling(Fix64 value)
        {
            var hasFractionalPart = (value.RawValue & 0x00000000FFFFFFFF) != 0;
            return hasFractionalPart ? Floor(value) + Fix64.One : value;
        }

        public static Fix64 Clamp(Fix64 value, Fix64 min, Fix64 max)
        {
            return Math.Clamp((float)value, (float)min, (float)max);
        }

        /// <summary>
        /// Returns the hyperbolic cosine of the specified angle.
        /// </summary>
        /// <param name="value">An angle, measured in radians.</param>
        /// <returns></returns>
        public static Fix64 Cosh(Fix64 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns e raised to the specified power.
        /// </summary>
        /// <param name="x">A number specifying a power.</param>
        /// <returns></returns>
        public static Fix64 Exp(Fix64 x)
        {
            throw new NotImplementedException();
        }

        // Returns the largest integer less than or equal to the specified number.
        /// <summary>
        /// Returns the largest integer less than or equal to the specified <see cref="Fix64"/> number.
        /// </summary>
        /// <param name="value">A <see cref="Fix64"/> number.</param>
        /// <returns></returns>
        public static Fix64 Floor(Fix64 value)
        {
            // Just zero out the fractional part
            return new Fix64((long)((ulong)value.RawValue & 0xFFFFFFFF00000000));
        }

        /// <summary>
        /// Returns the remainder resulting from the division of a specified number by another specified number.
        /// </summary>
        /// <param name="x">A dividend.</param>
        /// <param name="y">A divisor.</param>
        /// <returns></returns>
        public static Fix64 IEEERemainder(Fix64 x, Fix64 y)
        {
            // TODO
            return x % y;
        }

        // Provides at least 7 decimals of accuracy.
        /// <summary>
        /// Returns the natural (base e) logarithm of a specified number.
        /// </summary>
        /// <param name="x">The number whose logarithm is to be found.</param>
        /// <returns></returns>
        public static Fix64 Log(Fix64 x)
        {
            return Fix64.FastMultiply(Log2(x), E);
        }

        /// <summary>
        /// Returns the logarithm of a specified number in a specified base.
        /// </summary>
        /// <param name="a">The number whose logarithm is to be found.</param>
        /// <param name="newBase">The base of the logarithm.</param>
        /// <returns></returns>
        public static Fix64 Log(Fix64 a, Fix64 newBase)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the base 10 logarithm of a specified number.
        /// </summary>
        /// <param name="x">A number whose logarithm is to be found.</param>
        /// <returns></returns>
        public static Fix64 Log10(Fix64 x)
        {
            throw new NotImplementedException();
        }

        // Provides at least 9 decimals of accuracy.
        /// <summary>
        /// Returns the base 2 logarithm of a specified number.
        /// </summary>
        /// <param name="x">A number whose logarithm is to be found.</param>
        /// <returns></returns>
        public static Fix64 Log2(Fix64 x)
        {
            if (x.RawValue <= 0)
            {
                throw new ArgumentOutOfRangeException("Non-positive value passed to Ln", "x");
            }

            // This implementation is based on Clay. S. Turner's fast binary logarithm
            // algorithm (C. S. Turner,  "A Fast Binary Logarithm Algorithm", IEEE Signal
            //     Processing Mag., pp. 124,140, Sep. 2010.)

            long b = 1U << (Fix64.FRACTIONAL_PLACES - 1);
            long y = 0;

            long rawX = x.RawValue;
            while (rawX < Fix64.ONE)
            {
                rawX <<= 1;
                y -= Fix64.ONE;
            }

            while (rawX >= (Fix64.ONE << 1))
            {
                rawX >>= 1;
                y += Fix64.ONE;
            }

            var z = new Fix64(rawX);

            for (int i = 0; i < Fix64.FRACTIONAL_PLACES; i++)
            {
                z = Fix64.FastMultiply(z, z);
                if (z.RawValue >= (Fix64.ONE << 1))
                {
                    z = new Fix64(z.RawValue >> 1);
                    y += b;
                }
                b >>= 1;
            }

            return new Fix64(y);
        }

        /// <summary>
        /// Returns the larger of two <see cref="Fix64"/> numbers.
        /// </summary>
        /// <param name="val1">The first of two <see cref="Fix64"/> numbers to compare.</param>
        /// <param name="val2">The second of two <see cref="Fix64"/> numbers to compare.</param>
        /// <returns></returns>
        public static Fix64 Max(Fix64 val1, Fix64 val2)
        {
            return (val1 < val2) ? val2 : val1;
        }

        /// <summary>
        /// Returns the smaller of two <see cref="Fix64"/> numbers.
        /// </summary>
        /// <param name="val1">The first of two <see cref="Fix64"/> numbers to compare.</param>
        /// <param name="val2">The second of two <see cref="Fix64"/> numbers to compare.</param>
        /// <returns></returns>
        public static Fix64 Min(Fix64 val1, Fix64 val2)
        {
            return (val1 < val2) ? val1 : val2;
        }

        // Provides about 5 digits of accuracy for the result.
        /// <summary>
        /// Returns a specified number raised to the specified power.
        /// </summary>
        /// <param name="x">A <see cref="Fix64"/> number to be raised to a power.</param>
        /// <param name="y">A <see cref="Fix64"/> number that specifies a power.</param>
        /// <returns></returns>
        public static Fix64 Pow(Fix64 x, Fix64 y)
        {
            if (x == Fix64.One)
            {
                return Fix64.One;
            }
            if (y.RawValue == 0)
            {
                return Fix64.One;
            }
            if (x.RawValue == 0)
            {
                if (y.RawValue < 0)
                {
                    throw new DivideByZeroException();
                }
                return Fix64.Zero;
            }

            Fix64 log2 = Log2(x);
            return Pow2(y * log2);
        }

        // Provides at least 6 decimals of accuracy.
        /// <summary>
        /// Returns 2 raised to the specified power.
        /// </summary>
        /// <param name="x">A number specifying a power.</param>
        /// <returns></returns>
        public static Fix64 Pow2(Fix64 x)
        {
            if (x.RawValue == 0)
            {
                return Fix64.One;
            }

            // Avoid negative arguments by exploiting that exp(-x) = 1/exp(x).
            bool neg = x.RawValue < 0;
            if (neg)
            {
                x = -x;
            }

            if (x == Fix64.One)
            {
                return neg ? Fix64.One / (Fix64)2 : (Fix64)2;
            }
            if (x >= Log2Max)
            {
                return neg ? Fix64.One / Fix64.MaxValue : Fix64.MaxValue;
            }
            if (x <= Log2Min)
            {
                return neg ? Fix64.MaxValue : Fix64.Zero;
            }

            /* The algorithm is based on the power series for exp(x):
             * http://en.wikipedia.org/wiki/Exponential_function#Formal_definition
             * 
             * From term n, we get term n+1 by multiplying with x/n.
             * When the sum term drops to zero, we can stop summing.
             */

            int integerPart = (int)Floor(x);
            // Take fractional part of exponent
            x = new Fix64(x.RawValue & 0x00000000FFFFFFFF);

            var result = Fix64.One;
            var term = Fix64.One;
            int i = 1;
            while (term.RawValue != 0)
            {
                term = Fix64.FastMultiply(Fix64.FastMultiply(x, term), E) / (Fix64)i;
                result += term;
                i++;
            }

            result = Fix64.FromRaw(result.RawValue << integerPart);
            if (neg)
            {
                result = Fix64.One / result;
            }

            return result;
        }

        // If the value is halfway between an even and an uneven value, returns the even value.
        /// <summary>
        /// Rounds a <see cref="Fix64"/> value to the nearest integral value.
        /// </summary>
        /// <param name="value">A<see cref="Fix64"/> number to be rounded.</param>
        /// <returns></returns>
        public static Fix64 Round(Fix64 value)
        {
            var fractionalPart = value.RawValue & 0x00000000FFFFFFFF;
            var integralPart = Floor(value);
            if (fractionalPart < 0x80000000)
            {
                return integralPart;
            }
            if (fractionalPart > 0x80000000)
            {
                return integralPart + Fix64.One;
            }
            // if number is halfway between two values, round to the nearest even number
            // this is the method used by System.Math.Round().
            return (integralPart.RawValue & Fix64.ONE) == 0
                       ? integralPart
                       : integralPart + Fix64.One;
        }

        // Returns 1 if the value is positive, 0 if is 0, and -1 if it is negative.
        /// <summary>
        /// Returns an integer that indicates the sign of a <see cref="Fix64"/> number.
        /// </summary>
        /// <param name="value">A signed number.</param>
        /// <returns></returns>
        public static int Sign(Fix64 value)
        {
            return value.RawValue < 0 ? -1 :
                value.RawValue > 0 ? 1 :
                0;
        }

        /// <summary>
        /// Returns the square root of a specified number.
        /// </summary>
        /// <param name="x">The number whose square root is to be found.</param>
        /// <returns></returns>
        public static Fix64 Sqrt(Fix64 x)
        {
            var xl = x.RawValue;
            if (xl < 0)
            {
                // We cannot represent infinities like Single and Double, and Sqrt is
                // mathematically undefined for x < 0. So we just throw an exception.
                throw new ArgumentOutOfRangeException(nameof(x), "Negative value passed to Sqrt");
            }

            var num = (ulong)xl;
            var result = 0UL;

            // second-to-top bit
            var bit = 1UL << (Fix64.NUM_BITS - 2);

            while (bit > num)
            {
                bit >>= 2;
            }

            // The main part is executed twice, in order to avoid
            // using 128 bit values in computations.
            for (var i = 0; i < 2; ++i)
            {
                // First we get the top 48 bits of the answer.
                while (bit != 0)
                {
                    if (num >= result + bit)
                    {
                        num -= result + bit;
                        result = (result >> 1) + bit;
                    }
                    else
                    {
                        result = result >> 1;
                    }
                    bit >>= 2;
                }

                if (i == 0)
                {
                    // Then process it again to get the lowest 16 bits.
                    if (num > (1UL << (Fix64.NUM_BITS / 2)) - 1)
                    {
                        // The remainder 'num' is too large to be shifted left
                        // by 32, so we have to add 1 to result manually and
                        // adjust 'num' accordingly.
                        // num = a - (result + 0.5)^2
                        //       = num + result^2 - (result + 0.5)^2
                        //       = num - result - 0.5
                        num -= result;
                        num = (num << (Fix64.NUM_BITS / 2)) - 0x80000000UL;
                        result = (result << (Fix64.NUM_BITS / 2)) + 0x80000000UL;
                    }
                    else
                    {
                        num <<= (Fix64.NUM_BITS / 2);
                        result <<= (Fix64.NUM_BITS / 2);
                    }

                    bit = 1UL << (Fix64.NUM_BITS / 2 - 2);
                }
            }
            // Finally, if next bit would have been 1, round the result upwards.
            if (num > result)
            {
                ++result;
            }
            return new Fix64((long)result);
        }

        /// <summary>
        /// Calculates the integral part of a specified double-precision floating-point number.
        /// </summary>
        /// <param name="x">A number to truncate.</param>
        /// <returns></returns>
        public static Fix64 Truncate(Fix64 x)
        {
            throw new NotImplementedException();
        }
    }
}
