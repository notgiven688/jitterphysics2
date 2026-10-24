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

        /// <summary>The collision point on the moving query shape in world space at the sweep origin. Undefined when the shapes already overlap.</summary>
        public JVector PointA;

        /// <summary>The collision point on the hit object in world space at the sweep origin. Undefined when the shapes already overlap.</summary>
        public JVector PointB;

        /// <summary>
        /// The collision normal in world space, or <see cref="JVector.Zero"/> if the shapes already overlap.
        /// Do not use this to test whether a hit occurred.
        /// </summary>
        public JVector Normal;

        /// <summary>The sweep parameter at impact; time in seconds for casts specified by velocities. Zero for initial overlap.</summary>
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
    /// <param name="proxy">The proxy to evaluate.</param>
    /// <returns><c>false</c> to skip this proxy; <c>true</c> to test it.</returns>
    public delegate bool SweepCastFilterPre(IDynamicTreeProxy proxy);

    /// <summary>
    /// Sweeps a sphere through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="radius">The sphere radius.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
    public bool SweepCastSphere(Real radius, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <summary>
    /// Sweeps a sphere through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="radius">The sphere radius.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="maxLambda">The maximum sweep parameter to consider.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
    public bool SweepCastSphere(Real radius, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateSphere(radius), JQuaternion.Identity, position, direction, maxLambda, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <summary>
    /// Sweeps a box through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="halfExtents">The half extents of the box.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
    public bool SweepCastBox(in JVector halfExtents, in JQuaternion orientation, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateBox(halfExtents), orientation, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <summary>
    /// Sweeps a box through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="halfExtents">The half extents of the box.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="maxLambda">The maximum sweep parameter to consider.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
    public bool SweepCastBox(in JVector halfExtents, in JQuaternion orientation, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateBox(halfExtents), orientation, position, direction, maxLambda, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <summary>
    /// Sweeps a capsule through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="radius">The capsule radius.</param>
    /// <param name="halfLength">Half the cylindrical section length of the capsule.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
    public bool SweepCastCapsule(Real radius, Real halfLength, in JQuaternion orientation, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateCapsule(radius, halfLength), orientation, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <summary>
    /// Sweeps a capsule through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="radius">The capsule radius.</param>
    /// <param name="halfLength">Half the cylindrical section length of the capsule.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="maxLambda">The maximum sweep parameter to consider.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
    public bool SweepCastCapsule(Real radius, Real halfLength, in JQuaternion orientation, in JVector position, in JVector direction, Real maxLambda,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateCapsule(radius, halfLength), orientation, position, direction, maxLambda, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <summary>
    /// Sweeps a cylinder through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="radius">The cylinder radius.</param>
    /// <param name="halfHeight">Half the cylinder height.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
    public bool SweepCastCylinder(Real radius, Real halfHeight, in JQuaternion orientation, in JVector position, in JVector direction,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda) =>
        SweepCast(SupportPrimitives.CreateCylinder(radius, halfHeight), orientation, position, direction, pre,
            post, out proxy, out pointA, out pointB, out normal, out lambda);

    /// <summary>
    /// Sweeps a cylinder through the tree and returns the first accepted hit.
    /// </summary>
    /// <param name="radius">The cylinder radius.</param>
    /// <param name="halfHeight">Half the cylinder height.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="maxLambda">The maximum sweep parameter to consider.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
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

        public JVector AngularVelocity;
        public Real Extent;

        public SweepCastFilterPost? FilterPost;
        public SweepCastFilterPre? FilterPre;

        public Real Lambda;
    }

    /// <summary>
    /// Sweeps a support-mapped query shape against all <see cref="ISweepTestable"/> proxies in the tree
    /// and returns the closest exact hit.
    /// </summary>
    /// <param name="support">The query shape.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space at the start of the sweep.</param>
    /// <param name="direction">The sweep direction vector in world space.</param>
    /// <param name="pre">Optional pre-filter that can skip candidate proxies before the exact sweep test.</param>
    /// <param name="post">Optional post-filter that can reject exact results and continue the search.</param>
    /// <param name="proxy">The closest accepted hit proxy, or <c>null</c> if no hit is found.</param>
    /// <param name="pointA">Collision point on the query shape in world space at the sweep origin. Undefined when the shapes already overlap.</param>
    /// <param name="pointB">Collision point on the hit proxy in world space at the sweep origin. Undefined when the shapes already overlap.</param>
    /// <param name="normal">Collision normal in world space, or <see cref="JVector.Zero"/> when the shapes already overlap.</param>
    /// <param name="lambda">Time of impact in units of <paramref name="direction"/>. Zero when the shapes already overlap.</param>
    /// <returns>
    /// <c>true</c> if an accepted hit is found. This includes the overlapping case, where <paramref name="lambda"/>
    /// is zero. <c>false</c> if no accepted hit is found, in which case <paramref name="proxy"/> is <c>null</c>.
    /// </returns>
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

    /// <summary>
    /// Finds the first accepted sweep hit up to <paramref name="maxLambda"/>.
    /// </summary>
    /// <param name="support">The query shape.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="direction">The sweep direction in world space.</param>
    /// <param name="maxLambda">Maximum sweep parameter to consider along <paramref name="direction"/>.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">The contact point on the query shape in world space.</param>
    /// <param name="pointB">The contact point on the found proxy in world space.</param>
    /// <param name="normal">The result normal in world space.</param>
    /// <param name="lambda">The sweep parameter at the first accepted hit; zero for initial overlap.</param>
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

    /// <summary>
    /// Sweeps a translating and rotating convex shape through the tree and returns the first accepted hit.
    /// </summary>
    /// <remarks>
    /// Targets are held at their current poses. Only proxies implementing <see cref="IAngularSweepTestable"/>
    /// participate when angular velocity is nonzero. All built-in shapes support this query.
    /// As with other sweep casts, contact points refer to the initial poses; the normal is evaluated at impact.
    /// </remarks>
    /// <param name="support">The query shape.</param>
    /// <param name="orientation">The query shape's initial world-space orientation.</param>
    /// <param name="position">The query shape's initial world-space position and rotation origin.</param>
    /// <param name="velocity">The query shape's linear velocity in world space.</param>
    /// <param name="angularVelocity">The query shape's angular velocity in world space, in radians per second.</param>
    /// <param name="maximumTime">The last time to search, in seconds. Must be finite and nonnegative.</param>
    /// <param name="pre">Optional filter applied before the exact test.</param>
    /// <param name="post">Optional filter applied after the exact test.</param>
    /// <param name="proxy">The accepted proxy, or null if none is found.</param>
    /// <param name="pointA">Contact point on the query shape at its initial pose, in world space.</param>
    /// <param name="pointB">Contact point on the found proxy in world space.</param>
    /// <param name="normal">World-space normal from the query shape toward the target at impact; zero for initial overlap.</param>
    /// <param name="lambda">Time of impact in seconds; zero for initial overlap.</param>
    /// <returns>True if an accepted impact is found; false if no accepted hit is found or the searches fail.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The maximum time is negative or not finite.</exception>
    public bool SweepCast<T>(in T support, in JQuaternion orientation, in JVector position,
        in JVector velocity, in JVector angularVelocity, Real maximumTime,
        SweepCastFilterPre? pre, SweepCastFilterPost? post,
        out IDynamicTreeProxy? proxy, out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where T : ISupportMappable
    {
        ArgumentCheck.NonNegative(maximumTime, nameof(maximumTime));
        if (angularVelocity == JVector.Zero)
            return SweepCast(support, orientation, position, velocity, maximumTime, pre, post,
                out proxy, out pointA, out pointB, out normal, out lambda);

        ShapeHelper.CalculateBoundingBox(support, orientation, JVector.Zero, out JBoundingBox box);
        Real extent = JVector.Max(JVector.Abs(box.Min), JVector.Abs(box.Max)).Length();

        // Enclose every intermediate rotation, including a full turn with identical endpoint poses.
        Real scale = extent * MathR.Min(maximumTime, (Real)2.0 / angularVelocity.Length());
        JVector expansion = scale * new JVector(
            MathR.Sqrt(angularVelocity.Y * angularVelocity.Y + angularVelocity.Z * angularVelocity.Z),
            MathR.Sqrt(angularVelocity.X * angularVelocity.X + angularVelocity.Z * angularVelocity.Z),
            MathR.Sqrt(angularVelocity.X * angularVelocity.X + angularVelocity.Y * angularVelocity.Y));
        box.Min = JVector.Max(box.Min - expansion, new JVector(-extent)) + position;
        box.Max = JVector.Min(box.Max + expansion, new JVector(extent)) + position;

        SweepQuery sweep = new(box, orientation, position, velocity)
        {
            AngularVelocity = angularVelocity,
            Extent = extent,
            FilterPre = pre,
            FilterPost = post,
            Lambda = maximumTime
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

        Stack<int> stack = QueryStack;
        int baseCount = stack.Count;
        bool angular = sweep.AngularVelocity != JVector.Zero;
        try
        {
            stack.Push(root);

            while (stack.Count > baseCount)
            {
                int index = stack.Pop();
                ref Node node = ref nodes[index];

                if (node.IsLeaf)
                {
                    if (node.Proxy is not ISweepTestable sweepCastable) continue;
                    if (angular && sweepCastable is not IAngularSweepTestable) continue;
                    if (sweep.FilterPre != null && !sweep.FilterPre(node.Proxy!)) continue;

                    Unsafe.SkipInit(out SweepCastResult res);
                    bool hit = angular
                        ? ((IAngularSweepTestable)sweepCastable).Sweep(support,
                            sweep.Orientation, sweep.Position, sweep.Direction, sweep.AngularVelocity,
                            sweep.Extent, result.Lambda, out res.PointA, out res.PointB, out res.Normal, out res.Lambda)
                        : sweepCastable.Sweep(support,
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
