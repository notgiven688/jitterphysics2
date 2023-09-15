using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class Mesh
{
    public struct Group
    {
        public string Name;
        public int FromInclusive;
        public int ToExlusive;
    }

    public readonly Vertex[] Vertices;
    public readonly TriangleVertexIndex[] Indices;
    public readonly Group[] Groups;

    private Mesh(Vertex[] vertices, TriangleVertexIndex[] indices, Group[] groups)
    {
        Vertices = vertices;
        Indices = indices;
        Groups = groups;
    }

    public void Transform(Matrix4 matrix)
    {
        for (int i = 0; i < Vertices.Length; i++)
        {
            Vertices[i].Position = Vector3.Transform(Vertices[i].Position, matrix);
        }
    }

    public static Mesh LoadMesh(string filename, bool revertWinding = false)
    {
        var format = new NumberFormatInfo { NumberDecimalSeparator = "." };

        float PF(string str)
        {
            return float.Parse(str, format);
        } // Parse float

        int PIm1(string str)
        {
            return int.Parse(str) - 1;
        } // Parse int minus 1

        var lines = File.ReadAllLines(filename).Select(s => s.Trim()).Where(s => s != string.Empty);

        List<Vector3> v = new();
        List<Vector3> vn = new();
        List<Vector2> vt = new();

        foreach (string line in lines)
        {
            var s = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (s[0] == "v") v.Add(new Vector3(PF(s[1]), PF(s[2]), PF(s[3])));
            if (s[0] == "vn") vn.Add(new Vector3(PF(s[1]), PF(s[2]), PF(s[3])));
            if (s[0] == "vt") vt.Add(new Vector2(PF(s[1]), PF(s[2])));
        }

        bool hasTexture = vt.Count > 0;
        bool hasNormals = vn.Count > 0;

        Dictionary<string, int> dict = new();
        List<Vertex> vertices = new();
        List<TriangleVertexIndex> indices = new();

        int AddVertex(string s)
        {
            var a = s.Split("/");
            if (hasTexture && hasNormals) vertices.Add(new Vertex(v[PIm1(a[0])], vn[PIm1(a[2])], vt[PIm1(a[1])]));
            else if (hasNormals) vertices.Add(new Vertex(v[PIm1(a[0])], vn[PIm1(a[2])]));
            else vertices.Add(new Vertex(v[PIm1(a[0])]));

            return vertices.Count - 1;
        }

        foreach (string line in lines)
        {
            var s = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (s[0] != "f") continue;

            for (int i = 1; i <= 3; i++)
            {
                if (!dict.TryGetValue(s[i], out int index))
                {
                    index = AddVertex(s[i]);
                    dict.Add(s[i], index);
                }
            }
        }

        List<Group> groups = new();
        Group glast = new();
        bool firstg = true;

        foreach (string line in lines)
        {
            var s = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (s[0] == "o")
            {
                if (!firstg)
                {
                    glast.ToExlusive = indices.Count;
                    groups.Add(glast);
                }

                firstg = false;
                glast.FromInclusive = indices.Count;
                glast.Name = s.Length > 1 ? s[1] : string.Empty;
            }
            else if (s[0] == "f")
            {
                int i0 = dict[s[1]];
                int i1 = dict[s[2]];
                int i2 = dict[s[3]];

                if (revertWinding) indices.Add(new TriangleVertexIndex(i1, i0, i2));
                else indices.Add(new TriangleVertexIndex(i0, i1, i2));
            }
        }

        if (!firstg)
        {
            glast.ToExlusive = indices.Count;
            groups.Add(glast);
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), groups.ToArray());
    }
}