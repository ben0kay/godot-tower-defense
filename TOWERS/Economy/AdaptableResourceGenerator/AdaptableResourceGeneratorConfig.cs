// Editable AdaptableResourceGenerator identity and three absolute economy tiers.
using Godot;

[GlobalClass]
public partial class AdaptableResourceGeneratorConfig : CreditProducerConfig
{
    #region Defaults
    // =========================================================
    // Provisional balance; nested ProductionLevels in the .tres are editable in Godot Inspector.
    public AdaptableResourceGeneratorConfig()
    {
        DisplayName = "Adaptable Resource Generator";
        Description = "Stores Credits earned when other offensive towers within range make credited kills.";
        ProductionLevels = new()
        {
            new() { Price = 300, Capacity = 100, CreditsPerKill = 1, CreditsPerCycle = 0, RangePixels = 256 },
            new() { Price = 250, Capacity = 150, CreditsPerKill = 2, CreditsPerCycle = 0, RangePixels = 288, HullHealth = 200 },
            new() { Price = 350, Capacity = 200, CreditsPerKill = 3, CreditsPerCycle = 0, RangePixels = 320, HullHealth = 250 }
        };
        PrepareLevels();
    }
    #endregion
}
