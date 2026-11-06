/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using Jitter2.DataStructures;
using Jitter2.Dynamics;

namespace Jitter2.Collision;

/// <summary>
/// Represents a collection of bodies connected directly or indirectly by contacts or constraints.
/// </summary>
public sealed class Island : IPartitionedSetIndex
{
    internal readonly HashSet<RigidBody> InternalBodies = [];

    /// <summary>
    /// Vote to keep the island awake through the next deactivation check, which clears it.
    /// Set by moving bodies and by wake requests.
    /// </summary>
    internal bool KeepAwake;

    /// <summary>
    /// The island is awake but may still contain sleeping bodies, which the next deactivation
    /// check wakes. Only an awake island can have this set.
    /// </summary>
    internal bool WakePending;

    /// <summary>
    /// Gets a collection of all the bodies present in this island.
    /// </summary>
    public ReadOnlyHashSet<RigidBody> Bodies => new(InternalBodies);

    int IPartitionedSetIndex.SetIndex { get; set; } = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Island"/> class.
    /// </summary>
    public Island()
    {
    }

    /// <summary>
    /// Clears all the bodies from the lists within this island.
    /// </summary>
    internal void ClearLists()
    {
        InternalBodies.Clear();
    }

    internal void TrimLists()
    {
        InternalBodies.TrimExcess();
    }
}
