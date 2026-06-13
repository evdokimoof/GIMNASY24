using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  3D node catalog: meshes, cameras, lights, environment, particles, paths.
// ===========================================================================

[RegisteredType("MeshInstance3D", "3D")]
public class MeshInstance3D : Node3D
{
    [Export] public Mesh? Mesh { get; set; }
    [Export] public Material? MaterialOverride { get; set; }
    [Export] public bool CastShadow { get; set; } = true;
    [Export("range:0,1")] public float Transparency { get; set; }
}

[RegisteredType("MultiMeshInstance3D", "3D")]
public sealed class MultiMeshInstance3D : Node3D
{
    [Export] public Mesh? Mesh { get; set; }
    [Export] public int InstanceCount { get; set; }
}

[RegisteredType("Camera3D", "3D")]
public sealed class Camera3D : Node3D
{
    public enum ProjectionType { Perspective, Orthogonal }
    [Export] public bool Current { get; set; } = true;
    [Export] public ProjectionType Projection { get; set; } = ProjectionType.Perspective;
    [Export("range:1,179")] public float Fov { get; set; } = 75f;
    [Export] public float Near { get; set; } = 0.05f;
    [Export] public float Far { get; set; } = 4000f;
    [Export] public float Size { get; set; } = 1f; // orthogonal size
    [Export] public Vector3 HOffset { get; set; }
}

public abstract class Light3D : Node3D
{
    [Export] public Color LightColor { get; set; } = Color.White;
    [Export("range:0,16")] public float LightEnergy { get; set; } = 1f;
    [Export] public bool ShadowEnabled { get; set; } = true;
    [Export("range:0,1")] public float ShadowBias { get; set; } = 0.1f;
}

[RegisteredType("DirectionalLight3D", "3D/Light")]
public sealed class DirectionalLight3D : Light3D
{
    [Export] public bool SkyContribution { get; set; } = true;
    [Export] public int ShadowMode { get; set; } = 2;
}

[RegisteredType("OmniLight3D", "3D/Light")]
public sealed class OmniLight3D : Light3D
{
    [Export("range:0,4096")] public float Range { get; set; } = 5f;
    [Export("range:0,4")] public float Attenuation { get; set; } = 1f;
}

[RegisteredType("SpotLight3D", "3D/Light")]
public sealed class SpotLight3D : Light3D
{
    [Export("range:0,4096")] public float Range { get; set; } = 5f;
    [Export("range:0,180")] public float SpotAngleDegrees { get; set; } = 45f;
    [Export("range:0,1")] public float SpotAngleAttenuation { get; set; } = 1f;
}

[RegisteredType("WorldEnvironment", "3D")]
public sealed class WorldEnvironment : Gimnasy.Core.Scene.Node
{
    [Export] public Color AmbientColor { get; set; } = new Color(0.2f, 0.2f, 0.25f);
    [Export("range:0,16")] public float AmbientEnergy { get; set; } = 1f;
    [Export] public Color FogColor { get; set; } = new Color(0.5f, 0.6f, 0.7f);
    [Export] public bool FogEnabled { get; set; }
    [Export("range:0,1")] public float FogDensity { get; set; } = 0.01f;
    [Export] public bool GlowEnabled { get; set; }
    [Export("range:0,8")] public float Exposure { get; set; } = 1f;
}

[RegisteredType("ReflectionProbe", "3D")]
public sealed class ReflectionProbe : Node3D
{
    [Export] public Vector3 BoxSize { get; set; } = new Vector3(20, 20, 20);
    [Export("range:0,16")] public float Intensity { get; set; } = 1f;
}

[RegisteredType("Decal", "3D")]
public sealed class Decal : Node3D
{
    [Export] public Vector3 BoxSize { get; set; } = new Vector3(2, 2, 2);
    [Export] public Texture2D? Albedo { get; set; }
    [Export] public Color Modulate { get; set; } = Color.White;
}

[RegisteredType("GPUParticles3D", "3D")]
public sealed class GpuParticles3D : Node3D
{
    [Export] public bool Emitting { get; set; } = true;
    [Export] public int Amount { get; set; } = 8;
    [Export] public float Lifetime { get; set; } = 1f;
    [Export] public Mesh? DrawMesh { get; set; }
}

[RegisteredType("CPUParticles3D", "3D")]
public sealed class CpuParticles3D : Node3D
{
    [Export] public bool Emitting { get; set; } = true;
    [Export] public int Amount { get; set; } = 8;
    [Export] public Vector3 Gravity { get; set; } = new Vector3(0, -9.8f, 0);
}

[RegisteredType("Path3D", "3D")]
public sealed class Path3D : Node3D
{
    [Export] public Curve? Curve { get; set; }
}

[RegisteredType("PathFollow3D", "3D")]
public sealed class PathFollow3D : Node3D
{
    [Export] public float Progress { get; set; }
    [Export("range:0,1")] public float ProgressRatio { get; set; }
    [Export] public bool Loop { get; set; } = true;
}

[RegisteredType("RayCast3D", "3D")]
public sealed class RayCast3D : Node3D
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Vector3 TargetPosition { get; set; } = new Vector3(0, -1, 0);
    [Export] public int CollisionMask { get; set; } = 1;
}

[RegisteredType("Marker3D", "3D")]
public sealed class Marker3D : Node3D
{
    [Export] public float GizmoExtents { get; set; } = 0.25f;
}

[RegisteredType("Skeleton3D", "3D")]
public sealed class Skeleton3D : Node3D
{
    [Export] public int BoneCount { get; set; }
    [Export] public bool ShowRestOnly { get; set; }
}

[RegisteredType("BoneAttachment3D", "3D")]
public sealed class BoneAttachment3D : Node3D
{
    [Export] public string BoneName { get; set; } = string.Empty;
    [Export] public int BoneIdx { get; set; } = -1;
}

[RegisteredType("GridMap", "3D")]
public sealed class GridMap : Node3D
{
    [Export] public Vector3 CellSize { get; set; } = Vector3.One;
    [Export] public int OctantSize { get; set; } = 8;
}

[RegisteredType("CSGBox3D", "3D/CSG")]
public sealed class CsgBox3D : Node3D
{
    [Export] public Vector3 Size { get; set; } = Vector3.One * 2f;
    [Export] public Material? Material { get; set; }
}

[RegisteredType("CSGSphere3D", "3D/CSG")]
public sealed class CsgSphere3D : Node3D
{
    [Export] public float Radius { get; set; } = 1f;
    [Export] public int RadialSegments { get; set; } = 12;
}

[RegisteredType("CSGCylinder3D", "3D/CSG")]
public sealed class CsgCylinder3D : Node3D
{
    [Export] public float Radius { get; set; } = 1f;
    [Export] public float Height { get; set; } = 2f;
}

[RegisteredType("RemoteTransform3D", "3D")]
public sealed class RemoteTransform3D : Node3D
{
    [Export] public string RemotePath { get; set; } = string.Empty;
}

[RegisteredType("VoxelGI", "3D")]
public sealed class VoxelGI : Node3D
{
    [Export] public Vector3 Extents { get; set; } = new Vector3(10, 10, 10);
    [Export("range:0,2")] public float Energy { get; set; } = 1f;
}
