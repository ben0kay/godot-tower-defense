// Passive periodic Credit producer; shared base owns collection and lifecycle gating.
using Godot;

public partial class ResourceTower : CreditProducerTower
{
    #region Configuration
    [Export] public ResourceTowerConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    // =========================================================
    // Resolve nested authored levels before catalog preflight and private placement copies.
    public override bool AcceptsConfig(TowerConfig config)
    {
        if (config is not ResourceTowerConfig typed) return false;
        try { typed.PrepareLevels(); return true; } catch { return false; }
    }
    protected override void ApplyConfig(TowerConfig config) => Config = (ResourceTowerConfig)config;
    #endregion
}
