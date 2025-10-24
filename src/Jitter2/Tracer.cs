/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

/*
 * This file defines the Tracer utility used to record performance data during simulation.
 *
 * When the symbol PROFILE is defined at compile time, the Tracer class is fully included.
 * It records events with minimal overhead using thread-local buffers and can export them
 * in Chrome Trace Event format for external analysis. The public surface is intentionally minimal:
 * external code can write out recorded traces, but the recording itself is controlled internally
 * by the engine.
 *
 * When PROFILE is not defined, the Tracer class is replaced by a stub implementation.
 * All trace methods in the stub are decorated with [Conditional("_NEVER")], ensuring that
 * every call to Tracer.* is completely stripped by the compiler. This eliminates any
 * runtime cost or dependency on the tracing system in non-profile builds.
 */

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jitter2;

internal enum TraceCategory : ushort
{
    General,
    Solver,
    Runtime,
    Invoke,
}

internal enum TraceName : long
{
    Scope,
    PruneInvalidPairs,
    UpdateBoundingBoxes,
    ScanMoved,
    UpdateProxies,
    ScanOverlaps,
    Gc,
    Step,
    NarrowPhase,
    AddArbiter,
    ReorderContacts,
    CheckDeactivation,
    Solve,
    RemoveArbiter,
    UpdateContacts,
    UpdateBodies,
    BroadPhase,
    Queue,
    PreStep,
    PostStep,
}

internal enum TracePhase : byte
{
    Begin,
    End,
    Instant,
    Complete
}

#if PROFILE

/// <summary>
/// Provides access to performance tracing features.
/// </summary>
/// <remarks>
/// When tracing is enabled, performance data can be recorded during simulation
/// and exported for analysis with external tools such as Chrome's tracing viewer.
/// </remarks>
public static class Tracer
{
    private const string DefaultPath = "trace.json";

    static Tracer()
    {
        string message =
            $">>> PROFILING ENABLED! <<< Use {nameof(Tracer)}.{nameof(WriteToFile)} to dump trace to disk.";

        if(Logger.Listener == null) Console.WriteLine(message);
        else Logger.Warning(message);

        StartGcTracing();
    }

    private struct TraceEvent
    {
        public double TimestampMicro;
        public double DurationMicro;
        public int ThreadId;
        public TraceCategory Category;
        public TracePhase Phase;
        public TraceName Name;
    }

    private sealed class PerThreadBuffer(int initialCapacity)
    {
        public TraceEvent[] Events = ArrayPool<TraceEvent>.Shared.Rent(initialCapacity);
        public int Count = 0;
    }

    private static readonly ConcurrentBag<PerThreadBuffer> allBuffers = new();
    [ThreadStatic] private static PerThreadBuffer? _perThreadBuffer;

    private const int InitialCapacity = 16_384;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PerThreadBuffer EnsureThreadBuffer()
    {
        var b = _perThreadBuffer;
        if (b is not null) return b;

        b = new PerThreadBuffer(InitialCapacity);
        _perThreadBuffer = b;
        allBuffers.Add(b);
        return b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Record(ref TraceEvent e)
    {
        var b = EnsureThreadBuffer();
        int i = b.Count;
        if ((uint)i >= (uint)b.Events.Length)
        {
            Grow(b);
        }

        b.Events[i] = e;
        b.Count = i + 1;
    }

    private static void Grow(PerThreadBuffer b)
    {
        var oldArr = b.Events;
        int newLen = oldArr.Length << 1;
        var newArr = ArrayPool<TraceEvent>.Shared.Rent(newLen);
        Array.Copy(oldArr, 0, newArr, 0, oldArr.Length);
        b.Events = newArr;
        ArrayPool<TraceEvent>.Shared.Return(oldArr, clearArray: false);
    }

    private static readonly double ticksToMicro = 1_000_000.0 / Stopwatch.Frequency;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double NowMicroseconds()
        => Stopwatch.GetTimestamp() * ticksToMicro;

    [ThreadStatic] private static double _scopeStartMicro;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ProfileBegin(TraceName name, TraceCategory cat = TraceCategory.Solver)
    {
        var evt = new TraceEvent
        {
            TimestampMicro = NowMicroseconds(),
            ThreadId = Environment.CurrentManagedThreadId,
            Category = cat,
            Phase = TracePhase.Begin,
            Name = name
        };
        Record(ref evt);
    }

    internal static void ProfileBegin(Delegate @delegate)
    {
        var evt = new TraceEvent
        {
            TimestampMicro = NowMicroseconds(),
            ThreadId = Environment.CurrentManagedThreadId,
            Category = TraceCategory.Invoke,
            Phase = TracePhase.Begin,
            Name = (TraceName)@delegate.Method.MethodHandle.Value,
        };
        Record(ref evt);
    }

    internal static void ProfileEnd(Delegate @delegate)
    {
        var evt = new TraceEvent
        {
            TimestampMicro = NowMicroseconds(),
            ThreadId = Environment.CurrentManagedThreadId,
            Category = TraceCategory.Invoke,
            Phase = TracePhase.End,
            Name = (TraceName)@delegate.Method.MethodHandle.Value,
        };
        Record(ref evt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ProfileEnd(TraceName name, TraceCategory cat = TraceCategory.Solver)
    {
        var evt = new TraceEvent
        {
            TimestampMicro = NowMicroseconds(),
            ThreadId = Environment.CurrentManagedThreadId,
            Category = cat,
            Phase = TracePhase.End,
            Name = name
        };
        Record(ref evt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ProfileEvent(TraceName name, TraceCategory cat = TraceCategory.Solver)
    {
        var evt = new TraceEvent
        {
            TimestampMicro = NowMicroseconds(),
            ThreadId = Environment.CurrentManagedThreadId,
            Category = cat,
            Phase = TracePhase.Instant,
            Name = name
        };
        Record(ref evt);
    }

    /// <summary>
    /// Starts measuring the duration of a profiling scope on the current thread.
    /// </summary>
    /// <remarks>
    /// Only one scope measurement can be active per thread at a time. Nested calls to
    /// <see cref="ProfileScopeBegin"/> will overwrite the previous start timestamp,
    /// resulting in incorrect duration measurements.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ProfileScopeBegin()
    {
        _scopeStartMicro = NowMicroseconds();
    }

    /// <summary>
    /// Ends the current profiling scope and records its duration if it exceeds the given threshold.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ProfileScopeEnd(
        TraceName name = TraceName.Scope,
        TraceCategory cat = TraceCategory.Solver,
        double thresholdMicroseconds = 100.0)
    {
        double end = NowMicroseconds();
        double dur = end - _scopeStartMicro;
        if (dur < thresholdMicroseconds) return;

        var evt = new TraceEvent
        {
            TimestampMicro = _scopeStartMicro,
            DurationMicro = dur,
            ThreadId = Environment.CurrentManagedThreadId,
            Category = cat,
            Phase = TracePhase.Complete,
            Name = name
        };
        Record(ref evt);
    }

    private sealed class GcNotification
    {
        ~GcNotification()
        {
            var evt = new TraceEvent
            {
                TimestampMicro = NowMicroseconds(),
                ThreadId = Environment.CurrentManagedThreadId,
                Category = TraceCategory.Runtime,
                Phase = TracePhase.Instant,
                Name = TraceName.Gc
            };
            Record(ref evt);

            if (!Environment.HasShutdownStarted)
            {
                _ = new GcNotification();
            }
        }
    }

    private static void StartGcTracing()
    {
        _ = new GcNotification();
    }

    /// <summary>
    /// Writes all recorded trace events to a JSON file.
    /// </summary>
    /// <param name="filename">The file path to write to. Defaults to <c>trace.json</c>.</param>
    /// <param name="clear">
    /// If <see langword="true"/>, all recorded data is cleared after writing.
    /// If <see langword="false"/>, recorded data remains in memory.
    /// </param>
    /// <remarks>
    /// The output file uses the Chrome Trace Event format and can be opened in
    /// <a href="chrome://tracing">chrome://tracing</a> or compatible viewers.
    /// </remarks>
    public static void WriteToFile(string filename = DefaultPath, bool clear = true)
    {
        using var writer = new StreamWriter(filename, false, new UTF8Encoding(false));
        writer.Write('[');

        bool first = true;
        var inv = CultureInfo.InvariantCulture;

        foreach (var buf in allBuffers)
        {
            var arr = buf.Events;
            int count = buf.Count;

            for (int i = 0; i < count; i++)
            {
                ref readonly var e = ref arr[i];
                if (!first) writer.Write(',');
                first = false;

                writer.Write('{');

                writer.Write("\"name\":\"");

                if (e.Category == TraceCategory.Invoke)
                {
                    var method = System.Reflection.MethodBase.GetMethodFromHandle(
                        RuntimeMethodHandle.FromIntPtr((IntPtr)e.Name))!.Name;
                    writer.Write(method);
                }
                else
                {
                    writer.Write(e.Name.ToString());
                }

                writer.Write("\",");

                writer.Write("\"cat\":\"");
                writer.Write(e.Category.ToString());
                writer.Write("\",");

                writer.Write("\"ph\":\"");
                writer.Write(PhaseToChar(e.Phase));
                writer.Write("\",");

                writer.Write("\"ts\":");
                writer.Write(e.TimestampMicro.ToString(inv));

                if (e.Phase == TracePhase.Complete)
                {
                    writer.Write(",\"dur\":");
                    writer.Write(e.DurationMicro.ToString(inv));
                }

                writer.Write(",\"pid\":1,\"tid\":");
                writer.Write(e.ThreadId);

                writer.Write('}');
            }

            if (clear)
            {
                buf.Count = 0;
            }
        }

        writer.Write(']');

        if (clear)
        {
            foreach (var buf in allBuffers)
            {
                if (buf.Events.Length > InitialCapacity)
                {
                    var old = buf.Events;
                    buf.Events = ArrayPool<TraceEvent>.Shared.Rent(InitialCapacity);
                    buf.Count = 0;
                    ArrayPool<TraceEvent>.Shared.Return(old, clearArray: false);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char PhaseToChar(TracePhase p) => p switch
    {
        TracePhase.Begin => 'B',
        TracePhase.End => 'E',
        TracePhase.Instant => 'i',
        TracePhase.Complete => 'X',
        _ => '?'
    };
}

#else

internal static class Tracer
{
    [Conditional("_NEVER")]
    internal static void ProfileBegin(TraceName name, TraceCategory cat = TraceCategory.Solver)
    {
    }

    [Conditional("_NEVER")]
    internal static void ProfileEnd(TraceName name, TraceCategory cat = TraceCategory.Solver)
    {
    }

    [Conditional("_NEVER")]
    internal static void ProfileEvent(TraceName name, TraceCategory cat = TraceCategory.Solver)
    {
    }

    [Conditional("_NEVER")]
    internal static void ProfileScopeBegin()
    {
    }

    [Conditional("_NEVER")]
    internal static void ProfileBegin(Delegate @delegate)
    {
    }

    [Conditional("_NEVER")]
    internal static void ProfileEnd(Delegate @delegate)
    {
    }

    [Conditional("_NEVER")]
    internal static void ProfileScopeEnd(TraceName name = TraceName.Scope,
        TraceCategory cat = TraceCategory.Solver,
        double thresholdMicroseconds = 100.0)
    {

    }
}

#endif