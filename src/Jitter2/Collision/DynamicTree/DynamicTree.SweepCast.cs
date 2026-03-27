/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;
using Jitter2.Collision.Shapes;

namespace Jitter2.Collision;

public partial class DynamicTree
{
    /// <summary>
    /// Represents the result of a sweep against the dynamic tree.
    /// </summary>
    public struct SweepCastResult
    {
        /// <summary>The proxy that was hit.</summary>
        public IDynamicTreeProxy? Entity;

        /// <summary>The collision point on the moving query shape in world space at t = 0. Not well-defined when the shapes already overlap.</summary>
        public JVector PointA;

        /// <summary>The collision point on the hit object in world space at t = 0. Not well-defined when the shapes already overlap.</summary>
        public JVector PointB;

        /// <summary>
        /// The collision normal in world space, or <see cref="JVector.Zero"/> if the shapes already overlap.
        /// Do not use this to test whether a hit occurred.
        /// </summary>
        public JVector Normal;

        /// <summary>The time of impact expressed in units of the sweep direction vector. Zero if the shapes already overlap.</summary>
        public Real Lambda;
    }

    /// <summary>
    /// Delegate for filtering sweep results after the exact shape sweep test.
    /// </summary>
    /// <param name="result">The sweep result to evaluate.</param>
    /// <returns><c>false</c> to filter out this hit; <c>true</c> to keep it.</returns>
    public delegate bool SweepCastFilterPost(SweepCastResult result);

    /// <summary>
    /// Delegate for filtering sweep candidates before the exact shape sweep test.
    /// </summary>
    /// <param name="result">The proxy to evaluate.</param>
    /// <returns><c>false</c> to skip this proxy; <c>true</c> to test it.</returns>
    public delegate bool SweepCastFilterPre(IDynamicTreeProxy result);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The sphere radius.</param>
    public bool SweepCastSphere(Real radius, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, Real, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The sphere radius.</param>
    public bool SweepCastSphere(Real radius, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, direction, maxLambda, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="halfExtents">The half extents of the box.</param>
    public bool SweepCastBox(in JVector halfExtents, in JQuaternion orientation, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateBox(halfExtents), orientation, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, Real, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="halfExtents">The half extents of the box.</param>
    public bool SweepCastBox(in JVector halfExtents, in JQuaternion orientation, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateBox(halfExtents), orientation, position, direction, maxLambda, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The capsule radius.</param>
    /// <param name="halfLength">Half the cylindrical section length of the capsule.</param>
    public bool SweepCastCapsule(Real radius, Real halfLength, in JQuaternion orientation, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateCapsule(radius, halfLength), orientation, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, Real, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The capsule radius.</param>
    /// <param name="halfLength">Half the cylindrical section length of the capsule.</param>
    public bool SweepCastCapsule(Real radius, Real halfLength, in JQuaternion orientation, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateCapsule(radius, halfLength), orientation, position, direction, maxLambda, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The cylinder radius.</param>
    /// <param name="halfHeight">Half the cylinder height.</param>
    public bool SweepCastCylinder(Real radius, Real halfHeight, in JQuaternion orientation, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateCylinder(radius, halfHeight), orientation, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, Real, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="radius">The cylinder radius.</param>
    /// <param name="halfHeight">Half the cylinder height.</param>
    public bool SweepCastCylinder(Real radius, Real halfHeight, in JQuaternion orientation, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateCylinder(radius, halfHeight), orientation, position, direction, maxLambda, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);
    
    private struct SweepQuery(in JBoundingBox box, in JQuaternion orientation, in JVector position, in JVector direction)
    {
        public readonly JBoundingBox Box = box;
        public readonly JQuaternion Orientation = orientation;
        public readonly JVector Position = position;
        public readonly JVector Direction = direction;

        public SweepCastFilterPost? FilterPost;
        public SweepCastFilterPre? FilterPre;

        public Real Lambda;
    }

    /// <summary>
    /// Sweeps a support-mapped query shape against the world and returns the closest exact hit.
    /// </summary>
    /// <remarks>
    /// This overload performs an unbounded sweep along <paramref name="direction"/>.
    /// For static overlap checks, first query the tree with a bounding box and then run an exact narrow-phase test on
    /// the candidates. For example, to test a sphere against the tree:
    /// <code>
    /// var sphere = SupportPrimitives.CreateSphere(radius);
    /// ShapeHelper.CalculateBoundingBox(sphere, JQuaternion.Identity, center, out JBoundingBox box);
    ///
    /// List&lt;IDynamicTreeProxy&gt; candidates = [];
    /// tree.Query(candidates, box);
    ///
    /// bool overlap = candidates.Any(candidate =>
    ///     candidate is ISupportMappable support &amp;&amp;
    ///     NarrowPhase.Overlap(sphere, support,
    ///         JQuaternion.Identity, JQuaternion.Identity,
    ///         center, JVector.Zero));
    /// </code>
    /// </remarks>
    public bool SweepCast<T>(in T support, in JQuaternion orientation, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where T : ISupportMappable
    {
        ShapeHelper.CalculateBoundingBox(support, orientation, position, out JBoundingBox box);

        SweepQuery sweep = new(box, orientation, position, direction)
        {
            FilterPre = pre,
            FilterPost = post,
            Lambda = Real.MaxValue
        };

        bool hit = QuerySweep(support, sweep, out var result);
        proxy = result.Entity;
        pointA = result.PointA;
        pointB = result.PointB;
        normal = result.Normal;
        lambda = result.Lambda;
        return hit;
    }

    /// <inheritdoc cref="SweepCast{T}(in T, in JQuaternion, in JVector, in JVector, SweepCastFilterPre?, SweepCastFilterPost?, out IDynamicTreeProxy?, out JVector, out JVector, out JVector, out Real)"/>
    /// <param name="maxLambda">Maximum sweep parameter to consider along <paramref name="direction"/>.</param>
    public bool SweepCast<T>(in T support, in JQuaternion orientation, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where T : ISupportMappable
    {
        ShapeHelper.CalculateBoundingBox(support, orientation, position, out JBoundingBox box);

        SweepQuery sweep = new(box, orientation, position, direction)
        {
            FilterPre = pre,
            FilterPost = post,
            Lambda = maxLambda
        };

        bool hit = QuerySweep(support, sweep, out var result);
        proxy = result.Entity;
        pointA = result.PointA;
        pointB = result.PointB;
        normal = result.Normal;
        lambda = result.Lambda;
        return hit;
    }

    private static bool SweepBox(in JBoundingBox movingBox, in JVector translation, in TreeBox targetBox, out Real enter)
    {
        JVector extents = (movingBox.Max - movingBox.Min) * (Real)0.5;
        JVector center = (movingBox.Max + movingBox.Min) * (Real)0.5;

        JBoundingBox expanded = new(targetBox.Min - extents, targetBox.Max + extents);
        return expanded.RayIntersect(center, translation, out enter);
    }

    private bool QuerySweep<T>(in T support, in SweepQuery sweep, out SweepCastResult result)
        where T : ISupportMappable
    {
        result = new SweepCastResult
        {
            Lambda = sweep.Lambda
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
                if (node.Proxy is not ISweepTestable sweepCastable) continue;
                if (sweep.FilterPre != null && !sweep.FilterPre(node.Proxy!)) continue;

                Unsafe.SkipInit(out SweepCastResult res);
                bool hit = sweepCastable.Sweep(support,
                    sweep.Orientation, sweep.Position, sweep.Direction,
                    out res.PointA, out res.PointB, out res.Normal, out res.Lambda);
                res.Entity = node.Proxy;

                if (!hit || res.Lambda > result.Lambda) continue;
                if (sweep.FilterPost != null && !sweep.FilterPost(res)) continue;

                result = res;
                continue;
            }

            ref Node leftNode = ref nodes[node.Left];
            ref Node rightNode = ref nodes[node.Right];

            bool leftHit = SweepBox(sweep.Box, sweep.Direction, leftNode.ExpandedBox, out Real leftEnter);
            bool rightHit = SweepBox(sweep.Box, sweep.Direction, rightNode.ExpandedBox, out Real rightEnter);

            if (leftEnter > result.Lambda) leftHit = false;
            if (rightEnter > result.Lambda) rightHit = false;

            if (leftHit && rightHit)
            {
                if (leftEnter < rightEnter)
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
        return result.Entity != null;
    }

}
