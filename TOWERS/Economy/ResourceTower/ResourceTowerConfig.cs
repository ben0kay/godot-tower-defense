// Editable ResourceTower identity and three absolute economy tiers.
using Godot;

[GlobalClass]
public partial class ResourceTowerConfig : CreditProducerConfig
{
    #region Defaults
    // =========================================================
    // Provisional balance; nested ProductionLevels in the .tres are editable in Godot Inspector.
    public ResourceTowerConfig()
    {
        DisplayName = "Resource Tower";
        Description = "Produces Credits during active waves. Select it to collect its stored income.";
        ProductionLevels = new()
        {
            new() { Price = 350, Capacity = 100, CreditsPerKill = 0, CreditsPerCycle = 10, RangePixels = 0 },
            new() { Price = 250, Capacity = 150, CreditsPerKill = 0, CreditsPerCycle = 15, RangePixels = 0, HullHealth = 1575 },
            new() { Price = 350, Capacity = 200, CreditsPerKill = 0, CreditsPerCycle = 20, RangePixels = 0, HullHealth = 1653.75f }
        };
        PrepareLevels();
    }
    #endregion
}
