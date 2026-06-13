using Gimnasy.Core.Math;

namespace Gimnasy.Core.Geometry;

/// <summary>
/// Non-destructive-ish modelling operations on an <see cref="EditableMesh"/>:
/// subdivision, extrusion, laplacian smoothing and normal flipping. These are
/// the building blocks a modelling/edit mode exposes as menu commands.
/// </summary>
public static class MeshEditing
{
    /// <summary>Split every triangle into four (1→4 midpoint subdivision).</summary>
    public static void SubdivideMidpoint(EditableMesh mesh)
    {
        var src = mesh.Indices.ToArray();
        var midpointCache = new Dictionary<(int, int), int>();

        int Midpoint(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (midpointCache.TryGetValue(key, out int existing)) return existing;
            Vector3 p = (mesh.Vertices[a] + mesh.Vertices[b]) * 0.5f;
            Vector2 uv = (mesh.Uvs[a] + mesh.Uvs[b]) * 0.5f;
            int idx = mesh.AddVertex(p, uv);
            midpointCache[key] = idx;
            return idx;
        }

        mesh.Indices.Clear();
        for (int t = 0; t < src.Length; t += 3)
        {
            int a = src[t], b = src[t + 1], c = src[t + 2];
            int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
            mesh.AddTriangle(a, ab, ca);
            mesh.AddTriangle(b, bc, ab);
            mesh.AddTriangle(c, ca, bc);
            mesh.AddTriangle(ab, bc, ca);
        }
        mesh.RecalculateNormals();
    }

    /// <summary>Push every vertex out along its normal (a uniform extrude/shell).</summary>
    public static void ExtrudeAlongNormals(EditableMesh mesh, float distance)
    {
        mesh.RecalculateNormals();
        for (int i = 0; i < mesh.Vertices.Count; i++)
            mesh.Vertices[i] += mesh.Normals[i] * distance;
        mesh.RecalculateNormals();
    }

    /// <summary>Laplacian smoothing: relax vertices toward neighbour averages.</summary>
    public static void Smooth(EditableMesh mesh, int iterations = 1, float factor = 0.5f)
    {
        var adjacency = BuildAdjacency(mesh);
        for (int it = 0; it < iterations; it++)
        {
            var updated = new Vector3[mesh.Vertices.Count];
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var neighbours = adjacency[i];
                if (neighbours.Count == 0) { updated[i] = mesh.Vertices[i]; continue; }
                Vector3 avg = Vector3.Zero;
                foreach (int n in neighbours) avg += mesh.Vertices[n];
                avg /= neighbours.Count;
                updated[i] = mesh.Vertices[i].Lerp(avg, factor);
            }
            for (int i = 0; i < updated.Length; i++) mesh.Vertices[i] = updated[i];
        }
        mesh.RecalculateNormals();
    }

    /// <summary>Reverse winding so faces point the other way.</summary>
    public static void FlipNormals(EditableMesh mesh)
    {
        for (int t = 0; t < mesh.Indices.Count; t += 3)
            (mesh.Indices[t + 1], mesh.Indices[t + 2]) = (mesh.Indices[t + 2], mesh.Indices[t + 1]);
        mesh.RecalculateNormals();
    }

    private static List<HashSet<int>> BuildAdjacency(EditableMesh mesh)
    {
        var adj = new List<HashSet<int>>(mesh.Vertices.Count);
        for (int i = 0; i < mesh.Vertices.Count; i++) adj.Add(new HashSet<int>());
        for (int t = 0; t < mesh.Indices.Count; t += 3)
        {
            int a = mesh.Indices[t], b = mesh.Indices[t + 1], c = mesh.Indices[t + 2];
            adj[a].Add(b); adj[a].Add(c);
            adj[b].Add(a); adj[b].Add(c);
            adj[c].Add(a); adj[c].Add(b);
        }
        return adj;
    }
}
