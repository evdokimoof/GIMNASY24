using Gimnasy.Core.Math;
using Gimnasy.Core.Object;
using Gimnasy.Core.Resources;

namespace Gimnasy.Nodes;

// ===========================================================================
//  GUI / Control node catalog. Anchored, sized rectangles with layout.
// ===========================================================================

public enum SizeFlags { Fill, Expand, ExpandFill, ShrinkCenter, ShrinkEnd }
public enum LayoutDirection { Inherited, Ltr, Rtl }

/// <summary>Base of all GUI nodes: an anchored, sized rectangle.</summary>
[RegisteredType("Control", "GUI")]
public class Control : CanvasItem
{
    [Export] public Vector2 Position { get; set; }
    [Export] public Vector2 Size { get; set; } = new Vector2(100, 30);
    [Export] public Vector2 PivotOffset { get; set; }
    [Export("range:-360,360")] public float RotationDegrees { get; set; }
    [Export] public Vector2 Scale { get; set; } = Vector2.One;
    [Export("range:0,1")] public float AnchorLeft { get; set; }
    [Export("range:0,1")] public float AnchorTop { get; set; }
    [Export("range:0,1")] public float AnchorRight { get; set; }
    [Export("range:0,1")] public float AnchorBottom { get; set; }
    [Export] public SizeFlags HorizontalSizeFlags { get; set; } = SizeFlags.Fill;
    [Export] public SizeFlags VerticalSizeFlags { get; set; } = SizeFlags.Fill;
    [Export] public Vector2 CustomMinimumSize { get; set; }
    [Export] public string TooltipText { get; set; } = string.Empty;

    public Rect2 Rect => new(Position, Size);
}

[RegisteredType("Label", "GUI")]
public sealed class Label : Control
{
    public enum HAlign { Left, Center, Right, Fill }
    [Export] public string Text { get; set; } = string.Empty;
    [Export] public HAlign HorizontalAlignment { get; set; } = HAlign.Left;
    [Export] public Font? Font { get; set; }
    [Export] public int FontSize { get; set; } = 16;
    [Export] public Color FontColor { get; set; } = Color.White;
    [Export] public bool AutowrapEnabled { get; set; }
}

[RegisteredType("RichTextLabel", "GUI")]
public sealed class RichTextLabel : Control
{
    [Export] public string Text { get; set; } = string.Empty;
    [Export] public bool BbcodeEnabled { get; set; } = true;
    [Export] public bool ScrollActive { get; set; } = true;
    [Export] public bool FitContent { get; set; }
}

public abstract class BaseButton : Control
{
    [Export] public bool Disabled { get; set; }
    [Export] public bool Toggleable { get; set; }
    [Export] public bool ButtonPressed { get; set; }
}

[RegisteredType("Button", "GUI")]
[Signal("pressed")]
[Signal("toggled")]
public sealed class Button : BaseButton
{
    public enum Align { Left, Center, Right }
    [Export] public string Text { get; set; } = string.Empty;
    [Export] public Texture2D? Icon { get; set; }
    [Export] public Align TextAlignment { get; set; } = Align.Center;
    [Export] public bool Flat { get; set; }
}

[RegisteredType("TextureButton", "GUI")]
[Signal("pressed")]
public sealed class TextureButton : BaseButton
{
    [Export] public Texture2D? TextureNormal { get; set; }
    [Export] public Texture2D? TextureHover { get; set; }
    [Export] public Texture2D? TexturePressed { get; set; }
}

[RegisteredType("CheckBox", "GUI")]
[Signal("toggled")]
public sealed class CheckBox : BaseButton
{
    [Export] public string Text { get; set; } = string.Empty;
}

[RegisteredType("CheckButton", "GUI")]
[Signal("toggled")]
public sealed class CheckButton : BaseButton
{
    [Export] public string Text { get; set; } = string.Empty;
}

[RegisteredType("OptionButton", "GUI")]
[Signal("item_selected")]
public sealed class OptionButton : BaseButton
{
    [Export] public int Selected { get; set; } = -1;
}

[RegisteredType("LineEdit", "GUI")]
[Signal("text_changed")]
[Signal("text_submitted")]
public sealed class LineEdit : Control
{
    [Export] public string Text { get; set; } = string.Empty;
    [Export] public string PlaceholderText { get; set; } = string.Empty;
    [Export] public bool Secret { get; set; }
    [Export] public int MaxLength { get; set; }
    [Export] public bool Editable { get; set; } = true;
}

[RegisteredType("TextEdit", "GUI")]
[Signal("text_changed")]
public sealed class TextEdit : Control
{
    [Export] public string Text { get; set; } = string.Empty;
    [Export] public bool Editable { get; set; } = true;
    [Export] public bool WrapModeBoundary { get; set; }
    [Export] public bool ShowLineNumbers { get; set; }
}

[RegisteredType("Panel", "GUI")]
public sealed class Panel : Control { }

[RegisteredType("ColorRect", "GUI")]
public sealed class ColorRect : Control
{
    [Export] public Color Color { get; set; } = Color.White;
}

[RegisteredType("TextureRect", "GUI")]
public sealed class TextureRect : Control
{
    public enum StretchMode { Scale, Tile, Keep, KeepCentered, KeepAspect, KeepAspectCentered }
    [Export] public Texture2D? Texture { get; set; }
    [Export] public StretchMode Stretch { get; set; } = StretchMode.KeepAspect;
    [Export] public bool FlipH { get; set; }
}

[RegisteredType("NinePatchRect", "GUI")]
public sealed class NinePatchRect : Control
{
    [Export] public Texture2D? Texture { get; set; }
    [Export] public int PatchMarginLeft { get; set; }
    [Export] public int PatchMarginRight { get; set; }
    [Export] public int PatchMarginTop { get; set; }
    [Export] public int PatchMarginBottom { get; set; }
}

[RegisteredType("ProgressBar", "GUI")]
public sealed class ProgressBar : Control
{
    [Export] public float MinValue { get; set; }
    [Export] public float MaxValue { get; set; } = 100f;
    [Export] public float Value { get; set; }
    [Export] public bool ShowPercentage { get; set; } = true;
}

[RegisteredType("HSlider", "GUI")]
[Signal("value_changed")]
public sealed class HSlider : Control
{
    [Export] public float MinValue { get; set; }
    [Export] public float MaxValue { get; set; } = 100f;
    [Export] public float Step { get; set; } = 1f;
    [Export] public float Value { get; set; }
}

[RegisteredType("VSlider", "GUI")]
[Signal("value_changed")]
public sealed class VSlider : Control
{
    [Export] public float MinValue { get; set; }
    [Export] public float MaxValue { get; set; } = 100f;
    [Export] public float Value { get; set; }
}

[RegisteredType("SpinBox", "GUI")]
[Signal("value_changed")]
public sealed class SpinBox : Control
{
    [Export] public float MinValue { get; set; }
    [Export] public float MaxValue { get; set; } = 100f;
    [Export] public float Step { get; set; } = 1f;
    [Export] public string Prefix { get; set; } = string.Empty;
    [Export] public string Suffix { get; set; } = string.Empty;
}

[RegisteredType("ItemList", "GUI")]
[Signal("item_selected")]
public sealed class ItemList : Control
{
    [Export] public int MaxColumns { get; set; } = 1;
    [Export] public bool SameColumnWidth { get; set; }
}

[RegisteredType("Tree", "GUI")]
[Signal("item_selected")]
public sealed class Tree : Control
{
    [Export] public int Columns { get; set; } = 1;
    [Export] public bool HideRoot { get; set; }
}

// ---- Containers -----------------------------------------------------------

public abstract class Container : Control { }

[RegisteredType("BoxContainer", "GUI/Container")]
public class BoxContainer : Container
{
    [Export] public bool Vertical { get; set; }
    [Export] public int Separation { get; set; } = 4;
}

[RegisteredType("HBoxContainer", "GUI/Container")]
public sealed class HBoxContainer : BoxContainer { }

[RegisteredType("VBoxContainer", "GUI/Container")]
public sealed class VBoxContainer : BoxContainer
{
    public VBoxContainer() { Vertical = true; }
}

[RegisteredType("GridContainer", "GUI/Container")]
public sealed class GridContainer : Container
{
    [Export] public int Columns { get; set; } = 1;
}

[RegisteredType("MarginContainer", "GUI/Container")]
public sealed class MarginContainer : Container
{
    [Export] public int MarginLeft { get; set; }
    [Export] public int MarginRight { get; set; }
    [Export] public int MarginTop { get; set; }
    [Export] public int MarginBottom { get; set; }
}

[RegisteredType("CenterContainer", "GUI/Container")]
public sealed class CenterContainer : Container
{
    [Export] public bool UseTopLeft { get; set; }
}

[RegisteredType("PanelContainer", "GUI/Container")]
public sealed class PanelContainer : Container { }

[RegisteredType("ScrollContainer", "GUI/Container")]
public sealed class ScrollContainer : Container
{
    [Export] public bool HorizontalScrollEnabled { get; set; } = true;
    [Export] public bool VerticalScrollEnabled { get; set; } = true;
}

[RegisteredType("TabContainer", "GUI/Container")]
[Signal("tab_changed")]
public sealed class TabContainer : Container
{
    [Export] public int CurrentTab { get; set; }
    [Export] public bool TabsVisible { get; set; } = true;
}

[RegisteredType("SplitContainer", "GUI/Container")]
public sealed class SplitContainer : Container
{
    [Export] public bool Vertical { get; set; }
    [Export] public int SplitOffset { get; set; }
}

[RegisteredType("AspectRatioContainer", "GUI/Container")]
public sealed class AspectRatioContainer : Container
{
    [Export] public float Ratio { get; set; } = 1f;
}

[RegisteredType("FlowContainer", "GUI/Container")]
public sealed class FlowContainer : Container
{
    [Export] public bool Vertical { get; set; }
}

// ---- Popups / windows -----------------------------------------------------

[RegisteredType("Window", "GUI")]
[Signal("close_requested")]
public sealed class Window : Control
{
    [Export] public string Title { get; set; } = string.Empty;
    [Export] public bool Borderless { get; set; }
    [Export] public bool AlwaysOnTop { get; set; }
    [Export] public Vector2I InitialSize { get; set; } = new Vector2I(640, 360);
}

[RegisteredType("PopupMenu", "GUI")]
[Signal("id_pressed")]
public sealed class PopupMenu : Control
{
    [Export] public bool HideOnItemSelection { get; set; } = true;
}

[RegisteredType("MenuBar", "GUI")]
public sealed class MenuBar : Control
{
    [Export] public bool Flat { get; set; }
}

[RegisteredType("ColorPicker", "GUI")]
[Signal("color_changed")]
public sealed class ColorPicker : Control
{
    [Export] public Color Color { get; set; } = Color.White;
    [Export] public bool EditAlpha { get; set; } = true;
}

[RegisteredType("FileDialog", "GUI")]
[Signal("file_selected")]
public sealed class FileDialog : Control
{
    public enum FileMode { OpenFile, OpenFiles, OpenDir, SaveFile }
    [Export] public FileMode Mode { get; set; } = FileMode.OpenFile;
    [Export] public string Filters { get; set; } = "*";
}

[RegisteredType("VideoStreamPlayer", "GUI")]
public sealed class VideoStreamPlayer : Control
{
    [Export("file:*.ogv,*.mp4")] public string? StreamFile { get; set; }
    [Export] public bool Autoplay { get; set; }
    [Export("range:0,1")] public float Volume { get; set; } = 1f;
}

[RegisteredType("SubViewportContainer", "GUI")]
public sealed class SubViewportContainer : Container
{
    [Export] public bool Stretch { get; set; }
}
