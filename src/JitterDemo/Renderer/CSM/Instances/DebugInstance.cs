namespace JitterDemo.Renderer;

public class DebugInstance : CSMInstance
{
    private readonly Vertex[] vertices;
    private readonly TriangleVertexIndex[] indices;

    public DebugInstance(Vertex[] vertices, TriangleVertexIndex[] indices)
    {
        this.vertices = vertices;
        this.indices = indices;
    }

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        return (vertices, indices);
    }
}