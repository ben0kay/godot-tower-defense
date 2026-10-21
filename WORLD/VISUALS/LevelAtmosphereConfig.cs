// VE1 opt-in visual-only resource for level backgrounds.
using Godot;
[GlobalClass]
public partial class LevelAtmosphereConfig : Resource
{
    [ExportGroup("Master")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool StarsEnabled { get; set; } = true;
    [Export] public bool NebulaEnabled { get; set; } = true;
    [ExportGroup("Coverage")]
    [Export] public Vector2 CoveragePixels { get; set; } = new(6400, 5120);
    [ExportGroup("Stars")]
    [Export] public Color StarTint { get; set; } = new Color("#bedcf9");
    [Export(PropertyHint.Range, "0,1,.01")] public float StarDensity { get; set; } = .2f;
    [Export(PropertyHint.Range, "0,1,.01")] public float StarOpacity { get; set; } = .45f;
    [Export(PropertyHint.Range, "0,1,.01")] public float TwinkleAmount { get; set; } = .45f;
    [Export(PropertyHint.Range, "0,5,.01")] public float TwinkleSpeed { get; set; } = .65f;
    [ExportGroup("Nebula")]
    [Export] public Color NebulaTint { get; set; } = new Color("#3760a3");
    [Export(PropertyHint.Range, "0,1,.01")] public float NebulaOpacity { get; set; } = .12f;
    [Export(PropertyHint.Range, "0,0.1,.0001")] public float DriftSpeed { get; set; } = .002f;
}