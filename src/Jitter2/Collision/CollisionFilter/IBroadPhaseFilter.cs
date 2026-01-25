/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

namespace Jitter2.Collision;

/// <summary>
/// Provides a hook into the broadphase collision detection pipeline.
/// </summary>
/// <remarks>
/// Implement this interface to intercept shape pairs before narrowphase detection.
/// This can be used to filter out specific pairs, implement custom collision layers,
/// or handle collisions for custom proxy types. See <see cref="World.BroadPhaseFilter"/>.
/// </remarks>
public interface IBroadPhaseFilter
{
    /// <summary>
    /// Called for each pair of proxies whose bounding boxes overlap.
    /// </summary>
    /// <param name="proxyA">The first proxy.</param>
    /// <param name="proxyB">The second proxy.</param>
    /// <returns><c>true</c> to continue with narrowphase detection; <c>false</c> to skip this pair.</returns>
    [CallbackThread(ThreadContext.Any)]
    bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB);
}