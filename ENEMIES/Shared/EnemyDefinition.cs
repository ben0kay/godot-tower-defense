// Editable per-type identity, health and movement settings; filenames stay Enemy001.
using Godot;

[GlobalClass]
public partial class EnemyDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EnemyId { get; set; } = "enemy001";
    [Export] public string DisplayName { get; set; } = "Enemy 001";
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
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "1,24,1")] public float RadiusPixels { get; set; } = 12f;
    [Export(PropertyHint.Range, "0,30,0.1")] public float TurnSpeed { get; set; } = 8f;
}
