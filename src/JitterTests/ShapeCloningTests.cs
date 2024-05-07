namespace JitterTests;

public class ShapeCloningTests
{
    [TestCase]
    public void GenericShape_Clone_Is_Equal_and_Covariant()
    {
        ICloneableShape shape = new SphereShape();
        var clone = shape.Clone();
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
        Assert.That(clone.GetType(), Is.EqualTo(typeof(SphereShape)));
    }
    
    [TestCase]
    public void BoxShape_Clone_Is_Equal_and_Covariant()
    {
        var shape = new BoxShape(4, 2, 0);
        var clone = shape.Clone();
        Assert.That(clone.Size, Is.EqualTo(shape.Size));
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }

    [TestCase]
    public void SphereShape_Clone_Is_Equal_and_Covariant()
    {
        var shape = new SphereShape(69);
        var clone = shape.Clone();
        Assert.That(clone.Radius, Is.EqualTo(shape.Radius));
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }

    [TestCase]
    public void CapsuleShape_Clone_Is_Equal_and_Covariant()
    {
        var shape = new CapsuleShape(4, 2);
        var clone = shape.Clone();
        Assert.That(clone.Radius, Is.EqualTo(shape.Radius));
        Assert.That(clone.Length, Is.EqualTo(shape.Length));
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }

    [TestCase]
    public void CylinderShape_Clone_Is_Equal_and_Covariant()
    {
        var shape = new CylinderShape(13, 37);
        var clone = shape.Clone();
        Assert.That(clone.Radius, Is.EqualTo(shape.Radius));
        Assert.That(clone.Height, Is.EqualTo(shape.Height));
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }

    [TestCase]
    public void ConvexHullShape_Clone_Is_Equal_and_Covariant()
    {
        var triangles = new List<JTriangle>()
        {
            new(JVector.Zero, JVector.UnitX, JVector.UnitY),
            new(JVector.UnitY, JVector.UnitZ, JVector.UnitX),
            new(JVector.UnitZ, JVector.Zero, JVector.UnitY),
        };
        
        var shape = new ConvexHullShape(triangles);
        var clone = shape.Clone();
        //Can't test for equality because the vertices are private
        //Assert.That(clone.Vertices, Is.EqualTo(shape.Vertices));
        //TODO: Make internals visible to Test Assembly if desired
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }

    [TestCase]
    public void TransformedShape_Clone_Is_Equal_and_Covariant()
    {
        var shape = new TransformedShape(new BoxShape(9, 8, 7), JVector.UnitX, JMatrix.CreateScale(4, 2, 0));
        var clone = shape.Clone();
        Assert.That(clone.Transformation, Is.EqualTo(shape.Transformation));
        Assert.That(clone.Translation, Is.EqualTo(shape.Translation));
        Assert.That(clone.OriginalShape.GetType(), Is.EqualTo(shape.OriginalShape.GetType()));
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }

    [TestCase]
    public void TransformedShape_Clone_Clones_Original_Shape()
    {
        var shape = new TransformedShape(new SphereShape(3.1415f), JVector.UnitX, JMatrix.CreateScale(9000, 9001, 9002));
        var clone = shape.Clone();
        Assert.That(clone.OriginalShape, Is.Not.EqualTo(shape.OriginalShape));
    }
    
    [TestCase]
    public void PointCloudShape_Clone_Is_Equal_and_Covariant()
    {
        var shape = new PointCloudShape([
            JVector.Zero,
            JVector.UnitX,
            JVector.UnitY,
            JVector.UnitZ,
        ]);
        var clone = shape.Clone();
        //Can't test for equality because the vertices are private
        //Assert.That(clone.Vertices, Is.EqualTo(shape.Vertices));
        //TODO: Make internals visible to Test Assembly if desired
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }
    
    [TestCase]
    public void ConeShape_Clone_Is_Equal_and_Covariant()
    {
        var shape = new ConeShape(3, 141);
        var clone = shape.Clone();
        Assert.That(clone.Radius, Is.EqualTo(shape.Radius));
        Assert.That(clone.Height, Is.EqualTo(shape.Height));
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }
    
    [TestCase]
    public void TriangleShape_Clone_Is_Equal_and_Covariant()
    {
        var mesh = new TriangleMesh([new JTriangle(JVector.Zero, JVector.UnitX, JVector.UnitY)]);
        var shape = new TriangleShape(mesh, 0);
        var clone = shape.Clone();
        Assert.That(clone.Mesh, Is.EqualTo(shape.Mesh));
        Assert.That(clone.Index, Is.EqualTo(shape.Index));
        Assert.That(clone.GetType(), Is.EqualTo(shape.GetType()));
    }
}