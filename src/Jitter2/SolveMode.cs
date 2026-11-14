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
/// Formerly selected between two solvers. Jitter now has a single solver, whose results do not
/// depend on the number of threads, and both values select it.
/// </summary>
[Obsolete("Jitter has a single, deterministic solver. The solve mode no longer has an effect.")]
public enum SolveMode
{
    /// <summary>
    /// The solver. Formerly the default solver, whose multithreaded results could vary between runs.
    /// </summary>
    Regular,

    /// <summary>
    /// The solver. Formerly a separate, slower solver with repeatable results.
    /// </summary>
    Deterministic
}

public sealed partial class World
{
#pragma warning disable CS0618 // The property keeps the obsolete enum for source compatibility.
    private SolveMode solveMode = Jitter2.SolveMode.Regular;

    /// <summary>
    /// Has no effect. Jitter has a single solver; contacts and constraints are grouped so that no
    /// two of a group share a dynamic body, and each group is solved without locks. Its results
    /// do not depend on the number of threads.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an undefined <see cref="Jitter2.SolveMode"/>
    /// value is assigned.</exception>
    [Obsolete("Jitter has a single, deterministic solver. The solve mode no longer has an effect.")]
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
        }
    }
#pragma warning restore CS0618
}
