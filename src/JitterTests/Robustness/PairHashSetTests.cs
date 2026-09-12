using System.Threading;
using Jitter2.Collision;

namespace JitterTests.Robustness;

public class PairHashSetTests
{
    [Test]
    public void Remove_ShrinksWithGrowthHeadroom()
    {
        const int initialCount = PairHashSet.MinimumSize + 1;
        const int targetCount = PairHashSet.MinimumSize / 2 - 1;

        PairHashSet hashSet = new();
        PairHashSet.Pair[] pairs = new PairHashSet.Pair[initialCount];
        int rejected = 0;

        for (int i = 0; i < pairs.Length; i++)
        {
            pairs[i] = new PairHashSet.Pair(i + 1, i + initialCount + 1);
            if (!hashSet.Add(pairs[i])) rejected++;
        }

        int missing = 0;
        for (int i = targetCount; i < pairs.Length; i++)
        {
            if (!hashSet.Remove(pairs[i])) missing++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(rejected, Is.Zero);
            Assert.That(missing, Is.Zero);
            Assert.That(hashSet.Count, Is.EqualTo(targetCount));
            Assert.That(hashSet.Slots,
                Has.Length.EqualTo(PairHashSet.MinimumSize * 2));
        });
    }

    [Test]
    public void ConcurrentAdd_ResizesAndPreservesEntries()
    {
        const int pairCount = PairHashSet.MinimumSize / 2 + 512;

        PairHashSet hashSet = new();
        PairHashSet.Pair[] pairs = new PairHashSet.Pair[pairCount];
        int rejected = 0;

        for (int i = 0; i < pairs.Length; i++)
        {
            pairs[i] = new PairHashSet.Pair(i + 1, i + pairCount + 1);
        }

        System.Threading.Tasks.Parallel.For(0, pairs.Length, i =>
        {
            if (!hashSet.ConcurrentAdd(pairs[i])) Interlocked.Increment(ref rejected);
        });

        int missing = 0;
        foreach (PairHashSet.Pair pair in pairs)
        {
            if (!hashSet.Contains(pair)) missing++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(rejected, Is.Zero);
            Assert.That(missing, Is.Zero);
            Assert.That(hashSet.Count, Is.EqualTo(pairCount));
            Assert.That(hashSet.Slots, Has.Length.EqualTo(PairHashSet.MinimumSize * 2));
        });
    }

    [Test]
    public void ConcurrentAdd_WithDuplicateAndCollidingPairs_PreservesSetSemantics()
    {
        const int workerCount = 64;
        const int rounds = 400;

        PairHashSet hashSet = new();
        PairHashSet.Pair[] collidingPairs = FindCollidingPairs(workerCount + 1);
        PairHashSet.Pair duplicatePair = collidingPairs[^1];
        using Barrier phase = new(workerCount + 1);
        Thread[] workers = new Thread[workerCount];

        Exception? workerException = null;
        bool stop = false;
        bool addDuplicate = false;
        int failedRound = -1;
        int expectedCount = workerCount;
        int actualCount = workerCount;

        for (int i = 0; i < workers.Length; i++)
        {
            int workerIndex = i;
            workers[i] = new Thread(() =>
            {
                while (true)
                {
                    phase.SignalAndWait();
                    if (Volatile.Read(ref stop)) return;

                    try
                    {
                        PairHashSet.Pair pair = Volatile.Read(ref addDuplicate)
                            ? duplicatePair
                            : collidingPairs[workerIndex];

                        hashSet.ConcurrentAdd(pair);
                    }
                    catch (Exception exception)
                    {
                        Interlocked.CompareExchange(ref workerException, exception, null);
                    }
                    finally
                    {
                        phase.SignalAndWait();
                    }
                }
            })
            {
                IsBackground = true
            };
            workers[i].Start();
        }

        try
        {
            for (int round = 0; round < rounds; round++)
            {
                hashSet.Clear();
                Volatile.Write(ref addDuplicate, round >= rounds / 2);

                phase.SignalAndWait();
                phase.SignalAndWait();

                expectedCount = addDuplicate ? 1 : workerCount;
                actualCount = hashSet.Count;
                if (actualCount != expectedCount || workerException != null)
                {
                    failedRound = round;
                    break;
                }
            }
        }
        finally
        {
            Volatile.Write(ref stop, true);
            phase.SignalAndWait();

            foreach (Thread worker in workers)
            {
                worker.Join();
            }
        }

        Assert.That(workerException, Is.Null);
        Assert.That(actualCount, Is.EqualTo(expectedCount),
            $"Set semantics were violated while adding duplicate and colliding pairs concurrently in round {failedRound}.");
    }

    private static PairHashSet.Pair[] FindCollidingPairs(int count)
    {
        PairHashSet.Pair[] result = new PairHashSet.Pair[count];
        int mask = PairHashSet.MinimumSize - 1;
        int found = 0;

        for (int id = 1; found < count; id++)
        {
            PairHashSet.Pair pair = new(id, id + 1_000_000);

            if ((pair.GetHash() & mask) == 0)
            {
                result[found++] = pair;
            }
        }

        return result;
    }
}
