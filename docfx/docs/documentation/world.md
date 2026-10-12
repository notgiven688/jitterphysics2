# Jitter World

A `World` contains the bodies, contacts, and constraints in a simulation. Call `world.Step` to advance it.

## World.Step

Advance the simulation by `dt` seconds:

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

Use a fixed time step of 1/60 second or less. Smaller steps generally improve stability but require more work for the same amount of simulated time.

The following example accumulates elapsed time and advances physics in fixed 1/100-second steps. `maxSteps` limits the number of physics steps performed in one call if the application falls behind.

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

`world.Step` uses multiple threads by default. Jitter2's thread count includes the thread calling `Step`, so a count of four means that thread plus three workers. The default count is calculated by:

```cs
public const float ThreadsPerProcessor = 0.9f;
public static int ThreadCountSuggestion => Math.Max((int)(Environment.ProcessorCount * ThreadsPerProcessor), 1);
```

To change the count for the shared thread pool:

```cs
Jitter2.Parallelization.ThreadPool.Instance.ChangeThreadCount(4);
```

This selects four threads in total.

`world.ThreadModel` controls what workers do between steps. The default, `ThreadModelType.Regular`, lets them wait without consuming CPU time. `ThreadModelType.Persistent` keeps them active to reduce the delay before the next step, at the cost of higher CPU usage.

#### Threading Contract

Do not step or modify the same world from multiple threads at once. Also avoid reading its state while another thread is stepping or changing it.

To simulate independent worlds in parallel, run each on a separate application thread and use single-threaded stepping:

```cs
world.Step(dt, multiThread: false);
world.Stabilize(dt, iterations, multiThread: false);
```

Multithreaded calls to `Step` and `Stabilize` share one worker pool. If different worlds make these calls at the same time, one runs while the others wait.

Any callbacks shared by worlds running concurrently must be thread-safe. This includes logging, collision filters, and event handlers. The same applies if your application draws debug geometry from multiple threads.

## Solver Mode

Jitter2 offers three solver strategies through <xref:Jitter2.SolveMode>:

```cs
world.SolveMode = SolveMode.Regular;       // default
world.SolveMode = SolveMode.Deterministic; // reproducible, slower
world.SolveMode = SolveMode.GraphColored;  // SIMD across independent contact manifolds
```

`SolveMode.Regular` favors speed. With multithreading, the order in which contacts and constraints are solved can vary between runs.

`SolveMode.Deterministic` favors repeatable results and can be significantly slower. It supports both single-threaded and multithreaded stepping.

Use deterministic mode for tests, replays, or applications that need the same inputs to produce the same result. The engine version, precision, time steps, inputs, and creation order of interacting bodies must match. See [Deterministic Simulation](general.md#deterministic-simulation) for the full requirements and test coverage.

### Graph-colored contacts

`SolveMode.GraphColored` groups contact manifolds into colors so that no body written by
one manifold is accessed by another manifold in the same color. Static and kinematic
bodies can be shared when their velocities are read-only. Colors run sequentially;
manifolds within a color run without body locks and can be distributed across workers.

Each SIMD lane solves an independent manifold. The manifold's contact points retain
their sequential order, including friction, penetration bias, and accumulated impulses.
Preparation and warm starting use the existing contact code; coefficients are packed
once per substep and reused throughout solving and relaxation. Joints use the regular
solver in separate passes, with a synchronization barrier before the next contact pass.

This mode supports `Step`, `Stabilize`, substeps, float and double precision, and both
values of `multiThread`. SIMD width follows `System.Numerics.Vector<T>` on the current
runtime. Partial bundles and systems without hardware SIMD use the existing contact
iteration. Coloring is rebuilt after `PreSubStep` for each substep, using reusable
storage. At most 64 colors are used; excess conflicts are solved sequentially after
each sweep. `World.Trim()` releases the cached coloring and SIMD storage.

Graph coloring changes constraint order and does not guarantee deterministic results.
Its benefit depends on contact density, SIMD occupancy, iteration count, and worker
synchronization costs. Benchmark representative scenes before selecting it. The
`other/GraphColoredSolverBenchmark` program compares independent floor contacts and
a connected box pile. Coloring and packing costs are included in `World.Timings.Solve`.

## Solver Iterations

Jitter2 solves contacts and constraints through repeated adjustments to body velocities. More iterations generally make contacts and joints more accurate, at a higher cost:

```cs
world.SolverIterations = (solver: 6, relaxation: 4);
```

`solver` sets the number of iterations used to resolve contacts and constraints. These iterations also add motion to separate overlapping bodies and correct joint positions.

`relaxation` sets the number of iterations used afterward to remove unwanted velocity introduced by those position corrections. Solving time grows roughly in proportion to the total number of iterations.

## Substep Count

`world.SubstepCount` divides each simulation step into smaller steps. Each substep updates body motion and solves contacts and constraints. Existing contact points follow the moving bodies, but collision detection is not rerun to find new contacts.

Each substep uses the iteration counts in `world.SolverIterations`. For example:

```cs
world.SubstepCount = 4;
world.SolverIterations = (solver: 2, relaxation: 1);
```

This performs eight solver iterations and four relaxation iterations per call to `world.Step`. Substeps usually cost more than putting the same number of iterations into one step, but can give better results for joints, large stacks, and heavy objects resting on light ones.

## Contact Manifold Persistence

By default, Jitter2 reuses contact points and impulses from the previous step (`world.PersistentContactManifold = true`). This gives the solver a useful starting point and helps resting objects stay stable. The set of contact points between two shapes is called a **contact manifold**.

Set `world.PersistentContactManifold = false` to discard contacts after each step. Rebuilding them from scratch can require more solver iterations to achieve the same stability.

Individual bodies can discard their cached contacts without changing the global setting:

```cs
body.ClearContactCache();  // discard cached manifold for this body
```

This is useful after restoring low-level body data. Setting `body.Position` or `body.Orientation` already clears the contact cache in both solver modes.

## Warm-Start Reset

Constraints reuse impulses from the previous step, a technique called **warm starting**. After loading a saved state or making a sudden change to the bodies, those impulses may no longer be appropriate. Call `ResetWarmStart()` to clear them without removing the constraints:

```cs
foreach (var constraint in body.Constraints)
{
    constraint.ResetWarmStart();
}
```

`World.Stabilize` can then adjust velocities to satisfy the restored contacts and constraints before simulation resumes. It does not advance body positions or orientations, and it uses the selected solver mode.

## Auxiliary Contacts

When flat surfaces touch, one contact point may not be enough to keep them stable. Auxiliary contacts add more points during the same collision test, so the solver can support the touching surfaces without waiting several steps to collect more contacts.

This is enabled by default. Set `world.EnableAuxiliaryContactPoints = false` to disable it.

## Rigid Bodies

All rigid bodies registered with the world can be accessed using

```cs
world.RigidBodies
```

where `RigidBodies` is of type `ReadOnlyPartitionedSet<RigidBody>`.
The bodies are in no particular order and may be reordered during calls to `world.Step`.

## Raw Data

For low-level access to body, contact, and constraint data, use:

```cs
world.RawData
```

This provides spans over `RigidBodyData`, `ContactData`, `ConstraintData`, and `SmallConstraintData`. Most applications should use the properties on bodies, arbiters, and constraints instead.

> [!CAUTION]
> **Raw Memory Access**
> Do not retain spans or data references across calls to `Step` or changes to the world: the underlying storage can move.
> Do not access raw data while another thread is stepping or modifying the world.
> Direct field changes bypass normal updates, such as refreshing collision bounds, and can leave the simulation in an invalid state.

> [!WARNING]
> **Accessing Removed Entities**
> Removing a body or constraint makes its simulation data unavailable. Check `IsValid` before using a reference that may have been removed.
> Do not retain an arbiter after `EndCollide` or explicit removal; it may be reused for a different collision.

## Removing Entities

Rigid bodies, constraints, and shapes can be removed from the world:

```cs
world.Remove(body);        // removes body and its shapes
world.Remove(constraint);  // removes a constraint
world.Clear();             // removes all entities
```

Removing a body also removes its shapes, contacts, and attached constraints. Check `constraint.IsValid` before using a constraint whose body may have been removed. Changing body motion types suspends a constraint while neither body is dynamic; it does not remove the constraint. See [Constraint lifetime](constraints.md#constraint-lifetime).

## NullBody

The world provides a special static body `world.NullBody` that is pinned to the world.
It can be used to create constraints that fix a body relative to world space (see [Constraints](constraints.md)).

## Deactivation

The deactivation system can be globally disabled using `world.AllowDeactivation`.
Setting this to `false` prevents bodies from being deactivated but does not wake up already sleeping bodies.

Jitter2 puts connected groups of bodies, called **islands**, to sleep together. An island can sleep once all of its non-static bodies have stayed below their velocity thresholds for long enough. See [body activation](bodies.md#activationdeactivation) for the thresholds and controls.

Sleeping islands have very little simulation cost. They wake when your code activates a body, or when contact or a constraint connects them to active bodies.

Static bodies do not keep an island awake. Moving static geometry can still wake the bodies affected by the change.
