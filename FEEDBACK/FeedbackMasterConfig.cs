// Master visibility toggles shared by feedback components; defense bars are now active.
using Godot;

[GlobalClass]
public partial class FeedbackMasterConfig : Resource
{
    [ExportGroup("Feedback Visibility")]
    [Export] public bool DefenseBarsEnabled { get; set; } = true;
    [Export] public bool DeathExplosionsEnabled { get; set; } = true;
    [Export] public bool DamageNumbersEnabled { get; set; } = true;
    [Export] public bool CameraEffectsEnabled { get; set; } = true;
}
