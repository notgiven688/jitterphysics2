# General

This section covers fundamental design decisions and configuration options.

## Collision Detection Philosophy

Jitter2 takes a unified approach to collision detection that differs from many other physics engines. Unlike engines that implement dedicated algorithms for specific shape pairs (sphere-sphere, box-box, capsule-capsule, etc.), Jitter2 treats all collision detection uniformly using implicit shapes. Every shape is represented through a support function, and collisions are resolved via MPR (Minkowski Portal Refinement), falling back to EPA (Expanding Polytope Algorithm) for deep penetrations. This design simplifies the codebase and makes it straightforward to add custom shapes—any shape that provides a support mapping automatically works with all other shapes.

Traditional physics engines use a three-phase collision pipeline: broad phase (spatial partitioning), mid-phase (hierarchical bounding volumes for complex meshes), and narrow phase (exact shape intersection). Jitter2 eliminates the mid-phase entirely. Instead of building internal acceleration structures for complex geometry, Jitter2 relies on [collision filters](filters.md) to handle large-scale environments. This enables user-defined spatial partitioning strategies—heightmaps, detailed triangle meshes, or even infinite voxel worlds can be implemented by generating collision geometry on-demand within filter callbacks.

## Precision

Jitter2 supports both single-precision (`float`) and double-precision (`double`) floating-point arithmetic, selected at compile time. To build with double precision, either uncomment `#define USE_DOUBLE_PRECISION` in [Precision.cs](https://github.com/notgiven688/jitterphysics2/blob/main/src/Jitter2/Precision.cs), or use the command line option:

```bash
dotnet build -c Release -p:DoublePrecision=true
```

The active precision mode can be checked at runtime via `Precision.IsDoublePrecision`. In single precision, `JVector.X` is a `float`; in double precision, it is a `double`.

## Deterministic Simulation

Jitter2 provides an optional cross-platform deterministic solver mode:

```cs
world.SolveMode = SolveMode.Deterministic;
```

This mode is intended for reproducible simulation across platforms when the same Jitter2 version, precision mode, and stepping inputs are used. It is useful for automated tests, replay systems, debugging, and lockstep-style simulation.

The important requirement is not that the entire world matches. What matters is that each interacting island is assembled in the same order: the bodies, shapes, and constraints that participate in that island must be added in the same sequence. If the same island is created in the same order and receives the same inputs, it will evolve the same even if unrelated parts of the world were built differently.

The deterministic path keeps contacts and constraints in a stable order and uses internal stable trigonometric helpers (`StableMath`) so critical math does not depend on platform-specific `Math`/`MathF` behavior.

| Configuration | What is guaranteed | What must match | What is not guaranteed |
| --- | --- | --- | --- |
| `SolveMode.Deterministic` | Cross-platform reproducible simulation results, independent of threading mode and internal SIMD/scalar execution path | Same Jitter2 version, same precision mode, same step sequence / fixed time step, same order in which the participating bodies, shapes, and constraints are added within each interacting island | Float and double matching each other, results across different engine versions |
| `SolveMode.Regular` with `multiThread: false` | Reproducibility only in the narrow sense that the exact same world build can produce the same result again within the same .NET process | Exact same construction path, exact same addition order, same step sequence, same .NET process / runtime run | Cross-platform determinism, rebuilding the same final scene through a different history, matching results across different process launches |
| `SolveMode.Regular` with `multiThread: true` | No deterministic guarantee | None | Reproducible ordering or cross-platform bit identity |

> [!WARNING]
> To the best of our current knowledge, this feature is cross-platform deterministic for the cases described above, but the claim is based on the current implementation and test coverage. At the time of writing, CI exercises the deterministic hash test on `ubuntu-latest`, `windows-latest`, and `macos-latest`, which currently correspond to x64 Linux, x64 Windows, and arm64 macOS on GitHub-hosted runners.

`SolveMode.Deterministic` can be significantly slower than `SolveMode.Regular`, which remains the default and recommended option for normal gameplay or interactive sandbox scenes.

## Coordinate System

Jitter2 uses a right-handed coordinate system.
The engine itself is coordinate-system agnostic and does not enforce any particular axis convention.

The only default that assumes a specific orientation is `World.Gravity`, which is initialized to `(0, -9.81, 0)`, treating the Y-axis as up.
This can be changed:

```cs
world.Gravity = new JVector(0, 0, -9.81f);  // Z-up convention
```

## Tracing and Logging

Jitter2 provides mechanisms for logging and performance profiling.

### Logging

The `Logger` class provides a simple logging interface with three severity levels: `Information`, `Warning`, and `Error`.
To receive log messages, register a listener:

```cs
Logger.Listener = (level, message) =>
{
    Console.WriteLine($"[{level}] {message}");
};
```

The engine logs warnings for events such as EPA convergence issues or memory allocation fallbacks.

### Performance Tracing

When compiled with the `PROFILE` symbol, Jitter2 records detailed timing information for each simulation phase (broad phase, narrow phase, solver iterations, etc.).
The trace data is stored in thread-local buffers for minimal overhead.

To export the recorded data:

```cs
Tracer.WriteToFile("trace.json");
```

The output file uses the Chrome Trace Event format and can be visualized in:

- `chrome://tracing` (paste in Chrome's address bar)
- [Perfetto UI](https://ui.perfetto.dev/)

When `PROFILE` is not defined, all tracing calls are completely stripped by the compiler via `[Conditional]` attributes, resulting in zero runtime overhead.

## Custom Math Types

Jitter2 defines its own math types (`JVector`, `JMatrix`, `JQuaternion`, `JBoundingBox`) rather than using `System.Numerics`. This allows precision to be switched globally between `float` and `double` without code changes, gives explicit control over memory layout using `[StructLayout(LayoutKind.Explicit)]`, and avoids dependencies on external math library behavior.

The explicit field offsets guarantee a predictable memory layout, enabling zero-copy conversion to and from other libraries' types:

```cs
// Convert to any layout-compatible type
MyVector3 myVec = jitterVector.UnsafeAs<MyVector3>();

// Convert from any layout-compatible type
JVector jitterVector = JVector.UnsafeFrom(myVec);
```

For convenience, implicit conversions to and from `System.Numerics` types are also provided:

```cs
System.Numerics.Vector3 sysVec = jitterVector;  // implicit conversion
JVector jitterVec = sysVec;                      // implicit conversion
```

These conversions involve copying and potential precision loss when converting from double to float.

### Vector and matrix convention

Jitter2 treats vectors as **column vectors**.
A vector $v$ is transformed by a matrix $M$ using $M \cdot v$ (post-multiplication):

```cs
JVector result = JVector.Transform(v, M);  // computes M * v
```

This is the standard convention used in mathematics, physics, and most graphics APIs (e.g., OpenGL, Vulkan/GLSL).
`System.Numerics` uses the opposite convention: vectors are **row vectors** and transformation is written as $v \cdot M$ (pre-multiplication).

As a consequence, when converting transformation matrices between Jitter2 and `System.Numerics`, the matrices must be transposed.
Jitter2's `JMatrix` also stores its elements in column-major order in memory (i.e., `M11`, `M21`, `M31` are contiguous), whereas `System.Numerics.Matrix4x4` uses row-major storage.
