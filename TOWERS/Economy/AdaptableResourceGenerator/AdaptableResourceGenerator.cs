// Event-driven credited-kill harvester; radius is measured to the killing tower, not the victim.
using Godot;

public partial class AdaptableResourceGenerator : CreditProducerTower
{
    #region Configuration
    [Export] public AdaptableResourceGeneratorConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override bool UsesManualCollection => false;
    public long EarnedCredits { get; private set; }
    public long DisplayedIncome { get; private set; }
    public override string DescribeRuntime() => "Eligible kills pay directly to wallet";
    // =========================================================
    // Resolve nested authored levels before catalog preflight and private placement copies.
    public override bool AcceptsConfig(TowerConfig config)
    {
        if (config is not AdaptableResourceGeneratorConfig typed) return false;
        try { typed.PrepareLevels(); return true; } catch { return false; }
    }
    protected override void ApplyConfig(TowerConfig config) => Config = (AdaptableResourceGeneratorConfig)config;
    #endregion

    #region Floating Income
    private Label _incomeLabel;
    private double _popupAge;
    // =========================================================
    // Reuse one mouse-transparent world label; rapid earnings accumulate instead of spawning nodes.
    private void ShowIncome(int amount)
    {
        if (_incomeLabel == null)
        {
            _incomeLabel = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 100,
                HorizontalAlignment = HorizontalAlignment.Center, Size = new Vector2(160, 24) };
            _incomeLabel.AddThemeFontSizeOverride("font_size", 18);
            _incomeLabel.AddThemeColorOverride("font_outline_color", new Color(0.04f, 0.07f, 0.04f));
            _incomeLabel.AddThemeConstantOverride("outline_size", 4); AddChild(_incomeLabel);
        }
        DisplayedIncome += amount; _popupAge = 0;
        _incomeLabel.Text = $"+{DisplayedIncome}"; _incomeLabel.Modulate = Config.IncomePopupColor;
        _incomeLabel.Position = new Vector2(-80, -48); _incomeLabel.Show(); SetProcess(true);
    }
    // =========================================================
    // Only active while a popup exists; quiet expiry starts a fresh total for the next burst.
    public override void _Process(double delta)
    {
        _popupAge += delta;
        double duration = System.Math.Max(0.1, Config.IncomePopupSeconds);
        _incomeLabel.Position = new Vector2(-80, -48 - (float)(_popupAge / duration) * 16);
        Color tint = Config.IncomePopupColor; tint.A *= (float)System.Math.Clamp(2 * (1 - _popupAge / duration), 0, 1);
        _incomeLabel.Modulate = tint;
        if (_popupAge >= duration) { _incomeLabel.Hide(); DisplayedIncome = 0; SetProcess(false); }
    }
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
        if (!CanProduce || source == null || !source.TryGetTower(out TowerBase killer)
            || killer == this || killer.IsDestroyed || killer.GetParent() != GetParent()
            || killer.Definition?.Category != TowerDefinition.TowerCategory.Offensive) return;
        float range = FinalStats.RangePixels;
        if (GlobalPosition.DistanceSquaredTo(killer.GlobalPosition) <= range * range)
        {
            int amount = FinalStats.Production.Value.PerKill;
            if (amount > 0 && GodotObject.IsInstanceValid(Wallet) && Wallet.Grant(Wallet.CreditsResource, amount))
            { EarnedCredits += amount; ShowIncome(amount); }
        }
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
