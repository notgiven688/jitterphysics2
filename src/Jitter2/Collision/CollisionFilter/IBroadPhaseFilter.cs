/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

namespace Jitter2.Collision;

/// <summary>
/// Defines a filter for excluding shape pairs during broadphase collision detection.
/// </summary>
/// <remarks>
/// Implement this interface to prevent specific pairs from generating contacts.
/// See <see cref="World.BroadPhaseFilter"/>.
/// </remarks>
public interface IBroadPhaseFilter
{
    /// <summary>
    /// Determines whether a pair of proxies should be considered for collision.
    /// </summary>
    /// <param name="proxyA">The first proxy.</param>
    /// <param name="proxyB">The second proxy.</param>
    /// <returns><c>true</c> to allow collision; <c>false</c> to filter it out.</returns>
    [CallbackThread(ThreadContext.Any)]
    bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB);
}