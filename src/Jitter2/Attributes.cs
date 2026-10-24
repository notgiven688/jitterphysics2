/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;

namespace Jitter2;

/// <summary>
/// The coordinate system in which a value is expressed.
/// </summary>
public enum ReferenceFrame
{
    /// <summary>Coordinates relative to the body or shape.</summary>
    Local,
    /// <summary>Coordinates relative to the world.</summary>
    World
}

/// <summary>
/// Documents the coordinate system used by a member.
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

/// <summary>
/// Specifies the threading context in which code executes or a callback is invoked.
/// </summary>
public enum ThreadContext
{
    /// <summary>
    /// Runs on the thread calling the operation, such as <see cref="World.Step"/>.
    /// </summary>
    MainThread,

    /// <summary>
    /// Runs on a worker thread and may run concurrently with other callbacks. Shared mutable data must be protected.
    /// </summary>
    ParallelWorker,

    /// <summary>
    /// May run on the calling thread or a worker thread. Shared mutable data must be protected.
    /// </summary>
    Any
}

/// <summary>
/// Indicates the thread context in which a callback or event is expected to be invoked.
/// Documents the calling convention; it does not enforce thread safety.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
public sealed class CallbackThreadAttribute(ThreadContext context) : Attribute
{
    /// <summary>
    /// Gets the thread context in which the attributed member is expected to be invoked.
    /// </summary>
    public ThreadContext Context { get; } = context;
}
