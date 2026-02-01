# Aftermath

### `PointCloudShape` vs `ConvexHullShape`

In this example, we used the `PointCloudShape` to simulate a rigid body with a convex hull. As the name suggests, this shape only requires a set of points — they don't need to lie exactly on a convex surface. By design, Jitter treats the convex hull of these points as the actual shape for collision detection.

Although the algorithm used here is brute-force, it can be extremely fast: the data is stored in a linear memory layout, and SIMD instructions are used to accelerate the support function. Because of this, `PointCloudShape` is usually the best choice for quickly and efficiently adding simple convex geometry to your simulation.

The `ConvexHullShape`, on the other hand, is intended for more complex and detailed convex models. Unlike `PointCloudShape`, this shape requires a precomputed convex hull provided as a list of triangles (`List<JTriangle>`). Jitter does not generate this for you — you'll need to use third-party tools like Blender or dedicated convex hull libraries. The input mesh must be strictly convex for collision detection to work correctly.

Internally, `ConvexHullShape` uses a hill-climbing algorithm to compute the support function. While this approach is algorithmically more efficient than brute-force, the performance benefits only become noticeable with larger shapes. As a general rule of thumb, `ConvexHullShape` starts to outperform `PointCloudShape` at around 300 vertices or more.
