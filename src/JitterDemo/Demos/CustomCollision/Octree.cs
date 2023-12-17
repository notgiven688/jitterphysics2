using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.LinearMath;

namespace JitterDemo;

public class Octree
{
    public struct Node
    {
        #region public struct NeighborIndices

        [StructLayout(LayoutKind.Explicit, Size = 8 * sizeof(uint))]
        public struct NeighborIndices
        {
            public unsafe uint this[int index]
            {
                get
                {
                    uint* ptr = (uint*)Unsafe.AsPointer(ref this);
                    return ptr[index];
                }
                set
                {
                    uint* ptr = (uint*)Unsafe.AsPointer(ref this);
                    ptr[index] = value;
                }
            }
        }

        #endregion

        public JBBox Box;
        public NeighborIndices Neighbors;

        public List<uint>? Triangles;
    }

    public struct TriangleIndices
    {
        public uint IndexA;
        public uint IndexB;
        public uint IndexC;

        public TriangleIndices(uint indexA, uint indexB, uint indexC)
        {
            IndexA = indexA;
            IndexB = indexB;
            IndexC = indexC;
        }
    }

    private readonly TriangleIndices[] indices;
    private readonly JVector[] vertices;
    
    private readonly JBBox[] triangleBoxes;
    private Node[] nodes;
    private uint nodeCount;

    public JBBox Dimensions
    {
        get => nodes[0].Box;
    }

    public JVector[] Vertices => vertices;
    public TriangleIndices[] Indices => indices;

    public Octree(TriangleIndices[] indices, JVector[] vertices)
    {
        this.indices = indices;
        this.vertices = vertices;
        this.nodes = new Node[1024];
        this.triangleBoxes = new JBBox[indices.Length];
        Build();
    }

    private uint AllocateNode(in JBBox size)
    {
        if (nodes.Length == nodeCount)
        {
            Array.Resize(ref nodes, nodes.Length * 2);
        }

        nodes[nodeCount].Box = size;
        return nodeCount++;
    }

    private static JBBox[] Subdivide(in JBBox box)
    {
        JBBox[] result = new JBBox[8];

        for (uint i = 0; i < 8; i++)
        {
            JVector.Subtract(box.Max, box.Min, out var dims);
            JVector.Multiply(dims, 0.5f, out dims);

            JVector offset = new JVector((i & (1 << 0)) >> 0, (i & (1 << 1)) >> 1, (i & (1 << 2)) >> 2);

            result[i].Min = new JVector(offset.X * dims.X, offset.Y * dims.Y, offset.Z * dims.Z);
            JVector.Add(result[i].Min, box.Min, out result[i].Min);
            JVector.Add(result[i].Min, dims, out result[i].Max);

            // expand boxes by a tiny amount
            const float margin = 0.00001f;
            JVector.Multiply(dims, margin, out var temp);
            JVector.Subtract(result[i].Min, temp, out result[i].Min);
            JVector.Add(result[i].Max, temp, out result[i].Max);
        }

        return result;
    }

    private void InternalQuery(Stack<uint> triangles, in JBBox box, uint nodeIndex)
    {
        ref var node = ref nodes[nodeIndex];

        if (node.Box.Contains(box) == JBBox.ContainmentType.Disjoint)
            return;

        var tris = node.Triangles;

        if (tris != null)
        {
            foreach (var t in tris)
            {
                if (box.NotDisjoint(triangleBoxes[t]))
                {
                    triangles.Push(t);
                }
            }
        }

        for (int i = 0; i < 8; i++)
        {
            uint index = node.Neighbors[i];

            if (index != 0)
            {
                InternalQuery(triangles, box, index);
            }
        }
    }

    public void Query(Stack<uint> triangles, in JBBox box)
    {
        InternalQuery(triangles, box, 0);
    }

    private void Build()
    {
        for (int i = 0; i < indices.Length; i++)
        {
            ref JBBox triangleBox = ref triangleBoxes[i];
            ref var triangle = ref indices[i];

            triangleBox = JBBox.SmallBox;
            triangleBox.AddPoint(vertices[triangle.IndexA]);
            triangleBox.AddPoint(vertices[triangle.IndexB]);
            triangleBox.AddPoint(vertices[triangle.IndexC]);
        }

        JBBox box = JBBox.CreateFromPoints(vertices);
        
        JVector delta = box.Max - box.Min;
        JVector center = box.Center;

        float max = MathF.Max(MathF.Max(delta.X, delta.Y), delta.Z);
        delta = new JVector(max, max, max);

        box.Max = center + delta * 0.5f;
        box.Min = center - delta * 0.5f;

        AllocateNode(box);

        for (uint i = 0; i < indices.Length; i++)
        {
            AddNode(0, i);
        }
    }

    private int TestSubdivisions(JBBox[] box, uint triangle)
    {
        JBBox objBox = triangleBoxes[triangle];

        for (int i = 0; i < 8; i++)
        {
            if (box[i].Contains(objBox) == JBBox.ContainmentType.Contains)
            {
                return i;
            }
        }

        return -1;
    }

    private void AddNode(uint node, uint triangle)
    {
        const int maxDepth = 64;
        int depth = 0;
        
        while (true)
        {
            if (depth++ > maxDepth)
            {
                throw new InvalidOperationException("Maximum depth exceeded. " +
                                                    "Check you model for small or degenerate triangles.");
            }

            var subdivision = Subdivide(nodes[node].Box);
            int index = TestSubdivisions(subdivision, triangle);

            if (index == -1)
            {
                nodes[node].Triangles ??= new List<uint>(2);
                nodes[node].Triangles!.Add(triangle);
            }
            else
            {
                uint newNode = nodes[node].Neighbors[index];

                if (newNode == 0)
                {
                    newNode = AllocateNode(subdivision[index]);
                    nodes[node].Neighbors[index] = newNode;
                }

                node = newNode;
                continue;
            }

            break;
        }
    }
}