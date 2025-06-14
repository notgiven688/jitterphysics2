/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

// Uncomment here to build Jitter using double precision
// --------------------------------------------------------
#define USE_DOUBLE_PRECISION
// --------------------------------------------------------
// Or use command line option, e.g.,
// dotnet build -c Release -p:DoublePrecision=true

#if USE_DOUBLE_PRECISION

global using Real = System.Double;
global using MathR = System.Math;
global using Vector = System.Runtime.Intrinsics.Vector256;
global using VectorReal = System.Runtime.Intrinsics.Vector256<System.Double>;

#else

global using Real = System.Single;
global using MathR = System.MathF;
global using Vector = System.Runtime.Intrinsics.Vector128;
global using VectorReal = System.Runtime.Intrinsics.Vector128<System.Single>;

#endif

namespace Jitter2;
public static class Precision
{
#if USE_DOUBLE_PRECISION
        public const int ConstraintSizeFull = 512;
        public const int ConstraintSizeSmall = 256;
        public const int RigidBodyDataSize = 256;
#else
        public const int ConstraintSizeFull = 256;
        public const int ConstraintSizeSmall = 128;
        public const int RigidBodyDataSize = 128;
#endif

    /// <summary>
    /// Gets a value indicating whether the engine is configured to use double-precision floating-point numbers.
    /// </summary>
    public static bool IsDoublePrecision => sizeof(Real) == sizeof(double);
}