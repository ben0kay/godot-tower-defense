// Shared economy policy options (not wired into gameplay yet).
// Per-level StartingCredits belongs in WORLD/LEVELS/LevelConfig.cs, not here.
using Godot;

[GlobalClass]
public partial class EconomyMasterConfig : Resource
{
    [ExportGroup("Balance Rules (Future)")]
    [Export] public bool AllowNegativeBalances { get; set; } = false;
    [Export(PropertyHint.Range, "1000,2000000000,1000")]
    public int MaxBalancePerResource { get; set; } = 1000000000;
}
