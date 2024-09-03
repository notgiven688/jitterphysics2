namespace FixedMath
{
    public static partial class MathFix
    {
        // FastAbs(Fix64.MinValue) is undefined.
        public static Fix64 FastAbs(Fix64 value)
        {
            // branchless implementation, see http://www.strchr.com/optimized_abs_function
            var mask = value.RawValue >> 63;
            return new Fix64((value.RawValue + mask) ^ mask);
        }

        // Returns a rough approximation of the cosine of x.
        // See FastSin for more details.
        public static Fix64 FastCos(Fix64 x)
        {
            var xl = x.RawValue;
            var rawAngle = xl + (xl > 0 ? -_PI - PI_OVER_2 : PI_OVER_2);
            return FastSin(new Fix64(rawAngle));
        }

        // Returns a rough approximation of the Sine of x.
        // This is at least 3 times faster than Sin() on x86 and slightly faster than Math.Sin(),
        // however its accuracy is limited to 4-5 decimals, for small enough values of x.
        public static Fix64 FastSin(Fix64 x)
        {
            var clampedL = ClampSinValue(x.RawValue, out bool flipHorizontal, out bool flipVertical);

            // Here we use the fact that the SinLut table has a number of entries
            // equal to (PI_OVER_2 >> 15) to use the angle to index directly into it
            var rawIndex = (uint)(clampedL >> 15);
            if (rawIndex >= LUT_SIZE)
            {
                rawIndex = LUT_SIZE - 1;
            }
            var nearestValue = SinLut[flipHorizontal ?
                SinLut.Length - 1 - (int)rawIndex :
                (int)rawIndex];
            return new Fix64(flipVertical ? -nearestValue : nearestValue);
        }

        public static (Fix64 sin, Fix64 cos) SinCos(Fix64 x) 
        {
            return System.MathF.SinCos((float)x);
        }
    }
}
