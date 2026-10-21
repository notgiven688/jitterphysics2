# Aftermath

### `PointCloudShape` vs `ConvexHullShape`

The example uses `PointCloudShape`, which builds a convex collision shape from a set of points. Interior points are allowed, but do not change the shape.

`PointCloudShape` is a good starting point for simple convex geometry. For more detailed models, `ConvexHullShape` can be faster. It requires triangles forming a valid, closed convex hull, which you must prepare with a tool such as Blender or a convex hull library. Jitter does not generate that mesh for you.

As a rough guide, consider `ConvexHullShape` when the geometry has around 300 vertices or more. The crossover depends on the model, so measure your scene if collision performance matters.
