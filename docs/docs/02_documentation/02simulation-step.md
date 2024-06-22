# Simulation Step

Call `world.Step(float dt, bool multiThread = true)` to simulate a time step of length dt. Every time `world.Step` is called, the following phases are performed in the engine:

1. All collisions are detected.
2. Forces are integrated to update the velocities.
3. Contacts and Constraints are solved in an iterative solver.
4. Velocities are integrated to update the positions.

:::info Units in Jitter
The unit system of Jitter is not explicitly defined. The engine utilizes 32-bit floating-point arithmetic and is optimized for objects with a size of 1 [len_unit]. For instance, the collision system uses length thresholds on the order of 1e-04 [len_unit]. It assumes a unit density of 1 [mass_unit/len_unit³] for the mass properties of shapes. Consequently, the default mass of a unit cube is 1 [mass_unit]. The default value for gravity in Jitter is $9.81$ [len_unit/time_unit²], which aligns with the gravitational acceleration on Earth in metric units (m/s²). Therefore, it is reasonable to use metric units (kg, m, s) when conceptualizing these values.
:::info

### Step Size

To achieve accurate physical results, the time step (dt) should be as small as possible. However, to maintain interactive frame rates, it's not feasible to call `world.Step` too frequently. A suitable compromise, aligning with the engine's design, is to choose dt=1/100. The time step should not exceed 1/60 to prevent potential simulation instability.

:::info Time Step Size
Maintain a consistent time step size, if possible, to avoid instabilities introduced by fluctuating time steps.
:::info

Suppose your game using Jitter operates at a varying frame rate of 80-160 fps. In this case, calling `world.Step(1.0f / 100.0f)` each frame might cause the simulated time to desynchronize from real time. You can implement a strategy to accumulate delta times (the discrepancy between the elapsed real time and the simulation step) and accordingly adjust the calls to `world.Step(1.0f / 100.0f)` based on the magnitude of the delta time.

### Multithreading

Jitter employs its own thread pool (`Parallelization.ThreadPool`) to distribute tasks across multiple threads, potentially processed by multiple cores. You can modify the number of worker threads managed by the thread pool using `threadPool.ChangeThreadCount(int numThreads)`. A singleton pattern is used here, as demonstrated below:

```cs
ThreadPool.Instance.ChangeThreadCount(4);
```

This adjusts the number of worker threads to four. Jitter utilizes the thread pool when the `world.Step` method is invoked with the multiThread argument set to true. Aside from this, the engine manages multithreading internally. Except when injecting derived types of Jitter into the engine (like using custom constraints or registering Broad- and NarrowPhase interfaces), the program flow operates on a single main thread as usual.

:::info Thread Safety
Methods in Jitter are generally not thread-safe.
:::info

### Substepping

Improving the simulation quality is feasible by employing smaller time steps. Substepping involves conducting collision detection once per step and executing several smaller time steps for the remainder of the simulation step, with reduced solver iterations in each step. The process is as follows:

1. All collisions are detected.
2. Repeat the next steps $n$ times with a step size of dt / $n$, where $n$ is the number of substeps:
   	1. Forces are integrated to update the velocities.
   	2. Contacts and Constraints are solved in an iterative solver.
   	3. Velocities are integrated to update the positions.

Utilizing smaller substeps enhances the simulation quality disproportionately. However, it also introduces a significant overhead due to the alternating between contact solving and force- and velocity-integration. Substepping is excellent for enhancing the overall quality of constraints, stabilizing large stacks of objects, and simulating large mass ratios (like heavy objects resting on light objects) with greater accuracy.

To adjust the number of substeps, use `world.NumberSubsteps`. Setting it to one equates to conducting a regular step.
