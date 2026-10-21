# General

This page covers collision detection, precision, repeatable simulations, and math conventions.

## Collision Detection Philosophy

Jitter2 uses the same collision algorithms for all convex shapes. Each shape provides a **support function**: given a direction, it returns the furthest point in that direction. This is enough for a custom shape to collide with the built-in shapes. You do not need to write a separate collision test for each shape pair.

Collision detection has two stages. The **broad phase** finds pairs whose bounding boxes overlap. The **narrow phase** tests the shapes in each pair and calculates contact points. Jitter2 uses MPR (Minkowski Portal Refinement), with EPA (Expanding Polytope Algorithm) for deeper overlaps.

For large environments, [collision filters](filters.md) let you supply geometry as needed. For example, a filter can generate nearby triangles from a heightmap or voxel world without adding the entire environment to the simulation.

## Precision

Jitter2 supports both single-precision (`float`) and double-precision (`double`) floating-point arithmetic, selected at compile time. To build with double precision, either uncomment `#define USE_DOUBLE_PRECISION` in [Precision.cs](https://github.com/notgiven688/jitterphysics2/blob/main/src/Jitter2/Precision.cs), or use the command line option:

```bash
dotnet build -c Release -p:DoublePrecision=true
```

The active precision mode can be checked at runtime via `Precision.IsDoublePrecision`. In single precision, `JVector.X` is a `float`; in double precision, it is a `double`.

## Build Configuration and Validation

Jitter2 checks setup values such as shape dimensions, masses, constraint axes, and limits. Invalid values throw .NET exceptions in both Debug and Release builds.

Debug builds also check changes to positions, velocities, forces, and orientations. These extra checks catch values such as infinity, NaN, or a quaternion that is not normalized.

NuGet packages use Release builds. To get the extra Debug checks, build Jitter2 from source in the Debug configuration.

## Deterministic Simulation

Jitter2 provides an optional cross-platform deterministic solver mode via <xref:Jitter2.SolveMode>:

```cs
world.SolveMode = SolveMode.Deterministic;
```

Deterministic mode produces repeatable results across platforms when you use the same Jitter2 version, precision, time steps, and inputs. This is useful for tests, replays, and debugging.

The creation order matters within each **island**, a group of bodies connected through contacts or constraints. Add the participating bodies, shapes, and constraints in the same order on every run. Unrelated parts of the world can be created in a different order.

| Solver configuration | Repeatability |
| --- | --- |
| `SolveMode.Deterministic` | Designed to give the same results across platforms and thread counts when the setup and inputs above match. |
| `SolveMode.Regular` with `multiThread: false` | Repeating the exact same setup within one .NET process may give the same results. This is not a cross-platform guarantee. |
| `SolveMode.Regular` with `multiThread: true` | Results may vary between runs because the processing order can change. |

Float and double builds do not produce identical results, and results may change between Jitter2 versions.

> [!NOTE]
> Cross-platform determinism is tested in CI on Linux x64, Windows x64, and macOS arm64. These tests support the claim for the configurations they cover; they cannot establish it for every possible scene or platform.

Deterministic mode can be significantly slower. `SolveMode.Regular` remains the default for applications that do not need repeatable results.

## Coordinate System

Jitter2's vector and rotation math uses a right-handed coordinate system. You can choose which axis points up.

Gravity defaults to `(0, -9.81, 0)`, so the default is Y-up. For a Z-up scene, change it to:

```cs
world.Gravity = new JVector(0, 0, -9.81f);  // Z-up convention
```

## Tracing and Logging

Use logging to receive engine messages and tracing to investigate performance.

### Logging

To receive `Information`, `Warning`, and `Error` messages, assign a listener:

```cs
Logger.Listener = (level, message) =>
{
    Console.WriteLine($"[{level}] {message}");
};
```

The engine logs warnings for events such as EPA convergence issues or memory allocation fallbacks.

### Performance Tracing

Build Jitter2 with the `PROFILE` symbol to record how long collision detection, constraint solving, and other parts of each step take.

To export the recorded data:

```cs
Tracer.WriteToFile("trace.json");
```

The output file uses the Chrome Trace Event format and can be visualized in:

- `chrome://tracing` (paste in Chrome's address bar)
- [Perfetto UI](https://ui.perfetto.dev/)

Without `PROFILE`, tracing calls are omitted from the build and have no runtime cost.

## Custom Math Types

Jitter2 uses `JVector`, `JMatrix`, `JSymmetricMatrix`, `JQuaternion`, and `JBoundingBox`. Their components use `float` or `double`, depending on the library's precision setting.

The `UnsafeAs` and `UnsafeFrom` methods convert between Jitter2 types and other types with the same memory layout:

```cs
// Convert to any layout-compatible type
MyVector3 myVec = jitterVector.UnsafeAs<MyVector3>();

// Convert from any layout-compatible type
JVector jitterVector = JVector.UnsafeFrom(myVec);
```

Both types must have the same size and field layout. For `System.Numerics`, ordinary implicit conversions are also available:

```cs
System.Numerics.Vector3 sysVec = jitterVector;  // implicit conversion
JVector jitterVec = sysVec;                      // implicit conversion
```

Implicit conversions copy the component values. Converting from double to float can lose precision.

### Vector and matrix convention

Jitter2 treats vectors as **column vectors**.
To transform a vector $v$ by a matrix $M$, it calculates $M \cdot v$:

```cs
JVector result = JVector.Transform(v, M);  // computes M * v
```

`System.Numerics` uses **row vectors** and calculates $v \cdot M$.

Transpose matrices when converting between these conventions. In memory, `JMatrix` stores columns together (`M11`, `M21`, `M31`), while `System.Numerics.Matrix4x4` stores rows together.
