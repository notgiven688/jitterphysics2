namespace JitterTests.Behavior;

public class DynamicTreeBalanceTests
{
    [TestCase(0)]
    [TestCase(0.5)]
    [TestCase(2)]
    public void SurfaceAreaCostMatchesMaterializedMerge(double offset)
    {
        TreeBox first = new(JVector.Zero, new JVector((Real)1.2, (Real)3.4, (Real)5.6));
        JVector shift = new((Real)offset);
        TreeBox second = new(first.Min + shift, first.Max + shift);
        TreeBox.CreateMerged(first, second, out TreeBox merged);

        // Rounding must not make unchanged bounds look like a cost reduction.
        Assert.That(TreeBox.MergedSurface(first, second), Is.EqualTo(merged.GetSurfaceArea()));
    }

    [Test]
    public void NearlyCoincidentProxiesDoNotCycleDuringBalancing()
    {
        DynamicTree tree = new(static (_, _) => false);
        Random random = new(16);

        // These nearly identical bounds caused two inverse rotations to report a tiny
        // cost reduction each, recursively undoing one another during insertion.
        for (int i = 0; i < 64; i++)
        {
            JVector min = new(
                (Real)(random.NextDouble() * 1e-14),
                (Real)(random.NextDouble() * 1e-14),
                (Real)(random.NextDouble() * 1e-14));
            JVector size = new(
                (Real)(3.1 + random.NextDouble() * 1e-14),
                (Real)(1.7 + random.NextDouble() * 1e-14),
                (Real)(2.3 + random.NextDouble() * 1e-14));
            tree.AddProxy(new BoxProxy(new JBoundingBox(min, min + size)));
        }

        AssertValidTree(tree, 64);
        tree.AutomaticOptimizationBudget = 64;
        for (int i = 0; i < 4; i++) tree.Update(false, 0);
        AssertValidTree(tree, 64);

        List<IDynamicTreeProxy> hits = [];
        tree.Query(hits, new JBoundingBox(new JVector(-1), new JVector(10)));
        Assert.That(hits, Has.Count.EqualTo(64));
    }

    private sealed class BoxProxy(JBoundingBox box) : IDynamicTreeProxy
    {
        public int NodePtr { get; set; } = DynamicTree.NullNode;
        public int SetIndex { get; set; } = -1;
        public JVector Velocity => JVector.Zero;
        public JBoundingBox WorldBoundingBox => box;
    }

    [Test]
    public void ExplicitProxyLifecycleMutationsMaintainValidTree()
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;
        tree.EnableAutomaticOptimization = false;
        List<RigidBody> bodies = [];
        List<SphereShape> shapes = [];

        // Sorted insertion is deliberately hostile to an ordinary binary tree.
        for (int i = 0; i < 512; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector(i * 2, 0, 0);
            SphereShape shape = new((Real)0.4);
            body.AddShape(shape);
            bodies.Add(body);
            shapes.Add(shape);
        }

        AssertValidTree(tree, bodies.Count);

        for (int i = bodies.Count - 1; i >= 0; i -= 2)
        {
            world.Remove(bodies[i]);
            bodies.RemoveAt(i);
            shapes.RemoveAt(i);
        }

        AssertValidTree(tree, bodies.Count);
    }

    [Test]
    public void ProxyUpdatesRefitWithoutEagerBalancing()
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;
        tree.EnableAutomaticOptimization = false;
        List<RigidBody> bodies = [];
        List<SphereShape> shapes = [];

        for (int i = 0; i < 256; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector(i * 2, 0, 0);
            SphereShape shape = new((Real)0.4);
            body.AddShape(shape);
            bodies.Add(body);
            shapes.Add(shape);
        }

        Random random = new(1234);
        for (int i = 0; i < 1000; i++)
        {
            int index = random.Next(bodies.Count);
            bodies[index].Position = new JVector(
                random.Next(-1000, 1001), random.Next(-1000, 1001), 0);
            tree.Update(shapes[index]);

            if ((i & 63) == 0) AssertValidTree(tree, bodies.Count);
        }

        int updateImbalance = AssertValidTree(tree, bodies.Count);
        Assert.That(updateImbalance, Is.GreaterThan(1));
    }

    [Test]
    public void IncrementalOptimizationDoesNotIncreaseTreeCost()
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;
        tree.EnableAutomaticOptimization = false;
        List<RigidBody> bodies = [];
        List<SphereShape> shapes = [];

        for (int i = 0; i < 512; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector(i * 2, 0, 0);
            SphereShape shape = new((Real)0.4);
            body.AddShape(shape);
            bodies.Add(body);
            shapes.Add(shape);
        }

        Random random = new(5678);
        for (int i = 0; i < 2000; i++)
        {
            int index = random.Next(bodies.Count);
            bodies[index].Position = new JVector(
                random.Next(-2000, 2001), random.Next(-2000, 2001), 0);
            tree.Update(shapes[index]);
        }

        double initialCost = tree.CalculateCost();
        Assert.That(AssertValidTree(tree, 512), Is.GreaterThan(1));

        tree.EnableAutomaticOptimization = true;
        double previousCost = initialCost;
        for (int i = 0; i < 512; i++)
        {
            tree.Update(false, (Real)0.0);
            double cost = tree.CalculateCost();
            Assert.That(cost, Is.LessThanOrEqualTo(previousCost));
            previousCost = cost;
        }

        AssertValidTree(tree, 512);
        Assert.That(tree.CalculateCost(), Is.LessThan(initialCost));
    }

    [TestCase(1)]
    [TestCase(3)]
    public void AutomaticOptimizationKeepsEqualCostGrouping(int budget)
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;
        tree.EnableAutomaticOptimization = false;

        for (int i = 0; i < 3; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector(i * 2, 0, 0);
            BoxShape shape = new(1, 1, 1);
            body.AddShape(shape);
            tree.DeactivateProxy(shape);
        }

        // Pairing the middle box with either neighbor has exactly the same cost.
        List<TreeBox> initialBoxes = [];
        tree.EnumerateTreeBoxes((box, depth) =>
        {
            if (depth == 2) initialBoxes.Add(box);
        });
        double initialCost = tree.CalculateCost();

        tree.AutomaticOptimizationBudget = budget;
        tree.EnableAutomaticOptimization = true;
        for (int i = 0; i < 30; i++)
        {
            tree.Update(false, (Real)0.0);

            List<TreeBox> boxes = [];
            tree.EnumerateTreeBoxes((box, depth) =>
            {
                if (depth == 2) boxes.Add(box);
            });

            Assert.That(tree.UpdatedProxyCount, Is.Zero);
            Assert.That(tree.CalculateCost(), Is.EqualTo(initialCost));
            Assert.That(boxes, Is.EquivalentTo(initialBoxes),
                "Stationary boxes must keep their grouping when reinsertion offers no improvement.");
        }

        AssertValidTree(tree, 3);
    }

    [Test]
    public void AutomaticOptimizationPreservesHandlesAndActivity()
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;
        tree.EnableAutomaticOptimization = false;
        List<SphereShape> shapes = [];

        for (int i = 0; i < 32; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector((Real)(i * 0.25), 0, 0);
            SphereShape shape = new((Real)0.75);
            body.AddShape(shape);
            if ((i & 1) == 0) tree.DeactivateProxy(shape);
            shapes.Add(shape);
        }

        int[] leafIndices = shapes.Select(shape => ((IDynamicTreeProxy)shape).NodePtr).ToArray();
        bool[] activity = shapes.Select(tree.IsActive).ToArray();
        tree.AutomaticOptimizationBudget = 3;
        tree.EnableAutomaticOptimization = true;
        tree.Update(false, (Real)0.0);

        tree.AutomaticOptimizationBudget = 0;
        tree.Update(false, (Real)0.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => tree.AutomaticOptimizationBudget = -1);

        for (int i = 0; i < shapes.Count; i++)
        {
            Assert.That(((IDynamicTreeProxy)shapes[i]).NodePtr, Is.EqualTo(leafIndices[i]));
            Assert.That(tree.IsActive(shapes[i]), Is.EqualTo(activity[i]));
        }

        AssertValidTree(tree, shapes.Count);
    }

    [Test]
    public void OptimizePreservesExpandedBoxesHandlesPairsAndActivity()
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;
        tree.EnableAutomaticOptimization = false;
        List<SphereShape> shapes = [];

        for (int i = 0; i < 32; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector((Real)(i * 0.25), 0, 0);
            SphereShape shape = new((Real)0.75);
            body.AddShape(shape);

            // Force one regular update so the leaf receives a fat bounding box.
            tree.DeactivateProxy(shape);
            tree.ActivateProxy(shape);
            shapes.Add(shape);
        }

        tree.Update(false, (Real)(1.0 / 60.0));

        for (int i = 0; i < shapes.Count; i++)
        {
            if ((i & 1) == 0) tree.DeactivateProxy(shapes[i]);
        }

        int[] leafIndices = shapes.Select(shape => ((IDynamicTreeProxy)shape).NodePtr).ToArray();
        TreeBox[] expandedBoxes = shapes.Select(
            shape => tree.Nodes[((IDynamicTreeProxy)shape).NodePtr].ExpandedBox).ToArray();
        bool[] activity = shapes.Select(tree.IsActive).ToArray();
        int pairCount = tree.HashSetInfo.Count;

        tree.Optimize(sweeps: 2, chance: (Real)1.0, incremental: true);

        Assert.That(tree.HashSetInfo.Count, Is.EqualTo(pairCount));
        for (int i = 0; i < shapes.Count; i++)
        {
            int leaf = ((IDynamicTreeProxy)shapes[i]).NodePtr;
            Assert.That(leaf, Is.EqualTo(leafIndices[i]));
            Assert.That(TreeBox.Equals(tree.Nodes[leaf].ExpandedBox, expandedBoxes[i]), Is.True);
            Assert.That(tree.IsActive(shapes[i]), Is.EqualTo(activity[i]));
        }

        AssertValidTree(tree, shapes.Count);
    }

    [Test]
    public void OptimizeRejectsNonFiniteChance()
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            tree.Optimize(sweeps: 1, chance: Real.NaN, incremental: true));
    }

    [Test]
    public void OptimizeRejectsNullRandomSource()
    {
        using World world = new();

        Assert.Throws<ArgumentNullException>(() =>
            world.DynamicTree.Optimize(null!, sweeps: 1, chance: (Real)1.0, incremental: false));
    }

    [Test]
    public void Optimize_WhenRandomSourceThrows_RestoresDetachedLeaves()
    {
        using World world = new();
        DynamicTree tree = world.DynamicTree;

        for (int i = 0; i < 3; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector(i * 2, 0, 0);
            body.AddShape(new SphereShape((Real)0.4));
        }

        Assert.Throws<InvalidOperationException>(() =>
            tree.Optimize(() => throw new InvalidOperationException(), sweeps: 1,
                chance: (Real)1.0, incremental: false));

        AssertValidTree(tree, expectedLeafCount: 3);
    }

    private static int AssertValidTree(DynamicTree tree, int expectedLeafCount)
    {
        if (expectedLeafCount == 0)
        {
            Assert.That(tree.Root, Is.EqualTo(DynamicTree.NullNode));
            return 0;
        }

        HashSet<int> visited = [];
        (int height, int leaves, int maximumImbalance) = ValidateNode(
            tree, tree.Root, DynamicTree.NullNode, visited);

        Assert.That(leaves, Is.EqualTo(expectedLeafCount));
        Assert.That(visited.Count, Is.EqualTo(2 * expectedLeafCount - 1));

        Assert.That(tree.Nodes[tree.Root].Height, Is.EqualTo(height));
        return maximumImbalance;
    }

    private static (int Height, int Leaves, int MaximumImbalance) ValidateNode(
        DynamicTree tree, int index, int expectedParent, HashSet<int> visited)
    {
        Assert.That(visited.Add(index), Is.True, "Tree contains a cycle or duplicate child.");
        DynamicTree.Node node = tree.Nodes[index];
        Assert.That(node.Parent, Is.EqualTo(expectedParent));

        if (node.IsLeaf)
        {
            Assert.That(node.Height, Is.Zero);
            Assert.That(node.LeafCount, Is.EqualTo(1));
            Assert.That(node.Left, Is.EqualTo(DynamicTree.NullNode));
            Assert.That(node.Right, Is.EqualTo(DynamicTree.NullNode));
            return (0, 1, 0);
        }

        (int leftHeight, int leftLeaves, int leftImbalance) =
            ValidateNode(tree, node.Left, index, visited);
        (int rightHeight, int rightLeaves, int rightImbalance) =
            ValidateNode(tree, node.Right, index, visited);

        Assert.That(node.Height, Is.EqualTo(1 + int.Max(leftHeight, rightHeight)));
        Assert.That(node.LeafCount, Is.EqualTo(leftLeaves + rightLeaves));
        TreeBox.CreateMerged(tree.Nodes[node.Left].ExpandedBox,
            tree.Nodes[node.Right].ExpandedBox, out TreeBox expectedBox);
        Assert.That(TreeBox.Equals(node.ExpandedBox, expectedBox), Is.True);

        int imbalance = int.Abs(leftHeight - rightHeight);
        return (node.Height, node.LeafCount,
            int.Max(imbalance, int.Max(leftImbalance, rightImbalance)));
    }
}
