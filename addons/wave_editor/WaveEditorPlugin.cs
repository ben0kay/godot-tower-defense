// Godot editor-only main-screen plugin; provides a full-size Wave Editor next to 2D/3D/Script.
#if TOOLS
using Godot;

[Tool]
public partial class WaveEditorPlugin : EditorPlugin
{
    private WaveEditorPanel _panel;

    // =========================================================
    // Main-screen parent is a VBoxContainer: ExpandFill is required or our tab collapses into a short strip.
    public override void _EnterTree()
    {
        _panel = new WaveEditorPanel
        {
            Name = "Wave Editor", Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        EditorInterface.Singleton.GetEditorMainScreen().AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    // =========================================================
    // Free only this plugin's UI; waves and project files remain intact.
    public override void _ExitTree()
    {
        _panel?.QueueFree();
        _panel = null;
    }

    public override bool _HasMainScreen() => true;
    public override string _GetPluginName() => "Wave Editor";
    public override Texture2D _GetPluginIcon() =>
        EditorInterface.Singleton.GetEditorTheme().GetIcon("Animation", "EditorIcons");

    // =========================================================
    // No background processing: the panel is visible only when its workspace is selected.
    public override void _MakeVisible(bool visible)
    { if (GodotObject.IsInstanceValid(_panel)) _panel.Visible = visible; }
}
#endif
