# Unmanaged Memory

The constructor of the world class takes three optional arguments, each with quite large default values:

```cs
public World(int numBodies = 32768, int numContacts = 65536, int numConstraints = 32768)
```

These values specify the maximum number of entities that can exist in the world class. This is unusual for a managed library because the user must commit to a certain maximum size of the simulation, and the parameters cannot be changed after calling the constructor.

- **numBodies**: The maximum number of **RigidBodies** in the simulation. RigidBodies possess attributes such as position, velocity, mass, inertia, and also maintain a list of shapes that exist within the collision system of the engine.
  
- **numContacts**: Collisions reported by the collision system are utilized to construct contact manifolds between shapes. A contact manifold contains one to four collision points. The collision points for a pair of shapes are stored in a data structure called "Arbiter". Each pair of shapes can only have one Arbiter. The numContacts parameter refers to the maximum number of **Arbiters**.
  
- **numConstraints**: This parameter indicates the maximum number of **Constraints**. A constraint limits the positions or velocities of a pair of rigid bodies.

**RigidBodies**, **Arbiters**, and **Constraints** are regular C# classes that reside on the managed heap. However, these objects are linked to their unmanaged counterparts: **RigidBodyData**, **ContactData**, and **ConstraintData**. Memory management for these structures is facilitated by Jitter: An array of pointers is allocated (referred to as the index-list in the following), where each entry indicates the actual memory of the data structure. These data structures exist in a contiguous blob of allocated memory. The managed classes maintain pointers to the index-list. Whenever the memory position of a native data structure is altered, the index-list gets updated. *Jitter relocates the native structures in a manner that ensures active objects are housed in contiguous memory, enabling efficient access by the iterative solver*. The size of the index-list is dictated by the corresponding number specified in the constructor. For instance, with the default constructor, the index-list can hold 32768 pointers (256 KB on a 64-bit system). The continuous block of memory that hosts the native structures adapts to the actual number of rigid bodies in the simulation in a grow-only manner. Consequently, only 265 KB are initially earmarked for a maximum of 32768 bodies, scaling up to (256 KB + 32768 x \{size of RigidBodyData\}) of unmanaged memory usage if 32768 bodies are effectively integrated into the simulation.

## Accessing Unmanaged Memory

:::danger Raw Memory Access
Accessing raw memory is generally not required when utilizing the standard functionalities of Jitter. Although reading the raw data of objects is generally safe, modifying data can corrupt the internal state of the engine.
:::

`world.RawData` facilitates access to `Span`s into unmanaged contiguous memory. This data is also reachable via `body.Handle.Data`, `arbiter.Handle.Data`, and `constraint.Handle.Data`.