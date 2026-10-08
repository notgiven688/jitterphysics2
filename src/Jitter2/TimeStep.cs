/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;

namespace Jitter2;

/// <summary>Timing information for a simulation step and its solver substeps.</summary>
/// <remarks>
/// Force and torque limits integrate over <see cref="SubstepDt"/>. Position-correction
/// bias uses <see cref="InverseStepDt"/> so its rate is measured over the full step.
/// </remarks>
public readonly struct TimeStep
{
    /// <summary>The full simulation step duration in seconds.</summary>
    public Real StepDt { get; }

    /// <summary>The duration of one solver substep in seconds.</summary>
    public Real SubstepDt { get; }

    /// <summary>The inverse full step duration, or zero for a zero-duration step.</summary>
    public Real InverseStepDt { get; }

    /// <summary>The inverse substep duration, or zero for a zero-duration step.</summary>
    public Real InverseSubstepDt { get; }

    /// <summary>The number of substeps in the simulation step.</summary>
    public int SubstepCount { get; }

    /// <summary>Creates timing information for a full step divided into substeps.</summary>
    /// <param name="dt">The non-negative, finite full step duration in seconds.</param>
    /// <param name="substepCount">The positive number of solver substeps.</param>
    public TimeStep(Real dt, int substepCount = 1)
    {
        ArgumentCheck.NonNegative(dt, nameof(dt));
        if (substepCount < 1) throw new ArgumentOutOfRangeException(nameof(substepCount));

        StepDt = dt;
        SubstepCount = substepCount;
        SubstepDt = dt / substepCount;
        InverseStepDt = dt > 0 ? (Real)1.0 / dt : 0;
        InverseSubstepDt = SubstepDt > 0 ? (Real)1.0 / SubstepDt : 0;
    }
}
