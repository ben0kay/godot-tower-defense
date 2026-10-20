// World-owned hostile bullet budgets/lifetime/default presentation, captured once when a battlefield enters the tree.
using Godot;

[GlobalClass]
public partial class EnemyProjectileMasterConfig : Resource
{
    #region Inspector
    [ExportGroup("Bullet Budget")]
    [Export(PropertyHint.Range, "1,65536,1")] public int MaximumActive { get; set; } = 4096;
    [Export(PropertyHint.Range, "0.01,30,0.01,suffix:s")] public double DefaultLifetimeSeconds { get; set; } = 2.5;
    [ExportGroup("Default Presentation")]
    [Export] public ProjectileVisualStyle DefaultVisual { get; set; }
    #endregion
}
