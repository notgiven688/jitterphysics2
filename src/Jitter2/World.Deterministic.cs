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
/// Controls which solver strategy is used during <see cref="World.Step"/>.
/// </summary>
public enum SolveMode
{
    /// <summary>
    /// The default solver, optimized for speed. Multithreaded results may vary between runs.
    /// </summary>
    Regular,

    /// <summary>
    /// The graph-colored solver, see <see cref="GraphColored"/>. Its results are repeatable: the
    /// same inputs give the same result for any number of threads.
    /// </summary>
    Deterministic,

    /// <summary>
    /// Groups contacts and joints that share no dynamic body and solves each group without locks,
    /// contacts several at once with SIMD. Faster than <see cref="Regular"/> for scenes dominated
    /// by contacts, such as piles and stacks. The result does not depend on the number of threads
    /// or on their timing.
    /// </summary>
    GraphColored
}

public sealed partial class World
{
    private SolveMode solveMode = Jitter2.SolveMode.Regular;

    /// <summary>
    /// The solver strategy used during <see cref="Step"/>. Defaults to <see cref="Jitter2.SolveMode.Regular"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Jitter2.SolveMode.Deterministic"/> and <see cref="Jitter2.SolveMode.GraphColored"/>
    /// select the same solver.
    /// </remarks>
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
        }
    }
}
