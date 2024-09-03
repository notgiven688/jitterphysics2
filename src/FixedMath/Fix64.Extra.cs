namespace FixedMath
{
    public partial struct Fix64
    {
        public static readonly Fix64 Epsilon = One / 1000;
        public static readonly Fix64 PositiveInfinity = MaxValue;
        public static readonly Fix64 NegativeInfinity = MinValue;
        public static readonly Fix64 NaN = MinValue;

        public static bool IsNaN(Fix64 f)
        {
            return false;
        }

        public static bool IsInfinity(Fix64 f)
        {
            return false;
        }

        public static bool IsNegativeInfinity(Fix64 f)
        {
            return false;
        }

        public static bool IsPositiveInfinity(Fix64 f)
        {
            return false;
        }

        /// <summary>
        /// Adds x and y witout performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static Fix64 FastAdd(Fix64 x, Fix64 y)
        {
            return new Fix64(x.m_rawValue + y.m_rawValue);
        }

        /// <summary>
        /// Subtracts y from x witout performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static Fix64 FastSubtract(Fix64 x, Fix64 y)
        {
            return new Fix64(x.m_rawValue - y.m_rawValue);
        }

        /// <summary>
        /// Performs multiplication without checking for overflow.
        /// Useful for performance-critical code where the values are guaranteed not to cause overflow
        /// </summary>
        public static Fix64 FastMultiply(Fix64 x, Fix64 y)
        {
            var xl = x.m_rawValue;
            var yl = y.m_rawValue;

            var xlo = (ulong)(xl & 0x00000000FFFFFFFF);
            var xhi = xl >> FRACTIONAL_PLACES;
            var ylo = (ulong)(yl & 0x00000000FFFFFFFF);
            var yhi = yl >> FRACTIONAL_PLACES;

            var lolo = xlo * ylo;
            var lohi = (long)xlo * yhi;
            var hilo = xhi * (long)ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FRACTIONAL_PLACES;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FRACTIONAL_PLACES;

            var sum = (long)loResult + midResult1 + midResult2 + hiResult;
            return new Fix64(sum);
        }

        /// <summary>
        /// Performs modulo as fast as possible; throws if x == MinValue and y == -1.
        /// Use the operator (%) for a more reliable but slower modulo.
        /// </summary>
        public static Fix64 FastRemainder(Fix64 x, Fix64 y)
        {
            return new Fix64(x.m_rawValue % y.m_rawValue);
        }
    }
}
