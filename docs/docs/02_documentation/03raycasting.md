# Ray Casting

Retrieve information about where a ray, defined by an origin and a direction, hits a target. Generally, the actual hit point can be calculated using the fraction value returned by the appropriate functions using the formula:

$$
\textbf{hit} = \textbf{origin} + \textrm{fraction} \times \textbf{direction}, \quad \textrm{with fraction} \in [0,\infty).
$$

The direction given to Jitter does not need to be normalized. The normal returned by Jitter is the normalized surface normal of the collision target at the hit point.

## Generic Ray Cast for ISupportMap

To cast a ray against any object implementing `ISupportMap`, use `NarrowPhase.RayCast`:

```cs
public static bool RayCast(ISupportMap support, 
                           ref JMatrix orientation, ref JVector position,
                           ref JVector origin, ref JVector direction, 
                           out float fraction, out JVector normal)
```

## Ray Cast Against Shapes

The world class offers two ray cast methods:

```cs
public bool RayCast(Shape shape, JVector origin, JVector direction,
                    out JVector normal, out float fraction)
```
```cs
public bool RayCast(JVector origin, JVector direction, 
                    RayCastFilterPre? pre, RayCastFilterPost? post,
                    out Shape? shape, out JVector normal, out float fraction)
```

The first method can be used to cast a ray against a single shape. The second method tests a ray against the world. Use the ray cast filter to get information about possible candidates or to avoid collision with specific shapes entirely. For example, a bullet may always be able to penetrate a wall; the pre-filter can be used to discard any collision with the wall. For a wall with non-uniform thickness, the bullet may only be able to penetrate the thinner part. In this case, the post-filter can be used to get information on where the ray hit the wall and, based on that information, decide whether to drop the collision.
