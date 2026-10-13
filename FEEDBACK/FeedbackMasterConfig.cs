// Reserved shared effect toggles; these effects are not implemented in the first map test.
using Godot;

[GlobalClass]
public partial class FeedbackMasterConfig : Resource
{
    [ExportGroup("Future Feedback")]
    [Export] public bool DefenseBarsEnabled { get; set; } = true;
    [Export] public bool DamageNumbersEnabled { get; set; } = true;
    [Export] public bool CameraEffectsEnabled { get; set; } = true;
}
