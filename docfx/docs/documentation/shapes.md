# Shapes

Shapes define how the rigid body collides with other objects.
Shapes implement the `ISupportMappable` interface and are always convex.
They can be passed to static methods defined in the `NarrowPhase` class for collision detection.
Shapes also implement the `IDynamicTreeEntry` interface and can be added to the `DynamicTree` class.
When a shape is added to a rigid body this is done automatically (`world.DynamicTree`).

> [!NOTE]
> **Concave Shapes**
> A concave shape can be represented by combining multiple convex shapes on a single rigid body.
> Third-party libraries for 'convex decomposition' can be used to generate convex hulls from arbitrary meshes.

## Default types

The inheritance hierarchy for the default shapes:

```text
Shape
├── RigidBodyShape
│   ├── BoxShape
│   ├── CapsuleShape
│   ├── ConeShape
│   ├── ConvexHullShape
│   ├── CylinderShape
│   ├── PointCloudShape
│   ├── TransformedShape
│   └── TriangleShape
└── SoftBodyShape
    ├── SoftBodyTetrahedron
    └── SoftBodyTriangle
```

Most shapes are self-explanatory (additional details are in the API documentation), while some specifics are outlined below.

### ConvexHullShape

The constructor of the `ConvexHullShape` takes a list of triangles.

```cs
public ConvexHullShape(List<JTriangle> triangles)
```

The triangles provided *must* form a convex hull.
The validity of the convex shape is not checked.
Invalid shapes can lead to glitched
collisions and/or non-terminating algorithms during collision detection.
The triangles are used to construct an internal acceleration structure that speeds up collision detection for this shape through hill-climbing.

The `convexHullShape.Clone()` method can be used to clone the shape:
The internal data structure is then used for both shapes.

### PointCloudShape

The `PointCloudShape` is very similar to the `ConvexHullShape`.
The constructor takes a list of vertices.

```cs
public PointCloudShape(List<JVector> vertices)
```

The vertices do not need to form a convex hull; however, collision detection will 'shrink-wrap' these vertices, so the final collision shape is convex.
For example, passing the 8 vertices of a cube to the constructor generates a cube shape; adding a 9th vertex at the cube's center has no effect.

> [!WARNING]
> **Number of vertices**
> `PointCloudShape`s should only be used for a small to moderate number of vertices ($\approx{}300$). Larger numbers of vertices can negatively impact performance.
> `ConvexHullShape`s are the better choice for more complex hulls.

### TransformedShape

The `TransformedShape` takes another shape as input and transforms it.

```cs
public TransformedShape(RigidBodyShape shape, in JVector translation, in JMatrix transform)
```

Any affine transformation is possible.
The wrapped shape might be translated, rotated, scaled and sheared.
For example, a sphere shape could be transformed into an ellipsoid.

### TriangleShape

The `TriangleShape` has no volume.
It is mostly used for static geometry, although it can be added to non-static bodies.
The `TriangleShape` is constructed with a `TriangleMesh` and an index.

```cs
public TriangleShape(TriangleMesh mesh, int index)
```

A `TriangleMesh.Triangle` stores information about neighbour triangles.
This information is used in the `TriangleEdgeCollisionFilter` (enabled by default) to resolve collision artifacts that occur when shapes slide over the edges between connected triangles.
These edges are often referred to as 'internal edges' and can cause major problems when adding level geometry to a game.

### SoftBodyShape

The vertices of the `SoftBodyShape` are represented by rigid bodies.
The shapes (triangle and tetrahedron) are dynamically defined by the position of the vertices.
A `SoftBodyShape` is not added to a body.

## Custom shapes

Custom shapes can easily be implemented.
A shape is defined by its support function—which can be looked up or derived.

The following example demonstrates implementing a half-sphere (symmetry axis aligned with the y-axis) with a radius of one:

```cs
public class HalfSphereShape : RigidBodyShape
{
    public override void SupportMap(in JVector direction, out JVector result)
    {
        const float centerOfMassOffset = 3.0f / 8.0f;

        if (direction.Y >= 0.0f)
        {
            result = JVector.Normalize(direction);
        }
        else
        {
            JVector pDir = new JVector(direction.X, 0.0f, direction.Z);
            float pDirSq = pDir.LengthSquared();

            if (pDirSq < 1e-12f) result = JVector.Zero;
            else result = (1.0f / MathF.Sqrt(pDirSq)) * pDir;
        }

        // shift, such that (0, 0, 0) is the center of mass
        result.Y -= centerOfMassOffset;
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }
}
```

Mass properties and bounding boxes are automatically calculated from the support map using methods in the `ShapeHelper` class.
Performance can be optimized by providing overrides directly in the shape class:

```cs
public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)

public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)

public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda)
```

The `ShapeHelper` class can also be used to generate a triangle mesh representation of the shape (or any class implementing `ISupportMappable`):

```cs
public static List<JTriangle> Tessellate(ISupportMappable support, int subdivisions = 3)
```
