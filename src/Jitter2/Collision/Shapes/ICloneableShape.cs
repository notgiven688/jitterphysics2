/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Defines a method to create a new instance of a shape for use with another rigid body.
/// </summary>
/// <typeparam name="T">
/// The concrete shape type implementing this interface.
/// </typeparam>
public interface ICloneableShape<out T> where T : Shape
{
    /// <summary>
    /// Creates a copy of the current shape instance that shares underlying geometry data.
    /// </summary>
    /// <returns>
    /// A new shape instance of type <typeparamref name="T"/> that shares immutable data
    /// with the original but has its own instance state.
    /// </returns>
    T Clone();
}