---
sidebar_position: 5
---

# Changelog

### Jitter 2.3.0 (05-20-2024)
- Added RigidBody.RemoveShape overload to remove multiple shapes at once.
- Marked Rigid.ClearShapes deprecated.
- **Breaking Change:** Use JQuaternion for orientations. Sorry for the API break.

### Jitter 2.2.1 (04-29-2024)
- Add optional activate parameter to world.AddShape.
- Add NarrowPhase.SweepTest.
- EPA collision detection: various improvements.
- Improve exit condition for RayCast and PointTest.
- Remove redundant ArgumentException for zero mass shapes.
- Handle zero time steps. Throw ArgumentException for negative time steps.
- Add joint base class to joint classes.

### Jitter 2.2.0 (01-02-2024)
- **Breaking Change:** Renamed `Raycast` to `RayCast`.
- `world.Remove(world.NullBody)` does now remove all shapes, constraints and contacts associated with NullBody.
- `world.AddShape(shape)` respects the activation state of the associated rigid body. Most notable: performance improvement when directly adding `TriangleShape`s to world.NullBody for static geometry.
- Performance improvements for ConvexHullShape.
- Improved termination condition in GJKEPA collision detection.

### Jitter 2.1.1 (12-17-2023)
- Fixed O(n^2) problem in `TriangleMesh` due to hash collisions.
- `WorldBoundingBox` of `Shape` is now updated even if no `RigidBody` is attached.

### Jitter 2.1.0 (12-10-2023)

- Added debug drawing for rigid bodies (`RigidBody.DebugDraw`).
- Fixed a bug in `CalculateMassInertia` within `TransformedShape.cs`.
- Improved ray casting performance and introduced `NarrowPhase.PointTest`.
- **Breaking Change:** Inverted behavior of `BroadPhaseCollisionFilter`.
- **Breaking Change:** Inverted definition of damping factors in `RigidBody.Damping` (0 = no damping, 1 = immediate halt).
- Added `RigidBody.SetMassInertia` overload to enable setting the inverse inertia to zero.
- An exception is now thrown when a body's mass is set to zero.
- Fixed a bug in the friction handling in `Contact.cs`.

### Jitter 2.0.1 (10-28-2023)

- Fixed a bug in contact initialization which affected soft body physics.

### Jitter 2.0.0 (10-22-2023)

Initial stable Release.

### Jitter 2.0.0-beta (10-17-2023)

- Added softbodies.

### Jitter 2.0.0-alpha (09-18-2023)

Initial Release.
