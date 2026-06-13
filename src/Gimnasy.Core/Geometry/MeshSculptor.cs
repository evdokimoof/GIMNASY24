using Gimnasy.Core.Math;

namespace Gimnasy.Core.Geometry;

/// <summary>Sculpt brush kinds, matching the staples in Blender's sculpt mode.</summary>
public enum SculptBrush
{
    Draw,    // push vertices out along the normal
    Inflate, // push out along each vertex's own normal
    Grab,    // translate vertices by a delta
    Smooth,  // relax toward neighbour average
    Flatten, // pull toward the brush plane
    Pinch,   // pull toward the brush centre
    Crease,  // pinch + draw inward
}

/// <summary>
/// Applies sculpt brushes to an <see cref="EditableMesh"/> within a spherical
/// region of influence, with a smooth falloff. Operates directly on vertex
/// positions (object space) so it is fully unit-testable without a GPU.
/// </summary>
public static class MeshSculptor
{
    /// <summary>
    /// Apply a brush. <paramref name="center"/> and <paramref name="grabDelta"/>
    /// are in the mesh's local space. Returns how many vertices were affected.
    /// </summary>
    public static int Apply(EditableMesh mesh, SculptBrush brush, Vector3 center,
        float radius, float strength, Vector3 grabDelta = default, Vector3 brushNormal = default)
    {
        if (brushNormal.LengthSquared < Mathf.Epsilon) brushNormal = Vector3.Up;
        if (mesh.Normals.Count != mesh.Vertices.Count) mesh.RecalculateNormals();

        Vector3 planePoint = center;
        float r2 = radius * radius;
        int affected = 0;

        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            Vector3 v = mesh.Vertices[i];
            float d2 = v.DistanceSquaredTo(center);
            if (d2 > r2) continue;

            float falloff = Falloff(Mathf.Sqrt(d2), radius);
            float w = falloff * strength;
            Vector3 delta = brush switch
            {
                SculptBrush.Draw => brushNormal * w,
                SculptBrush.Inflate => mesh.Normals[i] * w,
                SculptBrush.Grab => grabDelta * falloff,
                SculptBrush.Smooth => (NeighbourAverage(mesh, i) - v) * Mathf.Clamp01(w),
                SculptBrush.Flatten => (ProjectToPlane(v, planePoint, brushNormal) - v) * Mathf.Clamp01(w),
                SculptBrush.Pinch => (center - v) * Mathf.Clamp01(w),
                SculptBrush.Crease => (center - v) * Mathf.Clamp01(w) + brushNormal * w * 0.5f,
                _ => Vector3.Zero,
            };
            mesh.Vertices[i] = v + delta;
            affected++;
        }

        if (affected > 0) mesh.RecalculateNormals();
        return affected;
    }

    private static float Falloff(float dist, float radius)
    {
        float t = Mathf.Clamp01(1f - dist / radius);
        return t * t * (3f - 2f * t); // smoothstep
    }

    private static Vector3 ProjectToPlane(Vector3 p, Vector3 planePoint, Vector3 normal)
    {
        Vector3 n = normal.Normalized;
        float dist = (p - planePoint).Dot(n);
        return p - n * dist;
    }

    private static Vector3 NeighbourAverage(EditableMesh mesh, int vertex)
    {
        // Average the endpoints of every triangle edge touching this vertex.
        Vector3 sum = Vector3.Zero; int count = 0;
        void Accumulate(int other)
        {
            if (other != vertex) { sum += mesh.Vertices[other]; count++; }
        }
        for (int t = 0; t < mesh.Indices.Count; t += 3)
        {
            int a = mesh.Indices[t], b = mesh.Indices[t + 1], c = mesh.Indices[t + 2];
            if (a != vertex && b != vertex && c != vertex) continue;
            Accumulate(a); Accumulate(b); Accumulate(c);
        }
        return count == 0 ? mesh.Vertices[vertex] : sum / count;
    }
}
