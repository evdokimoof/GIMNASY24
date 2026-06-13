using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;
using Gimnasy.Core.Terrain;

namespace Gimnasy.Nodes;

// ===========================================================================
//  Terrain node — Roblox-Studio-style editable ground. The node owns a
//  TerrainData height-field; the editor brush tools (and gameplay code) call
//  SculptWorld to raise/lower/smooth/flatten/paint the land in real time.
// ===========================================================================

[RegisteredType("Terrain3D", "3D/Terrain")]
public sealed class Terrain3D : Node3D
{
    [Export] public TerrainData Data { get; set; } = new TerrainData();
    [Export] public Material? MaterialLayer0 { get; set; } // grass
    [Export] public Material? MaterialLayer1 { get; set; } // rock
    [Export] public Material? MaterialLayer2 { get; set; } // sand
    [Export] public Material? MaterialLayer3 { get; set; } // snow
    [Export("range:0,1")] public float DefaultBrushFalloff { get; set; } = 0.5f;
    [Export] public bool CollisionEnabled { get; set; } = true;

    /// <summary>Convert a world position to terrain-local XZ (cells start at the
    /// node origin). Y is ignored — the brush works on the column.</summary>
    public Vector2 WorldToLocalXZ(Vector3 world)
    {
        Vector3 local = world - GlobalPosition;
        return new Vector2(local.X, local.Z);
    }

    /// <summary>Apply a brush at a world position. This is what an editor's
    /// click-drag or an in-game terrain-deform ability calls.</summary>
    public int SculptWorld(TerrainBrush brush, Vector3 worldPos, float radius, float strength,
        float targetHeight = 0f, byte material = 0)
    {
        return Data.Sculpt(brush, WorldToLocalXZ(worldPos), radius, strength,
            targetHeight, material, DefaultBrushFalloff);
    }

    /// <summary>Sample terrain height (world Y) under a world position — handy
    /// for snapping props or characters to the ground.</summary>
    public float HeightAtWorld(Vector3 worldPos)
    {
        Vector2 local = WorldToLocalXZ(worldPos);
        return GlobalPosition.Y + Data.SampleHeight(local.X, local.Y);
    }

    /// <summary>Generate a procedural base landscape.</summary>
    public void Generate(int seed = 1337, float amplitude = 0.5f, int octaves = 5) =>
        Data.GenerateFractal(seed, amplitude, octaves);
}

/// <summary>Editor-side brush configuration for the terrain tools panel.</summary>
[RegisteredType("TerrainBrushSettings", "3D/Terrain")]
public sealed class TerrainBrushSettings : Gimnasy.Core.Scene.Node
{
    [Export] public TerrainBrush Mode { get; set; } = TerrainBrush.Add;
    [Export("range:0.5,200")] public float Radius { get; set; } = 8f;
    [Export("range:0,50")] public float Strength { get; set; } = 2f;
    [Export("range:0,1")] public float Falloff { get; set; } = 0.5f;
    [Export] public float TargetHeight { get; set; }
    [Export("range:0,7")] public int PaintMaterial { get; set; }
    [Export] public bool SnapToGrid { get; set; }
}
