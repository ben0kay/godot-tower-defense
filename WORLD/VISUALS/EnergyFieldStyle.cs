// Per-structure Inspector settings for the reusable VE2 shader overlay.
using Godot;

[GlobalClass]
public partial class EnergyFieldStyle : Resource
{
    public enum FieldKind { Portal = 0, Reactor = 1 }
    [ExportGroup("Energy Field")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public FieldKind Kind { get; set; } = FieldKind.Portal;
    [Export] public Color Tint { get; set; } = new Color("#9d61ed");
    [Export(PropertyHint.Range, "0,1,0.01")] public float Opacity { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0,3,0.01")] public float AnimationSpeed { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0.01,0.5,0.01")] public float GlowWidth { get; set; } = 0.12f;
    [ExportGroup("Spawn Pulse")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float PulseExtraOpacity { get; set; } = 0.45f;
    [Export(PropertyHint.Range, "0.05,2,0.01")] public float PulseDuration { get; set; } = 0.40f;
}
