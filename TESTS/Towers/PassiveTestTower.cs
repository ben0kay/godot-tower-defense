// Test-only passive type: no weapon nodes, combat config, processing or final combat snapshot.
using Godot;

public partial class PassiveTestTower : TowerBase
{
    [Export] public TowerConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override Godot.Collections.Array<TowerUpgradeTier> UpgradeTiers => Config.UpgradeTiers;
    // =========================================================
    // Passive fixture deliberately accepts only the shared non-weapon config.
    public override bool AcceptsConfig(TowerConfig config) => config != null && config.GetType() == typeof(TowerConfig);
    // =========================================================
    // Own a private authored resource with no required weapon fields.
    protected override void ApplyConfig(TowerConfig config) => Config = config;
    // =========================================================
    // No combat-stat capability is present in this fixture.
    public override TowerFinalStats ResolveLevel(int level) => null;
    // =========================================================
    // Passive instances perform no physics or process work.
    public override void _Ready() { SetPhysicsProcess(false); SetProcess(false); }
}
