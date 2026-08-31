using Jitter2.Unmanaged;

namespace JitterTests.Robustness;

public class PartitionedBufferTests
{
    private struct BufferItem
    {
#pragma warning disable CS0649
        public int InternalId;
#pragma warning restore CS0649
        public int Value;
    }

    private sealed class Entry(JHandle<BufferItem> handle, int value, bool active)
    {
        public readonly JHandle<BufferItem> Handle = handle;
        public readonly int Value = value;
        public bool Active = active;
    }

    [TestCase]
    public void FreeActive_PreservesRemainingElementsAndPartitions()
    {
        using var buffer = new PartitionedBuffer<BufferItem>();

        JHandle<BufferItem> active0 = Allocate(buffer, 10, true);
        JHandle<BufferItem> removed = Allocate(buffer, 20, true);
        JHandle<BufferItem> active1 = Allocate(buffer, 30, true);
        JHandle<BufferItem> inactive0 = Allocate(buffer, 40, false);
        JHandle<BufferItem> inactive1 = Allocate(buffer, 50, false);

        buffer.Free(removed);

        Assert.That(buffer.Count, Is.EqualTo(4));
        Assert.That(buffer.Active.Length, Is.EqualTo(2));
        Assert.That(buffer.Inactive.Length, Is.EqualTo(2));
        AssertHandle(buffer, active0, 10, true);
        AssertHandle(buffer, active1, 30, true);
        AssertHandle(buffer, inactive0, 40, false);
        AssertHandle(buffer, inactive1, 50, false);
    }

    [TestCase]
    public void FreeInactive_PreservesRemainingElementsAndPartitions()
    {
        using var buffer = new PartitionedBuffer<BufferItem>();

        JHandle<BufferItem> active = Allocate(buffer, 10, true);
        JHandle<BufferItem> inactive0 = Allocate(buffer, 20, false);
        JHandle<BufferItem> removed = Allocate(buffer, 30, false);
        JHandle<BufferItem> inactive1 = Allocate(buffer, 40, false);

        buffer.Free(removed);

        Assert.That(buffer.Count, Is.EqualTo(3));
        Assert.That(buffer.Active.Length, Is.EqualTo(1));
        Assert.That(buffer.Inactive.Length, Is.EqualTo(2));
        AssertHandle(buffer, active, 10, true);
        AssertHandle(buffer, inactive0, 20, false);
        AssertHandle(buffer, inactive1, 40, false);
    }

    [TestCase]
    public void AllocateActive_WithInactiveElements_PreservesExistingHandles()
    {
        using var buffer = new PartitionedBuffer<BufferItem>();

        JHandle<BufferItem> active = Allocate(buffer, 10, true);
        JHandle<BufferItem> inactive0 = Allocate(buffer, 20, false);
        JHandle<BufferItem> inactive1 = Allocate(buffer, 30, false);
        JHandle<BufferItem> added = Allocate(buffer, 40, true);

        Assert.That(buffer.Count, Is.EqualTo(4));
        Assert.That(buffer.Active.Length, Is.EqualTo(2));
        Assert.That(buffer.Inactive.Length, Is.EqualTo(2));
        AssertHandle(buffer, active, 10, true);
        AssertHandle(buffer, added, 40, true);
        AssertHandle(buffer, inactive0, 20, false);
        AssertHandle(buffer, inactive1, 30, false);
    }

    [TestCase]
    public void RandomTransitions_PreserveEveryLiveHandleAndPartition()
    {
        using var buffer = new PartitionedBuffer<BufferItem>(2);
        var random = new Random(87123);
        var entries = new List<Entry>();
        int nextValue = 1;

        for (int iteration = 0; iteration < 2_000; iteration++)
        {
            int operation = entries.Count == 0 ? 0 : random.Next(4);

            switch (operation)
            {
                case 0:
                {
                    bool active = random.Next(2) == 0;
                    JHandle<BufferItem> handle = Allocate(buffer, nextValue, active);
                    entries.Add(new Entry(handle, nextValue, active));
                    nextValue++;
                    break;
                }
                case 1:
                {
                    int index = random.Next(entries.Count);
                    buffer.Free(entries[index].Handle);
                    entries.RemoveAt(index);
                    break;
                }
                case 2:
                {
                    Entry entry = entries[random.Next(entries.Count)];
                    buffer.MoveToActive(entry.Handle);
                    entry.Active = true;
                    break;
                }
                case 3:
                {
                    Entry entry = entries[random.Next(entries.Count)];
                    buffer.MoveToInactive(entry.Handle);
                    entry.Active = false;
                    break;
                }
            }

            AssertState(buffer, entries);
        }
    }

    private static JHandle<BufferItem> Allocate(PartitionedBuffer<BufferItem> buffer, int value, bool active)
    {
        JHandle<BufferItem> handle = buffer.Allocate(active, clear: true);
        handle.Data.Value = value;
        return handle;
    }

    private static void AssertHandle(
        PartitionedBuffer<BufferItem> buffer,
        JHandle<BufferItem> handle,
        int value,
        bool active)
    {
        Assert.That(handle.Data.Value, Is.EqualTo(value));
        Assert.That(buffer.IsActive(handle), Is.EqualTo(active));
    }

    private static void AssertState(PartitionedBuffer<BufferItem> buffer, List<Entry> entries)
    {
        Assert.That(buffer.Count, Is.EqualTo(entries.Count));
        Assert.That(buffer.Active.Length, Is.EqualTo(entries.Count(entry => entry.Active)));
        Assert.That(buffer.Inactive.Length, Is.EqualTo(entries.Count(entry => !entry.Active)));

        HashSet<int> expectedValues = entries.Select(entry => entry.Value).ToHashSet();
        HashSet<int> actualValues = [];

        foreach (ref BufferItem item in buffer.Elements)
        {
            actualValues.Add(item.Value);
        }

        Assert.That(actualValues, Is.EquivalentTo(expectedValues));

        foreach (Entry entry in entries)
        {
            AssertHandle(buffer, entry.Handle, entry.Value, entry.Active);
        }
    }
}
