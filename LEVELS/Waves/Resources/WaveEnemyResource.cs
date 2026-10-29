// Editor-authored enemy option within a batch; converted to the existing immutable wave entry at load.
using Godot;

[Tool, GlobalClass]
public partial class WaveEnemyResource : Resource
{
    [Export] public string EnemyId { get; set; } = "basic_mk1";
    [Export(PropertyHint.Range, "0,10000,1,or_greater")] public double Weight { get; set; } = 100;
    [Export] public string Portal { get; set; } = "";
    [Export] public bool Shield { get; set; }
    [Export] public bool Stealth { get; set; }

    // =========================================================
    // Clone resources explicitly so duplicated batches are independent in the visual editor.
    public WaveEnemyResource Copy() => new()
    { EnemyId = EnemyId, Weight = Weight, Portal = Portal, Shield = Shield, Stealth = Stealth };
}
