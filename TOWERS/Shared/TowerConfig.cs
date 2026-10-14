// Shared authored identity, recipes and optional progression; weapon values stay on typed configs.
using Godot;

[GlobalClass]
public partial class TowerConfig : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DisplayName { get; set; } = "Tower";
    [Export] public string Description { get; set; } = "";
    [ExportGroup("Construction")]
    [Export] public Godot.Collections.Array<ResourceCost> ConstructionCosts { get; set; } = new();
    [ExportGroup("Upgrades")]
    [Export] public Godot.Collections.Array<TowerUpgradeTier> UpgradeTiers { get; set; } = new();

    // =========================================================
    // Explicitly copy external mutable tier/cost resources rather than sharing array entries.
    public virtual TowerConfig CreateInstanceCopy()
    {
        TowerConfig copy = (TowerConfig)Duplicate(true);
        copy.ConstructionCosts = TowerUpgradeTier.CopyCosts(ConstructionCosts);
        if (UpgradeTiers == null) copy.UpgradeTiers = null;
        else
        {
            copy.UpgradeTiers = new();
            foreach (TowerUpgradeTier tier in UpgradeTiers) copy.UpgradeTiers.Add(tier?.CreateInstanceCopy());
        }
        return copy;
    }

    // =========================================================
    // Passive configs need no artificial weapon snapshot or weapon fields.
    public virtual TowerFinalStats ResolveStats(int level) => null;

    // =========================================================
    // Types opt into appropriate Inspector detail without controller type switches.
    public virtual string DescribeStats(TowerFinalStats final) => "Passive tower; no weapon capability.";

    // =========================================================
    // Types describe changes from their own resolver; no duplicated preview arithmetic.
    public virtual string DescribeChanges(TowerFinalStats now, TowerFinalStats future) => "";
}
