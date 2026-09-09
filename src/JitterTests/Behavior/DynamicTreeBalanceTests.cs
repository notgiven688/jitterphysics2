namespace JitterTests.Behavior;

public class DynamicTreeBalanceTests
{
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
        for (int i = 0; i < 512; i++)
        {
            tree.Update(false, (Real)0.0);
        }

        AssertValidTree(tree, 512);
        Assert.That(tree.CalculateCost(), Is.LessThanOrEqualTo(initialCost));
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
