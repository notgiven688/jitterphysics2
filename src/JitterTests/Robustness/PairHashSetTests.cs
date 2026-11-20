using System;
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

    [Test]
    public void RemoveWithin_LeavesThePairWhenTheRunLeavesTheBlock()
    {
        PairHashSet hashSet = new();
        for (int i = 0; i < 64; i++) hashSet.Add(new PairHashSet.Pair(i + 1, i + 1000));

        int slot = Array.FindIndex(hashSet.Slots, pair => pair.ID != 0);
        int mask = hashSet.Slots.Length - 1;
        int run = 1;
        while (hashSet.Slots[(slot + run) & mask].ID != 0) run++;
        PairHashSet.Pair pair = hashSet.Slots[slot];

        Assert.Multiple(() =>
        {
            // The empty slot behind the run is 'run' slots away and has to be within reach as well.
            Assert.That(hashSet.RemoveWithin(slot, run), Is.False);
            Assert.That(hashSet.Contains(pair), Is.True);
            Assert.That(hashSet.Count, Is.EqualTo(64));
            Assert.That(hashSet.RemoveWithin(slot, run + 1), Is.True);
            Assert.That(hashSet.Contains(pair), Is.False);
            Assert.That(hashSet.Count, Is.EqualTo(63));
        });
    }

    [Test]
    public void RemoveWithin_FromBlocksOnSeveralThreads_RemovesExactlyTheChosenPairs()
    {
        const int pairCount = 200_000;
        const int blocks = 16;

        PairHashSet hashSet = new();
        PairHashSet.Pair[] pairs = new PairHashSet.Pair[pairCount];
        for (int i = 0; i < pairCount; i++)
        {
            // Every third pair is to be removed; the second ID marks it.
            pairs[i] = new PairHashSet.Pair(i + 1, pairCount + 1 + i * 3 + (i % 3 == 0 ? 1 : 0));
            hashSet.Add(pairs[i]);
        }

        static bool Chosen(PairHashSet.Pair pair) => pair.ID2 % 3 == 2;

        int length = hashSet.Slots.Length;
        int mask = length - 1;

        // The block boundaries move with every pass, as in the tree's pruning.
        for (int pass = 0; pass < 8; pass++)
        {
            int offset = pass * 37;
            System.Threading.Tasks.Parallel.For(0, blocks, block =>
            {
                int start = offset + block * (length / blocks);
                int end = start + length / blocks;

                for (int i = start; i < end; i++)
                {
                    int slot = i & mask;
                    while (hashSet.Slots[slot].ID != 0 && Chosen(hashSet.Slots[slot]))
                    {
                        if (!hashSet.RemoveWithin(slot, end - i)) break;
                    }
                }
            });
        }

        int wrong = 0, kept = 0;
        foreach (PairHashSet.Pair pair in pairs)
        {
            bool chosen = Chosen(pair);
            if (hashSet.Contains(pair) == chosen) wrong++;
            if (!chosen) kept++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(wrong, Is.Zero);
            Assert.That(hashSet.Count, Is.EqualTo(kept));
            Assert.That(hashSet.Slots, Has.Length.EqualTo(length));
        });
    }
}
