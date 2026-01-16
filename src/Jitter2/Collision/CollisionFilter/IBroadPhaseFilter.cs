/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

namespace Jitter2.Collision;

/// <summary>
/// Interface for implementing a generic filter to exclude specific pairs of shapes
/// that should not be considered in the collision system pipeline of Jitter.
/// Refer to <see cref="World.BroadPhaseFilter"/> for more details.
/// </summary>
public interface IBroadPhaseFilter
{
    /// <summary>
    /// Filters out pairs of shapes that should not generate contacts.
    /// </summary>
    /// <returns>False if the collision should be filtered out; true otherwise.</returns>
    [CallbackThread(ThreadContext.Any)]
    bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB);
}