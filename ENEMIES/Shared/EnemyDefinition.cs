// Editable per-type identity, health and movement settings; plain types reuse shared gameplay scripts.
using Godot;

[GlobalClass]
public partial class EnemyDefinition : Resource
{
    #region Identity
    [ExportGroup("Identity")]
    [Export] public string EnemyId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "Unnamed Enemy";
    /// <summary>Fleet role only; does not imply mark, threat tier, resistances or abilities.</summary>
    [Export] public EnemyFamily Family { get; set; } = EnemyFamily.Drone;
    #endregion

    #region Balance and Feedback
    [ExportGroup("Balance")]
    [Export(PropertyHint.Range, "1,10000,1")] public float MaxHealth { get; set; } = 60f;
    [Export(PropertyHint.Range, "0,10000,1")] public float MaxShield { get; set; } = 0f;
    [Export(PropertyHint.Range, "1,1000,1")] public float MoveSpeed { get; set; } = 100f;
    [Export] public int CitadelDamage { get; set; } = 1;
    [Export] public int Armor { get; set; }
    [ExportGroup("Rewards")]
    [Export(PropertyHint.Range, "0,1000000,1")] public int KillRewardCredits { get; set; } = 0;
    [ExportGroup("Death Feedback")]
    [Export(PropertyHint.Range, "0,5,0.1")] public float DeathExplosionMultiplier { get; set; } = 1f;
    #endregion

    #region Status and Movement
    [ExportGroup("Status Immunities")]
    [Export] public Godot.Collections.Array<StatusEffectId> StatusImmunities { get; set; } = new();
    [ExportGroup("Movement Variation")]
    [Export] public bool AllowCorridorVariation { get; set; } = true;
    [Export] public bool AllowPathOffsets { get; set; } = true;
    [Export] public bool AllowPathDiversity { get; set; } = true;
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "1,24,1")] public float RadiusPixels { get; set; } = 12f;
    [Export(PropertyHint.Range, "0,30,0.1")] public float TurnSpeed { get; set; } = 8f;
    #endregion
}
