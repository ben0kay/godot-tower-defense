// Shared tier recipe; applicable to future weapon, support or production stats.
using Godot;

[GlobalClass]
public partial class TowerUpgradeTier : Resource
{
    [Export] public int Level { get; set; } = 2;
    [Export] public string DisplayName { get; set; } = "Improved";
    [Export] public Godot.Collections.Array<ResourceCost> Costs { get; set; } = new();
    [Export] public Godot.Collections.Array<TowerUpgradeModifier> Modifiers { get; set; } = new();

    // =========================================================
    // External .tres references inside arrays need explicit copies for per-instance editing.
    public TowerUpgradeTier CreateInstanceCopy()
    {
        TowerUpgradeTier copy = new() { Level = Level, DisplayName = DisplayName };
        copy.Costs = CopyCosts(Costs);
        if (Modifiers == null) copy.Modifiers = null;
        else foreach (TowerUpgradeModifier modifier in Modifiers)
            copy.Modifiers.Add(modifier == null ? null : (TowerUpgradeModifier)modifier.Duplicate());
        return copy;
    }

    // =========================================================
    // Price entries are mutable per instance; stable resource definitions remain shared identities.
    internal static Godot.Collections.Array<ResourceCost> CopyCosts(Godot.Collections.Array<ResourceCost> costs)
    {
        if (costs == null) return null;
        var copy = new Godot.Collections.Array<ResourceCost>();
        foreach (ResourceCost cost in costs)
            copy.Add(cost == null ? null : new ResourceCost { Resource = cost.Resource, Amount = cost.Amount });
        return copy;
    }
}
