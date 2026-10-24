# Debug Drawer

Jitter does not open a window or depend on a rendering library. Instead, objects that implement `IDebugDrawable` send geometry to an `IDebugDrawer` that you provide.

For this example, the flow is:

```text
RigidBody.DebugDraw / HingeJoint.DebugDraw
                  |
                  v
       IDebugDrawer callbacks
       DrawSegment / DrawTriangle
                  |
                  v
          Raylib drawing calls
```

## Implement the two callbacks

Our drawer renders segments as lines and triangles as filled surfaces. We draw the solid boxes first, then draw the hinge guides on top so the connection stays visible through the boxes.

[!code-csharp[](snippets/Program.cs#L90-L125)]

There are three details to notice:

- **Positions are already in world space.** Convert `JVector` to `Vector3` directly. Do not apply the body's position or orientation a second time.
- **Colors are packed as `0xRRGGBBAA`.** The four shifts extract red, green, blue, and alpha explicitly, without depending on the machine's byte order.
- **Both methods are required.** Bodies emit triangles for their shape surfaces. Constraints emit segments and can also emit triangles for anchor markers and solid arrows. An empty `DrawTriangle` method would hide the boxes.

`ShadeTriangles` adds a small amount of directional shading from each triangle's normal. This distinguishes the box faces without a shader or a separate mesh. It affects only the displayed color. We enable it for the bodies and leave it off for the joint's markers:

```cs
var bodyDrawer = new RaylibDebugDrawer { ShadeTriangles = true };
var jointDrawer = new RaylibDebugDrawer();
```

Jitter emits counterclockwise front faces. Raylib uses their winding for back-face culling. The normal is calculated from that same vertex order.

## Choose colors and guide sizes

[!code-csharp[](snippets/Program.cs#L28-L38)]

`CurrentColor` colors each body's shape geometry. For the joint, `ReferenceColor` and `CurrentColor` distinguish its reference and current guides. The boxes are warm yellow and coral against the same blue background used in the other tutorials. The joint guides use green and gold, drawn on top of the filled boxes.

`GuideSize` controls the angular guide radius in world units. `PointRadius` controls anchor markers separately. `SolidArrows = false` selects wire arrows. These settings affect only drawing: changing them does not change the hinge, mass, or collision shapes.

## Draw inside the camera's 3D pass

[!code-csharp[](snippets/Program.cs#L69-L79)]

The bodies draw their attached shapes in their current poses. The joint draws both of its constraints and automatically places the angular guide at the ball socket's anchor.

The boxes use normal depth testing, so their visible faces cover surfaces behind them. For the joint pass, `Rlgl.DisableDepthTest()` lets the guides appear over the filled boxes, even where the guides are physically inside or behind them. This is a visualization overlay; it does not change collisions or the constraint.

Raylib batches drawing commands. `Rlgl.DrawRenderBatchActive()` flushes the boxes before we disable depth testing, then flushes the guides before we restore it. Without those flushes, queued geometry could be drawn with the wrong depth-test state. Restore depth testing after the overlay so later 3D drawing behaves normally.

The lines from the body centers to the anchor come from `BallSocket`. Its two anchor markers should stay close together. The circular guide and axis arrow come from `HingeAngle`. With a full angular range there are no stop markers; try the limit from the previous page to see them.

Calling `DebugDraw` only draws the current state. `world.Step` advances the simulation.

Next, [Hello World](hello-world.md) brings these pieces together into a complete program and shows the hinge in motion.

For more options, see [Constraints](../../documentation/constraints.md), <xref:Jitter2.IDebugDrawer>, and <xref:Jitter2.DebugDrawSettings>.
