// Inspector-editable typed upgrade operation; no reflection or direct base-stat mutation.
using Godot;

[GlobalClass]
public partial class TowerUpgradeModifier : Resource
{
    [Export] public TowerStatId Stat { get; set; }
    [Export] public TowerStatOperation Operation { get; set; }
    [Export] public float Amount { get; set; }
}
