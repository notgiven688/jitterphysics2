# Graph-colored contact solver benchmark

This dependency-free .NET 10 console program compares the graph-colored
`SolveMode.Regular` solver with `SolveMode.Deterministic` in two contact-heavy scenes. `ground` has 1,024 independent
boxes sharing one static floor, exposing SIMD throughput and the benefit of not
coloring static-body references. `pile` has 768 boxes in an 8 × 8 × 12 pile with
alternating layers offset to connect neighboring columns. Sleeping is disabled;
both modes use a 0.01-second timestep, eight solver iterations, four relaxation
iterations, and one substep.

From the repository root, a short smoke run is:

```sh
dotnet run --project other/GraphColoredSolverBenchmark -c Release -- both both 4 80 120
```

Arguments are `mode scene threads warmupSteps measuredSteps`, with the defaults
shown above. Mode is `regular`, `deterministic`, or `both`; scene is `ground`,
`pile`, or `both`. Thread count includes the calling thread; `1` exercises the
single-threaded path. Wall-clock duration depends on the machine, typically a
few seconds to tens of seconds. For a longer comparison, build once and run each
configuration in separate processes on an otherwise idle host:

```sh
dotnet build other/GraphColoredSolverBenchmark -c Release
dotnet other/GraphColoredSolverBenchmark/bin/Release/net10.0/GraphColoredSolverBenchmark.dll regular pile 4 300 1000
dotnet other/GraphColoredSolverBenchmark/bin/Release/net10.0/GraphColoredSolverBenchmark.dll deterministic pile 4 300 1000
```

Output is CSV preceded by runtime/SIMD metadata. `step_ms` times only `World.Step`;
`solve_ms` includes graph coloring, contact packing, force integration, solving,
velocity integration, and relaxation. `reorder_ms` captures the regular solver's
contact ordering; graph coloring runs inside `Solve` after substep callbacks. Managed
allocation and GC counts cover the measured loop across all threads. The program
checks that all final body states are finite and no body center fell below the
floor, and reports the final minimum/maximum heights as a simple quality check.

Warm-up advances the scene before measurement. Different solver ordering can
produce different trajectories and contact counts, so compare both workloads
and heights with timings. These smoke checks do not replace solver correctness
tests. Repeat runs and alternate mode order when drawing performance conclusions.
