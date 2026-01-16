/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;

namespace Jitter2;

/// <summary>
/// Enum representing reference frames.
/// </summary>
public enum ReferenceFrame
{
    Local,
    World
}

/// <summary>
/// Attribute to specify the reference frame of a member.
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public sealed class ReferenceFrameAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the reference frame.
    /// </summary>
    public ReferenceFrame Frame { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceFrameAttribute"/> class.
    /// </summary>
    /// <param name="frame">The reference frame.</param>
    public ReferenceFrameAttribute(ReferenceFrame frame)
    {
        Frame = frame;
    }
}

public enum ThreadContext
{
    /// <summary>
    /// Called from the main thread (or the thread calling World.Step).
    /// Safe to access global state.
    /// </summary>
    MainThread,

    /// <summary>
    /// Called from background worker threads.
    /// Code must be thread-safe and lock-free.
    /// </summary>
    ParallelWorker,

    /// <summary>
    /// Could be called from either. Handle with care.
    /// </summary>
    Any
}

/// <summary>
/// Indicates the thread context in which a callback or event is expected to be invoked.
/// This attribute is primarily informational and used for documentation purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
public sealed class CallbackThreadAttribute(ThreadContext context) : Attribute
{
    public ThreadContext Context { get; } = context;
}