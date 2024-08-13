/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Jitter2.DataStructures;
using Jitter2.LinearMath;
using Jitter2.Parallelization;

namespace Jitter2.Collision;

/// <summary>
/// Represents a dynamic Axis Aligned Bounding Box (AABB) tree. A hashset (refer to <see cref="PairHashSet"/>)
/// maintains a record of potential overlapping pairs.
/// </summary>
public class DynamicTree
{
    private volatile SlimBag<IDynamicTreeProxy>[] lists = Array.Empty<SlimBag<IDynamicTreeProxy>>();

    private readonly ActiveList<IDynamicTreeProxy> activeList = new();

    public readonly ReadOnlyActiveList<IDynamicTreeProxy> ActiveList;

    /// <summary>
    /// Gets the PairHashSet that contains pairs representing potential collisions. This should not be modified directly.
    /// </summary>
    public readonly PairHashSet PotentialPairs = new();

    public const int NullNode = -1;
    public const int InitialSize = 1024;

    /// <summary>
    /// Specifies the factor by which the bounding box in the dynamic tree structure is expanded. The expansion is calculated as
    /// <see cref="IDynamicTreeProxy.Velocity"/> * ExpandFactor * alpha, where alpha is a pseudo-random number in the range [1,2].
    /// </summary>
    public const float ExpandFactor = 0.1f;

    /// <summary>
    /// Specifies a small additional expansion of the bounding box in the AABB tree structure to prevent
    /// the creation of bounding boxes with zero volume.
    /// </summary>
    public const float ExpandEps = 0.01f;

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
        public int Height;

        public JBBox ExpandedBox;
        public IDynamicTreeProxy Proxy;

        public bool ForceUpdate;

        public readonly bool IsLeaf => Proxy != null;
    }

    public Node[] Nodes = new Node[InitialSize];
    private readonly Stack<int> freeNodes = new();
    private int nodePointer = -1;
    private int root = NullNode;

    /// <summary>
    /// Gets the root of the dynamic tree.
    /// </summary>
    public int Root => root;

    private readonly Action<Parallel.Batch> scanOverlapsPre;
    private readonly Action<Parallel.Batch> scanOverlapsPost;

    public Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> Filter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicTree"/> class.
    /// </summary>
    /// <param name="filter">A collision filter function, used in Jitter to exclude collisions between Shapes belonging to the same body. The collision is filtered out if the function returns false.</param>
    public DynamicTree(Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> filter)
    {
        scanOverlapsPre = batch =>
        {
            ScanForMovedProxies(batch);
            ScanForOverlaps(batch.BatchIndex, false);
        };

        ActiveList = new ReadOnlyActiveList<IDynamicTreeProxy>(activeList);

        scanOverlapsPost = batch => { ScanForOverlaps(batch.BatchIndex, true); };

        Filter = filter;
    }

    public enum Timings
    {
        ScanOverlapsPre,
        UpdateProxies,
        ScanOverlapsPost,
        Last
    }

    public readonly double[] DebugTimings = new double[(int)Timings.Last];

    private int updatedProxies;

    /// <summary>
    /// Gets the number of updated proxies.
    /// </summary>
    public int UpdatedProxies => updatedProxies;

    /// <summary>
    /// Updates all entities that are marked as active in the active list.
    /// </summary>
    /// <param name="multiThread">A boolean indicating whether to perform a multi-threaded update.</param>
    public void Update(bool multiThread)
    {
        long time = Stopwatch.GetTimestamp();
        double invFrequency = 1.0d / Stopwatch.Frequency;

        CheckBagCount();

        void SetTime(Timings type)
        {
            long ctime = Stopwatch.GetTimestamp();
            double delta = (ctime - time) * 1000.0d;
            DebugTimings[(int)type] = delta * invFrequency;
            time = ctime;
        }

        SetTime(Timings.ScanOverlapsPre);

        if (multiThread)
        {
            const int taskThreshold = 24;
            int numTasks = Math.Clamp(activeList.Active / taskThreshold, 1, ThreadPool.Instance.ThreadCount);
            Parallel.ForBatch(0, activeList.Active, numTasks, scanOverlapsPre);

            SetTime(Timings.ScanOverlapsPre);

            updatedProxies = 0;

            for (int ntask = 0; ntask < numTasks; ntask++)
            {
                var sl = lists[ntask];
                updatedProxies += sl.Count;

                for (int i = 0; i < sl.Count; i++)
                {
                    var proxy = sl[i];
                    InternalRemoveProxy(proxy);
                    InternalAddProxy(proxy);
                }
            }

            SetTime(Timings.UpdateProxies);

            Parallel.ForBatch(0, activeList.Active, numTasks, scanOverlapsPost);

            SetTime(Timings.ScanOverlapsPost);
        }
        else
        {
            scanOverlapsPre(new Parallel.Batch(0, activeList.Active));
            SetTime(Timings.ScanOverlapsPre);

            var sl = lists[0];
            for (int i = 0; i < sl.Count; i++)
            {
                IDynamicTreeProxy proxy = sl[i];
                InternalRemoveProxy(proxy);
                InternalAddProxy(proxy);
            }

            SetTime(Timings.UpdateProxies);

            scanOverlapsPost(new Parallel.Batch(0, activeList.Active));
            SetTime(Timings.ScanOverlapsPost);
        }
    }

    /// <summary>
    /// Updates the state of the specified entity within the dynamic tree structure.
    /// </summary>
    /// <param name="proxy">The entity to update.</param>
    public void Update<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        OverlapCheck(proxy, false);
        InternalRemoveProxy(proxy);
        InternalAddProxy(proxy);
        OverlapCheck(proxy, true);
    }

    /// <summary>
    /// Add an entity to the tree.
    /// </summary>
    public void AddProxy<T>(T proxy, bool active = true) where T : class, IDynamicTreeProxy
    {
        InternalAddProxy(proxy);
        OverlapCheck(root, proxy.NodePtr, true);
        activeList.Add(proxy, active);
    }

    public bool IsActive<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        return activeList.IsActive(proxy);
    }

    public void Activate<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        if (activeList.MoveToActive(proxy))
        {
            Nodes[proxy.NodePtr].ForceUpdate = true;
        }
    }

    public void Deactivate<T>(T proxy) where T : class, IDynamicTreeProxy
    {
        activeList.MoveToInactive(proxy);
    }

    /// <summary>
    /// Removes an entity from the tree.
    /// </summary>
    public void RemoveProxy(IDynamicTreeProxy proxy)
    {
        OverlapCheck(root, proxy.NodePtr, false);
        InternalRemoveProxy(proxy);
        proxy.NodePtr = NullNode;
        activeList.Remove(proxy);
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
    /// Get the height of the tree.
    /// </summary>
    public int Height => root == NullNode ? 0 : Nodes[root].Height;

    /// <summary>
    /// Enumerates all axis-aligned bounding boxes in the tree.
    /// </summary>
    /// <param name="action">The action to perform on each bounding box and node height in the tree.</param>
    public void EnumerateAABB(Action<JBBox, int> action)
    {
        if (root == -1) return;
        EnumerateAABB(ref Nodes[root], action);
    }

    private void EnumerateAABB(ref Node node, Action<JBBox, int> action, int depth = 1)
    {
        action(node.ExpandedBox, depth);
        if (node.IsLeaf) return;

        EnumerateAABB(ref Nodes[node.Left], action, depth + 1);
        EnumerateAABB(ref Nodes[node.Right], action, depth + 1);
    }

    private uint stepper;

    /// <summary>
    /// Removes entries from <see cref="PotentialPairs"/> which are both marked as inactive.
    /// Only searches a small subset of the pair hashset (1/128) per call to reduce
    /// overhead.
    /// </summary>
    public void TrimInactivePairs()
    {
        // We actually only search 1/128 of the whole potentialPairs Hashset for
        // potentially prunable contacts. No need to sweep through the whole hashset
        // every step.
        const int divisions = 128;
        stepper += 1;

        for (int i = 0; i < PotentialPairs.Slots.Length / divisions; i++)
        {
            int t = (int)((i * divisions + stepper) % PotentialPairs.Slots.Length);

            var n = PotentialPairs.Slots[t];
            if (n.ID == 0) continue;

            var proxyA = Nodes[n.ID1].Proxy;
            var proxyB = Nodes[n.ID2].Proxy;

            if (IsActive(proxyA) || IsActive(proxyB)) continue;

            PotentialPairs.Remove(t);
            i -= 1;
        }
    }

    [ThreadStatic] private static Stack<int>? stack;

    /// <summary>
    /// Queries the tree to find proxies which intersect the specified ray.
    /// </summary>
    /// <param name="hits">An ICollection to store the entities found within the bounding box.</param>
    /// <param name="rayOrigin">The origin of the ray.</param>
    /// <param name="rayDirection">Direction of the ray.</param>
    public void Query<T>(T hits, JVector rayOrigin, JVector rayDirection) where T : ICollection<IDynamicTreeProxy>
    {
        stack ??= new Stack<int>(256);
        stack.Push(root);

        while (stack.Count > 0)
        {
            int pop = stack.Pop();

            ref Node node = ref Nodes[pop];

            if (node.IsLeaf)
            {
                if (node.Proxy.WorldBoundingBox.RayIntersect(rayOrigin, rayDirection, out _))
                {
                    hits.Add(node.Proxy);
                }

                continue;
            }

            ref Node leftNode = ref Nodes[node.Left];
            ref Node rightNode = ref Nodes[node.Right];

            bool leftHit = leftNode.ExpandedBox.RayIntersect(rayOrigin, rayDirection, out _);
            bool rightHit = rightNode.ExpandedBox.RayIntersect(rayOrigin, rayDirection, out _);

            if (leftHit) stack.Push(node.Left);
            if (rightHit) stack.Push(node.Right);
        }
    }

    /// <summary>
    /// Queries the tree to find entities within the specified axis-aligned bounding box.
    /// </summary>
    /// <param name="hits">An ICollection to store the entities found within the bounding box.</param>
    /// <param name="box">The axis-aligned bounding box used for the query.</param>
    public void Query<T>(T hits, in JBBox box) where T : ICollection<IDynamicTreeProxy>
    {
        stack ??= new Stack<int>(256);

        stack.Push(root);

        while (stack.Count > 0)
        {
            int index = stack.Pop();

            Node node = Nodes[index];

            if (node.IsLeaf)
            {
                if (node.Proxy.WorldBoundingBox.NotDisjoint(box))
                {
                    hits.Add(node.Proxy);
                }
            }
            else
            {
                int child1 = Nodes[index].Left;
                int child2 = Nodes[index].Right;

                if (Nodes[child1].ExpandedBox.NotDisjoint(box))
                    stack.Push(child1);

                if (Nodes[child2].ExpandedBox.NotDisjoint(box))
                    stack.Push(child2);
            }
        }

        stack.Clear();
    }

    private Random? optimizeRandom;

    /// <summary>
    /// Randomly removes and adds entities to the tree to facilitate optimization.
    /// </summary>
    /// <param name="sweeps">The number of optimization iterations to perform. The default value is 100.</param>
    public void Optimize(int sweeps = 100)
    {
        optimizeRandom ??= new Random(0);

        Stack<IDynamicTreeProxy> temp = new();
        for (int e = 0; e < sweeps; e++)
        {
            for (int i = 0; i < activeList.Count; i++)
            {
                var proxy = activeList[i];

                if (optimizeRandom.NextDouble() > 0.01d) continue;

                temp.Push(proxy);
                InternalRemoveProxy(proxy);
            }

            while (temp.Count > 0)
            {
                var proxy = temp.Pop();
                InternalAddProxy(proxy);
            }
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
            Trace.WriteLine($"Resized array of AABBTree to {Nodes.Length} elements.");
        }

        return nodePointer;
    }

    private void FreeNode(int node)
    {
        Nodes[node].Proxy = null!;
        freeNodes.Push(node);
    }

    private void CheckBagCount()
    {
        int numThreads = ThreadPool.Instance.ThreadCount;
        if (lists.Length != numThreads)
        {
            lists = new SlimBag<IDynamicTreeProxy>[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                lists[i] = new SlimBag<IDynamicTreeProxy>();
            }
        }
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

    private void OverlapCheck(IDynamicTreeProxy shape, bool add)
    {
        OverlapCheck(root, shape.NodePtr, add);
    }

    private void OverlapCheck(int index, int node, bool add)
    {
        if (Nodes[index].IsLeaf)
        {
            if (node == index) return;

            if (!Filter(Nodes[node].Proxy, Nodes[index].Proxy)) return;

            lock (PotentialPairs)
            {
                if (add) PotentialPairs.Add(new PairHashSet.Pair(index, node));
                else PotentialPairs.Remove(new PairHashSet.Pair(index, node));
            }
        }
        else
        {
            int child1 = Nodes[index].Left;
            int child2 = Nodes[index].Right;

            if (Nodes[child1].ExpandedBox.NotDisjoint(Nodes[node].ExpandedBox))
                OverlapCheck(child1, node, add);

            if (Nodes[child2].ExpandedBox.NotDisjoint(Nodes[node].ExpandedBox))
                OverlapCheck(child2, node, add);
        }
    }

    private void ScanForMovedProxies(Parallel.Batch batch)
    {
        var list = lists[batch.BatchIndex];
        list.Clear();

        for (int i = batch.Start; i < batch.End; i++)
        {
            var proxy = activeList[i];

            ref var node = ref Nodes[proxy.NodePtr];

            if (node.ForceUpdate || node.ExpandedBox.Contains(proxy.WorldBoundingBox) !=
                JBBox.ContainmentType.Contains)
            {
                node.ForceUpdate = false;
                list.Add(proxy);
            }

            // else proxy is well contained within the nodes expanded Box:
        }

        // Make sure we do not hold too many dangling references
        // in the internal array of the SlimBag<T> data structure which might
        // prevent GC. But do only free them one-by-one to prevent overhead.
        list.NullOutOne();
    }

    private void ScanForOverlaps(int fraction, bool add)
    {
        var sl = lists[fraction];
        for (int i = 0; i < sl.Count; i++)
        {
            OverlapCheck(root, sl[i].NodePtr, add);
        }
    }

    private static void ExpandBoundingBox(ref JBBox box, in JVector direction)
    {
        if (direction.X < 0.0f) box.Min.X += direction.X;
        else box.Max.X += direction.X;

        if (direction.Y < 0.0f) box.Min.Y += direction.Y;
        else box.Max.Y += direction.Y;

        if (direction.Z < 0.0f) box.Min.Z += direction.Z;
        else box.Max.Z += direction.Z;

        box.Min.X -= ExpandEps;
        box.Min.Y -= ExpandEps;
        box.Min.Z -= ExpandEps;

        box.Max.X += ExpandEps;
        box.Max.Y += ExpandEps;
        box.Max.Z += ExpandEps;
    }

    private static float GenerateRandom(ulong seed)
    {
        const uint a = 21_687_443;
        const uint b = 35_253_893;

        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;

        uint randomBits = (uint)seed * a + b;
        return MathF.Abs((float)randomBits / uint.MaxValue);
    }

    private void InternalAddProxy(IDynamicTreeProxy proxy)
    {
        JBBox box = proxy.WorldBoundingBox;

        int index = AllocateNode();
        float pseudoRandomExt = GenerateRandom((ulong)index);

        ExpandBoundingBox(ref box, proxy.Velocity * ExpandFactor * (1.0f + pseudoRandomExt));

        Nodes[index].Proxy = proxy;
        Nodes[index].Height = 1;
        proxy.NodePtr = index;

        Nodes[index].ExpandedBox = box;

        AddLeaf(index);
    }

    private void InternalRemoveProxy(IDynamicTreeProxy proxy)
    {
        Debug.Assert(Nodes[proxy.NodePtr].IsLeaf);

        RemoveLeaf(proxy.NodePtr);
        FreeNode(proxy.NodePtr);
    }

    private void RemoveLeaf(int node)
    {
        if (node == root)
        {
            root = NullNode;
            return;
        }

        int parent = Nodes[node].Parent;
        int grandParent = Nodes[parent].Parent;
        int sibling;

        if (Nodes[parent].Left == node) sibling = Nodes[parent].Right;
        else sibling = Nodes[parent].Left;

        if (grandParent != NullNode)
        {
            if (Nodes[grandParent].Left == parent) Nodes[grandParent].Left = sibling;
            else Nodes[grandParent].Right = sibling;

            Nodes[sibling].Parent = grandParent;
            FreeNode(parent);

            int index = grandParent;
            while (index != NullNode)
            {
                int left = Nodes[index].Left;
                int rght = Nodes[index].Right;

                JBBox.CreateMerged(Nodes[left].ExpandedBox, Nodes[rght].ExpandedBox, out Nodes[index].ExpandedBox);
                Nodes[index].Height = 1 + Math.Max(Nodes[left].Height, Nodes[rght].Height);
                index = Nodes[index].Parent;
            }
        }
        else
        {
            root = sibling;
            Nodes[sibling].Parent = NullNode;
            FreeNode(parent);
        }
    }

    private static double MergedSurface(in JBBox box1, in JBBox box2)
    {
        double a, b;
        double x, y, z;

        a = box1.Min.X < box2.Min.X ? box1.Min.X : box2.Min.X;
        b = box1.Max.X > box2.Max.X ? box1.Max.X : box2.Max.X;

        x = b - a;

        a = box1.Min.Y < box2.Min.Y ? box1.Min.Y : box2.Min.Y;
        b = box1.Max.Y > box2.Max.Y ? box1.Max.Y : box2.Max.Y;

        y = b - a;

        a = box1.Min.Z < box2.Min.Z ? box1.Min.Z : box2.Min.Z;
        b = box1.Max.Z > box2.Max.Z ? box1.Max.Z : box2.Max.Z;

        z = b - a;

        return 2.0d * (x * y + x * z + z * y);
    }

    private void AddLeaf(int node)
    {
        if (root == NullNode)
        {
            root = node;
            Nodes[root].Parent = NullNode;
            return;
        }

        // search for the best sibling
        // int sibling = root;
        JBBox nodeBox = Nodes[node].ExpandedBox;

        int sibling = root;

        while (!Nodes[sibling].IsLeaf)
        {
            int left = Nodes[sibling].Left;
            int rght = Nodes[sibling].Right;

            double area = Nodes[sibling].ExpandedBox.GetSurfaceArea();

            double combinedArea = MergedSurface(Nodes[sibling].ExpandedBox, nodeBox);

            double cost = 2.0d * combinedArea;
            double inhcost = 2.0d * (combinedArea - area);
            double costl, costr;

            if (Nodes[left].IsLeaf)
            {
                costl = inhcost + MergedSurface(Nodes[left].ExpandedBox, nodeBox);
            }
            else
            {
                double oldArea = Nodes[left].ExpandedBox.GetSurfaceArea();
                double newArea = MergedSurface(Nodes[left].ExpandedBox, nodeBox);
                costl = newArea - oldArea + inhcost;
            }

            if (Nodes[rght].IsLeaf)
            {
                costr = inhcost + MergedSurface(Nodes[rght].ExpandedBox, nodeBox);
            }
            else
            {
                double oldArea = Nodes[rght].ExpandedBox.GetSurfaceArea();
                double newArea = MergedSurface(Nodes[rght].ExpandedBox, nodeBox);
                costr = newArea - oldArea + inhcost;
            }

            // costl /= 2;
            // costr /= 2;

            // if this is true, the choice is actually the best for the current candidate
            if (cost < costl && cost < costr) break;

            sibling = costl < costr ? left : rght;
        }

        // create a new parent
        int oldParent = Nodes[sibling].Parent;
        int newParent = AllocateNode();

        Nodes[newParent].Parent = oldParent;
        Nodes[newParent].Height = Nodes[sibling].Height + 1;

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
        while (index != NullNode)
        {
            int lft = Nodes[index].Left;
            int rgt = Nodes[index].Right;

            JBBox.CreateMerged(Nodes[lft].ExpandedBox, Nodes[rgt].ExpandedBox, out Nodes[index].ExpandedBox);
            Nodes[index].Height = 1 + Math.Max(Nodes[lft].Height, Nodes[rgt].Height);
            index = Nodes[index].Parent;
        }
    }
}