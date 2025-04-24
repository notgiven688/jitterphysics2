---
sidebar_position: 5
---

# Changelog

### Jitter 2.6.0 (2025-04-24)

- Add `SampleHull` and `MakeHull` to `ShapeHelper`.
- Fix hill climbing getting stuck for `ConvexHullShape`s.
- Add SIMD support for `PointCloudShape`s.
- Add option to ignore degenerated triangles in `TriangleMesh`.
- Make thickness parameter mandatory in `FatTriangleShape`.
- Add Fisher-Yates shuffle to `DynamicTree.Optimize`.
- Optimized `TriangleEdgeCollisionFilter`.

### Jitter 2.5.9 (2025-04-17)

- Use `CollideEpsilon` 1e-5 in MPREPA.
- Fix a bug in `ShardedDictionary`.

### Jitter 2.5.8 (2025-04-16)

- Fixed `DynamicTree.Optimize` messing up collision pairs.
- Refactored `SoftBody.cs`
- Improved `TriangleEdgeCollisionFilter`.
- Further reduced GC.

### Jitter 2.5.7 (2025-04-06)

- Fixed possible crash when dynamically making bodies static.
- Improved memory footprint and reduced GC.
- Added `Logger` as a replacement for `Trace`.

### Jitter 2.5.6 (2025-03-08)

- Fixed concurrency bug in `world.GetArbiter`.

### Jitter 2.5.5 (2025-03-02)

- Added implicit conversion operators for System.Numerics Vector3 and Quaternion.
- Replaced Trace.WriteLine with Trace.Information, Warning, Error.

### Jitter 2.5.4 (2025-02-08)

- Renamed `JAngle.Radiant` to `JAngle.Radian`.
- Renamed namespace `Jitter2.UnmanagedMemory` to `Jitter2.Unmanaged`.
- Fix `body.AddShape(IEnumerable<RigidBodyShape> shapes)` for one-time-use iterators.
- Smaller improvements in XML-documentation.

### Jitter 2.5.3 (2025-01-12)

- DynamicTree, `Optimize` takes a delegate now.
- Fixed `TriangleShape` ray cast not returning a normalized normal.
- Removed the `CollisionHelper` class.
- Renamed `ActiveList` and `UnmanagedActiveList` to `PartitionedSet` and `PartitionedBuffer`, respectively.
- Various smaller improvements (ToString() overloads, IEquality\<T\> implementations, XML-documentation)

### Jitter 2.5.2 (2025-01-08)

- Added enumeration method to `DynamicTree` and made `PairHashSet` internal.
- **Breaking Change:** Removed `UseFullEPASolver` option.
- Further improved simulation performance under high lock contention scenarios.

### Jitter 2.5.1 (2024-12-31)

- Bugfix in `PairHashSet`.

### Jitter 2.5.0 (2024-12-23)

- Better utilization of multi core systems.
- Bugfix in collision detection (possible NaN values).

### Jitter 2.4.9 (2024-12-18)

- Huge improvements for the `DynamicTree` implementation.

### Jitter 2.4.8 (2024-11-27)

- Add option to build in double precision mode.
- Made `Constraint` constructor public to allow for custom constraints.

### Jitter 2.4.7 (2024-11-18)

- **Breaking Change:** Dropped .NET6 support, added .NET9.
- SIMD for contacts.
- Contact manifold overflow fix.
- Changed default damping.
- Improved auxiliary contact points.
- Minor API changes.

### Jitter 2.4.6 (2024-10-28)

- **Breaking Change:** Jitter world is now constructed using World.Capacity.
- **Breaking Change:** World.RayCast moved to World.DynamicTree.RayCast.
- **Breaking Change:** Renamed NumberSubsteps to SubstepCount.
- Added split impulses. **Breaking Change:** SolverIterations property is now a tuple.
- Several smaller improvements in the API.


### Jitter 2.4.5 (2024-10-07)

- Added new methods to NarrowPhase: Distance and Overlap.
- **Breaking Change:**  Renamed NarrowPhase.SweepTest to NarrowPhase.Sweep.
- **Breaking Change:**  Renamed NarrowPhase.GJKEPA to NarrowPhase.Collision.
- Made PointTest, Raycast and SweepTest to use new SimplexSolver and SimplexSolverAB implementations.
- Fixed normal in GJKEPA for separating case.

### Jitter 2.4.4 (2024-09-14)

- Implemented fixes and workarounds for using Jitter with a debugger attached.

### Jitter 2.4.3 (2024-08-31)

- Correct corner case beeing wrong in MPR collision detection due to typo (bug fix).
- FatTriangleShape level did not properly take transformations into account (bug fix).

### Jitter 2.4.2 (2024-08-26)

- Added FatTriangleShape to give triangles thickness which can be useful for static triangle meshes.
- Removal from potential pairs in DynamicTree ignores filters from now on (bug fix).
- Use sweep tests for speculative contacts, vastly improving simulation quality in this scenario.
- **Breaking Change:** Redefinition of NarrowPhase.SweepTest results.
- Improved TriangleEdgeCollisionFilter.

### Jitter 2.4.1 (2024-08-21)

- Improved TriangleEdgeCollisionFilter.
- Implemented analytical box and sphere ray casting.
- Made Restitution and Friction public in Contact.
- Improved DynamicTree interface.
- Add Debug.Asserts in ActiveList.
- Mark ArbiterKey as readonly.
- Added enumerator to PairHashSet.
- Changed ShapeHelper.MakeHull to take a generic of type ICollection.

### Jitter 2.4.0 (2024-08-10)

- Improved TrimPotentialPairs logic.
- Optimized quaternion vector transformation.
- Extended functionality of ContactData.UsageMask.
- **Breaking Change:** Overhauled the shape system. Regular shapes (box, sphere, capsule, ...) now derive from RigidBodyShape. Some method signatures changed slightly, e.g. ray casting.
- Improved exceptions.
- Added ReferenceFrameAttribute.

### Jitter 2.3.1 (2024-06-02)

- ReadOnly wrappers (ReadOnlyList, ReadOnlyHashset) are now structs.
- Shapes with very small dimensions might have close to zero or zero mass/inertia. Creating rigid bodies from them now throws an exception (use body.AddShape(shape, setMassInertia: false) to not use the shape's mass properties).
- Added BeginCollide and EndCollide events per body.

### Jitter 2.3.0 (2024-05-20)

- Added RigidBody.RemoveShape overload to remove multiple shapes at once.
- Marked Rigid.ClearShapes deprecated.
- **Breaking Change:** Use JQuaternion for orientations. Sorry for the API break.

### Jitter 2.2.1 (2024-04-29)

- Add optional activate parameter to world.AddShape.
- Add NarrowPhase.SweepTest.
- EPA collision detection: various improvements.
- Improve exit condition for RayCast and PointTest.
- Remove redundant ArgumentException for zero mass shapes.
- Handle zero time steps. Throw ArgumentException for negative time steps.
- Add joint base class to joint classes.

### Jitter 2.2.0 (2024-01-02)

- **Breaking Change:** Renamed `Raycast` to `RayCast`.
- `world.Remove(world.NullBody)` does now remove all shapes, constraints and contacts associated with NullBody.
- `world.AddShape(shape)` respects the activation state of the associated rigid body. Most notable: performance improvement when directly adding `TriangleShape`s to world.NullBody for static geometry.
- Performance improvements for ConvexHullShape.
- Improved termination condition in GJKEPA collision detection.

### Jitter 2.1.1 (2023-12-17)

- Fixed O(n^2) problem in `TriangleMesh` due to hash collisions.
- `WorldBoundingBox` of `Shape` is now updated even if no `RigidBody` is attached.

### Jitter 2.1.0 (2023-12-10)

- Added debug drawing for rigid bodies (`RigidBody.DebugDraw`).
- Fixed a bug in `CalculateMassInertia` within `TransformedShape.cs`.
- Improved ray casting performance and introduced `NarrowPhase.PointTest`.
- **Breaking Change:** Inverted behavior of `BroadPhaseCollisionFilter`.
- **Breaking Change:** Inverted definition of damping factors in `RigidBody.Damping` (0 = no damping, 1 = immediate halt).
- Added `RigidBody.SetMassInertia` overload to enable setting the inverse inertia to zero.
- An exception is now thrown when a body's mass is set to zero.
- Fixed a bug in the friction handling in `Contact.cs`.

### Jitter 2.0.1 (2023-10-28)

- Fixed a bug in contact initialization which affected soft body physics.

### Jitter 2.0.0 (2023-10-22)

Initial stable Release.
