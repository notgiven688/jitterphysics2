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
/// Represents an island, which is a collection of bodies that are either directly or indirectly in contact with each other.
/// </summary>
public sealed class Island : IPartitionedSetIndex
{
    internal readonly HashSet<RigidBody> InternalBodies = [];
    internal bool MarkedAsActive;

    /// <summary>
    /// Has to be set if an island is active but might contain inactive bodies.
    /// This happens, for example, if an inactive and an active island are merged.
    /// </summary>
    internal bool NeedsUpdate;

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
}