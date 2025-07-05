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
/// Holds a reference to all contacts (maximum 4) between two shapes.
/// </summary>
public class Arbiter
{
    internal static readonly Stack<Arbiter> Pool = new();

    public RigidBody Body1 = null!;
    public RigidBody Body2 = null!;

    public JHandle<ContactData> Handle;
}

/// <summary>
/// Look-up key for stored <see cref="Arbiter"/>.
/// </summary>
public readonly struct ArbiterKey(ulong key1, ulong key2) : IEquatable<ArbiterKey>
{
    public readonly ulong Key1 = key1;
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