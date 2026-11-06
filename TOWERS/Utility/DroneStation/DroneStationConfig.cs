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

    [ExportGroup("Station Service")]
    [Export(PropertyHint.Range, "1,1000000,1")] public int StationAmmoCapacity { get; set; } = 2000;
    [Export(PropertyHint.Range, ".5,120,.5")] public double FullFighterResupplySeconds { get; set; } = 6;
    [Export(PropertyHint.Range, "1,4,1")] public int ServiceBayCount { get; set; } = 1;
    [Export(PropertyHint.Range, "0,1000,1")] public float RepairHpPerSecond { get; set; } = 20;
    [Export(PropertyHint.Range, "0,10000,1")] public int RepairCreditsPer10Hp { get; set; } = 1;
    public bool ValidService => StationAmmoCapacity > 0 && StationAmmoCapacity <= 1000000 &&
        double.IsFinite(FullFighterResupplySeconds) && FullFighterResupplySeconds >= .5 && FullFighterResupplySeconds <= 120 &&
        ServiceBayCount >= 1 && ServiceBayCount <= 4 && float.IsFinite(RepairHpPerSecond) && RepairHpPerSecond >= 0 &&
        RepairHpPerSecond <= 1000 && RepairCreditsPer10Hp >= 0 && RepairCreditsPer10Hp <= 10000;

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
        Description = "Armoured fighter hangar. Purchase fighters, launch on Auto-pilot and recall to dock. Weapons, patrol and resupply are planned.";
        ConstructionCosts = new TowerLevel(1, 600, default).Costs;
        ConstructionSeconds = 8;
        MaxHealth = 1800;
    }

    public override string DescribeStats(TowerFinalStats final) =>
        $"Footprint: 2 × 3 cells\nFighter slots: {Mathf.Clamp(FighterCapacity, 1, 4)}\nFighter: {Mathf.Clamp(FighterCredits, 0, 1000000)} Credits";
}

