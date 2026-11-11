// Master visibility toggles shared by feedback components; defense bars are now active.
using Godot;

[GlobalClass]
public partial class FeedbackMasterConfig : Resource
{
	[ExportGroup("Feedback Visibility")]
	[Export] public bool ShieldRicochetsEnabled { get; set; } = true;
	[Export] public bool HullHitSparksEnabled { get; set; } = true;
	[Export] public bool DefenseBarsEnabled { get; set; } = true;
	[Export] public bool DeathExplosionsEnabled { get; set; } = true;
	[Export] public bool DamageNumbersEnabled { get; set; } = true;
	[Export] public bool CameraEffectsEnabled { get; set; } = true;
	[Export] public bool WorldCalloutsEnabled { get; set; } = true;
	[ExportGroup("Impact Stagger")]
	[Export] public bool ImpactStaggerEnabled { get; set; } = true;
	[Export(PropertyHint.Range, "0,12,0.1")] public float StaggerShovePixels { get; set; } = 8f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] public float StaggerReturnSeconds { get; set; } = .28f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] public float StaggerMaximumSlow { get; set; } = .2f;
	[Export(PropertyHint.Range, "0,0.3,0.01")] public float StaggerSlowSeconds { get; set; } = .12f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] public float StaggerCooldownSeconds { get; set; } = .18f;
}
