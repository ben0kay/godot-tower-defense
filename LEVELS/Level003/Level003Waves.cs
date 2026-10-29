// Level003 original C# recipe retained for resource-parity checks; gameplay now reads Level003Waves.tres.
// Enemy001=basic_mk1, Enemy002=scout_mk1, Enemy004=medium_mk1; no special variants.
using Godot;
using static WaveBuilder;

[GlobalClass]
public partial class Level003Waves : WaveSetSource
{
    #region Encounter Authoring
    // =========================================================
    // The shared scheduler starts each wave manually; the template's one portal is eligible after a path is painted.
    public override WaveDefinition[] Build() => Create();

    // =========================================================
    // Easy foundation → faster swarm → first Bulwarks → heavier mixed finale.
    public static WaveDefinition[] Create() =>
    [
        Wave(1,
            Batch(count: 12, interval: 0.85, after: 3, Enemy("basic_mk1")),
            Batch(count: 6, interval: 0.95, after: 0, Enemy("scout_mk1"))),

        Wave(2,
            Batch(count: 14, interval: 0.68, after: 3, Enemy("basic_mk1")),
            Batch(count: 8, interval: 0.85, after: 0, Enemy("scout_mk1"))),

        Wave(3,
            Batch(count: 14, interval: 0.68, after: 3, Enemy("basic_mk1")),
            Batch(count: 8, interval: 0.85, after: 3, Enemy("scout_mk1")),
            Batch(count: 4, interval: 1.3, after: 0, Enemy("medium_mk1"))),

        Wave(4,
            Batch(count: 16, interval: 0.6, after: 3,
                Enemy("basic_mk1", weight: 75), Enemy("scout_mk1", weight: 25)),
            Batch(count: 8, interval: 0.8, after: 3, Enemy("scout_mk1")),
            Batch(count: 8, interval: 1.1, after: 0, Enemy("medium_mk1")))
    ];
    #endregion
}
