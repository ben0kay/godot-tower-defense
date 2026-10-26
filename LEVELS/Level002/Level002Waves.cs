// Level002 tutorial: four small, unshielded waves introduce baseline, speed, then durability.
// The third plain wave role is Bulwark (medium_mk1 / Enemy004); Enemy003 is the Shield Emitter support asset.
using Godot;
using static WaveBuilder;

[GlobalClass]
public partial class Level002Waves : WaveSetSource
{
    // =========================================================
    // Assign this resource to LevelConfig without adding a wave-catalog switch entry.
    public override WaveDefinition[] Build() => Create();
    #region Tutorial Encounters
    // =========================================================
    // Manual wave starts leave unlimited building time; both authored portals remain eligible.
    public static WaveDefinition[] Create() =>
    [
        Wave(1, Batch(count: 12, interval: 0.8, after: 0, Enemy("basic_mk1"))),
        Wave(2,
            Batch(count: 10, interval: 0.7, after: 3, Enemy("basic_mk1")),
            Batch(count: 6, interval: 0.9, after: 0, Enemy("scout_mk1"))),
        Wave(3,
            Batch(count: 12, interval: 0.65, after: 3, Enemy("basic_mk1")),
            Batch(count: 4, interval: 1.4, after: 3, Enemy("medium_mk1")),
            Batch(count: 4, interval: 0.9, after: 0, Enemy("scout_mk1"))),
        Wave(4,
            Batch(count: 16, interval: 0.65, after: 3,
                Enemy("basic_mk1", weight: 75), Enemy("scout_mk1", weight: 25)),
            Batch(count: 8, interval: 1.0, after: 0,
                Enemy("basic_mk1", weight: 50), Enemy("medium_mk1", weight: 50)))
    ];
    #endregion
}
