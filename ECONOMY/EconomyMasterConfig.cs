// Shared economy limits used by the session wallet.
// Per-level StartingCredits belongs in WORLD/LEVELS/LevelConfig.cs, not here.
using Godot;

[GlobalClass]
public partial class EconomyMasterConfig : Resource
{
    [ExportGroup("Balance Rules")]
    [Export] public bool AllowNegativeBalances { get; set; } = false;
    [Export(PropertyHint.Range, "1000,2000000000,1000")]
    public int MaxBalancePerResource { get; set; } = 1000000000;
}
