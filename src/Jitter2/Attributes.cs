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
public class ReferenceFrameAttribute : Attribute
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