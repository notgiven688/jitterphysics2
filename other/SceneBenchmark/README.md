# Scene comparison benchmark

Build two Jitter versions, simulate the same scenes headlessly, and write one
PDF with an overview, provenance, two pages per scene, and a final bar scorecard.
Python orchestrates the experiment and creates the report; a small C# executable
calls the actual engine. No renderer or BenchmarkDotNet dependency is involved.

## Run

Requires Git, the .NET 10 SDK, and Python 3.12 or newer. From the repository root:

```sh
python3 -m venv other/SceneBenchmark/.venv
other/SceneBenchmark/.venv/bin/python -m pip install -r other/SceneBenchmark/requirements.txt
other/SceneBenchmark/.venv/bin/python other/SceneBenchmark/run.py compare 2.9.0 local
```

On Windows, use `.venv\Scripts\python.exe`. References can be tags, commit hashes,
or Git commit expressions such as `HEAD^`. `local` includes the current library's
tracked edits and untracked source files. Both references can be Git revisions,
and either side can be `local`. Revisions must be available in the local repository.

Defaults: both scenes, 20 simulated seconds each, timestep 0.01 s, three
fresh-process repetitions, one total thread, two seconds of warm-up, `(8, 4)`
solver/relaxation iterations, one substep, and single precision. Sleeping is
disabled in every scene. An example with single-threaded and parallel results:

```sh
other/SceneBenchmark/.venv/bin/python other/SceneBenchmark/run.py compare 6520fddb local \
  --threads 1 16 --repetitions 3 --output other/SceneBenchmark/results/tag-vs-local
```

`--threads` counts the calling thread. Multiple configurations get separate
scene pages and final scorecards in the same PDF. Run on an otherwise idle host.
The default single-threaded full experiment may take several minutes; simulated
duration does not imply real-time execution. For a quick integration check:

```sh
other/SceneBenchmark/.venv/bin/python other/SceneBenchmark/run.py compare HEAD local \
  --seconds 0.1 --warmup 0.1 --repetitions 1 --threads 1 16 \
  --output other/SceneBenchmark/results/quick-check
```

Other options: `--scenes colosseum ragdolls`, `--dt`, `--warmup`,
`--iterations`, `--relaxations`, `--substeps`, and `--trial-timeout` (wall seconds
per process, default 1,800). Durations must be whole numbers of timesteps and
`dt` cannot exceed 1/60 s. A nonempty output directory is never reused.

## Scenes

* **Colosseum:** Demo28's six tiers and 21 concentric double-wall/platform rings,
  on the same 200-unit floor. The geometry is a frozen copy of the demo, including
  its original BepuPhysics attribution and Apache 2.0 notice.
* **Rotating cube with ragdolls:** 500 ten-body ragdolls in a 50-unit hollow
  kinematic cube with one-unit walls and angular velocity `(0.14, 0.02, 0.03)`
  each step, spaced on a 10 x 5 x 10 grid. Each has 20 constraints, totaling 10,000. Shapes, joint
  limits, softness, and the eight adjacent-body collision exclusions come from
  `Common.BuildRagdoll`. RMS and maximum ball-socket anchor separation are
  sampled every ten steps, including hinge anchors (4,500 anchors total).

Both libraries compile against these exact same scene sources. No demo sources
are loaded from the selected revisions, so changing an old demo does not change
the benchmark's inputs. Older revisions must support the harness's public APIs
(`World.DebugTimings`, dynamic-tree profiling, `RawData`, motion types and
ragdoll constraints). An incompatible revision produces a build log and fails;
the runner does not patch either implementation or fall back to another ref.

## Measurement and report

Each trial is a fresh .NET process. It warms a discarded copy of the scene,
then recreates the original placement and measures the complete 0-20 second
trajectory. Only `World.Step` is timed with the external stopwatch. Scene setup,
container velocity assignment, preallocated observations, joint diagnostics,
and serialization are outside that stopwatch. Managed allocation counters cover
the measurement loop across all threads; diagnostics do not allocate per frame.

The PDF includes:

* A first-page result naming the faster version (A or B), its speedup, and the
  percentage reduction in mean step time. Equal results are labeled as a tie.
* Mean step and Solve curves versus simulated time, narrow/broad-phase curves,
  and active contact-manifold counts, with repeat ranges shaded.
* All named `World.Timings` and `DynamicTree.Timings` buckets, with numeric
  mean values, plus the elapsed time outside World timing buckets (step boundary
  overhead). Tree timings are nested inside BroadPhase, not additional cost.
  Buckets absent from one release are labeled `n/a` rather than treated as zero.
* P95/P99 step latency, the range of process means, managed allocations, GC
  collections, engine buffer memory, and broad-phase candidate counts.
* Engine memory curves or ragdoll joint-quality curves, plus finite-state checks.
* Final absolute mean step/Solve bar charts with repeat-range error bars and
  relative changes, plus accumulated elapsed step time for the full interval.

The `Solve` bucket includes integration and both contact/constraint solving;
it does not isolate joint iteration cost. Active contacts count manifolds, not
individual contact points. Engine memory is the unmanaged body, contact, and
constraint buffer allocation exposed by `RawData`, not total process memory.
Raw trials also retain process peak working set (including setup and warm-up)
and the final managed heap size.

Curves use 0.25-second bin means. Every frame is kept in raw JSON. Summary
statistics give each process equal weight; P95/P99 are calculated per process
and then averaged. Error bars/shading are observed ranges, not confidence
intervals. The overall speedup is the geometric mean of baseline/candidate
mean-step ratios, with each scene/thread configuration equally weighted. The
first-page speedup compares the faster version with the slower one; the JSON
`geometricMeanSpeedup` retains the baseline/candidate ratio for compatibility.

Solvers can change trajectories, contact workloads, and joint errors. The PDF
compares the resulting simulations, without claiming identical collision queries
or statistical significance from correlated frames. Repetitions alternate A/B
order within scenes, rotate scene order, and reverse thread order. They execute
serially. Tiered compilation and server GC are disabled identically for both
builds; all effective `DOTNET_` and `COMPlus_` environment variables are recorded.

## Output and provenance

The default destination is `other/SceneBenchmark/results/<timestamp>/`:

```text
comparison.pdf       # the single shareable report
summary.json         # aggregate metrics
manifest.json        # source/build hashes, settings, machine, schedule, status
build-baseline.log
build-candidate.log
raw/*.json           # frame timings, workload, memory, diagnostics
raw/*.log            # process output and failure details
```

The runner resolves Git refs once and archives just the library sources. `local`
is copied without `bin/obj`. It hashes the complete source snapshot before
building and records the built assembly hash, exact commit, local library status,
and common harness hash. Temporary builds are deleted when the command exits;
the checkout is never switched and its build outputs are not used. A failed trial
marks the manifest failed and preserves logs/data; no partial PDF is published.

Recreate the report from saved data without rebuilding or simulating:

```sh
other/SceneBenchmark/.venv/bin/python other/SceneBenchmark/run.py report \
  other/SceneBenchmark/results/tag-vs-local
```

Add `--output /path/to/report.pdf` to choose a different PDF path. Report generation
validates completed trials, settings, frame counts, finite observations, and
identical initial scene topology. Reports regenerated from older runs include
only colosseum and ragdolls, recalculating the aggregate result from those scenes.
The original raw trials and manifest are preserved. The JSON schema version is currently 1.

Run the small orchestration/statistics regression checks with:

```sh
other/SceneBenchmark/.venv/bin/python -m unittest discover -s other/SceneBenchmark -p 'test_*.py'
```
