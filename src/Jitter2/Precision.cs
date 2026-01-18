/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

// Uncomment here to build Jitter using double precision
// --------------------------------------------------------
// #define USE_DOUBLE_PRECISION
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

/// <summary>
/// Provides constants and utilities related to floating-point precision configuration.
/// The library can be compiled with single-precision (float) or double-precision (double)
/// by defining the <c>USE_DOUBLE_PRECISION</c> symbol.
/// </summary>
public static class Precision
{
#if USE_DOUBLE_PRECISION
    /// <summary>
    /// The size in bytes of a full constraint data structure.
    /// </summary>
    public const int ConstraintSizeFull = 512;

    /// <summary>
    /// The size in bytes of a small constraint data structure.
    /// </summary>
    public const int ConstraintSizeSmall = 256;

    /// <summary>
    /// The size in bytes of the <see cref="Dynamics.RigidBodyData"/> structure.
    /// </summary>
    public const int RigidBodyDataSize = 256;
#else
    /// <summary>
    /// The size in bytes of a full constraint data structure.
    /// </summary>
    public const int ConstraintSizeFull = 256;

    /// <summary>
    /// The size in bytes of a small constraint data structure.
    /// </summary>
    public const int ConstraintSizeSmall = 128;

    /// <summary>
    /// The size in bytes of the <see cref="Dynamics.RigidBodyData"/> structure.
    /// </summary>
    public const int RigidBodyDataSize = 128;
#endif

    /// <summary>
    /// Gets a value indicating whether the engine is configured to use double-precision floating-point numbers.
    /// </summary>
    public static bool IsDoublePrecision => sizeof(Real) == sizeof(double);
}