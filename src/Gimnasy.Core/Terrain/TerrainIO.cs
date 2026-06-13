using System.Text.Json;
using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Serialization;

namespace Gimnasy.Core.Terrain;

/// <summary>
/// Reads and writes the full <c>.terrain</c> document — a large JSON file that
/// carries everything needed to reconstruct the landscape exactly: dimensions,
/// the generation descriptor, water, every texture layer with its placement
/// rules, the complete heightmap, per-layer splat weights, the hole mask, baked
/// normals, an LOD-chunk manifest and computed statistics. This is the "huge
/// JSON like a 3D model" form of the terrain.
/// </summary>
public static class TerrainIO
{
    public static string Serialize(TerrainData data, bool includeNormals = true, bool includeChunks = true)
    {
        int res = data.Resolution;
        var stats = data.ComputeStats();

        var doc = new Dictionary<string, object?>
        {
            ["format"] = 2,
            ["type"] = "Terrain",
            ["name"] = string.IsNullOrEmpty(data.Name) ? "Terrain" : data.Name,
            ["dimensions"] = new Dictionary<string, object?>
            {
                ["resolution"] = res,
                ["cell_size"] = data.CellSize,
                ["max_height"] = data.MaxHeight,
                ["world_size"] = data.WorldSize,
            },
            ["water"] = new Dictionary<string, object?>
            {
                ["enabled"] = data.WaterEnabled,
                ["level"] = data.WaterLevel,
                ["color"] = ColorArr(data.WaterColor),
            },
            ["generation"] = new Dictionary<string, object?>
            {
                ["seed"] = data.Seed,
                ["octaves"] = data.Octaves,
                ["lacunarity"] = data.Lacunarity,
                ["gain"] = data.Gain,
                ["base_frequency"] = data.BaseFrequency,
                ["ridged"] = data.Ridged,
                ["domain_warp"] = data.DomainWarp,
            },
            ["layers"] = SerializeLayers(data),
            ["heightmap"] = new Dictionary<string, object?>
            {
                ["encoding"] = "f32-array",
                ["width"] = res,
                ["height"] = res,
                ["data"] = ToObjectList(data.Heights),
            },
            ["splatmaps"] = SerializeSplats(data),
            ["holes"] = new Dictionary<string, object?>
            {
                ["encoding"] = "indices",
                ["data"] = HoleIndices(data),
            },
            ["stats"] = new Dictionary<string, object?>
            {
                ["min_height"] = stats.MinHeight,
                ["max_height"] = stats.MaxHeight,
                ["mean_height"] = stats.MeanHeight,
                ["max_slope_degrees"] = stats.MaxSlopeDegrees,
                ["hole_count"] = stats.HoleCount,
                ["vertex_count"] = stats.VertexCount,
                ["triangle_count"] = stats.TriangleCount,
            },
        };

        if (includeNormals)
        {
            data.BakeNormals();
            doc["normals"] = new Dictionary<string, object?>
            {
                ["encoding"] = "vec3-array",
                ["data"] = NormalsToList(data.BakedNormals),
            };
        }

        if (includeChunks)
        {
            var chunks = TerrainMeshBuilder.BuildChunked(data);
            doc["chunks"] = new Dictionary<string, object?>
            {
                ["chunk_resolution"] = 33,
                ["count"] = chunks.Count,
                ["manifest"] = chunks.Select(c => (object)new Dictionary<string, object?>
                {
                    ["x"] = c.ChunkX, ["z"] = c.ChunkZ, ["lod"] = c.Lod,
                    ["vertices"] = c.Mesh.VertexCount, ["triangles"] = c.Mesh.TriangleCount,
                    ["bounds_min"] = Vec3Arr(c.Bounds.Position),
                    ["bounds_size"] = Vec3Arr(c.Bounds.Size),
                }).ToList(),
            };
        }

        return JsonLike.Write(doc, "Gimnasy Terrain — full landscape document");
    }

    public static void Save(TerrainData data, string path) =>
        System.IO.File.WriteAllText(path, Serialize(data));

    public static TerrainData Deserialize(string text)
    {
        using var json = JsonLike.Parse(text);
        var root = json.RootElement;
        var data = new TerrainData();

        if (root.TryGetProperty("name", out var nm)) data.Name = nm.GetString() ?? "Terrain";

        if (root.TryGetProperty("dimensions", out var dim))
        {
            data.Resolution = dim.GetProperty("resolution").GetInt32();
            data.CellSize = (float)dim.GetProperty("cell_size").GetDouble();
            data.MaxHeight = (float)dim.GetProperty("max_height").GetDouble();
        }

        if (root.TryGetProperty("water", out var water))
        {
            data.WaterEnabled = water.GetProperty("enabled").GetBoolean();
            data.WaterLevel = (float)water.GetProperty("level").GetDouble();
            data.WaterColor = ReadColor(water.GetProperty("color"));
        }

        if (root.TryGetProperty("generation", out var gen))
        {
            data.Seed = gen.GetProperty("seed").GetInt32();
            data.Octaves = gen.GetProperty("octaves").GetInt32();
            data.Lacunarity = (float)gen.GetProperty("lacunarity").GetDouble();
            data.Gain = (float)gen.GetProperty("gain").GetDouble();
            data.BaseFrequency = (float)gen.GetProperty("base_frequency").GetDouble();
            data.Ridged = (float)gen.GetProperty("ridged").GetDouble();
            data.DomainWarp = (float)gen.GetProperty("domain_warp").GetDouble();
        }

        if (root.TryGetProperty("layers", out var layers))
            foreach (var le in layers.EnumerateArray())
            {
                var layer = new TerrainLayer();
                if (le.TryGetProperty("properties", out var props)) PropertyBag.Apply(layer, props);
                data.Layers.Add(layer);
            }

        // Height/splat/holes need the buffers sized — touching them allocates.
        if (root.TryGetProperty("heightmap", out var hm))
            data.LoadHeights(ReadFloatArray(hm.GetProperty("data")));

        if (root.TryGetProperty("splatmaps", out var splats))
            foreach (var se in splats.EnumerateArray())
            {
                int layer = se.GetProperty("layer").GetInt32();
                data.LoadSplatLayer(layer, ReadFloatArray(se.GetProperty("data")));
            }

        if (root.TryGetProperty("holes", out var holes) && holes.TryGetProperty("data", out var holeData))
        {
            var mask = new bool[data.SampleCount];
            foreach (var idx in holeData.EnumerateArray())
            {
                int i = idx.GetInt32();
                if (i >= 0 && i < mask.Length) mask[i] = true;
            }
            data.LoadHoles(mask);
        }

        data.BakeNormals();
        return data;
    }

    public static TerrainData Load(string path) =>
        Deserialize(System.IO.File.ReadAllText(path));

    // ---- helpers ----------------------------------------------------------

    private static List<object> SerializeLayers(TerrainData data)
    {
        var list = new List<object>();
        foreach (var layer in data.Layers)
            list.Add(new Dictionary<string, object?>
            {
                ["type"] = layer.ResourceType,
                ["properties"] = PropertyBag.Capture(layer),
            });
        return list;
    }

    private static List<object> SerializeSplats(TerrainData data)
    {
        var list = new List<object>();
        for (int l = 0; l < data.LayerCount; l++)
            list.Add(new Dictionary<string, object?>
            {
                ["layer"] = l,
                ["encoding"] = "f32-array",
                ["data"] = ToObjectList(data.SplatLayer(l)),
            });
        return list;
    }

    private static List<object> ToObjectList(float[] src)
    {
        var list = new List<object>(src.Length);
        foreach (var f in src) list.Add(System.Math.Round(f, 4));
        return list;
    }

    private static List<object> NormalsToList(Vector3[] normals)
    {
        var list = new List<object>(normals.Length * 3);
        foreach (var n in normals)
        {
            list.Add(System.Math.Round(n.X, 4));
            list.Add(System.Math.Round(n.Y, 4));
            list.Add(System.Math.Round(n.Z, 4));
        }
        return list;
    }

    private static List<object> HoleIndices(TerrainData data)
    {
        var list = new List<object>();
        var holes = data.Holes;
        for (int i = 0; i < holes.Length; i++) if (holes[i]) list.Add(i);
        return list;
    }

    private static float[] ReadFloatArray(JsonElement el)
    {
        var list = new List<float>(el.GetArrayLength());
        foreach (var e in el.EnumerateArray()) list.Add((float)e.GetDouble());
        return list.ToArray();
    }

    private static List<object> ColorArr(Color c) => new() { c.R, c.G, c.B, c.A };
    private static List<object> Vec3Arr(Vector3 v) => new() { v.X, v.Y, v.Z };

    private static Color ReadColor(JsonElement el)
    {
        var a = ReadFloatArray(el);
        return new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1f);
    }
}
