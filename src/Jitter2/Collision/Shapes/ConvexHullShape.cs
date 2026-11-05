/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.DataStructures;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a convex hull shape defined by a set of triangles.
/// </summary>
public class ConvexHullShape : RigidBodyShape, ICloneableShape<ConvexHullShape>
{
    private struct CHullVector(in JVector vertex) : IEquatable<CHullVector>
    {
        public readonly JVector Vertex = vertex;
        // The flattened neighbor list may exceed ushort.MaxValue before the vertex list does.
        public int NeighborMinIndex = 0;
        public int NeighborMaxIndex = 0;

        public readonly override bool Equals(object? obj)
        {
            return obj is CHullVector other && Equals(other);
        }

        public readonly override int GetHashCode()
        {
            return Vertex.GetHashCode();
        }

        public readonly bool Equals(CHullVector other)
        {
            return Vertex.Equals(other.Vertex);
        }
    }

    private readonly struct CHullTriangle(ushort a, ushort b, ushort c)
    {
        public readonly ushort IndexA = a;
        public readonly ushort IndexB = b;
        public readonly ushort IndexC = c;
    }

    private JBoundingBox cachedBoundingBox;
    private JSymmetricMatrix cachedInertia;
    private Real cachedMass;
    private JVector cachedCenter;

    private CHullVector[] vertices = null!;
    private CHullTriangle[] indices = null!;
    private ushort[] neighborList = null!;
    // The vertices farthest along +X, -X, +Y, -Y, +Z and -Z; see CalcExtremesAndInitBox.
    private ushort[] extremes = null!;

    private JVector shifted;

    /// <inheritdoc/>
    protected internal override void DebugDrawLocal<TSink>(ref TSink triangleSink, in DebugDrawSettings settings)
    {
        foreach (var triangle in indices)
        {
            JVector a = vertices[triangle.IndexA].Vertex + shifted;
            JVector b = vertices[triangle.IndexB].Vertex + shifted;
            JVector c = vertices[triangle.IndexC].Vertex + shifted;
            JVector normal = (b - a) % (c - a);
            if (normal.LengthSquared() == 0) continue;

            // Input faces may use either winding. The center of mass lies inside the convex hull.
            if (JVector.Dot(normal, a - cachedCenter) < 0) (b, c) = (c, b);
            triangleSink.Add(new JTriangle(a, b, c));
        }
    }

    /// <summary>
    /// Initializes a new instance of the ConvexHullShape class, creating a convex hull.
    /// </summary>
    /// <param name="triangles">Triangles describing the convex hull surface. Their vertices must lie
    /// on the surface of the convex hull to avoid incorrect results or indefinite hangs in the collision algorithm.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="triangles"/> is empty, or when the convex hull consists of more than
    /// <see cref="ushort.MaxValue"/> vertices.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="triangles"/> does not define a non-degenerate volume.
    /// </exception>
    public ConvexHullShape(ReadOnlySpan<JTriangle> triangles)
    {
        if (triangles.Length == 0)
        {
            throw new ArgumentException("Triangle set must contain at least one triangle.", nameof(triangles));
        }

        int maxVertexCount = triangles.Length * 3;
        Dictionary<CHullVector, ushort> tmpIndices = new(maxVertexCount);
        List<CHullVector> tmpVertices = new(maxVertexCount);

        ushort PushVector(CHullVector v)
        {
            if (tmpIndices.TryGetValue(v, out ushort result)) return result;

            if (tmpVertices.Count >= ushort.MaxValue)
            {
                throw new ArgumentException(
                    $"The convex hull consists of too many vertices (>{ushort.MaxValue}).", nameof(triangles));
            }

            result = (ushort)tmpVertices.Count;
            tmpIndices.Add(v, result);
            tmpVertices.Add(v);

            return result;
        }

        foreach (var tti in triangles)
        {
            PushVector(new CHullVector(tti.V0));
            PushVector(new CHullVector(tti.V1));
            PushVector(new CHullVector(tti.V2));
        }

        var tmpNeighbors = new List<ushort>[tmpVertices.Count];
        indices = new CHullTriangle[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            JTriangle tti = triangles[i];

            ushort a = PushVector(new CHullVector(tti.V0));
            ushort b = PushVector(new CHullVector(tti.V1));
            ushort c = PushVector(new CHullVector(tti.V2));

            indices[i] = new CHullTriangle(a, b, c);

            (tmpNeighbors[a] ??= []).Add(b);
            tmpNeighbors[a].Add(c);

            (tmpNeighbors[b] ??= []).Add(a);
            tmpNeighbors[b].Add(c);

            (tmpNeighbors[c] ??= []).Add(a);
            tmpNeighbors[c].Add(b);
        }

        // Estimate capacity to avoid resizing (Euler characteristic ~ 6 neighbors per vertex)
        var finalNeighbors = new List<ushort>(tmpVertices.Count * 6);

        var tmpVerticesSpan = CollectionsMarshal.AsSpan(tmpVertices);

        for (int i = 0; i < tmpVerticesSpan.Length; i++)
        {
            ref var element = ref tmpVerticesSpan[i];
            element.NeighborMinIndex = finalNeighbors.Count;
            AddDistinct(tmpNeighbors[i], finalNeighbors);
            element.NeighborMaxIndex = finalNeighbors.Count;

            // Help GC
            tmpNeighbors[i] = null!;
        }

        vertices = tmpVertices.ToArray();
        neighborList = finalNeighbors.ToArray();

        tmpIndices.Clear();
        tmpVertices.Clear();

        UpdateShape();
    }

    /// <inheritdoc cref="ConvexHullShape(ReadOnlySpan{JTriangle})"/>
    public ConvexHullShape(IEnumerable<JTriangle> triangles) :
        this(SpanHelper.AsReadOnlySpan(triangles))
    {
    }

    private ConvexHullShape()
    {
    }

    /// <summary>
    /// Helper to sort and add unique elements from source to destination.
    /// Replaces LINQ Distinct() for better performance and zero allocations.
    /// </summary>
    private static void AddDistinct(List<ushort>? source, List<ushort> destination)
    {
        if (source == null || source.Count == 0) return;

        source.Sort();

        ushort previous = source[0];
        destination.Add(previous);

        for (int i = 1; i < source.Count; i++)
        {
            ushort current = source[i];
            if (current != previous)
            {
                destination.Add(current);
                previous = current;
            }
        }
    }

    /// <summary>
    /// Creates a copy that shares the original hull geometry.
    /// </summary>
    public ConvexHullShape Clone()
    {
        ConvexHullShape result = new()
        {
            neighborList = neighborList,
            vertices = vertices,
            indices = indices,
            extremes = extremes,
            cachedBoundingBox = cachedBoundingBox,
            cachedCenter = cachedCenter,
            cachedInertia = cachedInertia,
            cachedMass = cachedMass,
            shifted = shifted
        };
        result.UpdateWorldBoundingBox();
        return result;
    }

    /// <summary>
    /// Gets or sets the offset added to the hull vertices in local coordinates.
    /// </summary>
    public JVector Shift
    {
        get => shifted;
        set
        {
            shifted = value;
            UpdateShape();
        }
    }

    /// <summary>
    /// Updates the shape's cached mass, inertia, and bounding box.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the convex hull does not define a non-degenerate volume.
    /// </exception>
    public void UpdateShape()
    {
        CalculateMassInertia();
        CalcExtremesAndInitBox();
        UpdateWorldBoundingBox();
    }

    /// <inheritdoc/>
    public override void CalculateMassInertia(out JSymmetricMatrix inertia, out JVector com, out Real mass)
    {
        inertia = cachedInertia;
        com = cachedCenter;
        mass = cachedMass;
    }

    /// <summary>
    /// Recalculates the mass, center of mass, and inertia tensor from the convex hull triangles.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the convex hull does not define a non-degenerate volume.
    /// </exception>
    public void CalculateMassInertia()
    {
        cachedCenter = JVector.Zero;
        cachedInertia = JSymmetricMatrix.Zero;
        cachedMass = 0;

        const Real a = (Real)(1.0 / 60.0);
        const Real b = (Real)(1.0 / 120.0);
        JSymmetricMatrix canonicalInertia = new(a, b, b, a, b, a);

        JVector pointWithin = JVector.Zero;

        for (int i = 0; i < vertices.Length; i++)
        {
            pointWithin += vertices[i].Vertex;
        }

        pointWithin = pointWithin * ((Real)1.0 / vertices.Length) + shifted;

        foreach (CHullTriangle tri in indices)
        {
            JVector column0 = vertices[tri.IndexA].Vertex + shifted;
            JVector column1 = vertices[tri.IndexB].Vertex + shifted;
            JVector column2 = vertices[tri.IndexC].Vertex + shifted;

            // check winding
            {
                JVector normal = (column1 - column0) % (column2 - column0);
                Real dot = JVector.Dot(normal, column0 - pointWithin);
                if (dot < (Real)0.0)
                {
                    (column0, column1) = (column1, column0);
                }
            }

            JMatrix transformation = new(
                column0.X, column1.X, column2.X,
                column0.Y, column1.Y, column2.Y,
                column0.Z, column1.Z, column2.Z);

            Real detA = transformation.Determinant();

            JSymmetricMatrix tetrahedronInertia = JSymmetricMatrix.Transform(canonicalInertia, transformation) * detA;

            JVector tetrahedronCom = (Real)(1.0 / 4.0) * (column0 + column1 + column2);
            Real tetrahedronMass = (Real)(1.0 / 6.0) * detA;

            cachedInertia += tetrahedronInertia;
            cachedCenter += tetrahedronMass * tetrahedronCom;
            cachedMass += tetrahedronMass;
        }

        const Real minimumMass = (Real)1e-12;

        if (!Real.IsFinite(cachedMass) || cachedMass <= minimumMass)
        {
            throw new InvalidOperationException("Convex hull must define a non-degenerate volume.");
        }

        cachedInertia = JSymmetricMatrix.Identity * cachedInertia.Trace() - cachedInertia;
        cachedCenter *= (Real)1.0 / cachedMass;
    }

    /// <inheritdoc/>
    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        JVector halfSize = (Real)0.5 * (cachedBoundingBox.Max - cachedBoundingBox.Min);
        JVector center = (Real)0.5 * (cachedBoundingBox.Max + cachedBoundingBox.Min);

        JMatrix ori = JMatrix.CreateFromQuaternion(orientation);
        JMatrix.Absolute(in ori, out JMatrix abs);
        JVector.Transform(halfSize, abs, out JVector temp);
        JVector.Transform(center, orientation, out JVector temp2);

        box.Max = temp;
        JVector.Negate(temp, out box.Min);

        JVector.Add(box.Min, position + temp2, out box.Min);
        JVector.Add(box.Max, position + temp2, out box.Max);
    }

    // Finds the vertices farthest along the six axis directions by brute force. They give the local
    // bounding box, and the support climb starts from the one farthest along its direction. The six
    // cannot all lie in one plane, so for any direction at least one of them sits a real distance
    // above the face of the hull that is turned away from it. A climb that starts there never
    // visits that face, where roundoff in the vertices of a flat tessellated region could stop it
    // before it has climbed anywhere, since it only ever moves to vertices farther along the
    // direction. The climb itself is not used here for the same reason.
    private void CalcExtremesAndInitBox()
    {
        ushort[] found = new ushort[6];
        JVector min = vertices[0].Vertex;
        JVector max = vertices[0].Vertex;

        for (ushort i = 1; i < vertices.Length; i++)
        {
            JVector v = vertices[i].Vertex;
            if (v.X > max.X) { max.X = v.X; found[0] = i; }
            if (v.X < min.X) { min.X = v.X; found[1] = i; }
            if (v.Y > max.Y) { max.Y = v.Y; found[2] = i; }
            if (v.Y < min.Y) { min.Y = v.Y; found[3] = i; }
            if (v.Z > max.Z) { max.Z = v.Z; found[4] = i; }
            if (v.Z < min.Z) { min.Z = v.Z; found[5] = i; }
        }

        extremes = found;
        cachedBoundingBox.Min = min + shifted;
        cachedBoundingBox.Max = max + shifted;
    }

    private ushort InternalSupportMap(in JVector direction, out JVector result)
    {
        // Start from the axis extreme farthest along the direction; see CalcExtremesAndInitBox.
        ushort current = extremes[0];
        Real dotProduct = JVector.Dot(vertices[current].Vertex, direction);

        for (int e = 1; e < 6; e++)
        {
            ushort candidate = extremes[e];
            Real candidateProduct = JVector.Dot(vertices[candidate].Vertex, direction);

            if (candidateProduct > dotProduct)
            {
                dotProduct = candidateProduct;
                current = candidate;
            }
        }

    main:
        var min = vertices[current].NeighborMinIndex;
        var max = vertices[current].NeighborMaxIndex;

        for (int i = min; i < max; i++)
        {
            ushort nb = neighborList[i];
            Real nbProduct = JVector.Dot(vertices[nb].Vertex, direction);

            if (nbProduct > dotProduct)
            {
                // no need to find the "best" neighbor - as soon as we found a better
                // candidate we move there.
                dotProduct = nbProduct;
                current = nb;
                goto main;
            }
        }

        // A vertex with no neighbour farther along the direction is a support vertex. The one
        // exception would be an interior vertex of a flat tessellated region facing away from
        // the direction, where every neighbour ties; the start above never reaches such a
        // region, so no walk across tied vertices is needed.
        result = vertices[current].Vertex + shifted;
        return current;
    }

    /// <inheritdoc/>
    public override void SupportMap(in JVector direction, out JVector result)
    {
        InternalSupportMap(direction, out result);
    }

    // Vertices collected for one feature. A face with more vertices is reported by the part of it
    // around the support vertex.
    private const int MaxFeatureVertices = 64;

    /// <inheritdoc/>
    /// <remarks>
    /// Climbs to the support vertex and walks from it over the connected vertices that tilting the
    /// direction by the tolerance angle can raise to its height: the whole face near its normal, an
    /// edge near the normals of the edge, otherwise a single vertex. A face is reported as its
    /// convex outline in winding order; an outline of more than <see cref="ISupportFeature.MaxVertices"/>
    /// vertices is thinned to that many, spread evenly around it.
    /// </remarks>
    [SkipLocalsInit]
    public override int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices)
    {
        Span<ushort> feature = stackalloc ushort[MaxFeatureVertices];
        int count = CollectFeature(direction, tolerance, feature);

        if (count > 2) count = OutlineFeature(direction, feature, count);

        int written = Math.Min(count, ISupportFeature.MaxVertices);

        for (int i = 0; i < written; i++)
        {
            // Every vertex of a small outline, evenly spaced ones of a larger outline.
            int selected = (2 * i + 1) * count / (2 * written);
            vertices[i] = this.vertices[feature[selected]].Vertex + shifted;
        }

        return written;
    }

    // Collects the support vertex and, walking over neighbours, every vertex connected to it that
    // tilting the direction by the tolerance angle can raise to its height. Stops when the span is
    // full, which then holds the vertices reached first.
    private int CollectFeature(in JVector direction, Real tolerance, Span<ushort> feature)
    {
        ushort top = InternalSupportMap(direction, out _);
        JVector topVertex = vertices[top].Vertex;
        Real slackSq = tolerance * tolerance * direction.LengthSquared();

        feature[0] = top;
        int count = 1;

        for (int next = 0; next < count && count < feature.Length; next++)
        {
            int min = vertices[feature[next]].NeighborMinIndex;
            int max = vertices[feature[next]].NeighborMaxIndex;

            for (int i = min; i < max && count < feature.Length; i++)
            {
                ushort candidate = neighborList[i];
                JVector offset = topVertex - vertices[candidate].Vertex;

                // Below the support vertex by more than the sine of the tolerance angle times the
                // distance to it, no tilt within the angle raises the candidate to its height.
                Real gap = JVector.Dot(offset, direction);
                if (gap > (Real)0.0 && gap * gap > slackSq * offset.LengthSquared()) continue;

                if (feature[..count].Contains(candidate)) continue;
                feature[count++] = candidate;
            }
        }

        return count;
    }

    // Reorders the collected vertices of a face to their convex outline in the plane of the face, in
    // winding order around the direction, and returns its length. Vertices inside the outline or on
    // its edges are dropped.
    [SkipLocalsInit]
    private int OutlineFeature(in JVector direction, Span<ushort> feature, int count)
    {
        JVector normal = JVector.Normalize(direction);
        JVector tangent1 = MathHelper.CreateOrthonormal(normal);
        JVector tangent2 = normal % tangent1;

        Span<Real> xs = stackalloc Real[MaxFeatureVertices];
        Span<Real> ys = stackalloc Real[MaxFeatureVertices];
        int start = 0;

        for (int i = 0; i < count; i++)
        {
            JVector vertex = vertices[feature[i]].Vertex;
            xs[i] = JVector.Dot(vertex, tangent1);
            ys[i] = JVector.Dot(vertex, tangent2);
            if (xs[i] < xs[start] || (xs[i] == xs[start] && ys[i] < ys[start])) start = i;
        }

        // Gift wrapping from the leftmost vertex: the outline continues with the vertex that has all
        // others on its left, the farthest one when some are in line.
        Span<ushort> outline = stackalloc ushort[MaxFeatureVertices];
        int outlineCount = 0;
        int current = start;

        do
        {
            outline[outlineCount++] = feature[current];
            int next = current == 0 ? 1 : 0;

            for (int i = 0; i < count; i++)
            {
                if (i == current) continue;

                Real nextX = xs[next] - xs[current];
                Real nextY = ys[next] - ys[current];
                Real x = xs[i] - xs[current];
                Real y = ys[i] - ys[current];
                Real cross = nextX * y - nextY * x;

                if (cross < (Real)0.0 || (cross == (Real)0.0 && x * x + y * y > nextX * nextX + nextY * nextY))
                {
                    next = i;
                }
            }

            current = next;
        } while (current != start && outlineCount < count);

        outline[..outlineCount].CopyTo(feature);
        return outlineCount;
    }

    /// <inheritdoc/>
    public override void GetCenter(out JVector point)
    {
        point = cachedCenter;
    }
}
