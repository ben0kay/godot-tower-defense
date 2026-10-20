// Shared presentation tuning; transparency never owns targeting or reveal state.
using Godot;

[GlobalClass]
public partial class StealthVisualStyle : Resource
{
    [Export(PropertyHint.Range, "0,1,0.01")] public float HiddenAlpha { get; set; } = .3f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RevealedAlpha { get; set; } = 1;
    [Export] public bool DimDefenseFeedback { get; set; } = true;
}
