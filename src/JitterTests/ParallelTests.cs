using System.Diagnostics;
using Parallel = Jitter2.Parallelization.Parallel;
using ReaderWriterLock = Jitter2.Parallelization.ReaderWriterLock;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace JitterTests;

public class ParallelTests
{
    private static volatile int current;

    [TestCase]
    public static void ThreadPoolTest()
    {
        ThreadPool.Instance.ChangeThreadCount(4);
        var batches = new Parallel.Batch[4];

        Parallel.ForBatch(0, 1024, 4,
            batch => batches[batch.BatchIndex] = batch, execute: true);

        for (int i = 0; i < 4; i++)
        {
            Assert.That(batches[i].Start, Is.EqualTo(256 * i));
            Assert.That(batches[i].End, Is.EqualTo(256 * (i + 1)));
        }
    }

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
}