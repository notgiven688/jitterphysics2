/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using Jitter2.LinearMath;
using Jitter2.Collision.Shapes;

namespace Jitter2.Collision;

public partial class DynamicTree
{
    /// <summary>
    /// Represents a proxy overlapping a query shape.
    /// </summary>
    public struct OverlapResult
    {
        /// <summary>The overlapping proxy.</summary>
        public IDynamicTreeProxy? Entity;

        /// <summary>The deepest point on the query shape inside the proxy, in world space.</summary>
        public JVector PointA;

        /// <summary>The deepest point on the proxy inside the query shape, in world space.</summary>
        public JVector PointB;

        /// <summary>
        /// Unit direction from the query shape toward the proxy. Moving the query shape by
        /// <see cref="Penetration"/> against it separates the two.
        /// </summary>
        public JVector Normal;

        /// <summary>The penetration depth.</summary>
        public Real Penetration;
    }

    /// <summary>
    /// Delegate for filtering overlap results after the exact shape test.
    /// </summary>
    /// <param name="result">The overlap result to evaluate.</param>
    /// <returns><c>false</c> to filter out this result; <c>true</c> to keep it.</returns>
    public delegate bool OverlapFilterPost(OverlapResult result);

    /// <summary>
    /// Delegate for filtering overlap candidates before the exact shape test.
    /// </summary>
    /// <param name="proxy">The proxy to evaluate.</param>
    /// <returns><c>false</c> to skip this proxy; <c>true</c> to test it.</returns>
    public delegate bool OverlapFilterPre(IDynamicTreeProxy proxy);

    /// <summary>
    /// Finds every <see cref="IOverlapTestable"/> proxy in the tree that a support-mapped query shape overlaps,
    /// with how deeply and in which direction each overlaps it.
    /// </summary>
    /// <param name="support">The query shape.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="pre">Optional pre-filter that can skip candidate proxies before the exact overlap test.</param>
    /// <param name="post">Optional post-filter that can reject exact results.</param>
    /// <param name="results">Cleared, then filled with every accepted overlap, in no particular order.</param>
    /// <returns>The number of accepted overlaps.</returns>
    public int Overlap<T>(in T support, in JQuaternion orientation, in JVector position,
        OverlapFilterPre? pre, OverlapFilterPost? post, List<OverlapResult> results)
        where T : ISupportMappable
    {
        results.Clear();
        if (root == NullNode) return 0;

        ShapeHelper.CalculateBoundingBox(support, orientation, position, out JBoundingBox box);
        var treeBox = new TreeBox(box);

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
                    if (node.Proxy is not IOverlapTestable overlapTestable) continue;
                    if (JBoundingBox.Disjoint(node.Proxy.WorldBoundingBox, box)) continue;
                    if (pre != null && !pre(node.Proxy)) continue;

                    OverlapResult result = default;
                    if (!overlapTestable.Overlap(support, orientation, position,
                            out result.PointA, out result.PointB, out result.Normal, out result.Penetration)) continue;
                    result.Entity = node.Proxy;

                    if (post != null && !post(result)) continue;
                    results.Add(result);
                    continue;
                }

                if (!TreeBox.Disjoint(nodes[node.Left].ExpandedBox, treeBox)) stack.Push(node.Left);
                if (!TreeBox.Disjoint(nodes[node.Right].ExpandedBox, treeBox)) stack.Push(node.Right);
            }
        }
        finally
        {
            PopTo(stack, baseCount);
        }

        return results.Count;
    }
}
