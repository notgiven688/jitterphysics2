using System.Diagnostics;
using Jitter2.Unmanaged;
using Parallel = Jitter2.Parallelization.Parallel;
using ReaderWriterLock = Jitter2.Parallelization.ReaderWriterLock;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace JitterTests.Robustness;

public class ParallelTests
{
    private static volatile int current;

#pragma warning disable CS0649
    private struct BufferItem
    {
        public int InternalId;
        public int Value;
    }
#pragma warning restore CS0649

    [TestCase]
    public static void ReaderWriterLockTest()
    {
        ReaderWriterLock rwl = new();

        int[] test = new int[40];
        List<Task> tasks = new();

        // 4 readers
        for (int i = 0; i < 4; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (true)
                {
                    if (sw.ElapsedMilliseconds > 1000) break;
                    rwl.EnterReadLock();
                    test[current]++;
                    rwl.ExitReadLock();
                }
            }));
        }

        // 1 writer
        tasks.Add(Task.Run(() =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (true)
            {
                if (sw.ElapsedMilliseconds > 1000) break;
                Thread.Sleep(10);
                rwl.EnterWriteLock();
                test[current] = 0;
                current = (current + 1) % test.Length;
                rwl.ExitWriteLock();
            }
        }));

        Task.WaitAll(tasks.ToArray());
        test[current] = 0;

        for (int i = 0; i < test.Length; i++)
        {
            Assert.That(test[i], Is.EqualTo(0));
        }
    }

    [TestCase]
    public static void ThreadPool_RethrowsTaskExceptionAndCanExecuteAgain()
    {
        var threadPool = ThreadPool.Instance;
        int originalThreadCount = threadPool.ThreadCount;

        try
        {
            threadPool.ChangeThreadCount(2);

            threadPool.AddTask(static _ => throw new InvalidOperationException("task failed"), 0);

            var exception = Assert.Throws<InvalidOperationException>(() => threadPool.Execute());
            Assert.That(exception!.Message, Is.EqualTo("task failed"));

            int[] executed = [0];
            threadPool.AddTask(static state => Interlocked.Increment(ref state[0]), executed);

            Assert.DoesNotThrow(() => threadPool.Execute());
            Assert.That(executed[0], Is.EqualTo(1));
        }
        finally
        {
            threadPool.ChangeThreadCount(originalThreadCount);
        }
    }

    [TestCase(-1)]
    [TestCase(0)]
    public static void ThreadPool_RejectsInvalidThreadCountWithoutChangingState(int threadCount)
    {
        var threadPool = ThreadPool.Instance;
        int originalThreadCount = threadPool.ThreadCount;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            threadPool.ChangeThreadCount(threadCount));

        Assert.That(exception!.ParamName, Is.EqualTo("numThreads"));
        Assert.That(threadPool.ThreadCount, Is.EqualTo(originalThreadCount));
    }

    [TestCase]
    public static void ThreadPool_RejectsNullTask()
    {
        Assert.Throws<ArgumentNullException>(() => ThreadPool.Instance.AddTask<int>(null!, 0));
    }

    [TestCase]
    public static void PartitionedBuffer_ResizePreservesHandlesAndReleasesLock()
    {
        using var buffer = new PartitionedBuffer<BufferItem>(1);

        var first = buffer.Allocate(active: true, clear: true);
        first.Data.Value = 17;

        var second = buffer.Allocate(active: false, clear: true);
        second.Data.Value = 23;

        Assert.That(buffer.Count, Is.EqualTo(2));
        Assert.That(buffer.Active.Length, Is.EqualTo(1));
        Assert.That(buffer.Inactive.Length, Is.EqualTo(1));
        Assert.That(first.Data.Value, Is.EqualTo(17));
        Assert.That(second.Data.Value, Is.EqualTo(23));
        Assert.That(buffer.IsActive(first), Is.True);
        Assert.That(buffer.IsActive(second), Is.False);

        buffer.ResizeLock.EnterReadLock();
        buffer.ResizeLock.ExitReadLock();
    }
}
