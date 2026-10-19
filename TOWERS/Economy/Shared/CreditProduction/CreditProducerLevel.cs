// Inspector-authored absolute economy tier; copied privately when a tower is placed.
using Godot;
using System;

[GlobalClass]
public partial class CreditProducerLevel : Resource
{
    #region Settings
    [Export] public int Price { get; set; } = 300;
    [Export] public int Capacity { get; set; } = 100;
    [Export] public int CreditsPerKill { get; set; }
    [Export] public int CreditsPerCycle { get; set; }
    [Export] public double CycleSeconds { get; set; } = 5;
    [Export] public float RangePixels { get; set; }
    [Export] public float HullHealth { get; set; } = 150;
    [Export] public double WorkSeconds { get; set; } = 2;
    #endregion

    // =========================================================
    // Reject invalid tuning before purchase; capacities may only increase across upgrade tiers.
    public void Validate()
    {
        if (Price < 0 || Capacity < 1 || CreditsPerKill < 0 || CreditsPerCycle < 0
            || !double.IsFinite(CycleSeconds) || CycleSeconds <= 0 || !float.IsFinite(RangePixels) || RangePixels < 0
            || !float.IsFinite(HullHealth) || HullHealth <= 0) throw new ArgumentException("Invalid economy tower tier.");
        TowerConfig.ValidateWorkSeconds(WorkSeconds);
    }
}
