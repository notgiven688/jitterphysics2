using Jitter2.DataStructures;

namespace JitterTests;

public class SequentialTests
{
    [SetUp]
    public void Setup()
    {
    }

    public class Number : IListIndex
    {
        public int ListIndex { get; set; }
        public int Value;

        public Number(int num)
        {
            ListIndex = -1;
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

        ActiveList<Number> ts = new();

        Assert.That(ts.Count, Is.EqualTo(0));
        Assert.That(ts.Active, Is.EqualTo(0));

        ts.Add(num5);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.Active, Is.EqualTo(0));

        ts.MoveToInactive(num5);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.Active, Is.EqualTo(0));

        ts.MoveToActive(num5);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.Active, Is.EqualTo(1));

        ts.Remove(num5);

        Assert.That(ts.Count, Is.EqualTo(0));
        Assert.That(ts.Active, Is.EqualTo(0));

        ts.Add(num5, true);

        Assert.That(ts.Count, Is.EqualTo(1));
        Assert.That(ts.Active, Is.EqualTo(1));

        ts.Add(num1);
        ts.Add(num2);
        ts.Add(num3);
        ts.Add(num4);

        Assert.That(ts.Count, Is.EqualTo(5));
        Assert.That(ts.Active, Is.EqualTo(1));

        ts.MoveToActive(num2);
        ts.MoveToActive(num1);
        ts.MoveToActive(num4);
        ts.Remove(num4);
        ts.MoveToInactive(num5);

        Assert.That(ts.Count, Is.EqualTo(4));
        Assert.That(ts.Active, Is.EqualTo(2));

        List<Number> elements = new();
        for (int i = 0; i < ts.Active; i++)
        {
            elements.Add(ts[i]);
        }

        Assert.That(elements, Does.Contain(num1));
        Assert.That(elements, Does.Contain(num2));
    }
}