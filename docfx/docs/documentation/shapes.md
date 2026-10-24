# Shapes

Shapes define the convex geometry used for collisions. Attach them to a rigid body with `body.AddShape`; the body supplies their position and rotation.

Each shape implements `ISupportMappable`, so it can also be used directly in `NarrowPhase` collision queries. Shapes implement `IDynamicTreeProxy` as well, allowing the dynamic tree to track them. Adding a shape to a body registers it in `world.DynamicTree` automatically.

> [!NOTE]
> **Concave Shapes**
> A concave shape can be represented by combining multiple convex shapes on a single rigid body.
> Third-party libraries for 'convex decomposition' can be used to generate convex hulls from arbitrary meshes.

## Changing an attached shape

After changing an attached shape's dimensions or local transform, update its collision bounds, cached contacts, and, if needed, the body's mass properties. Do this between calls to `world.Step`:

```cs
box.Size = new JVector(2, 1, 1);
world.DynamicTree.Update(box); // Refresh this shape's bounding box and broadphase pairs.
body.ClearContactCache();      // Discard contact points based on the old geometry.
body.SetMassInertia();          // Only if the body uses mass properties derived from its shapes.
```

`world.DynamicTree.Update(shape)` refreshes that shape immediately, even if its body is asleep. A regular world step only updates active objects. Wake a changed dynamic body with `body.SetActivationState(true)`. If changing static geometry creates new overlaps with sleeping bodies, wake those bodies yourself.

Resizing a shape can leave it overlapping other bodies. Decide whether to reposition those bodies or let the solver separate them. If you set mass or inertia manually, decide whether to keep or recalculate those values; `SetMassInertia()` without arguments replaces them with values derived from the shapes.

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

The API reference describes each shape's dimensions and options. The following types need a little more setup.

### ConvexHullShape

The constructor of the `ConvexHullShape` takes a list of triangles.

```cs
public ConvexHullShape(List<JTriangle> triangles)
```

The triangles *must* form a valid convex hull. Jitter2 rejects zero-mass hulls but does not fully validate the mesh. Invalid input can cause incorrect collisions or collision tests that fail to finish.

Use `convexHullShape.Clone()` when several bodies need the same geometry. Each clone is a separate shape instance that shares the hull data.

### PointCloudShape

The `PointCloudShape` is very similar to the `ConvexHullShape`.
The constructor takes a list of vertices.

```cs
public PointCloudShape(List<JVector> vertices)
```

Jitter2 uses the convex hull of the supplied points: imagine stretching a tight skin around them. Points inside that hull do not change the collision shape. For example, the eight corners of a cube define a cube; adding a point at its center changes nothing.

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

The mesh records which triangles share edges. `TriangleEdgeCollisionFilter`, enabled by default, uses this information to reduce snagging when bodies slide across those edges.

### SoftBodyShape

A soft-body triangle or tetrahedron uses a separate rigid body for each vertex. Its geometry changes as those bodies move. Add it to a `SoftBody`, rather than attaching it to one rigid body.

## Debug drawing

Shapes can be drawn individually:

```cs
var shape = new SphereShape(radius: 1);
shape.DebugDraw(drawer);
```

Attached shapes follow their body's position and rotation. The sphere above appears at the world
origin until it is attached to a body. See [debugging shapes on a body](bodies.md#debugging-shapes)
for setting up the drawer and adjusting color and smoothness.

Your drawer's `DrawTriangle` implementation chooses filled triangles or wireframes.
See <xref:Jitter2.IDebugDrawer> for the drawing methods.

> [!WARNING]
> Debug drawing can be slow. Point clouds and custom shapes may appear with gaps or overlapping
> faces. Higher detail settings increase the cost and do not guarantee a more accurate drawing.

## Custom shapes

To define a custom convex shape, implement its **support function**: given a direction, return the point furthest in that direction. `GetCenter` must return a point inside the shape. All coordinates are local to the shape.

This example defines a half-sphere with radius 1, aligned with the Y axis:

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

The default implementation calculates bounding boxes and approximates mass properties from the support function. Override these methods if you can provide faster or more accurate calculations, or a custom ray test:

```cs
public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)

public override void CalculateMassInertia(out JSymmetricMatrix inertia, out JVector com, out float mass)

public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda)
```

The `ShapeHelper` class can also be used to generate a triangle mesh representation of the shape (or any class implementing `ISupportMappable`):

```cs
public static List<JTriangle> Tessellate(ISupportMappable support, int subdivisions = 3)
```
