using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace JitterTests.Robustness;

[TestFixture, NonParallelizable]
public class WorkerLoopTests
{
    private const int Threads = 6;
    private int previousThreadCount;
    private readonly int[] calls = new int[Threads];
    private Action<int> countCall = null!;

    [OneTimeSetUp]
    public void EnableWorkers()
    {
        var pool = ThreadPool.Instance;
        previousThreadCount = pool.ThreadCount;
        pool.ChangeThreadCount(Threads);
        countCall = worker => Interlocked.Increment(ref calls[worker]);
    }

    [OneTimeTearDown]
    public void RestoreWorkers()
    {
        var pool = ThreadPool.Instance;
        pool.ChangeThreadCount(previousThreadCount);
        pool.PauseWorkers();
    }

    [SetUp]
    public void ClearCalls() => Array.Clear(calls);

    private void AssertCalls(int participants, int loops)
    {
        for (int worker = 0; worker < Threads; worker++)
            Assert.That(calls[worker], Is.EqualTo(worker < participants ? loops : 0), $"worker {worker}");
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(Threads)]
    public void WorkerLoop_RunsOnceOnEachParticipant(int participants)
    {
        ThreadPool.Instance.ExecuteWorkerLoop(countCall, participants);
        AssertCalls(participants, 1);
    }

    [Test]
    public void WorkerLoop_ClampsParticipantsToThePool()
    {
        ThreadPool.Instance.ExecuteWorkerLoop(countCall, Threads + 10);
        AssertCalls(Threads, 1);
    }

    [Test]
    public void ParkedWorkers_JoinALargerLoop()
    {
        var pool = ThreadPool.Instance;
        for (int i = 0; i < 50; i++) pool.ExecuteWorkerLoop(countCall, 2);
        AssertCalls(2, 50);

        Array.Clear(calls);
        pool.ExecuteWorkerLoop(countCall, Threads);
        AssertCalls(Threads, 1);
    }

    [Test]
    public void ParkedWorkers_ReturnForQueuedTasks()
    {
        var pool = ThreadPool.Instance;
        pool.ExecuteWorkerLoop(countCall, 1);

        // Tasks that only finish once every thread of the pool is inside one prove that the
        // parked workers were woken; the caller alone could not complete them.
        using var everyone = new Barrier(Threads);
        bool allArrived = true;
        for (int i = 0; i < Threads; i++)
        {
            pool.AddTask(_ =>
            {
                if (!everyone.SignalAndWait(TimeSpan.FromSeconds(30))) Volatile.Write(ref allArrived, false);
            }, 0);
        }

        pool.Execute();
        Assert.That(allArrived, Is.True);
    }

    [Test]
    public void ChangingParticipants_NeverLosesOrRepeatsALoop()
    {
        var pool = ThreadPool.Instance;
        var random = new Random(1234);
        int[] expected = new int[Threads];
        int tasksRun = 0, tasksQueued = 0;

        for (int i = 0; i < 5000; i++)
        {
            int participants = 1 + random.Next(Threads);
            pool.ExecuteWorkerLoop(countCall, participants);
            for (int worker = 0; worker < participants; worker++) expected[worker]++;

            if (random.Next(8) == 0)
            {
                int count = 1 + random.Next(2 * Threads);
                for (int t = 0; t < count; t++) pool.AddTask(_ => Interlocked.Increment(ref tasksRun), 0);
                tasksQueued += count;
                pool.Execute();
            }

            if (random.Next(64) == 0) pool.PauseWorkers();
        }

        Assert.That(calls, Is.EqualTo(expected));
        Assert.That(tasksRun, Is.EqualTo(tasksQueued));
    }

    [Test]
    public void ChangingTheThreadCount_ReleasesParkedWorkers()
    {
        var pool = ThreadPool.Instance;
        pool.ExecuteWorkerLoop(countCall, 1);
        pool.ChangeThreadCount(3);
        try
        {
            pool.ExecuteWorkerLoop(countCall, 3);
            Assert.That(calls[..3], Is.EqualTo(new[] { 2, 1, 1 }));
        }
        finally
        {
            pool.ChangeThreadCount(Threads);
        }
    }
}
