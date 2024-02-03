using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class Mesh
{
    public struct Group
    {
        public string Name;
        public int FromInclusive;
        public int ToExclusive;
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

    private static IEnumerable<string> ReadFromZip(string filename)
    {
        using var zip = new ZipArchive(File.OpenRead(filename));
        if (zip.Entries.Count != 1)
        {
            throw new InvalidOperationException("Invalid zip file. There should be exactly one entry.");
        }

        using var sr = new StreamReader(zip.Entries[0].Open(), System.Text.Encoding.UTF8);
        while (!sr.EndOfStream)
        {
            yield return sr.ReadLine()!;
        }
    }

    public static Mesh LoadMesh(string filename, bool revertWinding = false)
    {
        var format = new NumberFormatInfo { NumberDecimalSeparator = "." };

        float ParseFloat(string str) => float.Parse(str, format);
        int ParseIndex(string str) => int.Parse(str) - 1;

        string[] content = filename.EndsWith(".zip") ? ReadFromZip(filename).ToArray() : File.ReadAllLines(filename);
        var lines = content.Select(s => s.Trim()).Where(s => s != string.Empty);

        List<Vector3> v = [], vn = [];
        List<Vector2> vt = [];

        foreach (string line in lines)
        {
            var s = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            switch (s[0])
            {
                case "v":
                    v.Add(new Vector3(ParseFloat(s[1]), ParseFloat(s[2]), ParseFloat(s[3])));
                    break;
                case "vn":
                    vn.Add(new Vector3(ParseFloat(s[1]), ParseFloat(s[2]), ParseFloat(s[3])));
                    break;
                case "vt":
                    vt.Add(new Vector2(ParseFloat(s[1]), ParseFloat(s[2])));
                    break;
            }
        }

        bool hasTexture = vt.Count > 0;
        bool hasNormals = vn.Count > 0;

        Dictionary<string, int> dict = new();
        List<Vertex> vertices = [];
        List<TriangleVertexIndex> indices = [];

        int AddVertex(string s)
        {
            var a = s.Split("/");
            if (hasTexture && hasNormals) vertices.Add(new Vertex(v[ParseIndex(a[0])], vn[ParseIndex(a[2])], vt[ParseIndex(a[1])]));
            else if (hasNormals) vertices.Add(new Vertex(v[ParseIndex(a[0])], vn[ParseIndex(a[2])]));
            else vertices.Add(new Vertex(v[ParseIndex(a[0])]));

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

        List<Group> groups = [];
        Group groupLast = new();
        bool firstGroup = true;

        foreach (string line in lines)
        {
            var s = line.Split(' ', StringSplitOptions.TrimEntries |
                                    StringSplitOptions.RemoveEmptyEntries);

            switch (s[0])
            {
                case "o":
                {
                    if (!firstGroup)
                    {
                        groupLast.ToExclusive = indices.Count;
                        groups.Add(groupLast);
                    }

                    firstGroup = false;
                    groupLast.FromInclusive = indices.Count;
                    groupLast.Name = s.Length > 1 ? s[1] : string.Empty;
                    break;
                }
                case "f":
                {
                    int i0 = dict[s[1]];
                    int i1 = dict[s[2]];
                    int i2 = dict[s[3]];

                    indices.Add(revertWinding ? new TriangleVertexIndex(i1, i0, i2) : new TriangleVertexIndex(i0, i1, i2));
                    break;
                }
            }
        }

        if (!firstGroup)
        {
            groupLast.ToExclusive = indices.Count;
            groups.Add(groupLast);
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), groups.ToArray());
    }
}