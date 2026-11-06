// Placeable hangar and finite paid fighter slots; combat, repair and logistics remain later phases.
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DroneStationConfig : TowerConfig
{
    [ExportGroup("Fighter Ownership")]
    [Export(PropertyHint.Range, "1,4,1")] public int FighterCapacity { get; set; } = 2;
    [Export(PropertyHint.Range, "0,1000000,1")] public int FighterCredits { get; set; } = 250;
    [Export] public FighterDroneConfig FighterConfig { get; set; }
    [Export] public PackedScene FighterScene { get; set; }

    [ExportGroup("Construction")]
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")]
    public int ConstructionCredits
    {
        get => ConstructionCosts[0].Amount;
        set { int amount = Mathf.Max(0, value); if (ConstructionCosts[0].Amount == amount) return; ConstructionCosts[0].Amount = amount; EmitChanged(); }
    }

    // =========================================================
    // Preserve FD1-A building costs/durability; fighters are separately purchased and never refill in this phase.
    public DroneStationConfig()
    {
        DisplayName = "Drone Station";
        Description = "Armoured fighter hangar. Purchase fighters, launch to a nearby holding point and recall to dock. Combat, patrol and resupply are planned.";
        ConstructionCosts = new TowerLevel(1, 600, default).Costs;
        ConstructionSeconds = 8;
        MaxHealth = 1800;
    }

    public override string DescribeStats(TowerFinalStats final) =>
        $"Footprint: 2 × 3 cells\nFighter slots: {Mathf.Clamp(FighterCapacity, 1, 4)}\nFighter: {Mathf.Clamp(FighterCredits, 0, 1000000)} Credits";
}

