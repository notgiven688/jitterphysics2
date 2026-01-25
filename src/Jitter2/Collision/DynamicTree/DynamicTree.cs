/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitter2.DataStructures;
using Jitter2.LinearMath;
using Jitter2.Parallelization;

namespace Jitter2.Collision;

/// <summary>
/// Represents a dynamic AABB tree for broadphase collision detection.
/// </summary>
/// <remarks>
/// <para>
/// Uses a bounding volume hierarchy with Surface Area Heuristic (SAH) for efficient
/// insertion and query operations. Supports incremental updates for moving objects.
/// </para>
/// <para>
/// Complexity: O(log n) for insertion/removal, O(n) worst-case for overlap queries.
/// </para>
/// </remarks>
public partial class DynamicTree
{
    private struct OverlapEnumerationParam
    {
        public Action<IDynamicTreeProxy, IDynamicTreeProxy> Action;
        public Parallel.Batch Batch;
    }

    private readonly PartitionedSet<IDynamicTreeProxy> proxies = [];

    private readonly SlimBag<IDynamicTreeProxy> movedProxies = [];

    /// <summary>
    /// Gets a read-only view of all proxies registered with this tree.
    /// </summary>
    public ReadOnlyPartitionedSet<IDynamicTreeProxy> Proxies => new(proxies);

    private readonly PairHashSet potentialPairs = [];

    /// <summary>
    /// Sentinel value indicating a null/invalid node index.
    /// </summary>
    public const int NullNode = -1;

    /// <summary>
    /// Initial capacity of the internal node array.
    /// </summary>
    public const int InitialSize = 1024;

    /// <summary>
    /// Fraction of the potential pairs hash set to scan per update for pruning invalid entries.
    /// A value of 128 means 1/128th of the hash set is scanned each update.
    /// </summary>
    public const int PruningFraction = 128;

    /// <summary>
    /// Specifies the factor by which the bounding box in the dynamic tree structure is expanded. The expansion is calculated as
    /// <see cref="IDynamicTreeProxy.Velocity"/> * ExpandFactor * (1 + alpha), where alpha is a pseudo-random number in the range [0, 1].
    /// </summary>
    public const Real ExpandFactor = (Real)0.1;

    /// <summary>
    /// Specifies a small additional expansion of the bounding box which is constant.
    /// </summary>
    public const Real ExpandEps = (Real)0.1;

    /// <summary>
    /// Represents a node in the AABB tree.
    /// </summary>
    public struct Node
    {
        /// <summary>Index of the left child node, or <see cref="NullNode"/> if this is a leaf.</summary>
        public int Left;

        /// <summary>Index of the right child node, or <see cref="NullNode"/> if this is a leaf.</summary>
        public int Right;

        /// <summary>Index of the parent node, or <see cref="NullNode"/> if this is the root.</summary>
        public int Parent;

        /// <summary>
        /// The expanded bounding box of this node, used for broadphase culling.
        /// For leaf nodes, this is the proxy's bounding box expanded by velocity and a margin.
        /// For internal nodes, this is the union of its children's boxes.
        /// </summary>
        public TreeBox ExpandedBox;

        /// <summary>
        /// The proxy associated with this node, or <c>null</c> for internal nodes.
        /// </summary>
        public IDynamicTreeProxy? Proxy;

        /// <summary>
        /// When set, forces the node to be updated in the next <see cref="DynamicTree.Update"/> call,
        /// even if its bounding box hasn't changed.
        /// </summary>
        public bool ForceUpdate;

        /// <summary>
        /// Returns <c>true</c> if this is a leaf node (has an associated proxy).
        /// </summary>
        public readonly bool IsLeaf => Proxy != null;
    }

    /// <summary>
    /// The internal array of tree nodes. Exposed for advanced scenarios such as debugging or visualization.
    /// </summary>
    /// <remarks>
    /// This array may be resized during tree operations. Do not cache references to elements.
    /// </remarks>
    public Node[] Nodes = new Node[InitialSize];

    private readonly Stack<int> freeNodes = [];
    private int nodePointer = -1;
    private int root = NullNode;

    /// <summary>
    /// Gets the index of the root node, or <see cref="NullNode"/> if the tree is empty.
    /// </summary>
    public int Root => root;

    /// <summary>
    /// Gets or sets the filter function used to exclude proxy pairs from collision detection.
    /// </summary>
    /// <remarks>
    /// The filter is called during overlap enumeration. Return <c>false</c> to exclude a pair.
    /// In Jitter, this is typically used to exclude shapes belonging to the same rigid body.
    /// </remarks>
    public Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> Filter { get; set; }

    private readonly Action<OverlapEnumerationParam> enumerateOverlaps;
    private readonly Action<Parallel.Batch> updateBoundingBoxes;
    private readonly Action<Parallel.Batch> scanForMovedProxies;
    private readonly Action<Parallel.Batch> scanForOverlaps;

    private readonly Random random = new(1234);
    private readonly Func<double> rndFunc;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicTree"/> class.
    /// </summary>
    /// <param name="filter">A collision filter function, used in Jitter to exclude collisions between Shapes belonging
    /// to the same body. The collision is filtered out if the function returns false.</param>
    public DynamicTree(Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> filter)
    {
        enumerateOverlaps = EnumerateOverlapsCallback;
        updateBoundingBoxes = UpdateBoundingBoxesCallback;
        scanForMovedProxies = ScanForMovedProxies;
        scanForOverlaps = ScanForOverlapsCallback;

        rndFunc = () => random.NextDouble();

        Filter = filter;
    }

    /// <summary>
    /// Profiling buckets for <see cref="DebugTimings"/>, representing stages of <see cref="Update"/>.
    /// </summary>
    public enum Timings
    {
        /// <summary>Time spent removing stale pairs from the potential pairs set.</summary>
        PruneInvalidPairs,

        /// <summary>Time spent updating proxy bounding boxes.</summary>
        UpdateBoundingBoxes,

        /// <summary>Time spent scanning for proxies that moved outside their expanded boxes.</summary>
        ScanMoved,

        /// <summary>Time spent reinserting moved proxies into the tree.</summary>
        UpdateProxies,

        /// <summary>Time spent scanning for new overlapping pairs.</summary>
        ScanOverlaps,

        /// <summary>Sentinel value for array sizing. Not a real timing bucket.</summary>
        Last
    }

    /// <summary>
    /// Contains timings for the stages of the last call to <see cref="Update"/>.
    /// Values are in milliseconds. Index using <c>(int)Timings.XYZ</c>.
    /// </summary>
    public readonly double[] DebugTimings = new double[(int)Timings.Last];

    /// <summary>
    /// Gets the number of updated proxies during the last call to <see cref="Update"/>.
    /// </summary>
    public int UpdatedProxyCount => movedProxies.Count;

    /// <summary>
    /// Retrieve information of the size and filling of the internal hash set used to
    /// store potential overlaps.
    /// </summary>
    public (int TotalSize, int Count) HashSetInfo => (potentialPairs.Slots.Length, potentialPairs.Count);

    private void EnumerateOverlapsCallback(OverlapEnumerationParam parameter)
    {
        var batch = parameter.Batch;

        for (int e = batch.Start; e < batch.End; e++)
        {
            var node = potentialPairs.Slots[e];
            if (node.ID == 0) continue;

            var proxyA = Nodes[node.ID1].Proxy;
            var proxyB = Nodes[node.ID2].Proxy;

            if(proxyA == null || proxyB == null) continue;
            if(!Filter(proxyA, proxyB)) continue;

            if (!JBoundingBox.Disjoint(proxyA.WorldBoundingBox, proxyB.WorldBoundingBox))
            {
                parameter.Action(proxyA, proxyB);
            }
        }
    }

    /// <summary>
    /// Enumerates all potential collision pairs and invokes the specified action for each.
    /// </summary>
    /// <param name="action">The action to invoke for each overlapping pair.</param>
    /// <param name="multiThread">If <c>true</c>, uses multithreading for enumeration.</param>
    public void EnumerateOverlaps(Action<IDynamicTreeProxy, IDynamicTreeProxy> action, bool multiThread = false)
    {
        OverlapEnumerationParam overlapEnumerationParam;
        overlapEnumerationParam.Action = action;

        int slotsLength = potentialPairs.Slots.Length;

        if (multiThread)
        {
            var tpi = ThreadPool.Instance;

            // Typically, this is the first multithreaded phase of a simulation step.
            // Threads may still be asleep, so we use multiple tasks per thread
            // to improve load distribution as they wake up.
            const int taskMultiplier = 6;
            int taskCount = tpi.ThreadCount * taskMultiplier;

            for (int i = 0; i < taskCount; i++)
            {
                Parallel.GetBounds(slotsLength, taskCount, i, out int start, out int end);
                overlapEnumerationParam.Batch = new Parallel.Batch(start, end);
                ThreadPool.Instance.AddTask(enumerateOverlaps, overlapEnumerationParam);
            }

            tpi.Execute();
        }
        else
        {
            overlapEnumerationParam.Batch = new Parallel.Batch(0, slotsLength);
            EnumerateOverlapsCallback(overlapEnumerationParam);
        }
    }

    /// <summary>
    /// Updates all active proxies in the tree.
    /// </summary>
    /// <param name="multiThread">If <c>true</c>, uses multithreading for the update.</param>
    /// <param name="dt">The timestep in seconds, used for velocity-based bounding box expansion.</param>
    public void Update(bool multiThread, Real dt)
    {
        long time = Stopwatch.GetTimestamp();
        double invFrequency = 1.0d / Stopwatch.Frequency;

        void SetTime(Timings type)
        {
            long ctime = Stopwatch.GetTimestamp();
            double delta = (ctime - time) * 1000.0d;
            DebugTimings[(int)type] = delta * invFrequency;
            time = ctime;
        }

        this.stepDt = dt;

        Tracer.ProfileBegin(TraceName.PruneInvalidPairs);
        PruneInvalidPairs();
        Tracer.ProfileEnd(TraceName.PruneInvalidPairs);
        SetTime(Timings.PruneInvalidPairs);

        if (multiThread)
        {
            Tracer.ProfileBegin(TraceName.UpdateBoundingBoxes);
            proxies.ParallelForBatch(256, updateBoundingBoxes);
            Tracer.ProfileEnd(TraceName.UpdateBoundingBoxes);
            SetTime(Timings.UpdateBoundingBoxes);

            Tracer.ProfileBegin(TraceName.ScanMoved);
            movedProxies.Clear();
            proxies.ParallelForBatch(24, scanForMovedProxies);
            Tracer.ProfileEnd(TraceName.ScanMoved);
            SetTime(Timings.ScanMoved);

            Tracer.ProfileBegin(TraceName.UpdateProxies);
            for (int i = 0; i < movedProxies.Count; i++)
            {
                InternalAddRemoveProxy(movedProxies[i]);
            }
            Tracer.ProfileEnd(TraceName.UpdateProxies);
            SetTime(Timings.UpdateProxies);

            Tracer.ProfileBegin(TraceName.ScanOverlaps);
            movedProxies.ParallelForBatch(24, scanForOverlaps);
            Tracer.ProfileEnd(TraceName.ScanOverlaps);
            SetTime(Timings.ScanOverlaps);
        }
        else
        {
            Tracer.ProfileBegin(TraceName.UpdateBoundingBoxes);
            var batch = new Parallel.Batch(0, proxies.ActiveCount);
            UpdateBoundingBoxesCallback(batch);
            Tracer.ProfileEnd(TraceName.UpdateBoundingBoxes);
            SetTime(Timings.UpdateBoundingBoxes);

            Tracer.ProfileBegin(TraceName.ScanMoved);
            movedProxies.Clear();
            ScanForMovedProxies(batch);
            Tracer.ProfileEnd(TraceName.ScanMoved);
            SetTime(Timings.ScanMoved);

            Tracer.ProfileBegin(TraceName.UpdateProxies);
            for (int i = 0; i < movedProxies.Count; i++)
            {
                InternalAddRemoveProxy(movedProxies[i]);
            }
            Tracer.ProfileEnd(TraceName.UpdateProxies);
            SetTime(Timings.UpdateProxies);

            Tracer.ProfileBegin(TraceName.ScanOverlaps);
            ScanForOverlapsCallback(new Parallel.Batch(0, movedProxies.Count));
            Tracer.ProfileEnd(TraceName.ScanOverlaps);
            SetTime(Timings.ScanOverlaps);
        }

        // Make sure we do not hold too many dangling references
        // in the internal array of the SlimBag<T> data structure which might
        // prevent GC. But do only free them one-by-one to prevent overhead.
        movedProxies.TrackAndNullOutOne();
    }

    private Real stepDt;

    private void UpdateBoundingBoxesCallback(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            var proxy = proxies[i];
            if (proxy is IUpdatableBoundingBox sh) sh.UpdateWorldBoundingBox(stepDt);
        }
    }

    /// <summary>
    /// Forces an immediate update of a single proxy in the tree.
    /// </summary>
    /// <typeparam name="T">The proxy type.</typeparam>
    /// <param name="proxy">The proxy to update.</param>
    public void Update<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        if (proxy is IUpdatableBoundingBox sh) sh.UpdateWorldBoundingBox();
        OverlapCheckRemove(root, proxy.NodePtr);
        InternalRemoveProxy(proxy);
        InternalAddProxy(proxy);
        OverlapCheckAdd(root, proxy.NodePtr);
    }

    /// <summary>
    /// Adds a proxy to the tree.
    /// </summary>
    /// <typeparam name="T">The proxy type.</typeparam>
    /// <param name="proxy">The proxy to add.</param>
    /// <param name="active">If <c>true</c>, the proxy is tracked for movement each update.</param>
    /// <exception cref="InvalidOperationException">Thrown if the proxy is already in this tree.</exception>
    public void AddProxy<T>(T proxy, bool active = true) where T : class, IDynamicTreeProxy
    {
        if (proxies.Contains(proxy))
        {
            throw new InvalidOperationException(
                $"The proxy '{proxy}' has already been added to this tree instance.");
        }

        // 2^53 (approx 9e15) is the limit where double-precision values lose integer precision
        // (i.e., x + 1.0 == x). Beyond this surface area, the Surface Area Heuristic (SAH)
        // cannot detect small changes, causing the tree balancing to degrade.
        //
        // Note: Since TreeBox calculates surface area using 'double', this assertion
        // is valid and necessary for both Single (float) and Double (double) precision builds.
        if (proxy.WorldBoundingBox.GetSurfaceArea() > 9.007e15)
        {
            throw new InvalidOperationException(
                $"Added extremely large proxy to dynamic tree. Surface Area exceeds double precision limits (2^53).");
        }

        InternalAddProxy(proxy);
        OverlapCheckAdd(root, proxy.NodePtr);
        proxies.Add(proxy, active);
    }

    /// <summary>
    /// Checks whether the specified proxy is currently active.
    /// </summary>
    /// <typeparam name="T">The proxy type.</typeparam>
    /// <param name="proxy">The proxy to check.</param>
    /// <returns><c>true</c> if the proxy is active; otherwise, <c>false</c>.</returns>
    public bool IsActive<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        return proxies.IsActive(proxy);
    }

    /// <summary>
    /// Marks a proxy as active, causing it to be tracked for movement during updates.
    /// </summary>
    /// <typeparam name="T">The proxy type.</typeparam>
    /// <param name="proxy">The proxy to activate.</param>
    public void ActivateProxy<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        if (proxies.MoveToActive(proxy))
        {
            Nodes[proxy.NodePtr].ForceUpdate = true;
        }
    }

    /// <summary>
    /// Marks a proxy as inactive, assuming it will not move outside its expanded bounding box.
    /// </summary>
    /// <typeparam name="T">The proxy type.</typeparam>
    /// <param name="proxy">The proxy to deactivate.</param>
    public void DeactivateProxy<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        proxies.MoveToInactive(proxy);
    }

    /// <summary>
    /// Removes a proxy from the tree.
    /// </summary>
    /// <param name="proxy">The proxy to remove.</param>
    /// <exception cref="InvalidOperationException">Thrown if the proxy is not in this tree.</exception>
    public void RemoveProxy(IDynamicTreeProxy proxy)
    {
        if (!proxies.Contains(proxy))
        {
            throw new InvalidOperationException(
                $"The proxy '{proxy}' is not registered with this tree instance.");
        }

        OverlapCheckRemove(root, proxy.NodePtr);
        InternalRemoveProxy(proxy);
        proxy.NodePtr = NullNode;
        proxies.Remove(proxy);
    }

    /// <summary>
    /// Calculates the SAH cost of the tree (sum of all node surface areas).
    /// </summary>
    /// <returns>The total cost. Lower values indicate a more balanced tree.</returns>
    public double CalculateCost()
    {
        return Cost(ref Nodes[root]);
    }

    /// <summary>
    /// Enumerates all axis-aligned bounding boxes in the tree.
    /// </summary>
    /// <param name="action">The action to perform on each bounding box and node height in the tree.</param>
    public void EnumerateTreeBoxes(Action<TreeBox, int> action)
    {
        if (root == -1) return;
        EnumerateTreeBoxes(ref Nodes[root], action);
    }

    private void EnumerateTreeBoxes(ref Node node, Action<TreeBox, int> action, int depth = 1)
    {
        action(node.ExpandedBox, depth);
        if (node.IsLeaf) return;

        EnumerateTreeBoxes(ref Nodes[node.Left], action, depth + 1);
        EnumerateTreeBoxes(ref Nodes[node.Right], action, depth + 1);
    }

    private uint stepper;

    /// <summary>
    /// Removes entries from the internal bookkeeping which are both marked as inactive or
    /// whose expanded bounding box do not overlap any longer.
    /// Only searches a small subset of all elements per call to reduce overhead.
    /// </summary>
    private void PruneInvalidPairs()
    {
        stepper += 1;

        for (int i = 0; i < potentialPairs.Slots.Length / PruningFraction; i++)
        {
            int t = (int)((i * PruningFraction + stepper) % potentialPairs.Slots.Length);

            var n = potentialPairs.Slots[t];
            if (n.ID == 0) continue;

            var proxyA = Nodes[n.ID1].Proxy;
            var proxyB = Nodes[n.ID2].Proxy;

            if (proxyA != null && proxyB != null &&
                !TreeBox.Disjoint(Nodes[proxyA.NodePtr].ExpandedBox, Nodes[proxyB.NodePtr].ExpandedBox) &&
                (IsActive(proxyA) || IsActive(proxyB)))
            {
                continue;
            }

            potentialPairs.Remove(t);
            i -= 1;
        }
    }

    [ThreadStatic] private static Stack<int>? _stack;

    /// <summary>
    /// Queries the tree for proxies intersecting a ray.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="hits">Collection to store intersected proxies.</param>
    /// <param name="rayOrigin">The origin of the ray.</param>
    /// <param name="rayDirection">The direction of the ray.</param>
    public void Query<T>(T hits, JVector rayOrigin, JVector rayDirection) where T : ICollection<IDynamicTreeProxy>
    {
        _stack ??= new Stack<int>(256);
        _stack.Push(root);

        while (_stack.Count > 0)
        {
            int pop = _stack.Pop();

            ref Node node = ref Nodes[pop];

            if (node.IsLeaf)
            {
                if (node.Proxy!.WorldBoundingBox.RayIntersect(rayOrigin, rayDirection, out _))
                {
                    hits.Add(node.Proxy);
                }

                continue;
            }

            ref Node leftNode = ref Nodes[node.Left];
            ref Node rightNode = ref Nodes[node.Right];

            bool leftHit = leftNode.ExpandedBox.RayIntersect(rayOrigin, rayDirection, out _);
            bool rightHit = rightNode.ExpandedBox.RayIntersect(rayOrigin, rayDirection, out _);

            if (leftHit) _stack.Push(node.Left);
            if (rightHit) _stack.Push(node.Right);
        }
    }

    /// <summary>
    /// Queries the tree for proxies overlapping an axis-aligned bounding box.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="hits">Collection to store overlapping proxies.</param>
    /// <param name="box">The bounding box to query.</param>
    public void Query<T>(T hits, in JBoundingBox box) where T : ICollection<IDynamicTreeProxy>
    {
        var sbox = new TreeBox(box);

        _stack ??= new Stack<int>(256);

        _stack.Push(root);

        while (_stack.Count > 0)
        {
            int index = _stack.Pop();

            Node node = Nodes[index];

            if (node.IsLeaf)
            {
                if (!JBoundingBox.Disjoint(node.Proxy!.WorldBoundingBox, box))
                {
                    hits.Add(node.Proxy);
                }
            }
            else
            {
                int child1 = Nodes[index].Left;
                int child2 = Nodes[index].Right;

                if (!TreeBox.Disjoint(Nodes[child1].ExpandedBox, sbox))
                    _stack.Push(child1);

                if (!TreeBox.Disjoint(Nodes[child2].ExpandedBox, sbox))
                    _stack.Push(child2);
            }
        }

        _stack.Clear();
    }

    readonly List<IDynamicTreeProxy> tempList = new();

    /// <summary>
    /// Optimizes the tree structure by randomly reinserting proxies.
    /// </summary>
    /// <param name="sweeps">Number of optimization passes. Must be greater than zero.</param>
    /// <param name="chance">Probability of reinserting each proxy per sweep. Range: [0, 1].</param>
    /// <param name="incremental">If <c>false</c>, all proxies are reinserted in random order on the first sweep.</param>
    public void Optimize(int sweeps = 100, Real chance = (Real)0.01, bool incremental = false)
    {
        Optimize(rndFunc, sweeps, chance, incremental);
    }

    /// <inheritdoc cref="Optimize(int, Real, bool)"/>
    /// <param name="getNextRandom">A function returning random values in [0, 1).</param>
    public void Optimize(Func<double> getNextRandom, int sweeps, Real chance, bool incremental)
    {
        if (sweeps <= 0) throw new ArgumentOutOfRangeException(nameof(sweeps), "Sweeps must be greater than zero.");
        if (chance is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(chance), "Chance must be between 0 and 1.");

        for (int e = 0; e < sweeps; e++)
        {
            bool takeAll = (e == 0) && !incremental;

            for (int i = 0; i < proxies.Count; i++)
            {
                if (!takeAll && getNextRandom() > chance) continue;

                var proxy = proxies[i];
                tempList.Add(proxy);
                OverlapCheckRemove(root, proxy.NodePtr);
                InternalRemoveProxy(proxy);
            }

            // Fisher-Yates shuffle
            int n = tempList.Count;

            for (int i = n - 1; i > 0; i--)
            {
                double scaledValue = getNextRandom() * (i + 1);
                int j = (int)scaledValue;
                (tempList[i], tempList[j]) = (tempList[j], tempList[i]);
            }

            foreach (var proxy in tempList)
            {
                InternalAddProxy(proxy);
                OverlapCheckAdd(root, proxy.NodePtr);
            }

            tempList.Clear();
        }

        if (!incremental)
        {
            // In non-incremental mode, the first sweep processes all proxies and may
            // cause tempList to grow significantly. Since we're unlikely to need that
            // capacity again, we trim the excess to reduce memory usage.
            tempList.TrimExcess();
        }
    }

    private int AllocateNode()
    {
        if (freeNodes.Count > 0)
        {
            return freeNodes.Pop();
        }

        nodePointer += 1;
        if (nodePointer == Nodes.Length)
        {
            Array.Resize(ref Nodes, Nodes.Length * 2);
            Logger.Information("{0}: Resized array of tree to {1} elements.", nameof(DynamicTree), Nodes.Length);
        }

        return nodePointer;
    }

    private void FreeNode(int node)
    {
        Nodes[node].Proxy = null!;
        freeNodes.Push(node);
    }

    private double Cost(ref Node node)
    {
        if (node.IsLeaf)
        {
            return node.ExpandedBox.GetSurfaceArea();
        }

        return node.ExpandedBox.GetSurfaceArea() + Cost(ref Nodes[node.Left]) + Cost(ref Nodes[node.Right]);
    }

    private void OverlapCheckAdd(int index, int node)
    {
        if (Nodes[index].IsLeaf)
        {
            if (node == index) return;
            if (!Filter(Nodes[node].Proxy!, Nodes[index].Proxy!)) return;
            potentialPairs.ConcurrentAdd(new PairHashSet.Pair(index, node));
        }
        else
        {
            int child1 = Nodes[index].Left;
            int child2 = Nodes[index].Right;

            if (!TreeBox.Disjoint(Nodes[child1].ExpandedBox, Nodes[node].ExpandedBox))
                OverlapCheckAdd(child1, node);

            if (!TreeBox.Disjoint(Nodes[child2].ExpandedBox, Nodes[node].ExpandedBox))
                OverlapCheckAdd(child2, node);
        }
    }

    private void OverlapCheckRemove(int index, int node)
    {
        if (Nodes[index].IsLeaf)
        {
            if (node == index) return;
            if (!Filter(Nodes[node].Proxy!, Nodes[index].Proxy!)) return;
            potentialPairs.Remove(new PairHashSet.Pair(index, node));
        }
        else
        {
            int child1 = Nodes[index].Left;
            int child2 = Nodes[index].Right;

            if (!TreeBox.Disjoint(Nodes[child1].ExpandedBox, Nodes[node].ExpandedBox))
                OverlapCheckRemove(child1, node);

            if (!TreeBox.Disjoint(Nodes[child2].ExpandedBox, Nodes[node].ExpandedBox))
                OverlapCheckRemove(child2, node);
        }
    }

    private void ScanForMovedProxies(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            var proxy = proxies[i];

            ref var node = ref Nodes[proxy.NodePtr];

            if (node.ForceUpdate || !node.ExpandedBox.Contains(proxy.WorldBoundingBox))
            {
                node.ForceUpdate = false;
                movedProxies.ConcurrentAdd(proxy);
            }
        }
    }

    private void ScanForOverlapsCallback(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            OverlapCheckAdd(root, movedProxies[i].NodePtr);
        }
    }

    private static void ExpandBoundingBox(ref JBoundingBox box, in JVector direction)
    {
        if (direction.X < (Real)0.0) box.Min.X += direction.X;
        else box.Max.X += direction.X;

        if (direction.Y < (Real)0.0) box.Min.Y += direction.Y;
        else box.Max.Y += direction.Y;

        if (direction.Z < (Real)0.0) box.Min.Z += direction.Z;
        else box.Max.Z += direction.Z;

        box.Min -= new JVector(ExpandEps);
        box.Max += new JVector(ExpandEps);
    }

    private void InternalAddRemoveProxy(IDynamicTreeProxy proxy)
    {
        JBoundingBox box = proxy.WorldBoundingBox;

        // We store the parent of the node which gets removed. This
        // information is later used to reinsert the node *keeping updates local*.
        int parent = RemoveLeaf(proxy.NodePtr);

        int index = proxy.NodePtr;

        Real pseudoRandomExt = (Real)random.NextDouble();

        ExpandBoundingBox(ref box, proxy.Velocity * ExpandFactor * ((Real)1.0 + pseudoRandomExt));

        Nodes[index].Proxy = proxy;
        proxy.NodePtr = index;

        Nodes[index].ExpandedBox = new TreeBox(box);

        // InsertLeaf takes 'where' as a hint, i.e. it still walks up the tree until
        // the new node is fully contained. Note: The insertion node could also be found when searching
        // from the root, since the search always descents into child nodes fully containing the new node.
        InsertLeaf(index, parent);
    }

    private void InternalAddProxy(IDynamicTreeProxy proxy)
    {
        JBoundingBox box = proxy.WorldBoundingBox;

        int index = AllocateNode();

        Nodes[index].Proxy = proxy;
        proxy.NodePtr = index;

        Nodes[index].ExpandedBox = new TreeBox(box);

        InsertLeaf(index, root);
    }

    private void InternalRemoveProxy(IDynamicTreeProxy proxy)
    {
        Debug.Assert(Nodes[proxy.NodePtr].IsLeaf);
        RemoveLeaf(proxy.NodePtr);
        FreeNode(proxy.NodePtr);
    }

    private int RemoveLeaf(int node)
    {
        if (node == root)
        {
            root = NullNode;
            return NullNode;
        }

        int parent = Nodes[node].Parent;
        int grandParent = Nodes[parent].Parent;

        int sibling = Nodes[parent].Left == node ? Nodes[parent].Right : Nodes[parent].Left;

        if (grandParent == NullNode)
        {
            root = sibling;
            Nodes[sibling].Parent = NullNode;
            FreeNode(parent);
            return root;
        }

        if (Nodes[grandParent].Left == parent) Nodes[grandParent].Left = sibling;
        else Nodes[grandParent].Right = sibling;

        Nodes[sibling].Parent = grandParent;
        FreeNode(parent);

        int index = grandParent;
        while (index != NullNode)
        {
            int left = Nodes[index].Left;
            int rght = Nodes[index].Right;

            ref TreeBox indexNode = ref Nodes[index].ExpandedBox;

            TreeBox treeBoxBefore = indexNode;
            TreeBox.CreateMerged(Nodes[left].ExpandedBox, Nodes[rght].ExpandedBox, out indexNode);
            if(TreeBox.Equals(treeBoxBefore, indexNode)) goto early_out;

            index = Nodes[index].Parent;
        }

        early_out:
        return grandParent;
    }

    private readonly PriorityQueue<int, double> priorityQueue = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int FindBest(int node, int where)
    {
        ref Node nb = ref Nodes[node];

        double rootMergedArea = TreeBox.MergedSurface(Nodes[where].ExpandedBox, nb.ExpandedBox);

        double bestCost = double.MaxValue;
        int currentBest = where;

        // value = node index, priority = full cost at that node
        priorityQueue.Enqueue(where, rootMergedArea);

        while (priorityQueue.TryDequeue(out int currentIndex, out double cost))
        {
            ref Node cn = ref Nodes[currentIndex];

            // Reconstruct inherited cost *before* this node:
            // cost = inhCostBefore + SA(merge(node, nb))
            double mergedHere = TreeBox.MergedSurface(cn.ExpandedBox, nb.ExpandedBox);
            double inhCostBeforeNode = cost - mergedHere;

            // Prune: even the lower bound is already worse than best
            if (inhCostBeforeNode > bestCost)
                continue;

            // This node is a candidate insertion position
            if (cost < bestCost)
            {
                bestCost = cost;
                currentBest = currentIndex;
            }

            if (cn.IsLeaf) continue;

            double oldSurface = cn.ExpandedBox.GetSurfaceArea();

            // Inherited cost *after* this node
            double inhCostAfterNode = cost - oldSurface;

            // Expand to children
            double leftMerged  = TreeBox.MergedSurface(Nodes[cn.Left].ExpandedBox, nb.ExpandedBox);
            double rightMerged = TreeBox.MergedSurface(Nodes[cn.Right].ExpandedBox, nb.ExpandedBox);

            double leftCost  = inhCostAfterNode + leftMerged;
            double rightCost = inhCostAfterNode + rightMerged;

            // Store only index + full cost; inhCost will be reconstructed on pop
            priorityQueue.Enqueue(cn.Left,  leftCost);
            priorityQueue.Enqueue(cn.Right, rightCost);
        }

        return currentBest;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int FindBestGreedy(int node, int where)
    {
        /*
        This method is adapted from Box2D.
        https://github.com/erincatto/box2d/blob/3a4f0da8374af61293a03021c9a0b3ebcfe67948/src/dynamic_tree.c#L187
        Modified from the original version.

        Box2D is available under the MIT license:

        MIT License

        Copyright (c) 2022 Erin Catto

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        */

        ref TreeBox nodeTreeBox = ref Nodes[node].ExpandedBox;

        double areaD = Nodes[node].ExpandedBox.GetSurfaceArea();
        double areaBase = Nodes[where].ExpandedBox.GetSurfaceArea();
        double directCost = TreeBox.MergedSurface(Nodes[where].ExpandedBox, nodeTreeBox);
        double inheritedCost = 0.0d;

        int bestSibling = where;
        double bestCost = directCost;

        while (!Nodes[where].IsLeaf)
        {
            int left = Nodes[where].Left;
            int right = Nodes[where].Right;

            double cost = directCost + inheritedCost;

            if (cost < bestCost)
            {
                bestSibling = where;
                bestCost = cost;
            }

            inheritedCost += directCost - areaBase;

            //
            // Cost of descending into left child
            //
            double lowerCostLeft = double.MaxValue;
            double directCostLeft = TreeBox.MergedSurface(Nodes[left].ExpandedBox, nodeTreeBox);
            double areaLeft = 0.0d;

            if (Nodes[left].IsLeaf)
            {
                // Left child is a leaf
                // Cost of creating new node and increasing area of node P
                double costLeft = directCostLeft + inheritedCost;

                // Need this here due to while condition above
                if (costLeft < bestCost)
                {
                    bestSibling = left;
                    bestCost = costLeft;
                }
            }
            else
            {
                // Left child is an internal node
                areaLeft = Nodes[left].ExpandedBox.GetSurfaceArea();

                // Lower bound cost of inserting under left child.
                lowerCostLeft = inheritedCost + directCostLeft + double.Min(areaD - areaLeft, 0.0d);
            }

            //
            // Cost of descending into right child
            //
            double lowerCostRight = double.MaxValue;
            double directCostRight = TreeBox.MergedSurface(Nodes[right].ExpandedBox, nodeTreeBox);
            double areaRight = 0.0d;

            if (Nodes[right].IsLeaf)
            {
                // Right child is a leaf
                double costRight = directCostRight + inheritedCost;

                if (costRight < bestCost)
                {
                    bestSibling = right;
                    bestCost = costRight;
                }
            }
            else
            {
                // Right child is an internal node
                areaRight = Nodes[right].ExpandedBox.GetSurfaceArea();
                lowerCostRight = inheritedCost + directCostRight + double.Min(areaD - areaRight, 0.0d);
            }

            // If neither subtree offers improvement, stop descending.
            if (bestCost <= lowerCostLeft && bestCost <= lowerCostRight)
                break;

            // Tie-break by proximity to the new node's center
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (lowerCostLeft == lowerCostRight)
            {
                var center = nodeTreeBox.Center;
                lowerCostLeft = (Nodes[left].ExpandedBox.Center - center).LengthSquared();
                lowerCostRight = (Nodes[right].ExpandedBox.Center - center).LengthSquared();
            }

            // Descend into whichever child is better
            if (lowerCostLeft < lowerCostRight)
            {
                where = left;
                areaBase = areaLeft;
                directCost = directCostLeft;
            }
            else
            {
                where = right;
                areaBase = areaRight;
                directCost = directCostRight;
            }
        }

        return bestSibling;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private int FindBestHeuristic(int node, int where)
    {
        ref TreeBox nodeTreeBox = ref Nodes[node].ExpandedBox;

        while (!Nodes[where].IsLeaf)
        {
            int left = Nodes[where].Left;
            int rght = Nodes[where].Right;

            double cost = 2.0d * Nodes[where].ExpandedBox.GetSurfaceArea();

            double leftCost, rightCost;

            if (Nodes[left].IsLeaf)
            {
                // cost of additional node
                leftCost = TreeBox.MergedSurface(Nodes[left].ExpandedBox, nodeTreeBox);
            }
            else
            {
                // cost of ascending
                double oldArea = Nodes[left].ExpandedBox.GetSurfaceArea();
                double newArea = TreeBox.MergedSurface(Nodes[left].ExpandedBox, nodeTreeBox);
                leftCost = newArea - oldArea;
            }

            if (Nodes[rght].IsLeaf)
            {
                rightCost = TreeBox.MergedSurface(Nodes[rght].ExpandedBox, nodeTreeBox);
            }
            else
            {
                double oldArea = Nodes[rght].ExpandedBox.GetSurfaceArea();
                double newArea = TreeBox.MergedSurface(Nodes[rght].ExpandedBox, nodeTreeBox);
                rightCost = newArea - oldArea;
            }

            if (cost < leftCost && cost < rightCost) break;

            where = leftCost < rightCost ? left : rght;
        }

        return where;
    }

    private void InsertLeaf(int node, int where)
    {
        if (root == NullNode)
        {
            root = node;
            Nodes[root].Parent = NullNode;
            return;
        }

        ref TreeBox nodeTreeBox = ref Nodes[node].ExpandedBox;

        while (where != root)
        {
            if (TreeBox.Contains(Nodes[where].ExpandedBox,nodeTreeBox))
            {
                break;
            }

            where = Nodes[where].Parent;
        }

        int insertionParent = Nodes[where].Parent;

        // search for the best sibling
        int sibling = FindBestGreedy(node, where);

        // create a new parent
        int oldParent = Nodes[sibling].Parent;
        int newParent = AllocateNode();

        Nodes[newParent].Parent = oldParent;

        if (oldParent != NullNode)
        {
            if (Nodes[oldParent].Left == sibling) Nodes[oldParent].Left = newParent;
            else Nodes[oldParent].Right = newParent;

            Nodes[newParent].Left = sibling;
            Nodes[newParent].Right = node;
            Nodes[sibling].Parent = newParent;
            Nodes[node].Parent = newParent;
        }
        else
        {
            Nodes[newParent].Left = sibling;
            Nodes[newParent].Right = node;
            Nodes[sibling].Parent = newParent;
            Nodes[node].Parent = newParent;
            root = newParent;
        }

        int index = Nodes[node].Parent;
        while (index != insertionParent)
        {
            int lft = Nodes[index].Left;
            int rgt = Nodes[index].Right;

            TreeBox.CreateMerged(Nodes[lft].ExpandedBox, Nodes[rgt].ExpandedBox, out Nodes[index].ExpandedBox);

            index = Nodes[index].Parent;
        }
    }
}