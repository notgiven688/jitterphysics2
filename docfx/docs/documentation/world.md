# Jitter World

The `World` class contains all entities in the physics simulation and provides the `World.Step` method to advance the simulation by a single time step.

## World.Step

Forward the world by a single time step using

```cs
Step(float dt, bool multiThread = true)
```

### Time step size

> [!NOTE]
> **Units in Jitter2**
> The unit system is not explicitly defined.
> The engine is optimized for objects with a size of 1 [len_unit].
> For example, the collision system uses length thresholds on the order of 1e-04 [len_unit].
> It assumes a unit density of 1 [mass_unit/len_unit³] for mass properties of shapes.
> Consequently, the default mass of a unit cube is 1 [mass_unit].
> The default value for gravity is 9.81 [len_unit/time_unit²], which aligns with the gravitational acceleration on Earth in metric units (m/s²).
> Therefore, it is reasonable to use metric units (kg, m, s) when conceptualizing these values.

The smaller the time step size, the more stable the simulation.
Time steps larger than $\mathrm{dt}=1/60\,\mathrm{s}$ are not advised.
It is also recommended to use fixed time steps.
Typical code accumulates delta times and calls `world.Step` only at fixed time intervals, as shown in the following example.

```cs
private float accumulatedTime = 0.0f;

public void FixedTimeStep(float dt, int maxSteps = 4)
{
    const float fixedStep = 1.0f / 100.0f;

    int steps = 0;
    accumulatedTime += dt;

    while (accumulatedTime >= fixedStep)
    {
        world.Step(fixedStep);
        accumulatedTime -= fixedStep;

        // we cannot keep up with real time, i.e. the simulation
        // is running slower than the real time is passing.
        if (++steps >= maxSteps) return;
    }
}
```

### Multithreading

Jitter2 employs its own thread pool (`Parallelization.ThreadPool`) to distribute tasks across multiple threads.
The thread pool is utilized when `world.Step` is invoked with `multiThread` set to true.
By default, `ThreadPool.ThreadCountSuggestion`$-1$ additional threads are spawned, where the suggestion is calculated by

```cs
public const float ThreadsPerProcessor = 0.9f;
public static int ThreadCountSuggestion => Math.Max((int)(Environment.ProcessorCount * ThreadsPerProcessor), 1);
```

The number of worker threads managed by the thread pool can be adjusted using `ChangeThreadCount(int numThreads)`.
A singleton pattern is used here, as demonstrated below:

```cs
ThreadPool.Instance.ChangeThreadCount(4);
```

This adjusts the number of additional (with respect to the main thread) worker threads to $4-1=3$.

The `world.ThreadModel` property may be used to keep the thread pool in a tight loop waiting for work to be processed after `world.Step` has been run (`ThreadModelType.Persistent`), or to yield threads afterwards (`ThreadModelType.Regular`).
The latter option is recommended to free processing power for other code, such as rendering.

#### Threading Contract

Each `World` instance has a single external owner at a time.
Do not call `Step`, `Stabilize`, `CreateRigidBody`, `CreateConstraint`, `Remove`, `Clear`, or other world-changing APIs on the same world concurrently from multiple external threads.
Also avoid reading world state from one thread while another thread is stepping or modifying that same world.

Multiple worlds may be stepped concurrently from different host threads when every world uses single-threaded stepping:

```cs
world.Step(dt, multiThread: false);
world.Stabilize(dt, iterations, multiThread: false);
```

This is the recommended setup when an application wants to parallelize many independent worlds.

> [!WARNING]
> `world.Step(dt, multiThread: true)` and `world.Stabilize(..., multiThread: true)` use Jitter's process-wide worker pool.
> If several worlds call these methods at the same time, Jitter serializes the calls with a process-wide lock: one multithreaded step runs while the others wait.
> To step many independent worlds in parallel, run those worlds on your own host threads and call `world.Step(dt, multiThread: false)` for each world.

Callbacks and global hooks shared by multiple worlds must be thread-safe.
When separate worlds are stepped concurrently, Jitter may invoke shared callbacks from different external threads at the same time.
This includes `Logger.Listener`, broad-phase and narrow-phase filters, collision callbacks, step callbacks, and debug drawers.

## Solver Mode

Jitter2 offers two solver strategies through <xref:Jitter2.SolveMode>:

```cs
world.SolveMode = SolveMode.Regular;       // default
world.SolveMode = SolveMode.Deterministic; // reproducible, slower
```

`SolveMode.Regular` is the default and optimized for throughput. It keeps the solver fast, but the exact order in which contacts and constraints are processed may vary.

`SolveMode.Deterministic` is the best-effort cross-platform deterministic path. It processes each simulation island in a stable order and uses deterministic tie-breakers for contacts and constraints. Islands can still be distributed across threads, so `world.Step(dt, multiThread: true)` remains valid in deterministic mode.

Use deterministic mode when repeatability matters more than raw performance, for example for tests, replays, debugging, or deterministic gameplay simulation. It can be significantly slower than `SolveMode.Regular`, so it should generally be enabled deliberately rather than used as the default for all worlds.

By contrast, `SolveMode.Regular` with `multiThread: false` can still look reproducible when the world is built in the exact same way inside the same .NET process, but that is a much weaker property and should not be confused with cross-platform determinism.

For deterministic mode, it is not necessary that the entire world was built through the same history. What must match is the setup order inside each interacting island: the participating bodies, shapes, and constraints must be added in the same order. Precision still matters: a float build and a double build are both deterministic, but they will not produce the same bit pattern. See [General](general.md#deterministic-simulation) for a summary of what each solver configuration guarantees and the current CI coverage behind that claim.

## Solver Iterations

Jitter2 employs an iterative solver to solve contacts and constraints.
The number of iterations can be raised to improve simulation quality (`world.SolverIterations`).

```cs
world.SolverIterations = (solver: 6, relaxation: 4);
```

Jitter2 solves physical contacts (and constraints) on the velocity level ('solver iterations').
Jitter2 also adds velocities to rigid bodies to resolve unphysical interpenetrations of bodies.
These additional velocities add unwanted energy to the system which can be removed by an additional relaxation phase after integrating the new positions from these velocities.
The number of iterations in the relaxation phase ('relaxation iterations') is specified here as well.
The runtime for solving contacts and constraints scales linearly with the number of iterations.

## Substep Count

The time step can be divided into smaller steps, defined by `world.SubstepCount`.
These smaller time steps are solved similarly to regular full steps; however, collision information is not updated.
Each substep is solved with the number of solver iterations specified in `world.SolverIterations`.
For example

```cs
world.SubstepCount = 4;
world.SolverIterations = (solver: 2, relaxation: 1);
```

performs $12$ solver iterations in total for each call to `world.Step`.
The runtime is slower than a single regular step with $12$ iterations but this approach enhances the stability of the simulation.
Substepping is excellent for enhancing the overall quality of constraints, stabilizing large stacks of objects, and simulating large mass ratios (like heavy objects resting on light objects) with greater accuracy.

## Contact Manifold Persistence

By default, Jitter2 caches contact points and their accumulated impulses between frames (`world.PersistentContactManifold = true`). This allows the solver to warm-start from the previous solution and lets the manifold grow over several steps, which improves stability for resting contacts.

Setting `world.PersistentContactManifold = false` discards all contact data at the end of each frame, so every contact is treated as brand-new. This removes frame-to-frame contact memory at the cost of solver convergence speed.

Individual bodies can discard their cached contacts without changing the global setting:

```cs
body.ClearContactCache();  // discard cached manifold for this body
```

This is useful after discontinuous user-driven transforms such as teleports. When `SolveMode.Deterministic` is active, setting a body's `Position` or `Orientation` property automatically calls `ClearContactCache()`.

## Warm-Start Reset

The iterative solver warm-starts each frame from the accumulated impulses of the previous frame. After restoring a snapshot or any other discontinuous state change, this cached state can be stale. Every constraint exposes a `ResetWarmStart()` method that clears its accumulated impulses without removing the constraint:

```cs
foreach (var constraint in body.Constraints)
{
    constraint.ResetWarmStart();
}
```

`World.Stabilize` can then be called to re-solve the restored contacts and constraints before resuming normal simulation. `Stabilize` respects `SolveMode.Deterministic` when it is set.

## Auxiliary Contacts

Jitter2 employs a technique termed 'auxiliary contacts', where additional contacts are generated for the general case where two flat surfaces of shapes are in contact.
These additional contacts are calculated within one frame, generating the full contact manifold in a 'single pass' and preventing jitter commonly encountered with incrementally constructed collision manifolds.
The `world.EnableAuxiliaryContactPoints` property can be used to enable or disable the usage of auxiliary contact point generation.

## Rigid Bodies

All rigid bodies registered with the world can be accessed using

```cs
world.RigidBodies
```

where `RigidBodies` is of type `ReadOnlyPartitionedSet<RigidBody>`.
The bodies are in no particular order and may be reordered during calls to `world.Step`.

## Raw Data

`RigidBody`s, `Arbiter`s, and `(Small)Constraint`s are regular C# classes that reside on the managed heap.
However, these objects are linked to their unmanaged counterparts: `RigidBodyData`, `ContactData`, and `(Small)ConstraintData` which can be accessed using:

```cs
world.RawData
```

Jitter2 relocates native structures so that active objects are stored in contiguous memory, enabling efficient access by the iterative solver.

> [!CAUTION]
> **Raw Memory Access**
> Accessing raw memory is generally not required when utilizing the standard functionalities of Jitter2.
> Although reading the raw data of objects is generally safe, modifying data can corrupt the internal state of the engine.

> [!WARNING]
> **Accessing Removed Entities**
> Instances of `RigidBody`, `Arbiter`, and `Constraint` store some of their data in unmanaged memory, which is automatically freed once the entities are removed (`world.Remove`) from the world.
> Before accessing a retained `RigidBody` or `Constraint` reference, check its `IsValid` property.
> If `IsValid` is `false`, its unmanaged data is no longer available and data-backed properties and methods must not be accessed.
> Arbiter references must not be used after their collision event lifetime ends.

## Removing Entities

Rigid bodies, constraints, and shapes can be removed from the world:

```cs
world.Remove(body);        // removes body and its shapes
world.Remove(constraint);  // removes a constraint
world.Clear();             // removes all entities
```

Removing a rigid body also removes its shapes, contacts, and attached constraints. Consequently,
a constraint can become invalid even when `world.Remove(constraint)` was not called directly.
Changing body motion types can also remove a constraint when neither connected body remains dynamic.
Use `constraint.IsValid` when retaining constraint references across such operations. See
[Constraint lifetime](constraints.md#constraint-lifetime) for details.

## NullBody

The world provides a special static body `world.NullBody` that is pinned to the world.
It can be used to create constraints that fix a body relative to world space (see [Constraints](constraints.md)).

## Deactivation

The deactivation system can be globally disabled using `world.AllowDeactivation`.
Setting this to `false` prevents bodies from being deactivated but does not wake up already sleeping bodies.

Jitter2 deactivates complete simulation islands, not individual bodies in isolation. An island remains
active while at least one non-static body in it is moving, has been explicitly woken, or is connected
to newly active contact or constraint data. Once every non-static body in the island remains below
its deactivation thresholds for long enough, the island is moved to the inactive partition.

Inactive islands have very little runtime cost: their bodies, contacts, constraints, and broad-phase
proxies are skipped by the active simulation. They can be activated again when user code wakes a body
or when an active body creates a contact with a sleeping island. If an active island and an inactive
island become connected through a contact or constraint, Jitter2 marks the merged island for an update
and moves its non-static bodies back into the active partition before solving.

Static bodies are a special case. They are normally inactive, do not form regular simulation island
connections, and do not by themselves keep an island awake. Moving a static body can still wake
affected non-static bodies so contacts can be updated on the next step.
