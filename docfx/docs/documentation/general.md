# General

This section covers fundamental design decisions and configuration options.

## Collision Detection Philosophy

Jitter2 takes a unified approach to collision detection that differs from many other physics engines. Unlike engines that implement dedicated algorithms for specific shape pairs (sphere-sphere, box-box, capsule-capsule, etc.), Jitter2 treats all collision detection uniformly using implicit shapes. Every shape is represented through a support function, and collisions are resolved via MPR (Minkowski Portal Refinement), falling back to EPA (Expanding Polytope Algorithm) for deep penetrations. This design simplifies the codebase and makes it straightforward to add custom shapes—any shape that provides a support mapping automatically works with all other shapes.

Traditional physics engines use a three-phase collision pipeline: broad phase (spatial partitioning), mid-phase (hierarchical bounding volumes for complex meshes), and narrow phase (exact shape intersection). Jitter2 eliminates the mid-phase entirely. Instead of building internal acceleration structures for complex geometry, Jitter2 relies on [collision filters](filters.md) to handle large-scale environments. This enables user-defined spatial partitioning strategies—heightmaps, detailed triangle meshes, or even infinite voxel worlds can be implemented by generating collision geometry on-demand within filter callbacks.

## Precision

Jitter2 supports both single-precision (`float`) and double-precision (`double`) floating-point arithmetic.
The precision is selected at compile time.

### Compiling for Double Precision

To build with double precision, either:

- Uncomment `#define USE_DOUBLE_PRECISION` in [Precision.cs](https://github.com/notgiven688/jitterphysics2/blob/main/src/Jitter2/Precision.cs), or
- Use the command line option:

```bash
dotnet build -c Release -p:DoublePrecision=true
```

### Runtime Detection

The active precision mode can be checked at runtime:

```cs
if (Precision.IsDoublePrecision)
{
    Console.WriteLine("Running in double precision mode.");
}
```

### Type Differences

Depending on the precision mode, the math types use different component types:

| Precision | Vector/Matrix components | Example |
|-----------|--------------------------|---------|
| Single    | `float` | `JVector.X` is `float` |
| Double    | `double` | `JVector.X` is `double` |

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

Jitter2 defines its own math types (`JVector`, `JMatrix`, `JQuaternion`, `JBoundingBox`) rather than using `System.Numerics`.

### Full Control

- Precision can be switched globally between `float` and `double` without code changes.
- Memory layout is explicitly controlled using `[StructLayout(LayoutKind.Explicit)]`.
- No dependency on external math library behavior or updates.

### Zero-Cost Interop

The math types are defined with explicit field offsets, guaranteeing a predictable memory layout.
This enables zero-copy conversion to and from other libraries' types using unsafe bit reinterpretation:

```cs
// Convert to any layout-compatible type
MyVector3 myVec = jitterVector.UnsafeAs<MyVector3>();

// Convert from any layout-compatible type
JVector jitterVector = JVector.UnsafeFrom(myVec);
```

The methods perform a compile-time size check and reinterpret the memory directly—no copying, no allocations.

### Safe Conversions

For convenience, implicit conversions to and from `System.Numerics` types are also provided:

```cs
System.Numerics.Vector3 sysVec = jitterVector;  // implicit conversion
JVector jitterVec = sysVec;                      // implicit conversion
```

These conversions involve copying and potential precision loss when converting from double to float.
