/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jitter2.Collision;

public partial class DynamicTree
{
    /// <summary>Default number of leaves reinserted by automatic tree maintenance.</summary>
    public const int DefaultIncrementalOptimizationBudget = 1;

    /// <summary>
    /// Gets or sets whether <see cref="Update(bool, Real)"/> reinserts a bounded number of
    /// leaves from the root and applies non-cost-increasing rotations to their affected paths. Enabled by default.
    /// </summary>
    public bool EnableAutomaticOptimization { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of leaves reinserted by each <see cref="Update(bool, Real)"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    public int AutomaticOptimizationBudget
    {
        get => automaticOptimizationBudget;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "The automatic optimization budget cannot be negative.");
            }

            automaticOptimizationBudget = value;
        }
    }

    private int automaticOptimizationBudget = DefaultIncrementalOptimizationBudget;
    private int incrementalOptimizationCursor;

    private readonly struct RotationCandidate(
        int promoted, int moved, int maximumImbalance, int height,
        int totalHeightChange, double costChange)
    {
        public readonly int Promoted = promoted;
        public readonly int Moved = moved;
        public readonly int MaximumImbalance = maximumImbalance;
        public readonly int Height = height;
        public readonly int TotalHeightChange = totalHeightChange;
        public readonly double CostChange = costChange;
    }

    /// <summary>
    /// Reinserts at most <paramref name="proxyBudget"/> leaves from the root.
    /// </summary>
    /// <param name="proxyBudget">Maximum number of leaves to reinsert.</param>
    /// <returns>The number of leaves reinserted.</returns>
    /// <remarks>
    /// A persistent cursor selects leaves without scanning the proxy collection. Leaf node
    /// indices, proxy activity and the potential-pair cache are preserved. Root reinsertion
    /// gradually removes topology history, while cost-constrained rotations may repair height
    /// imbalance without charging every ordinary tree mutation for rotation work.
    /// </remarks>
    private int OptimizeIncrementally(
        int proxyBudget = DefaultIncrementalOptimizationBudget)
    {
        if (proxyBudget < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(proxyBudget),
                "The proxy budget cannot be negative.");
        }

        int count = int.Min(proxyBudget, proxies.Count);

        for (int i = 0; i < count; i++)
        {
            if (incrementalOptimizationCursor >= proxies.Count)
            {
                incrementalOptimizationCursor = 0;
            }

            IDynamicTreeProxy proxy = proxies[incrementalOptimizationCursor++];
            int leaf = proxy.NodePtr;

            RemoveLeaf(leaf, balance: true);
            nodes[leaf].Parent = NullNode;
            InsertLeaf(leaf, root, balance: true);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RefitNode(int node)
    {
        Debug.Assert(!nodes[node].IsLeaf);

        int left = nodes[node].Left;
        int right = nodes[node].Right;

        TreeBox.CreateMerged(nodes[left].ExpandedBox, nodes[right].ExpandedBox,
            out nodes[node].ExpandedBox);
        nodes[node].LeafCount = nodes[left].LeafCount + nodes[right].LeafCount;
        nodes[node].Height = 1 + int.Max(nodes[left].Height, nodes[right].Height);
    }

    private void RefitAndBalanceAncestors(int node)
    {
        while (node != NullNode)
        {
            RefitNode(node);
            int subtreeRoot = BalanceNode(node);
            node = nodes[subtreeRoot].Parent;
        }
    }

    private void RefitAncestors(int node)
    {
        while (node != NullNode)
        {
            RefitNode(node);
            node = nodes[node].Parent;
        }
    }

    private int BalanceNode(int node)
    {
        if (nodes[node].IsLeaf) return node;
        return RestoreHeightBalanceWithoutIncreasingCost(node);
    }

    private int RestoreHeightBalanceWithoutIncreasingCost(int node)
    {
        int left = nodes[node].Left;
        int right = nodes[node].Right;
        int balance = nodes[right].Height - nodes[left].Height;

        if (balance is >= -1 and <= 1) return node;

        int promoted = balance > 1 ? right : left;
        Debug.Assert(!nodes[promoted].IsLeaf);

        RotationCandidate first = CreateRotationCandidate(
            node, promoted, nodes[promoted].Left);
        RotationCandidate second = CreateRotationCandidate(
            node, promoted, nodes[promoted].Right);

        bool firstAccepted = IsAcceptedHeightRotation(first);
        bool secondAccepted = IsAcceptedHeightRotation(second);

        if (!firstAccepted && !secondAccepted) return node;

        RotationCandidate selected = !firstAccepted ? second :
            !secondAccepted ? first : SelectBalancingRotation(first, second);
        int subtreeRoot = ApplyRotation(node, selected);

        // A newly inserted leaf may have been paired with an arbitrarily tall subtree.
        // In that case one rotation balances the new root but leaves the old root skewed.
        // Continue inside that child before checking the new root again. Either operation may
        // leave height imbalance in place when every applicable rotation would increase cost.
        RestoreHeightBalanceWithoutIncreasingCost(node);
        RefitNode(subtreeRoot);
        return RestoreHeightBalanceWithoutIncreasingCost(subtreeRoot);
    }

    private RotationCandidate CreateRotationCandidate(int node, int promoted, int moved)
    {
        int sibling = nodes[node].Left == promoted ? nodes[node].Right : nodes[node].Left;
        int stay = nodes[promoted].Left == moved ? nodes[promoted].Right : nodes[promoted].Left;

        int nodeHeight = 1 + int.Max(nodes[sibling].Height, nodes[moved].Height);
        int promotedHeight = 1 + int.Max(nodes[stay].Height, nodeHeight);
        int totalHeightChange = nodeHeight + promotedHeight -
            nodes[node].Height - nodes[promoted].Height;

        int nodeImbalance = int.Abs(nodes[sibling].Height - nodes[moved].Height);
        int promotedImbalance = int.Abs(nodes[stay].Height - nodeHeight);

        double newNodeArea = TreeBox.MergedSurface(
            nodes[sibling].ExpandedBox, nodes[moved].ExpandedBox);
        double costChange = newNodeArea - nodes[promoted].ExpandedBox.GetSurfaceArea();

        return new RotationCandidate(promoted, moved,
            int.Max(nodeImbalance, promotedImbalance), promotedHeight,
            totalHeightChange, costChange);
    }

    private static bool IsAcceptedHeightRotation(RotationCandidate candidate)
    {
        if (candidate.CostChange < 0.0d) return true;

        // Equal-cost rotations need a second strictly decreasing metric; otherwise two
        // equivalent topologies can rotate back and forth indefinitely.
        return candidate.CostChange == 0.0d && candidate.TotalHeightChange < 0;
    }

    private static RotationCandidate SelectBalancingRotation(
        RotationCandidate first, RotationCandidate second)
    {
        bool firstBalanced = first.MaximumImbalance <= 1;
        bool secondBalanced = second.MaximumImbalance <= 1;

        if (firstBalanced != secondBalanced) return firstBalanced ? first : second;

        if (first.MaximumImbalance != second.MaximumImbalance)
        {
            return first.MaximumImbalance < second.MaximumImbalance ? first : second;
        }

        if (first.Height != second.Height) return first.Height < second.Height ? first : second;
        return first.CostChange <= second.CostChange ? first : second;
    }

    private int ApplyRotation(int node, RotationCandidate candidate)
    {
        int promoted = candidate.Promoted;
        int moved = candidate.Moved;
        int parent = nodes[node].Parent;
        bool promotedWasLeft = nodes[node].Left == promoted;
        bool movedWasLeft = nodes[promoted].Left == moved;

        if (parent == NullNode)
        {
            root = promoted;
        }
        else if (nodes[parent].Left == node)
        {
            nodes[parent].Left = promoted;
        }
        else
        {
            Debug.Assert(nodes[parent].Right == node);
            nodes[parent].Right = promoted;
        }

        nodes[promoted].Parent = parent;
        nodes[node].Parent = promoted;
        nodes[moved].Parent = node;

        if (promotedWasLeft) nodes[node].Left = moved;
        else nodes[node].Right = moved;

        if (movedWasLeft) nodes[promoted].Left = node;
        else nodes[promoted].Right = node;

        RefitNode(node);
        RefitNode(promoted);
        return promoted;
    }
}
