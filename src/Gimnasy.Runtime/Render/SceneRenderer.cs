using Gimnasy.Core.Math;
using Gimnasy.Core.Scene;
using Gimnasy.Nodes;

namespace Gimnasy.Runtime.Render;

/// <summary>
/// Translates the node tree into renderer draw commands. Visibility is honored
/// and drawing is depth-first (parents before children) so later siblings paint
/// on top — the painter's-algorithm order canvas engines expect.
/// </summary>
public static class SceneRenderer
{
    public static void Render(Node node, IRenderingServer r)
    {
        Draw(node, r);
        foreach (var child in node.Children)
            Render(child, r);
    }

    private static void Draw(Node node, IRenderingServer r)
    {
        switch (node)
        {
            case Sprite2D s when s.Visible:
                r.DrawSprite(default, s.GlobalTransform, s.Modulate);
                break;

            case ColorRect cr when cr.Visible:
                r.DrawRect(new Rect2(cr.Position, cr.Size), cr.Color);
                break;

            case Panel p when p.Visible:
                r.DrawRect(new Rect2(p.Position, p.Size), new Color(0.18f, 0.18f, 0.2f, 0.9f));
                break;

            case Label l when l.Visible:
                r.DrawText(l.Text, l.Position, l.FontSize, l.FontColor);
                break;

            case MeshInstance3D m when m.Visible:
                r.DrawMesh(default, m.GlobalTransform, default);
                break;

            case Node2D n2 when n2.Visible && n2.GetType() == typeof(Node2D):
                break; // pure grouping node, nothing to paint
        }
    }
}
