/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics;

/// <summary>
/// Manages contact information between two different rigid bodies.
/// </summary>
/// <remarks>
/// <para>
/// An arbiter is created when two shapes begin overlapping and is removed when they separate
/// or when one of the involved bodies is removed from the world. Each arbiter can hold up to
/// four cached contact points (see <see cref="ContactData"/>).
/// </para>
/// <para>
/// In most cases arbiters are keyed by shape identifiers. For arbiters created by the engine, the ordering
/// is canonical: the shape with the smaller ID is always first.
/// </para>
/// <para>
/// The <see cref="Handle"/> property provides access to the underlying <see cref="ContactData"/>
/// stored in unmanaged memory. This data is only valid while the arbiter exists and must not
/// be accessed concurrently with <see cref="World.Step(Real, bool)"/>.
/// </para>
/// </remarks>
public sealed class Arbiter
{
    internal static readonly Stack<Arbiter> Pool = new();

    /// <summary>
    /// Gets the first rigid body involved in this contact.
    /// </summary>
    public RigidBody Body1 { get; internal set; } = null!;

    /// <summary>
    /// Gets the second rigid body involved in this contact.
    /// </summary>
    public RigidBody Body2 { get; internal set; } = null!;

    /// <summary>
    /// Gets the handle to the <see cref="ContactData"/> stored in unmanaged memory.
    /// </summary>
    /// <remarks>
    /// The underlying data is valid only while this arbiter is registered with the world.
    /// After removal, accessing <see cref="JHandle{T}.Data"/> results in undefined behavior.
    /// </remarks>
    public JHandle<ContactData> Handle { get; internal set; }
}

/// <summary>
/// Represents an ordered pair of identifiers used to look up an <see cref="Arbiter"/>.
/// </summary>
/// <remarks>
/// The order of <paramref name="key1"/> and <paramref name="key2"/> matters for equality comparison.
/// For arbiters created by the physics engine, <paramref name="key1"/> is always less than <paramref name="key2"/>.
/// </remarks>
/// <param name="key1">The first identifier (typically the smaller shape ID).</param>
/// <param name="key2">The second identifier (typically the larger shape ID).</param>
public readonly struct ArbiterKey(ulong key1, ulong key2) : IEquatable<ArbiterKey>
{
    /// <summary>
    /// The first identifier in the pair.
    /// </summary>
    public readonly ulong Key1 = key1;

    /// <summary>
    /// The second identifier in the pair.
    /// </summary>
    public readonly ulong Key2 = key2;

    public bool Equals(ArbiterKey other)
    {
        return Key1 == other.Key1 && Key2 == other.Key2;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArbiterKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key1, Key2);
    }

    public static bool operator ==(ArbiterKey left, ArbiterKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArbiterKey left, ArbiterKey right)
    {
        return !(left == right);
    }
}