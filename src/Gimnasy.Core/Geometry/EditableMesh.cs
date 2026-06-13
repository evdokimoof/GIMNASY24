using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Core.Geometry;

/// <summary>
/// A CPU-side, editable triangle mesh: positions, normals, UVs and an index
/// buffer. This is the data the sculpt brushes (<see cref="MeshSculptor"/>) and
/// modelling operations (<see cref="MeshEditing"/>) mutate, and what a GPU mesh
/// is uploaded from. It is deliberately a simple SoA layout so operations are
/// easy to reason about and test.
/// </summary>
[RegisteredType("EditableMesh", "Mesh")]
public sealed class EditableMesh : Resource
{
    public override string ResourceType => "EditableMesh";

    public List<Vector3> Vertices { get; } = new();
    public List<Vector3> Normals { get; } = new();
    public List<Vector4> Tangents { get; } = new();  // xyz + handedness sign in w
    public List<Vector2> Uvs { get; } = new();
    public List<Color> Colors { get; } = new();      // per-vertex colour
    public List<int> Indices { get; } = new();

    [Export] public bool Smooth { get; set; } = true;

    public int VertexCount => Vertices.Count;
    public int TriangleCount => Indices.Count / 3;

    public int AddVertex(Vector3 position, Vector2 uv = default)
    {
        Vertices.Add(position);
        Normals.Add(Vector3.Up);
        Tangents.Add(new Vector4(1, 0, 0, 1));
        Uvs.Add(uv);
        Colors.Add(Color.White);
        return Vertices.Count - 1;
    }

    public void AddTriangle(int a, int b, int c)
    {
        Indices.Add(a); Indices.Add(b); Indices.Add(c);
    }

    public void Clear()
    {
        Vertices.Clear(); Normals.Clear(); Tangents.Clear();
        Uvs.Clear(); Colors.Clear(); Indices.Clear();
    }

    /// <summary>Compute per-vertex tangents from UVs (Lengyel's method) for
    /// correct normal-mapping. Requires UVs and up-to-date normals.</summary>
    public void RecalculateTangents()
    {
        int n = Vertices.Count;
        var tan = new Vector3[n];
        var bitan = new Vector3[n];
        for (int t = 0; t < Indices.Count; t += 3)
        {
            int i0 = Indices[t], i1 = Indices[t + 1], i2 = Indices[t + 2];
            Vector3 e1 = Vertices[i1] - Vertices[i0], e2 = Vertices[i2] - Vertices[i0];
            Vector2 d1 = Uvs[i1] - Uvs[i0], d2 = Uvs[i2] - Uvs[i0];
            float denom = d1.X * d2.Y - d2.X * d1.Y;
            float r = Mathf.Abs(denom) < Mathf.Epsilon ? 0f : 1f / denom;
            Vector3 sdir = (e1 * d2.Y - e2 * d1.Y) * r;
            Vector3 tdir = (e2 * d1.X - e1 * d2.X) * r;
            tan[i0] += sdir; tan[i1] += sdir; tan[i2] += sdir;
            bitan[i0] += tdir; bitan[i1] += tdir; bitan[i2] += tdir;
        }
        for (int i = 0; i < n; i++)
        {
            Vector3 nrm = Normals[i];
            Vector3 t3 = (tan[i] - nrm * nrm.Dot(tan[i])).Normalized;     // Gram-Schmidt
            float w = nrm.Cross(tan[i]).Dot(bitan[i]) < 0f ? -1f : 1f;
            Tangents[i] = new Vector4(t3.X, t3.Y, t3.Z, w);
        }
    }

    /// <summary>Recompute smooth per-vertex normals from face geometry.</summary>
    public void RecalculateNormals()
    {
        for (int i = 0; i < Normals.Count; i++) Normals[i] = Vector3.Zero;
        for (int t = 0; t < Indices.Count; t += 3)
        {
            int ia = Indices[t], ib = Indices[t + 1], ic = Indices[t + 2];
            Vector3 n = (Vertices[ib] - Vertices[ia]).Cross(Vertices[ic] - Vertices[ia]);
            Normals[ia] += n; Normals[ib] += n; Normals[ic] += n;
        }
        for (int i = 0; i < Normals.Count; i++) Normals[i] = Normals[i].Normalized;
    }

    public Aabb Bounds()
    {
        if (Vertices.Count == 0) return new Aabb(Vector3.Zero, Vector3.Zero);
        Vector3 min = Vertices[0], max = Vertices[0];
        foreach (var v in Vertices)
        {
            min = new Vector3(Mathf.Min(min.X, v.X), Mathf.Min(min.Y, v.Y), Mathf.Min(min.Z, v.Z));
            max = new Vector3(Mathf.Max(max.X, v.X), Mathf.Max(max.Y, v.Y), Mathf.Max(max.Z, v.Z));
        }
        return new Aabb(min, max - min);
    }

    // ---- Primitive factories ----------------------------------------------

    public static EditableMesh CreatePlane(int subdivisions = 8, float size = 2f)
    {
        var m = new EditableMesh();
        int n = subdivisions + 1;
        for (int z = 0; z < n; z++)
        for (int x = 0; x < n; x++)
        {
            float fx = (float)x / subdivisions, fz = (float)z / subdivisions;
            m.AddVertex(new Vector3((fx - 0.5f) * size, 0, (fz - 0.5f) * size), new Vector2(fx, fz));
        }
        for (int z = 0; z < subdivisions; z++)
        for (int x = 0; x < subdivisions; x++)
        {
            int i = z * n + x;
            m.AddTriangle(i, i + 1, i + n);
            m.AddTriangle(i + 1, i + n + 1, i + n);
        }
        m.RecalculateNormals();
        return m;
    }

    public static EditableMesh CreateBox(Vector3 size)
    {
        var m = new EditableMesh();
        Vector3 h = size * 0.5f;
        Vector3[] c =
        {
            new(-h.X,-h.Y,-h.Z), new(h.X,-h.Y,-h.Z), new(h.X,h.Y,-h.Z), new(-h.X,h.Y,-h.Z),
            new(-h.X,-h.Y, h.Z), new(h.X,-h.Y, h.Z), new(h.X,h.Y, h.Z), new(-h.X,h.Y, h.Z),
        };
        foreach (var v in c) m.AddVertex(v);
        int[][] faces =
        {
            new[]{0,1,2,3}, new[]{5,4,7,6}, new[]{4,0,3,7},
            new[]{1,5,6,2}, new[]{3,2,6,7}, new[]{4,5,1,0},
        };
        foreach (var f in faces)
        {
            m.AddTriangle(f[0], f[1], f[2]);
            m.AddTriangle(f[0], f[2], f[3]);
        }
        m.RecalculateNormals();
        return m;
    }
}
