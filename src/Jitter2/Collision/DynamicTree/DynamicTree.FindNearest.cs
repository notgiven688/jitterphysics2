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

        /// <summary>Closest point on the query shape in world space. Undefined when the shapes overlap.</summary>
        public JVector PointA;

        /// <summary>Closest point on the found proxy in world space. Undefined when the shapes overlap.</summary>
        public JVector PointB;

        /// <summary>
        /// Unit direction from the query shape toward the found proxy, or <see cref="JVector.Zero"/>
        /// when the shapes overlap. Do not use this to test whether a result was found.
        /// </summary>
        public JVector Normal;

        /// <summary>The separation distance between the query shape and the found proxy. Zero when the shapes overlap.</summary>
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

    /// <summary>
    /// Convenience overload of <see cref="FindNearest{T}(in T, in JQuaternion, in JVector, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// that queries with a <see cref="SupportPrimitives.Point"/>.
    /// </summary>
    public bool FindNearestPoint(in JVector position,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreatePoint(), JQuaternion.Identity, position, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    /// <summary>
    /// Convenience overload of <see cref="FindNearest{T}(in T, in JQuaternion, in JVector, Real, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// that queries with a <see cref="SupportPrimitives.Point"/>.
    /// </summary>
    public bool FindNearestPoint(in JVector position, Real maxDistance,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreatePoint(), JQuaternion.Identity, position, maxDistance, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    /// <summary>
    /// Convenience overload of <see cref="FindNearest{T}(in T, in JQuaternion, in JVector, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// that queries with a <see cref="SupportPrimitives.Sphere"/>.
    /// </summary>
    /// <param name="radius">The sphere radius.</param>
    public bool FindNearestSphere(Real radius, in JVector position,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    /// <summary>
    /// Convenience overload of <see cref="FindNearest{T}(in T, in JQuaternion, in JVector, Real, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// that queries with a <see cref="SupportPrimitives.Sphere"/>.
    /// </summary>
    /// <param name="radius">The sphere radius.</param>
    public bool FindNearestSphere(Real radius, in JVector position, Real maxDistance,
        FindNearestFilterPre? pre, FindNearestFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real distance) =>
        FindNearest(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, maxDistance, pre, post,
            out proxy, out pointA, out pointB, out normal, out distance);

    private struct DistanceQuery(in JBoundingBox box, in JQuaternion orientation, in JVector position)
    {
        public readonly JVector BoxExtents = (box.Max - box.Min) * (Real)0.5;
        public readonly JVector BoxCenter = (box.Max + box.Min) * (Real)0.5;
        public readonly JQuaternion Orientation = orientation;
        public readonly JVector Position = position;

        public FindNearestFilterPost? FilterPost;
        public FindNearestFilterPre? FilterPre;

        public Real Distance;
    }

    /// <summary>
    /// Finds the closest <see cref="IDistanceTestable"/> proxy in the tree to the query shape
    /// and returns the exact closest points.
    /// </summary>
    /// <param name="support">The query shape.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="pre">Optional pre-filter that can skip candidate proxies before the exact distance test.</param>
    /// <param name="post">Optional post-filter that can reject exact results and continue the search.</param>
    /// <param name="proxy">The nearest accepted proxy, or <c>null</c> if no proxy is found.</param>
    /// <param name="pointA">Closest point on the query shape in world space. Undefined when the shapes overlap.</param>
    /// <param name="pointB">Closest point on the found proxy in world space. Undefined when the shapes overlap.</param>
    /// <param name="normal">Unit direction from the query shape toward the found proxy, or <see cref="JVector.Zero"/> when the shapes overlap.</param>
    /// <param name="distance">Separation distance between the query shape and the found proxy. Zero when they overlap.</param>
    /// <returns>
    /// <c>true</c> if an accepted proxy is found. This includes the overlapping case, where <paramref name="distance"/>
    /// is zero. <c>false</c> if no accepted proxy is found, in which case <paramref name="proxy"/> is <c>null</c>.
    /// </returns>
    /// <remarks>
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

    /// <summary>
    /// Bounded variant of <see cref="FindNearest{T}(in T, in JQuaternion, in JVector, FindNearestFilterPre?, FindNearestFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// that limits the search to <paramref name="maxDistance"/>.
    /// </summary>
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
    private static Real MinDistBox(in JVector queryExtents, in JVector queryCenter, in TreeBox targetBox)
    {
        Real dx = MathR.Max(MathR.Max(targetBox.Min.X - queryExtents.X - queryCenter.X, queryCenter.X - targetBox.Max.X - queryExtents.X), (Real)0.0);
        Real dy = MathR.Max(MathR.Max(targetBox.Min.Y - queryExtents.Y - queryCenter.Y, queryCenter.Y - targetBox.Max.Y - queryExtents.Y), (Real)0.0);
        Real dz = MathR.Max(MathR.Max(targetBox.Min.Z - queryExtents.Z - queryCenter.Z, queryCenter.Z - targetBox.Max.Z - queryExtents.Z), (Real)0.0);

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

        Stack<int> stack = QueryStack;
        int baseCount = stack.Count;
        try
        {
            stack.Push(root);

            while (stack.Count > baseCount)
            {
                int index = stack.Pop();
                ref Node node = ref nodes[index];

                if (node.IsLeaf)
                {
                    IDynamicTreeProxy proxy = node.Proxy!;
                    if (proxy is not IDistanceTestable distanceTestable) continue;
                    if (query.FilterPre != null && !query.FilterPre(proxy)) continue;

                    Unsafe.SkipInit(out FindNearestResult res);
                    bool separated = distanceTestable.Distance(support,
                        query.Orientation, query.Position,
                        out res.PointA, out res.PointB, out res.Normal, out res.Distance);
                    res.Entity = proxy;

                    if (!separated)
                    {
                        res.Distance = (Real)0.0;
                        res.Normal = JVector.Zero;
                        if (query.FilterPost != null && !query.FilterPost(res)) continue;
                        result = res;
                        return true;
                    }

                    if (res.Distance > result.Distance) continue;
                    if (query.FilterPost != null && !query.FilterPost(res)) continue;

                    result = res;
                    continue;
                }

                ref Node leftNode = ref nodes[node.Left];
                ref Node rightNode = ref nodes[node.Right];

                Real leftDist = MinDistBox(query.BoxExtents, query.BoxCenter, leftNode.ExpandedBox);
                Real rightDist = MinDistBox(query.BoxExtents, query.BoxCenter, rightNode.ExpandedBox);

                bool leftHit = leftDist <= result.Distance;
                bool rightHit = rightDist <= result.Distance;

                if (leftHit && rightHit)
                {
                    if (leftDist < rightDist)
                    {
                        stack.Push(node.Right);
                        stack.Push(node.Left);
                    }
                    else
                    {
                        stack.Push(node.Left);
                        stack.Push(node.Right);
                    }
                }
                else
                {
                    if (leftHit) stack.Push(node.Left);
                    if (rightHit) stack.Push(node.Right);
                }
            }

            return result.Entity != null;
        }
        finally
        {
            PopTo(stack, baseCount);
        }
    }
}
