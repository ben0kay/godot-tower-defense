// Event-driven credited-kill harvester; radius is measured to the killing tower, not the victim.
using Godot;

public partial class AdaptableResourceGenerator : CreditProducerTower
{
    #region Configuration
    [Export] public AdaptableResourceGeneratorConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    // =========================================================
    // Resolve nested authored levels before catalog preflight and private placement copies.
    public override bool AcceptsConfig(TowerConfig config)
    {
        if (config is not AdaptableResourceGeneratorConfig typed) return false;
        try { typed.PrepareLevels(); return true; } catch { return false; }
    }
    protected override void ApplyConfig(TowerConfig config) => Config = (AdaptableResourceGeneratorConfig)config;
    #endregion

    #region Attributed Kills
    private EnemyRegistry _registry;
    public override float? InspectionRange => FinalStats?.RangePixels;
    // =========================================================
    // One event subscription per ARG, no polling/scanning of towers or enemies.
    public override void _Ready()
    {
        base._Ready(); _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        if (_registry != null) _registry.AttributedKill += OnKill;
    }
    // =========================================================
    // Only the lethal hit's surviving offensive owner qualifies; each eligible ARG earns independently.
    private void OnKill(Enemy enemy, DamageSource source)
    {
        if (!CanProduce || Buffer.IsFull || source == null || !source.TryGetTower(out TowerBase killer)
            || killer == this || killer.IsDestroyed || killer.GetParent() != GetParent()
            || killer.Definition?.Category != TowerDefinition.TowerCategory.Offensive) return;
        float range = FinalStats.RangePixels;
        if (GlobalPosition.DistanceSquaredTo(killer.GlobalPosition) <= range * range)
            Buffer.Produce(FinalStats.Production.Value.PerKill);
    }
    // =========================================================
    // Never retain the previous world's kill notifications after removal/restart.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_registry)) _registry.AttributedKill -= OnKill;
        base._ExitTree();
    }
    #endregion
}
