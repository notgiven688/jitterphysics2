/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

public partial class DynamicTree
{
    /// <summary>
    /// Represents the result of a distance query against the dynamic tree.
    /// </summary>
    public struct FindNearestResult
    {
        /// <summary>The closest proxy found.</summary>
        public IDynamicTreeProxy? Entity;

        /// <summary>Closest point on the query shape in world space. Not well-defined when shapes overlap.</summary>
        public JVector PointA;

        /// <summary>Closest point on the found proxy in world space. Not well-defined when shapes overlap.</summary>
        public JVector PointB;

        /// <summary>
        /// Unit direction from the query shape toward the found proxy, or <see cref="JVector.Zero"/>
        /// when the shapes overlap. Do not use this to test whether a result was found.
        /// </summary>
        public JVector Normal;

        /// <summary>The separation distance between the query shape and the found proxy.</summary>
        public Real Distance;
    }

    /// <summary>
    /// Delegate for filtering distance query results after the exact shape distance test.
    /// </summary>
    /// <param name="result">The distance result to evaluate.</param>
    /// <returns><c>false</c> to filter out this result; <c>true</c> to keep it.</returns>
    public delegate bool FindNearestFilterPost(FindNearestResult result);

    /// <summary>
    /// Delegate for filtering distance query candidates before the exact shape distance test.
    /// </summary>
    /// <param name="proxy">The proxy to evaluate.</param>
    /// <returns><c>false</c> to skip this proxy; <c>true</c> to test it.</returns>
    public delegate bool FindNearestFilterPre(IDynamicTreeProxy proxy);

    /// <inheritdoc cref="FindNearest{T}(in T, in JQuaternion, in JVector, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    public bool FindNearestPoint(in JVector position,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreatePoint(), JQuaternion.Identity, position, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    /// <inheritdoc cref="FindNearest{T}(in T, in JQuaternion, in JVector, Real, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    public bool FindNearestPoint(in JVector position, Real maxDistance,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreatePoint(), JQuaternion.Identity, position, maxDistance, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    /// <inheritdoc cref="FindNearest{T}(in T, in JQuaternion, in JVector, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The sphere radius.</param>
    public bool FindNearestSphere(Real radius, in JVector position,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    /// <inheritdoc cref="FindNearest{T}(in T, in JQuaternion, in JVector, Real, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The sphere radius.</param>
    public bool FindNearestSphere(Real radius, in JVector position, Real maxDistance,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, maxDistance, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    private struct DistanceQuery(in JBoundingBox box, in JQuaternion orientation, in JVector position)
    {
        public readonly JBoundingBox Box = box;
        public readonly JQuaternion Orientation = orientation;
        public readonly JVector Position = position;

        public FindNearestFilterPost? FilterPost;
        public FindNearestFilterPre? FilterPre;

        public Real Distance;
    }

    /// <summary>
    /// Finds the closest proxy in the tree to the query shape and returns the exact closest points.
    /// </summary>
    /// <remarks>
    /// This overload is unbounded and considers all proxies in the tree.
    /// Returns <c>true</c> when a separated proxy is found (<c>distance &gt; 0</c>).
    /// Returns <c>false</c> with <c>distance = 0</c> and a non-null proxy when the query shape overlaps a proxy.
    /// Returns <c>false</c> with a null proxy when nothing is found within range.
    /// To skip overlapping proxies and continue searching for separated ones, use a <paramref name="post"/>
    /// filter that returns <c>false</c> for results with <c>distance == 0</c>.
    /// </remarks>
    public bool FindNearest<T>(in T support, in JQuaternion orientation, in JVector position,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where T : ISupportMappable
    {
        ShapeHelper.CalculateBoundingBox(support, orientation, position, out JBoundingBox box);

        DistanceQuery query = new(box, orientation, position)
        {
            FilterPre = pre,
            FilterPost = post,
            Distance = Real.MaxValue
        };

        bool hit = QueryDistance(support, query, out var result);
        proxy = result.Entity;
        pointA = result.PointA;
        pointB = result.PointB;
        normal = result.Normal;
        distance = result.Distance;
        return hit;
    }

    /// <inheritdoc cref="FindNearest{T}(in T, in JQuaternion, in JVector, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="maxDistance">Maximum separation distance to consider. Proxies farther than this are ignored.</param>
    public bool FindNearest<T>(in T support, in JQuaternion orientation, in JVector position, Real maxDistance,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where T : ISupportMappable
    {
        ShapeHelper.CalculateBoundingBox(support, orientation, position, out JBoundingBox box);

        DistanceQuery query = new(box, orientation, position)
        {
            FilterPre = pre,
            FilterPost = post,
            Distance = maxDistance
        };

        bool hit = QueryDistance(support, query, out var result);
        proxy = result.Entity;
        pointA = result.PointA;
        pointB = result.PointB;
        normal = result.Normal;
        distance = result.Distance;
        return hit;
    }

    // Returns the minimum distance between the query AABB and a tree node's expanded AABB.
    // Uses the Minkowski sum: expand the target by the query half-extents, then measure
    // point-to-AABB distance from the query center. Returns 0 if the AABBs overlap.
    private static Real MinDistBox(in JBoundingBox queryBox, in TreeBox targetBox)
    {
        JVector extents = (queryBox.Max - queryBox.Min) * (Real)0.5;
        JVector center = (queryBox.Max + queryBox.Min) * (Real)0.5;

        Real dx = MathR.Max(MathR.Max(targetBox.Min.X - extents.X - center.X, center.X - targetBox.Max.X - extents.X), (Real)0.0);
        Real dy = MathR.Max(MathR.Max(targetBox.Min.Y - extents.Y - center.Y, center.Y - targetBox.Max.Y - extents.Y), (Real)0.0);
        Real dz = MathR.Max(MathR.Max(targetBox.Min.Z - extents.Z - center.Z, center.Z - targetBox.Max.Z - extents.Z), (Real)0.0);

        return MathR.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private bool QueryDistance<T>(in T support, in DistanceQuery query, out FindNearestResult result)
        where T : ISupportMappable
    {
        result = new FindNearestResult
        {
            Distance = query.Distance
        };

        if (root == NullNode)
        {
            return false;
        }

        _stack ??= new Stack<int>(256);
        _stack.Push(root);

        while (_stack.Count > 0)
        {
            int index = _stack.Pop();
            ref Node node = ref nodes[index];

            if (node.IsLeaf)
            {
                if (node.Proxy is not IDistanceTestable distCastable) continue;
                if (query.FilterPre != null && !query.FilterPre(node.Proxy!)) continue;

                Unsafe.SkipInit(out FindNearestResult res);
                bool separated = distCastable.Distance(support,
                    query.Orientation, query.Position,
                    out res.PointA, out res.PointB, out res.Normal, out res.Distance);
                res.Entity = node.Proxy;

                if (!separated)
                {
                    res.Distance = (Real)0.0;
                    res.Normal = JVector.Zero;
                    if (query.FilterPost != null && !query.FilterPost(res)) continue;
                    _stack.Clear();
                    result = res;
                    return false;
                }

                if (res.Distance > result.Distance) continue;
                if (query.FilterPost != null && !query.FilterPost(res)) continue;

                result = res;
                continue;
            }

            ref Node leftNode = ref nodes[node.Left];
            ref Node rightNode = ref nodes[node.Right];

            Real leftDist = MinDistBox(query.Box, leftNode.ExpandedBox);
            Real rightDist = MinDistBox(query.Box, rightNode.ExpandedBox);

            bool leftHit = leftDist <= result.Distance;
            bool rightHit = rightDist <= result.Distance;

            if (leftHit && rightHit)
            {
                if (leftDist < rightDist)
                {
                    _stack.Push(node.Right);
                    _stack.Push(node.Left);
                }
                else
                {
                    _stack.Push(node.Left);
                    _stack.Push(node.Right);
                }
            }
            else
            {
                if (leftHit) _stack.Push(node.Left);
                if (rightHit) _stack.Push(node.Right);
            }
        }

        _stack.Clear();
        return result.Entity != null && result.Distance > (Real)0.0;
    }
}
