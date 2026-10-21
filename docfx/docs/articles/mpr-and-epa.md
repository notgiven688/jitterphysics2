# MPR and EPA for Penetration Depth

A physics engine usually asks for contact information when two shapes are just touching or overlap by a small amount. Occasionally, however, it receives a pair that overlaps deeply: a body may have been teleported, a large timestep may have missed the first contact, or a stack may have compressed. The same collision query has to handle both cases.

Minkowski Portal Refinement (MPR) and the Expanding Polytope Algorithm (EPA) fit together particularly well here. MPR finds a contact through a small, stable local search. EPA can check whether a different direction would separate the shapes with less movement, and continue searching if needed. The trick is to run EPA only when MPR's reported depth gives us reason to doubt the local answer.

This article assumes familiarity with Minkowski differences and support maps. Gary Snethen introduced MPR in [XenoCollide](https://github.com/notgiven688/xenocollide).

## Follow a portal to the surface

Let $M=A-B$ be the Minkowski difference. Overlapping shapes put the origin inside $M$. The penetration depth is the shortest distance from the origin to its boundary; the direction to that nearest boundary gives a separating normal.

MPR starts with a point $v_0$ in the interior of $M$ and keeps it fixed. It obtains three more points on the boundary from support queries. Together, the four points form a tetrahedron. The face opposite $v_0$ is the **portal**. A ray from $v_0$ through the origin passes through this face. MPR repeatedly asks the support map for a point beyond the portal, then replaces one portal vertex. This searches the surface near the ray without constructing a mesh of the whole boundary.

The final portal gives a contact normal, an estimated penetration depth, and contact points.

## Where a local answer goes wrong

Deep inside a long, thin Minkowski difference, the portal can settle on the wrong side. The figure shows a two-dimensional section, where the tetrahedron becomes a triangle. The origin lies inside the triangle made from an interior point $v_0$ and the two vertices on the right. Its right edge is a valid portal, with a normal pointing right. But the *top boundary of the Minkowski difference* is much closer to the origin.

<figure class="mpr-figure">
  <img src="images/mpr-local-portal.svg" width="760" height="440" alt="A long thin Minkowski difference contains the origin. A triangle from an interior point to the two right vertices also contains it. The selected right portal has depth 2 and a rightward normal, while the nearest top boundary has depth 0.3 and an upward normal.">
  <figcaption>The right edge is the portal selected by this local search. The top edge of the Minkowski difference is the global answer. The slanted sides of the triangle are interior chords, not candidate collision boundaries.</figcaption>
</figure>

In this example, the MPR direction gives depth $2$, while the minimum depth is $0.3$. MPR found a way out, just not the shortest way out. Notice that the reported depth is an **upper bound** on the true minimum. If $h_M(n)$ is the support plane distance of $M$ in unit direction $n$, then

$$
d_* = \min_{\lVert n\rVert=1} h_M(n)
\quad\text{and}\quad
d_* \le d_{\mathrm{MPR}} = h_M(n_{\mathrm{MPR}}).
$$

The upper bound gives us a simple rule for deciding when to run EPA. If MPR reports a depth below a chosen tolerance, the true depth is no larger than that tolerance either. A larger reported depth leaves open the possibility that another direction is much shallower, as in the figure. The tolerance controls how much depth error we are willing to accept from the local answer. It does **not** certify that the MPR normal is globally correct: scaling the entire thin-bar example down can make its wrong rightward answer arbitrarily small.

## Give the portal to EPA

EPA takes a convex polytope containing the origin and expands its closest face using support queries. When no support point significantly extends that face, it has found the nearest boundary within its numerical tolerance. In the thin-bar example, it does not have to accept the right portal: it can expand the triangle past its interior chords until the top boundary becomes the closest edge.

EPA needs a starting polytope that encloses the origin. GJK can detect overlap, but its final set of points may be flat or nearly degenerate when the shapes barely touch. Extra work is then needed to turn those points into a tetrahedron that encloses the origin. [Van den Bergen's original EPA description](https://graphics.stanford.edu/courses/cs468-01-fall/Papers/van-den-bergen.pdf) discusses these cases.

MPR has already built a suitable tetrahedron: its fixed interior point and three portal vertices enclose the origin. If the MPR depth exceeds the chosen threshold, those four vertices can be passed straight to EPA. EPA then examines the closest face of that tetrahedron. If the existing portal was already the global answer, its support test can confirm that quickly. Otherwise, EPA grows the hull toward a closer boundary. The cost of that broader search is paid only for contacts beyond the threshold.

## Interactive example

Drag the circle across the fixed bar. Green shows the penetration normal when MPR finds a minimum-depth exit. When it finds a longer way out, its normal turns red and EPA's result appears in green. The readout gives both depths and normals; the arrows have a fixed length so their directions remain visible. Use **Near contact** and **Wrong portal** to jump between the two cases.

The lower view shows the Minkowski sum of the bar and a disk, shifted opposite the circle's motion. The origin stays fixed while the shape moves. MPR's final triangle is drawn inside it, with the portal edge highlighted in green or red. The green line reaches EPA's nearest boundary point. When the results differ, a red dashed line shows MPR's depth to its support plane; the small tick at its end is not necessarily on the boundary.

The demo runs EPA at every overlapping position so that the answers can be compared. The cutoff described above would skip EPA for sufficiently small MPR depths.

<iframe class="mpr-demo" src="mpr-epa-demo.html" width="760" height="900" loading="lazy" title="Interactive MPR and EPA comparison for a draggable circle, fixed bar, and Minkowski sum"></iframe>

This lets a physics engine use MPR for shallow contacts and pay for EPA's broader search when the reported depth is large enough to matter.

For a concrete implementation of this MPR–EPA handoff, see [NarrowPhase.cs](https://github.com/notgiven688/jitterphysics2/blob/main/src/Jitter2/Collision/NarrowPhase/NarrowPhase.cs).

*Disclosure: AI helped write this article. The observations and the idea of using a depth threshold to hand MPR's portal to EPA came from the author.*

<style>
.mpr-figure { margin: 1.5rem 0; }
.mpr-figure img { display: block; width: 100%; height: auto; border-radius: 0.75rem; }
.mpr-figure figcaption { margin-top: 0.75rem; font-size: 0.9rem; line-height: 1.6; color: var(--bs-secondary-color); }
.mpr-demo { display: block; width: 100%; max-width: 760px; border: 0; margin: 1.5rem 0; }
</style>
