using Jitter2.DataStructures;

namespace JitterTests.Api;

public class SequentialTests
{
    [SetUp]
    public void Setup()
    {
    }

    public class Number : IPartitionedSetIndex
    {
        public int SetIndex { get; set; }
        public int Value;

        public Number(int num)
        {
            SetIndex = -1;
            Value = num;
        }
    }

    [TestCase]
    public static void AddRemoveTest()
    {
        var num1 = new Number(1);
        var num2 = new Number(2);
        var num3 = new Number(3);
        var num4 = new Number(4);
        var num5 = new Number(5);

        PartitionedSet<Number> ts = new();

        Assert.That(ts.Count, Is.EqualTo(0));
        Assert.That(ts.ActiveCount, Is.EqualTo(0));

        ts.Add(num5);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.ActiveCount, Is.EqualTo(0));

        ts.MoveToInactive(num5);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.ActiveCount, Is.EqualTo(0));

        ts.MoveToActive(num5);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.ActiveCount, Is.EqualTo(1));

        ts.Remove(num5);

        Assert.That(ts.Count, Is.EqualTo(0));
        Assert.That(ts.ActiveCount, Is.EqualTo(0));

        ts.Add(num5, true);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.ActiveCount, Is.EqualTo(1));

        ts.Add(num1);
        ts.Add(num2);
        ts.Add(num3);
        ts.Add(num4);

        Assert.That(ts.Count, Is.EqualTo(5));
        Assert.That(ts.ActiveCount, Is.EqualTo(1));

        ts.MoveToActive(num2);
        ts.MoveToActive(num1);
        ts.MoveToActive(num4);
        ts.Remove(num4);
        ts.MoveToInactive(num5);

        Assert.That(ts.Count, Is.EqualTo(4));
        Assert.That(ts.ActiveCount, Is.EqualTo(2));

        List<Number> elements = new();
        for (int i = 0; i < ts.ActiveCount; i++)
        {
            elements.Add(ts[i]);
        }

        Assert.That(elements, Does.Contain(num1));
        Assert.That(elements, Does.Contain(num2));
    }
}

public class TrimTests
{
    [TestCase]
    public static void PartitionedSet_TrimPreservesElementsAndInitialCapacityFloor()
    {
        var set = new PartitionedSet<SequentialTests.Number>(4);
        var entries = Enumerable.Range(0, 10).Select(i => new SequentialTests.Number(i)).ToArray();

        foreach (var entry in entries)
        {
            set.Add(entry, entry.Value % 2 == 0);
        }

        Assert.That(set.Capacity, Is.EqualTo(16));

        for (int i = entries.Length - 1; i >= 5; i--)
        {
            set.Remove(entries[i]);
        }

        Assert.That(set.Trim(), Is.True);
        Assert.That(set.Capacity, Is.EqualTo(8));
        Assert.That(set.Count, Is.EqualTo(5));
        Assert.That(set.ActiveCount, Is.EqualTo(3));
        Assert.That(set.Elements.ToArray(), Is.EquivalentTo(entries[..5]));

        foreach (var entry in entries[..5])
        {
            Assert.That(set.Contains(entry), Is.True);
        }

        for (int i = 4; i >= 0; i--)
        {
            set.Remove(entries[i]);
        }

        Assert.That(set.Trim(), Is.True);
        Assert.That(set.Capacity, Is.EqualTo(4));
        Assert.That(set.Trim(), Is.False);
        Assert.That(set.Capacity, Is.EqualTo(4));
    }
}
