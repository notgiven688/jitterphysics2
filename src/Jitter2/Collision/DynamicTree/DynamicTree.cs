/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Jitter2.DataStructures;
using Jitter2.LinearMath;
using Jitter2.Parallelization;

namespace Jitter2.Collision;

/// <summary>
/// Represents a dynamic Axis Aligned Bounding Box (AABB) tree. The tree can be queried for potential overlapping pairs.
/// </summary>
public partial class DynamicTree
{
    private struct OverlapEnumerationParam
    {
        public Action<IDynamicTreeProxy, IDynamicTreeProxy> Action;
        public Parallel.Batch Batch;
    }

    private readonly PartitionedSet<IDynamicTreeProxy> proxies = [];

    private readonly SlimBag<IDynamicTreeProxy> movedProxies = [];

    public ReadOnlyPartitionedSet<IDynamicTreeProxy> Proxies => new(proxies);

    /// <summary>
    /// The PairHashSet that contains pairs representing potential collisions.
    /// </summary>
    private readonly PairHashSet potentialPairs = [];

    public const int NullNode = -1;
    public const int InitialSize = 1024;

    // Every update we search 1/PruningFraction of the potential pair hashset
    // for pairs which are inactive or no longer colliding and remove them.
    public const int PruningFraction = 128;

    /// <summary>
    /// Specifies the factor by which the bounding box in the dynamic tree structure is expanded. The expansion is calculated as
    /// <see cref="IDynamicTreeProxy.Velocity"/> * ExpandFactor * alpha, where alpha is a pseudo-random number in the range [1,2].
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
        public int Left, Right;
        public int Parent;

        /// <summary>
        /// The height of the tree if this was the root node.
        /// </summary>
        public TreeBox ExpandedBox;
        public IDynamicTreeProxy? Proxy;

        public bool ForceUpdate;

        public readonly bool IsLeaf => Proxy != null;
    }

    public Node[] Nodes = new Node[InitialSize];
    private readonly Stack<int> freeNodes = [];
    private int nodePointer = -1;
    private int root = NullNode;

    /// <summary>
    /// Gets the root of the dynamic tree.
    /// </summary>
    public int Root => root;

    public Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> Filter { get; set; }

    private readonly Action<OverlapEnumerationParam> enumerateOverlaps;
    private readonly Action<Parallel.Batch> updateBoundingBoxes;
    private readonly Action<Parallel.Batch> scanForMovedProxies;
    private readonly Action<Parallel.Batch> scanForOverlaps;

    private readonly Random random = new();

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

        Filter = filter;
    }

    public enum Timings
    {
        PruneInvalidPairs,
        UpdateBoundingBoxes,
        ScanMoved,
        UpdateProxies,
        ScanOverlaps,
        Last
    }

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

            if (JBoundingBox.NotDisjoint(proxyA.WorldBoundingBox, proxyB.WorldBoundingBox))
            {
                parameter.Action(proxyA, proxyB);
            }
        }
    }

    public void EnumerateOverlaps(Action<IDynamicTreeProxy, IDynamicTreeProxy> action, bool multiThread = false)
    {
        OverlapEnumerationParam overlapEnumerationParam;
        overlapEnumerationParam.Action = action;

        int slotsLength = potentialPairs.Slots.Length;

        if (multiThread)
        {
            var tpi = ThreadPool.Instance;
            int threadCount = tpi.ThreadCount;

            for (int i = 0; i < threadCount; i++)
            {
                Parallel.GetBounds(slotsLength, threadCount, i, out int start, out int end);
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
    /// Updates all entities that are marked as active in the active list.
    /// </summary>
    /// <param name="multiThread">A boolean indicating whether to perform a multi-threaded update.</param>
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

        PruneInvalidPairs();

        SetTime(Timings.PruneInvalidPairs);

        if (multiThread)
        {
            proxies.ParallelForBatch(256, updateBoundingBoxes);
            SetTime(Timings.UpdateBoundingBoxes);

            movedProxies.Clear();
            proxies.ParallelForBatch(24, scanForMovedProxies);
            SetTime(Timings.ScanMoved);

            for (int i = 0; i < movedProxies.Count; i++)
            {
                InternalAddRemoveProxy(movedProxies[i]);
            }

            SetTime(Timings.UpdateProxies);

            movedProxies.ParallelForBatch(24, scanForOverlaps);

            SetTime(Timings.ScanOverlaps);
        }
        else
        {
            var batch = new Parallel.Batch(0, proxies.ActiveCount);
            UpdateBoundingBoxesCallback(batch);
            SetTime(Timings.UpdateBoundingBoxes);

            movedProxies.Clear();
            ScanForMovedProxies(batch);
            SetTime(Timings.ScanMoved);

            for (int i = 0; i < movedProxies.Count; i++)
            {
                InternalAddRemoveProxy(movedProxies[i]);
            }

            SetTime(Timings.UpdateProxies);

            ScanForOverlapsCallback(new Parallel.Batch(0, movedProxies.Count));

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
    /// Updates the state of the specified entity within the dynamic tree structure.
    /// </summary>
    /// <param name="proxy">The entity to update.</param>
    public void Update<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        if (proxy is IUpdatableBoundingBox sh) sh.UpdateWorldBoundingBox();
        OverlapCheckRemove(root, proxy.NodePtr);
        InternalRemoveProxy(proxy);
        InternalAddProxy(proxy);
        OverlapCheckAdd(root, proxy.NodePtr);
    }

    /// <summary>
    /// Add an entity to the tree.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified <paramref name="proxy"/> has already been added to this tree.
    /// </exception>
    public void AddProxy<T>(T proxy, bool active = true) where T : class, IDynamicTreeProxy
    {
        if (proxies.Contains(proxy))
        {
            throw new InvalidOperationException(
                $"The proxy '{proxy}' has already been added to this tree instance.");
        }

        InternalAddProxy(proxy);
        OverlapCheckAdd(root, proxy.NodePtr);
        proxies.Add(proxy, active);
    }

    public bool IsActive<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        return proxies.IsActive(proxy);
    }

    /// <summary>
    /// The tree actively tracks the proxy.
    /// </summary>
    public void ActivateProxy<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        if (proxies.MoveToActive(proxy))
        {
            Nodes[proxy.NodePtr].ForceUpdate = true;
        }
    }

    /// <summary>
    /// The tree assumes that the proxy is not active, i.e., it does not move out
    /// of its expanded bounding box.
    /// </summary>
    public void DeactivateProxy<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        proxies.MoveToInactive(proxy);
    }

    /// <summary>
    /// Removes an entity from the tree.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified <paramref name="proxy"/> is not registered with the tree.
    /// </exception>
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
    /// Calculates the cost function of the tree.
    /// </summary>
    /// <returns>The calculated cost.</returns>
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
                TreeBox.NotDisjoint(Nodes[proxyA.NodePtr].ExpandedBox, Nodes[proxyB.NodePtr].ExpandedBox) &&
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
    /// Queries the tree to find proxies which intersect the specified ray.
    /// </summary>
    /// <param name="hits">An ICollection to store the entities found within the bounding box.</param>
    /// <param name="rayOrigin">The origin of the ray.</param>
    /// <param name="rayDirection">Direction of the ray.</param>
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
    /// Queries the tree to find entities within the specified axis-aligned bounding box.
    /// </summary>
    /// <param name="hits">An ICollection to store the entities found within the bounding box.</param>
    /// <param name="box">The axis-aligned bounding box used for the query.</param>
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
                if (JBoundingBox.NotDisjoint(node.Proxy!.WorldBoundingBox, box))
                {
                    hits.Add(node.Proxy);
                }
            }
            else
            {
                int child1 = Nodes[index].Left;
                int child2 = Nodes[index].Right;

                if (TreeBox.NotDisjoint(Nodes[child1].ExpandedBox, sbox))
                    _stack.Push(child1);

                if (TreeBox.NotDisjoint(Nodes[child2].ExpandedBox, sbox))
                    _stack.Push(child2);
            }
        }

        _stack.Clear();
    }

    readonly List<IDynamicTreeProxy> tempList = new();

    /// <summary>
    /// Randomly removes and adds entities to the tree to facilitate optimization.
    /// </summary>
    /// <param name="sweeps">The number of times to iterate over all proxies in the tree. Must be greater than zero.</param>
    /// <param name="chance">The chance of a proxy to be removed and re-added to the tree for each sweep. Must be between 0 and 1.</param>
    /// <param name="incremental">If false, all entities of the tree are removed and reinserted at random order during the first sweep (chance = 1).</param>
    public void Optimize(int sweeps = 100, Real chance = (Real)0.01, bool incremental = false)
    {
        Optimize(() => random.NextDouble(), sweeps, chance, incremental);
    }

    /// <inheritdoc cref="Optimize(int, Real, bool)" />
    /// <param name="getNextRandom">Delegate to create a sequence of random numbers.</param>
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
            Debug.Assert(node.ExpandedBox.GetSurfaceArea() < 1e8);
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

            if (TreeBox.NotDisjoint(Nodes[child1].ExpandedBox, Nodes[node].ExpandedBox))
                OverlapCheckAdd(child1, node);

            if (TreeBox.NotDisjoint(Nodes[child2].ExpandedBox, Nodes[node].ExpandedBox))
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

            if (TreeBox.NotDisjoint(Nodes[child1].ExpandedBox, Nodes[node].ExpandedBox))
                OverlapCheckRemove(child1, node);

            if (TreeBox.NotDisjoint(Nodes[child2].ExpandedBox, Nodes[node].ExpandedBox))
                OverlapCheckRemove(child2, node);
        }
    }

    private void ScanForMovedProxies(Parallel.Batch batch)
    {
        for (int i = batch.Start; i < batch.End; i++)
        {
            var proxy = proxies[i];

            ref var node = ref Nodes[proxy.NodePtr];

            if (node.ForceUpdate || !node.ExpandedBox.Encompasses(proxy.WorldBoundingBox))
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
        
        int parent = RemoveLeaf(proxy.NodePtr);

        int index = proxy.NodePtr;

        Real pseudoRandomExt = (Real)random.NextDouble();

        ExpandBoundingBox(ref box, proxy.Velocity * ExpandFactor * ((Real)1.0 + pseudoRandomExt));

        Nodes[index].Proxy = proxy;
        proxy.NodePtr = index;

        Nodes[index].ExpandedBox = new TreeBox(box);

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
        int sibling;

        if (Nodes[parent].Left == node) sibling = Nodes[parent].Right;
        else sibling = Nodes[parent].Left;

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
            if (TreeBox.Encompasses(Nodes[where].ExpandedBox,nodeTreeBox))
            {
                break;
            }

            where = Nodes[where].Parent;
        }
        
        int insertionParent = Nodes[where].Parent;

        // search for the best sibling
        int sibling = where;

        while (!Nodes[sibling].IsLeaf)
        {
            int left = Nodes[sibling].Left;
            int rght = Nodes[sibling].Right;

            double area = Nodes[sibling].ExpandedBox.GetSurfaceArea();

            double cost = 2.0d * area;
            double costl, costr;

            if (Nodes[left].IsLeaf)
            {
                costl = TreeBox.MergedSurface(Nodes[left].ExpandedBox, nodeTreeBox);
            }
            else
            {
                double oldArea = Nodes[left].ExpandedBox.GetSurfaceArea();
                double newArea = TreeBox.MergedSurface(Nodes[left].ExpandedBox, nodeTreeBox);
                costl = newArea - oldArea;
            }

            if (Nodes[rght].IsLeaf)
            {
                costr = TreeBox.MergedSurface(Nodes[rght].ExpandedBox, nodeTreeBox);
            }
            else
            {
                double oldArea = Nodes[rght].ExpandedBox.GetSurfaceArea();
                double newArea = TreeBox.MergedSurface(Nodes[rght].ExpandedBox, nodeTreeBox);
                costr = newArea - oldArea;
            }

            // if this is true, the choice is actually the best for the current candidate
            if (cost < costl && cost < costr) break;

            sibling = costl < costr ? left : rght;
        }

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