namespace Jitter2.Collision.Shapes;

/// <summary>
/// RTTI marker interface for shapes that can be cloned.
/// Implement <see cref="ICloneableShape{T}"/> for a covariant return type.
/// </summary>
public interface ICloneableShape
{
    public Shape Clone();
}

public interface ICloneableShape<out T> : ICloneableShape where T : Shape, ICloneableShape<T>
{
    /// <summary>
    /// Clones this shape
    /// </summary>
    /// <remarks>
    /// <c>Clone()</c> creates a new shape instance that has the same values,
    /// but is not attached to any rigid body).
    /// Implementors may retain underlying data structures to conserve memory and
    /// improve instantiation performance.
    /// <p><c>Clone()</c> should be implemented with a covariant return type
    /// enabling convenient and powerful type inference.</p> 
    /// </remarks>
    /// <example>
    /// <see cref="BoxShape"/> declares
    /// <br/><c>public override BoxShape Clone()</c><br/>
    /// to implement this interface, allowing for the following:<br/>
    /// <c>BoxShape box2 = box1.Clone();</c><br/>
    /// </example>
    /// <returns>
    /// A new Shape which is a clone of this Shape.
    /// </returns>
    public new T Clone();
}