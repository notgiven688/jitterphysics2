/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Diagnostics;
using Jitter2.LinearMath;

namespace Jitter2;

/// <summary>
/// Runs additional input sanity checks in Debug builds.
/// </summary>
/// <remarks>
/// Calls to this helper are compiled only when the <c>DEBUG</c> symbol is defined. It is used for
/// runtime state mutation paths where invalid values should be caught during development without
/// adding Release-build overhead.
/// </remarks>
internal static class DebugCheck
{
    [Conditional("DEBUG")]
    public static void IsFinite(Real value, string paramName) => ArgumentCheck.Finite(value, paramName);

    [Conditional("DEBUG")]
    public static void IsFinite(JAngle value, string paramName) => ArgumentCheck.Finite(value, paramName);

    [Conditional("DEBUG")]
    public static void IsNotNaN(Real value, string paramName) => ArgumentCheck.NotNaN(value, paramName);

    [Conditional("DEBUG")]
    public static void IsNotNaN(JAngle value, string paramName) => ArgumentCheck.NotNaN(value, paramName);

    [Conditional("DEBUG")]
    public static void IsFinite(in JVector value, string paramName) => ArgumentCheck.Finite(value, paramName);

    [Conditional("DEBUG")]
    public static void IsFinite(in JQuaternion value, string paramName) => ArgumentCheck.Finite(value, paramName);

    [Conditional("DEBUG")]
    public static void IsFinite(in JMatrix value, string paramName) => ArgumentCheck.Finite(value, paramName);

    [Conditional("DEBUG")]
    public static void IsNonNegative(Real value, string paramName) => ArgumentCheck.NonNegative(value, paramName);

    [Conditional("DEBUG")]
    public static void IsPositive(Real value, string paramName) => ArgumentCheck.Positive(value, paramName);

    [Conditional("DEBUG")]
    public static void IsInRange(Real value, Real min, Real max, string paramName) => ArgumentCheck.InRange(value, min, max, paramName);

    [Conditional("DEBUG")]
    public static void IsNonZero(in JVector value, string paramName) => ArgumentCheck.NonZero(value, paramName);

    [Conditional("DEBUG")]
    public static void IsUnitVector(in JVector value, string paramName) => ArgumentCheck.UnitVector(value, paramName);

    [Conditional("DEBUG")]
    public static void IsUnitQuaternion(in JQuaternion value, string paramName) => ArgumentCheck.UnitQuaternion(value, paramName);
}
