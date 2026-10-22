/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

namespace Jitter2.Collision;

/// <summary>
/// Decides which pairs of objects should be checked for collisions.
/// </summary>
/// <remarks>
/// Use <see cref="World.BroadPhaseFilter"/> to exclude pairs, apply collision layers,
/// or handle custom proxy types before shapes are tested for contact.
/// </remarks>
public interface IBroadPhaseFilter
{
    /// <summary>
    /// Called for each pair of proxies whose bounding boxes overlap.
    /// </summary>
    /// <param name="proxyA">The first proxy.</param>
    /// <param name="proxyB">The second proxy.</param>
    /// <returns><c>true</c> to test the shapes for contact; <c>false</c> to skip this pair.</returns>
    [CallbackThread(ThreadContext.Any)]
    bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB);
}
