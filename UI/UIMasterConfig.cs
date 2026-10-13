// Inspector-editable visual and layout defaults shared by the gameplay HUD.
// Configure the interface from DefaultUIConfig.tres, not hardcoded gameplay values.
using Godot;

[GlobalClass]
public partial class UIMasterConfig : Resource
{
    [ExportGroup("HUD Layout")]
    [Export(PropertyHint.Range, "32,96,1")] public int TopBarHeight { get; set; } = 46;
    [Export(PropertyHint.Range, "110,270,1")] public int BottomBarHeight { get; set; } = 154;
    [Export(PropertyHint.Range, "200,480,1")] public int InspectorWidth { get; set; } = 292;
    [Export(PropertyHint.Range, "0,36,1")] public int EdgeMargin { get; set; } = 12;
    [Export(PropertyHint.Range, "80,180,1")] public int TowerButtonWidth { get; set; } = 112;

    [ExportGroup("Interaction")]
    [Export] public bool StartWithTowerBarOpen { get; set; } = false;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float TowerBarTransitionSeconds { get; set; } = 0.17f;

    [ExportGroup("Palette")]
    [Export] public Color TopBarColor { get; set; } = new Color("#132633");
    [Export] public Color PanelColor { get; set; } = new Color("#122330");
    [Export] public Color ButtonColor { get; set; } = new Color("#1b3645");
    [Export] public Color AccentColor { get; set; } = new Color("#69cddc");
    [Export] public Color TextColor { get; set; } = new Color("#e0f2f5");
    [Export] public Color MutedTextColor { get; set; } = new Color("#97b4bf");
}
