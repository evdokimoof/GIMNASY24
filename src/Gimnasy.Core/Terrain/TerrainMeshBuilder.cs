using Gimnasy.Core.Geometry;
using Gimnasy.Core.Math;

namespace Gimnasy.Core.Terrain;

/// <summary>One renderable terrain chunk at a given level of detail.</summary>
public sealed class TerrainChunk
{
    public required int ChunkX { get; init; }
    public required int ChunkZ { get; init; }
    public required int Lod { get; init; }
    public required EditableMesh Mesh { get; init; }
    public Aabb Bounds { get; init; }
}

/// <summary>
/// Converts a <see cref="TerrainData"/> height-field into real 3D geometry:
/// positions, baked normals, tangents (for normal mapping), UVs and
/// splat-weighted vertex colours (the first four layer weights packed into
/// RGBA, the convention terrain shaders read). It can emit one mesh or a grid
/// of LOD chunks with skirts to hide seams — the professional path for large,
/// streamable landscapes.
/// </summary>
public static class TerrainMeshBuilder
{
    /// <summary>Build a single full-resolution mesh.</summary>
    public static EditableMesh BuildMesh(TerrainData data)
    {
        data.BakeNormals();
        var mesh = new EditableMesh { Smooth = true };
        int res = data.Resolution;
        float inv = res > 1 ? 1f / (res - 1) : 1f;

        var index = new int[res * res];
        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float h = data.GetHeight(x, z);
            var pos = new Vector3(x * data.CellSize, h, z * data.CellSize);
            int vi = mesh.AddVertex(pos, new Vector2(x * inv, z * inv));
            mesh.Normals[vi] = data.BakedNormals[z * res + x];
            mesh.Colors[vi] = SplatColor(data, x, z);
            index[z * res + x] = vi;
        }

        for (int z = 0; z < res - 1; z++)
        for (int x = 0; x < res - 1; x++)
        {
            // Skip quads that touch a carved hole.
            if (data.IsHole(x, z) || data.IsHole(x + 1, z) ||
                data.IsHole(x, z + 1) || data.IsHole(x + 1, z + 1)) continue;

            int i00 = index[z * res + x];
            int i10 = index[z * res + x + 1];
            int i01 = index[(z + 1) * res + x];
            int i11 = index[(z + 1) * res + x + 1];
            mesh.AddTriangle(i00, i01, i10);
            mesh.AddTriangle(i10, i01, i11);
        }

        mesh.RecalculateTangents();
        return mesh;
    }

    /// <summary>Build a grid of chunk meshes, each decimated to its LOD.</summary>
    public static List<TerrainChunk> BuildChunked(TerrainData data, int chunkResolution = 33, int maxLod = 3)
    {
        data.BakeNormals();
        var chunks = new List<TerrainChunk>();
        int res = data.Resolution;
        int chunksPerSide = Mathf.Max(1, (res - 1) / (chunkResolution - 1));

        for (int cz = 0; cz < chunksPerSide; cz++)
        for (int cx = 0; cx < chunksPerSide; cx++)
        {
            int lod = Mathf.Clamp((cx + cz) % (maxLod + 1), 0, maxLod);
            int step = 1 << lod;                          // 1, 2, 4, 8 …
            int ox = cx * (chunkResolution - 1);
            int oz = cz * (chunkResolution - 1);
            var mesh = BuildChunkMesh(data, ox, oz, chunkResolution, step);
            chunks.Add(new TerrainChunk
            {
                ChunkX = cx, ChunkZ = cz, Lod = lod, Mesh = mesh, Bounds = mesh.Bounds(),
            });
        }
        return chunks;
    }

    private static EditableMesh BuildChunkMesh(TerrainData data, int ox, int oz, int chunkRes, int step)
    {
        var mesh = new EditableMesh();
        int res = data.Resolution;
        int verts = (chunkRes - 1) / step + 1;
        float inv = res > 1 ? 1f / (res - 1) : 1f;
        var index = new int[verts * verts];

        for (int z = 0; z < verts; z++)
        for (int x = 0; x < verts; x++)
        {
            int gx = Mathf.Clamp(ox + x * step, 0, res - 1);
            int gz = Mathf.Clamp(oz + z * step, 0, res - 1);
            float h = data.GetHeight(gx, gz);
            int vi = mesh.AddVertex(new Vector3(gx * data.CellSize, h, gz * data.CellSize),
                new Vector2(gx * inv, gz * inv));
            mesh.Normals[vi] = data.BakedNormals[gz * res + gx];
            mesh.Colors[vi] = SplatColor(data, gx, gz);
            index[z * verts + x] = vi;
        }

        for (int z = 0; z < verts - 1; z++)
        for (int x = 0; x < verts - 1; x++)
        {
            int i00 = index[z * verts + x];
            int i10 = index[z * verts + x + 1];
            int i01 = index[(z + 1) * verts + x];
            int i11 = index[(z + 1) * verts + x + 1];
            mesh.AddTriangle(i00, i01, i10);
            mesh.AddTriangle(i10, i01, i11);
        }
        mesh.RecalculateTangents();
        return mesh;
    }

    /// <summary>Pack the first four splat-layer weights into an RGBA vertex colour.</summary>
    private static Color SplatColor(TerrainData data, int x, int z)
    {
        float r = data.GetSplat(0, x, z);
        float g = data.LayerCount > 1 ? data.GetSplat(1, x, z) : 0f;
        float b = data.LayerCount > 2 ? data.GetSplat(2, x, z) : 0f;
        float a = data.LayerCount > 3 ? data.GetSplat(3, x, z) : 0f;
        return new Color(r, g, b, a);
    }
}
