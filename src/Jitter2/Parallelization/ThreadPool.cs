/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Jitter2.DataStructures;
using Jitter2.Unmanaged;

namespace Jitter2.Parallelization;

/*
 * ---------------------------------------------------------------------------
 *  Jitter2 ThreadPool – Persistent Worker Thread Model
 * ---------------------------------------------------------------------------
 *
 *  This thread pool is built around the idea of keeping a fixed set of worker
 *  threads alive for the entire lifetime of the simulation. Workers are kept
 *  in a tight loop, only parking when explicitly instructed. This avoids the
 *  cost of repeatedly waking threads *during* each simulation step.
 *
 * Main Thread:
 *
 *  [ 1. Queue Tasks ]
 *      Tasks are added to a temporary list via AddTask(). Tasks are lightweight
 *      objects wrapping an Action<T> and parameter, pooled for reuse.
 *
 *  [ 2. Execute ]
 *      When Execute() is called, all staged tasks are distributed to
 *      per-thread queues in a round-robin fashion.
 *
 *      The main thread then participates in task execution as "worker 0",
 *      draining its own queue and stealing work from others until all tasks
 *      are completed.
 *
 * Worker Threads:
 *
 *  [ 3. Worker Loop ]
 *      Each worker thread repeatedly:
 *
 *        - Drains its own queue fully.
 *        - If it did any work, attempts to steal tasks from other queues
 *          (draining each queue it visits).
 *        - If no work is available, spins briefly and waits on a ManualResetEventSlim.
 *
 *      Workers stay in this loop permanently. When the event is set
 *      (via ResumeWorkers), they run; when reset (via PauseWorkers),
 *      they block once they have no work left.
 *
 *  [ 4. Completion ]
 *      Execute() blocks until all tasks have finished (tasksLeft == 0),
 *      but the threads themselves remain hot. The simulation loop can
 *      run multiple Execute() calls per step without waking threads.
 *
 *      At the end of the simulation step, PauseWorkers() is called to
 *      park the threads until the next step.
 *
 *  ---------------------------------------------------------------------------
 *  Why this design?
 *  ---------------------------------------------------------------------------
 *  - No thread wake-up overhead during simulation steps (threads are persistent).
 *  - Tasks tend to be picked up by the same thread each simulation step, improving
 *    cache locality.
 *  - Work stealing improves load balancing.
 * ---------------------------------------------------------------------------
 */

/// <summary>
/// Provides a persistent worker thread pool for parallel task execution within the physics engine.
/// </summary>
/// <remarks>
/// <para>
/// Worker threads are kept alive for the lifetime of the pool to avoid wake-up overhead.
/// Tasks are distributed via per-thread queues with work stealing for load balancing.
/// </para>
/// <para>
/// The main thread participates as "worker 0" during <see cref="Execute"/> calls.
/// Use <see cref="ResumeWorkers"/> and <see cref="PauseWorkers"/> to control thread activity
/// between simulation steps.
/// </para>
/// <para>
/// Thread-safe. All task queuing and execution methods are safe to call from multiple threads.
/// </para>
/// </remarks>
public sealed class ThreadPool
{
    private interface ITask
    {
        public void Perform();
    }

    private sealed class Task<T> : ITask
    {
        public Action<T> Action = null!;
        public T Parameter = default!;

        public void Perform()
        {
            _counter = _total;

            Tracer.ProfileBegin(Action);
            Action(Parameter);
            Tracer.ProfileEnd(Action);
        }

        private static readonly List<Task<T>> pool = new(32);

        private static int _counter;
        private static int _total;

        public static Task<T> GetFree()
        {
            if (_counter == 0)
            {
                _counter++;
                _total++;
                pool.Add(new Task<T>());
            }

            return pool[^_counter--];
        }
    }

    /// <summary>
    /// The fraction of available processors to use for worker threads.
    /// </summary>
    public const float ThreadsPerProcessor = 0.9f;

    private readonly ManualResetEventSlim mainResetEvent;
    private Thread[] threads = [];

    private readonly SlimBag<ITask> taskList = [];

    private ConcurrentQueue<ITask>[] queues = [];

    private volatile bool running = true;

    private MemoryHelper.IsolatedInt tasksLeft;
    private int threadCount;

    private static ThreadPool? _instance;

    /// <summary>
    /// Gets the number of worker threads managed by this pool.
    /// </summary>
    /// <value>Includes the main thread as worker 0.</value>
    public int ThreadCount => threadCount;

    private ThreadPool()
    {
        threadCount = 0;
        mainResetEvent = new ManualResetEventSlim(true);

        int initialThreadCount = ThreadCountSuggestion;

#if !NET9_0_OR_GREATER
        // .NET versions below 9.0 have a known issue that can cause hangups or freezing
        // when debugging on non-Windows systems. See: https://github.com/dotnet/runtime/pull/95555
        // To avoid this issue, multi-threading is disabled when a debugger is attached on non-Windows systems.
        if (!OperatingSystem.IsWindows() && Debugger.IsAttached)
        {
            Debug.WriteLine(
                "Multi-threading disabled to prevent potential hangups: Debugger attached, " +
                ".NET version < 9.0, non-Windows system detected.");
            initialThreadCount = 1; // Forces single-threading to avoid hangups
        }
#endif

        ChangeThreadCount(initialThreadCount);
    }

    /// <summary>
    /// Gets the suggested number of threads based on available processors.
    /// </summary>
    /// <value>At least 1, computed as <see cref="ThreadsPerProcessor"/> × processor count.</value>
    public static int ThreadCountSuggestion => Math.Max((int)(Environment.ProcessorCount * ThreadsPerProcessor), 1);

    /// <summary>
    /// Changes the number of worker threads.
    /// </summary>
    /// <param name="numThreads">The new thread count.</param>
    /// <remarks>
    /// Existing worker threads are stopped and new ones are created.
    /// This operation blocks until all previous threads have terminated.
    /// </remarks>
    public void ChangeThreadCount(int numThreads)
    {
        if (numThreads == threadCount) return;

        running = false;
        mainResetEvent.Set();

        for (int i = 0; i < threadCount - 1; i++)
        {
            threads[i].Join();
        }

        running = true;
        threadCount = numThreads;

        queues = new ConcurrentQueue<ITask>[threadCount];
        for (int i = 0; i < threadCount; i++)
            queues[i] = new ConcurrentQueue<ITask>();

        threads = new Thread[threadCount - 1];

        var initWaitHandle = new AutoResetEvent(false);

        for (int i = 0; i < threadCount - 1; i++)
        {
            int index = i;

            threads[i] = new Thread(() =>
            {
                initWaitHandle.Set();
                ThreadProc(index + 1);
            });

            threads[i].IsBackground = true;
            threads[i].Start();
            initWaitHandle.WaitOne();
        }

        PauseWorkers();
    }

    /// <summary>
    /// Adds a task to the queue for later execution.
    /// </summary>
    /// <typeparam name="T">The type of the task parameter.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="parameter">The parameter to pass to the action.</param>
    /// <remarks>
    /// Tasks are not executed until <see cref="Execute"/> is called.
    /// This method is thread-safe.
    /// </remarks>
    public void AddTask<T>(Action<T> action, T parameter)
    {
        var instance = Task<T>.GetFree();
        instance.Action = action;
        instance.Parameter = parameter;
        taskList.Add(instance);
    }

    /// <summary>
    /// Indicates whether the <see cref="ThreadPool"/> instance is initialized.
    /// </summary>
    /// <value><c>true</c> if initialized; otherwise, <c>false</c>.</value>
    public static bool InstanceInitialized => _instance != null;

    /// <summary>
    /// Implements the singleton pattern to provide a single instance of the ThreadPool.
    /// </summary>
    public static ThreadPool Instance
    {
        get
        {
            _instance ??= new ThreadPool();
            return _instance;
        }
    }

    /// <summary>
    /// Resumes all worker threads so they can process queued tasks.
    /// </summary>
    /// <remarks>
    /// Called automatically by <see cref="Execute"/>. Manual calls are typically only needed
    /// when using <see cref="World.ThreadModelType.Persistent"/>.
    /// </remarks>
    public void ResumeWorkers()
    {
        mainResetEvent.Set();
    }

    /// <summary>
    /// Pauses all worker threads after they finish their current tasks.
    /// </summary>
    /// <remarks>
    /// Workers will block on a wait handle until <see cref="ResumeWorkers"/> is called.
    /// This reduces CPU usage between simulation steps.
    /// </remarks>
    public void PauseWorkers()
    {
        mainResetEvent.Reset();
    }

    private void ThreadProc(int index)
    {
        var myQueue = queues[index];

        while (running)
        {
            int performedTasks = 0;

            while (myQueue.TryDequeue(out var task))
            {
                task.Perform();
                Interlocked.Decrement(ref tasksLeft.Value);
                performedTasks++;
            }

            // done performing all own tasks. only now try to steal work from others.
            if (performedTasks > 0)
            {
                // steal from other queues
                for (int i = 1; i < queues.Length; i++)
                {
                    int queueIndex = (i + index) % queues.Length;

                    while (queues[queueIndex].TryDequeue(out var task))
                    {
                        task.Perform();
                        Interlocked.Decrement(ref tasksLeft.Value);
                    }
                }
            }

            Thread.Sleep(0);
            mainResetEvent.Wait();
        }
    }

    /// <summary>
    /// Executes all queued tasks and blocks until completion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tasks are distributed to per-thread queues in round-robin order. The main thread
    /// participates as worker 0 and performs work stealing from other queues.
    /// </para>
    /// <para>
    /// This method automatically calls <see cref="ResumeWorkers"/> at the start.
    /// </para>
    /// </remarks>
    public void Execute()
    {
        ResumeWorkers();

        int totalTasks = taskList.Count;
        Volatile.Write(ref tasksLeft.Value, totalTasks);

        for (int i = 0; i < totalTasks; i++)
        {
            queues[i % this.ThreadCount].Enqueue(taskList[i]);
        }

        taskList.Clear();

        // the main thread's queue.
        var myQueue = queues[0];

        while (myQueue.TryDequeue(out var task))
        {
            task.Perform();
            Interlocked.Decrement(ref tasksLeft.Value);
        }

        // steal from other queues
        for (int i = 1; i < queues.Length; i++)
        {
            while (queues[i].TryDequeue(out var task))
            {
                task.Perform();
                Interlocked.Decrement(ref tasksLeft.Value);
            }
        }

        while (Volatile.Read(ref tasksLeft.Value) > 0)
        {
            Thread.SpinWait(1);
        }
    }
}