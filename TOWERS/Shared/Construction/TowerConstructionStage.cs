// One optional initial-build artwork threshold; presentation never changes construction time or combat.
using Godot;

[GlobalClass]
public partial class TowerConstructionStage : Resource
{
    [Export(PropertyHint.Range, "0,1,0.01")] public float Progress { get; set; }
    [Export] public Texture2D Texture { get; set; }
}
