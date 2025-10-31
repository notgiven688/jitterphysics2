using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public JBoundingBox Box;
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

    private readonly JBoundingBox[] triangleBoxes;
    private Node[] nodes;
    private uint nodeCount;

    private int numLeafs;

    public JBoundingBox Dimensions => nodes[0].Box;

    public JVector[] Vertices => vertices;
    public TriangleIndices[] Indices => indices;

    public Octree(TriangleIndices[] indices, JVector[] vertices)
    {
        this.indices = indices;
        this.vertices = vertices;
        nodes = new Node[1024];
        triangleBoxes = new JBoundingBox[indices.Length];

        var sw = Stopwatch.StartNew();

        Build();

        Console.WriteLine($"Build octree ({indices.Length} triangles, {nodeCount} nodes, {numLeafs} leafs)" +
                          $" in {sw.ElapsedMilliseconds} ms.");
    }

    private uint AllocateNode(in JBoundingBox size)
    {
        if (nodes.Length == nodeCount)
        {
            Array.Resize(ref nodes, nodes.Length * 2);
        }

        nodes[nodeCount].Box = size;
        return nodeCount++;
    }

    private void InternalQuery(Stack<uint> triangles, in JBoundingBox box, uint nodeIndex)
    {
        ref var node = ref nodes[nodeIndex];

        if (node.Box.Contains(box) == JBoundingBox.ContainmentType.Disjoint)
        {
            return;
        }

        var tris = node.Triangles;

        if (tris != null)
        {
            foreach (var t in tris)
            {
                if (JBoundingBox.NotDisjoint(box, triangleBoxes[t]))
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

    public void Query(Stack<uint> triangles, in JBoundingBox box)
    {
        InternalQuery(triangles, box, 0);
    }

    private void Build()
    {
        for (int i = 0; i < indices.Length; i++)
        {
            ref JBoundingBox triangleBox = ref triangleBoxes[i];
            ref var triangle = ref indices[i];

            triangleBox = JBoundingBox.SmallBox;
            JBoundingBox.AddPointInPlace(ref triangleBox, vertices[triangle.IndexA]);
            JBoundingBox.AddPointInPlace(ref triangleBox, vertices[triangle.IndexB]);
            JBoundingBox.AddPointInPlace(ref triangleBox, vertices[triangle.IndexC]);
        }

        JBoundingBox box = JBoundingBox.CreateFromPoints(vertices);

        JVector delta = box.Max - box.Min;
        JVector center = box.Center;

        double max = Math.Max(Math.Max(delta.X, delta.Y), delta.Z);
        delta = new JVector(max, max, max);

        box.Max = center + delta * 0.5d;
        box.Min = center - delta * 0.5d;

        AllocateNode(box);

        for (uint i = 0; i < indices.Length; i++)
        {
            AddNode(0, i);
        }
    }

    public bool Raycast(in JVector origin, in JVector direction, out JVector normal, out double lambda)
    {
        lambda = float.MaxValue;
        normal = JVector.Zero;

        return InternalRaycast(origin, direction, 0, ref normal, ref lambda);
    }

    private bool InternalRaycast(in JVector origin, in JVector direction, uint nodeIndex, ref JVector normal, ref double lambda)
    {
        ref var node = ref nodes[nodeIndex];

        if (!node.Box.RayIntersect(origin, direction, out _))
            return false;

        bool hit = false;

        if (node.Triangles != null)
        {
            foreach (var triIdx in node.Triangles)
            {
                ref var triangleIndex = ref indices[triIdx];

                JTriangle tri = new JTriangle(vertices[triangleIndex.IndexA], vertices[triangleIndex.IndexB],
                    vertices[triangleIndex.IndexC]);

                if (tri.RayIntersect(origin, direction, JTriangle.CullMode.BackFacing,
                        out JVector currentNormal, out var currentLambda))
                {
                    if (currentLambda < lambda)
                    {
                        lambda = currentLambda;
                        normal = currentNormal;
                        hit = true;
                    }
                }
            }
        }

        for (int i = 0; i < 8; i++)
        {
            uint childIdx = node.Neighbors[i];
            if (childIdx == 0) continue;

            // TODO: Optimize traversal by only visiting child nodes whose bounding boxes
            // intersect the ray at a distance less than the current closest hit (lambda).
            // Sort the child nodes by ray-box intersection distance to prioritize nearer hits,
            // potentially avoiding unnecessary deeper traversal.

            if (InternalRaycast(origin, direction, childIdx,  ref normal, ref lambda))
            {
                hit = true;
            }
        }

        return hit;
    }

    private int TestSubdivision(in JBoundingBox parent, uint triangle)
    {
        JBoundingBox objBox = triangleBoxes[triangle];
        JVector center = parent.Center;

        int bits = 0;

        if (objBox.Min.X > center.X) bits |= 1;
        else if (objBox.Max.X > center.X) return -1;

        if (objBox.Min.Y > center.Y) bits |= 2;
        else if (objBox.Max.Y > center.Y) return -1;

        if (objBox.Min.Z > center.Z) bits |= 4;
        else if (objBox.Max.Z > center.Z) return -1;

        return bits;
    }

    private void GetSubdivison(in JBoundingBox parent, int index, out JBoundingBox result)
    {
        JVector.Subtract(parent.Max, parent.Min, out var dims);
        JVector.Multiply(dims, 0.5d, out dims);

        JVector offset = new JVector((index & (1 << 0)) >> 0, (index & (1 << 1)) >> 1, (index & (1 << 2)) >> 2);

        result.Min = new JVector(offset.X * dims.X, offset.Y * dims.Y, offset.Z * dims.Z);
        JVector.Add(result.Min, parent.Min, out result.Min);
        JVector.Add(result.Min, dims, out result.Max);

        const double margin = 1e-6d; // expand boxes by a tiny amount
        JVector.Multiply(dims, margin, out var temp);
        JVector.Subtract(result.Min, temp, out result.Min);
        JVector.Add(result.Max, temp, out result.Max);
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

            ref var nn = ref nodes[node];

            int index = TestSubdivision(nn.Box, triangle);

            if (index == -1)
            {
                if (nn.Triangles == null)
                {
                    nn.Triangles = new List<uint>(8);
                    numLeafs++;
                }

                nn.Triangles.Add(triangle);
            }
            else
            {
                uint newNode = nn.Neighbors[index];

                if (newNode == 0)
                {
                    GetSubdivison(nn.Box, index, out JBoundingBox newBox);
                    newNode = AllocateNode(newBox);
                    nodes[node].Neighbors[index] = newNode;
                }

                node = newNode;
                continue;
            }

            break;
        }
    }
}