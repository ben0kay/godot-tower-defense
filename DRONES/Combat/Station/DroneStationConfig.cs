// Passive 2x3 fighter-drone hangar: construction and durability only until Codex adds FD1-B fighter service.
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class DroneStationConfig : TowerConfig
{
    [ExportGroup("Construction")]
    [Export(PropertyHint.Range, "0,1000000,1,or_greater")]
    public int ConstructionCredits
    {
        get => ConstructionCosts[0].Amount;
        set { int amount = Mathf.Max(0, value); if (ConstructionCosts[0].Amount == amount) return; ConstructionCosts[0].Amount = amount; EmitChanged(); }
    }

    // =========================================================
    // No fictitious combat capability, dock capacity or generated ammo until the fighter pass owns those systems.
    public DroneStationConfig()
    {
        DisplayName = "Drone Station";
        Description = "Armoured two-bay drone hangar. Fighter launches, patrols and resupply are planned for FD1-B onwards.";
        ConstructionCosts = new TowerLevel(1, 600, default).Costs;
        ConstructionSeconds = 8;
        MaxHealth = 1800;
    }

    public override string DescribeStats(TowerFinalStats final) =>
        "Footprint: 2 × 3 cells\nTwo illustrated docking bays\nFighter operations: future FD1-B";
}
