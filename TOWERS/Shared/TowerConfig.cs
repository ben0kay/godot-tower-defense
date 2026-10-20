// Shared Inspector identity and private runtime copies of explicit C# level rows.
using Godot;

[GlobalClass]
public partial class TowerConfig : Resource
{
    [ExportGroup("Detection")]
    [Export] public bool CanDetectStealth { get; set; } = false;

    private float _passiveMaxHealth = 100;
    [ExportGroup("Durability")]
    [Export(PropertyHint.Range, "1,10000,1,or_greater")]
    public float MaxHealth
    {
        get => Levels.Count == 0 ? _passiveMaxHealth : Levels[0].Stats.MaxHealth;
        set
        {
            if (Levels.Count == 0) { if (_passiveMaxHealth == value) return; _passiveMaxHealth = value; EmitChanged(); }
            else SetLevelStats(1, Levels[0].Stats with { MaxHealth = value });
        }
    }

    [ExportGroup("Passive Construction (no level rows)")]
    [Export(PropertyHint.Range, "0,300,0.1,or_greater")] public double ConstructionSeconds { get; set; }
    public double BuildSeconds => Levels.Count == 0 ? ConstructionSeconds : Levels[0].WorkSeconds;

    // =========================================================
    // Durations are source values, not combat modifiers; passive types need no fake level rows.
    public static void ValidateWorkSeconds(double seconds)
    { if (!double.IsFinite(seconds) || seconds < 0) throw new System.ArgumentOutOfRangeException(nameof(seconds), "Work duration must be finite and nonnegative."); }

    [ExportGroup("Construction Artwork")]
    [Export] public TowerConstructionArt ConstructionArt { get; set; }

    [ExportGroup("Starting Ammunition")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float InitialAmmoFraction { get; set; } = 1f;
    [ExportGroup("Identity")]
    [Export] public string DisplayName { get; set; } = "Tower";
    [Export] public string Description { get; set; } = "";
    public virtual HeatTiming? SustainedHeatTiming => null;
    public virtual System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => TowerRankController.StandardOnly;
    // Runtime copies of the tower's C# rows; empty for passive test-only configs.
    public System.Collections.Generic.List<TowerLevel> Levels { get; protected set; } = new();
    private Godot.Collections.Array<ResourceCost> _passiveCosts = new();
    public Godot.Collections.Array<ResourceCost> ConstructionCosts
    {
        get => Levels.Count == 0 ? _passiveCosts : Levels[0].Costs;
        set { if (Levels.Count == 0) _passiveCosts = value; else Levels[0].Costs = value; }
    }

    // =========================================================
    // Absolute per-instance tuning replaces a single row and invalidates listeners once.
    public void SetLevelStats(int level, TowerStatBlock stats)
    {
        if (level < 1 || level > Levels.Count) throw new System.ArgumentOutOfRangeException(nameof(level));
        if (Levels[level - 1].Stats == stats) return;
        Levels[level - 1].Stats = stats; EmitChanged();
    }

    // =========================================================
    // Copy absolute rows and mutable recipes rather than sharing placed-instance inputs.
    public virtual TowerConfig CreateInstanceCopy()
    {
        // Keep presentation profiles and textures shared; mutable combat rows/recipes still get private copies below.
        TowerConfig copy = (TowerConfig)Duplicate(false);
        copy.ConstructionArt = ConstructionArt;
        copy._passiveCosts = TowerLevel.CopyCosts(_passiveCosts);
        copy.Levels = new();
        foreach (TowerLevel level in Levels) copy.Levels.Add(level?.CreateInstanceCopy());
        return copy;
    }

    // =========================================================
    // Passive configs need no artificial weapon snapshot or weapon fields.
    public virtual TowerFinalStats ResolveStats(int level) => null;

    // =========================================================
    // Types opt into appropriate Inspector detail without controller type switches.
    public virtual string DescribeStats(TowerFinalStats final) => "Passive tower; no weapon capability.";

    // =========================================================
    // Read-only target priority for aimed weapons. IT3 adds a live per-instance selector.
    public virtual string InspectorTargetPriorityLabel => "";

    // =========================================================
    // Types describe changes from their own resolver; no duplicated preview arithmetic.
    public virtual string DescribeChanges(TowerFinalStats now, TowerFinalStats future) => "";
    // =========================================================
    // Optional response changes are described from final snapshots, with the same fallback profile.
    protected static string DescribeWeaponChanges(TowerFinalStats now, TowerFinalStats future)
    {
        if (now.Weapon == future.Weapon) return "";
        string changes = "";
        if (now.Weapon?.Type != future.Weapon?.Type)
            changes += $"Damage type: {now.Weapon?.Type} → {future.Weapon?.Type}\n";
        if (now.Weapon.HasValue && future.Weapon.HasValue)
        {
            WeaponDamageStats a = now.Weapon.Value, b = future.Weapon.Value;
            var oldDefaults = DamageResolver.DefaultProfile.Coefficients(a.Type);
            var newDefaults = DamageResolver.DefaultProfile.Coefficients(b.Type);
            float oldShield = a.OverrideShield ? a.ShieldCoefficient : oldDefaults.Shield;
            float newShield = b.OverrideShield ? b.ShieldCoefficient : newDefaults.Shield;
            float oldHull = a.OverrideHull ? a.HullCoefficient : oldDefaults.Hull;
            float newHull = b.OverrideHull ? b.HullCoefficient : newDefaults.Hull;
            if (oldShield != newShield) changes += $"Shield: {oldShield:0.00}x → {newShield:0.00}x\n";
            if (oldHull != newHull) changes += $"Hull: {oldHull:0.00}x → {newHull:0.00}x\n";
        }
        return changes;
    }
}
