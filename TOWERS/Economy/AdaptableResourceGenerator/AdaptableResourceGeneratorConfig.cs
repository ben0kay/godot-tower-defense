// Editable AdaptableResourceGenerator identity and three absolute economy tiers.
using Godot;

[GlobalClass]
public partial class AdaptableResourceGeneratorConfig : CreditProducerConfig
{
    #region Popup Settings
    [Export(PropertyHint.Range, "0.1,3,0.05")] public float IncomePopupSeconds { get; set; } = 0.8f;
    [Export] public Color IncomePopupColor { get; set; } = new(0.62f, 0.78f, 0.53f);
    #endregion

    #region Defaults
    // =========================================================
    // Provisional balance; nested ProductionLevels in the .tres are editable in Godot Inspector.
    public AdaptableResourceGeneratorConfig()
    {
        DisplayName = "Adaptable Resource Generator";
        Description = "Automatically earns Credits when other offensive towers within range make credited kills.";
        ProductionLevels = new()
        {
            new() { Price = 300, Capacity = 100, CreditsPerKill = 1, CreditsPerCycle = 0, RangePixels = 256 },
            new() { Price = 250, Capacity = 150, CreditsPerKill = 2, CreditsPerCycle = 0, RangePixels = 288, HullHealth = 1575 },
            new() { Price = 350, Capacity = 200, CreditsPerKill = 3, CreditsPerCycle = 0, RangePixels = 320, HullHealth = 1653.75f }
        };
        PrepareLevels();
    }
    #endregion
}
