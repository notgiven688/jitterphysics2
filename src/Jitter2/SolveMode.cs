/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Jitter2.Collision;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.Parallelization;
using Jitter2.Unmanaged;

namespace Jitter2;

/// <summary>
/// Selects how the solver groups the contacts that it solves at the same time. Both modes use the
/// same solver and produce equally valid results.
/// </summary>
public enum SolveMode
{
    /// <summary>
    /// A contact keeps its group for as long as it exists, so only new contacts have to be
    /// grouped in a step. This is the fastest mode. The result depends on the order in which the
    /// contacts appeared, and therefore on the history of the world, not only on its current state.
    /// With several threads that order depends on timing, so two runs may differ.
    /// </summary>
    Regular,

    /// <summary>
    /// The contacts are grouped anew in every step, in an order that depends only on which
    /// contacts exist. The same world state then gives the same result on every run, for any
    /// number of threads and after restoring an earlier state.
    /// </summary>
    Deterministic
}

public sealed partial class World
{
    private SolveMode solveMode = SolveMode.Regular;

    /// <summary>
    /// Gets or sets how the solver groups the contacts that it solves at the same time. The
    /// default is <see cref="SolveMode.Regular"/>. The mode may be changed between steps.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an undefined <see cref="Jitter2.SolveMode"/>
    /// value is assigned.</exception>
    public SolveMode SolveMode
    {
        get => solveMode;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "The solve mode must be a defined enum value.");
            }

            solveMode = value;
            constraintColoring.KeepColors = value == SolveMode.Regular;
        }
    }

    // A body that becomes or stops being dynamic changes which contacts may share a color.
    internal void InvalidateContactColoring()
    {
        contactColoring.Invalidate();
        constraintColoring.Invalidate();
    }
}
