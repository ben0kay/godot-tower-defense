// Level001 introductory wave recipe, retained as a C# reference; the mission reads Level001Waves.tres.
// Waves 1–2 teach Standard Drones; halfway through Wave 3, Scouts join the last ten ships.
using Godot;
using static WaveBuilder;

[GlobalClass]
public partial class Level001Waves : WaveSetSource
{
    // =========================================================
    // The editable WaveSetResource is authoritative; keep this reference for legacy fixtures and comparisons.
    public override WaveDefinition[] Build() => Create();

    // =========================================================
    // No shields, stealth or heavy enemies in this first tutorial mission.
    public static WaveDefinition[] Create() =>
    [
        Wave(1,
            Batch(count: 10, interval: 0.9, after: 0, Enemy("basic_mk1"))),

        Wave(2,
            Batch(count: 14, interval: 0.8, after: 0, Enemy("basic_mk1"))),

        Wave(3,
            Batch(count: 10, interval: 0.75, after: 1.0, Enemy("basic_mk1")),
            Batch(count: 10, interval: 0.65, after: 0,
                Enemy("basic_mk1", weight: 50),
                Enemy("scout_mk1", weight: 50)))
    ];
}
