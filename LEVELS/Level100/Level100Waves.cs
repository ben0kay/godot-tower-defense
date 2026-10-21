// Level100: intentionally enormous ship swarms for stress tests, not normal campaign balance.
// Explicit counts are scheduled ships; no Transporters/DeathSpawn are included so totals stay exact.
using Godot;
using static WaveBuilder;

[GlobalClass]
public partial class Level100Waves : WaveSetSource
{
    #region Wave Source
    // =========================================================
    // Required Inspector-assignable source, never change back into a static-only class.
    public override WaveDefinition[] Build() => Create();
    #endregion

    #region Authored Stress Waves
    // =========================================================
    // Four minigun groups of five in every wave; light swarms contain 380 then 480 additional ships.
    // All five enabled portals are equally eligible for unpinned spawns. Manually start each wave.
    public static WaveDefinition[] Create() =>
    [
        // Wave 1 (formerly wave 4): 400 scheduled ships = 380 mixed attackers + exactly 20 armed gunners.
        Wave(1,
            Batch(count: 95, interval: 0.065, after: 0.90,
                Enemy("basic_mk1", 34),
                Enemy("scout_mk1", 28),
                Enemy("racer_mk1", 18),
                Enemy("reinforced_drone_mk1", 15),
                Enemy("medium_mk1", 5)),
            Batch(count: 5, interval: 0.110, after: 0.45,
                Enemy("gunner_drone_mk1")),
            Batch(count: 90, interval: 0.065, after: 0.80,
                Enemy("basic_mk1", 32),
                Enemy("scout_mk1", 26),
                Enemy("reinforced_drone_mk1", 25),
                Enemy("medium_mk1", 12),
                Enemy("shield_frigate_mk1", 3, shield: true),
                Enemy("shield_emitter_mk1", 2)),
            Batch(count: 5, interval: 0.110, after: 0.45,
                Enemy("gunner_drone_mk1")),
            Batch(count: 100, interval: 0.060, after: 1.00,
                Enemy("basic_mk1", 30),
                Enemy("scout_mk1", 28),
                Enemy("racer_mk1", 17),
                Enemy("reinforced_drone_mk1", 15),
                Enemy("medium_mk1", 8),
                Enemy("heavy_bulwark_mk1", 2)),
            Batch(count: 5, interval: 0.110, after: 0.45,
                Enemy("gunner_drone_mk1")),
            Batch(count: 95, interval: 0.067, after: 0.00,
                Enemy("basic_mk1", 29),
                Enemy("scout_mk1", 25),
                Enemy("reinforced_drone_mk1", 25),
                Enemy("medium_mk1", 13),
                Enemy("shield_frigate_mk1", 5, shield: true),
                Enemy("elite_mk1", 3)),
            Batch(count: 5, interval: 0.110, after: 0.00,
                Enemy("gunner_drone_mk1"))
        ),

        // Wave 2 (formerly wave 5): 500 scheduled ships = 480 mixed attackers + exactly 20 armed gunners.
        Wave(2,
            Batch(count: 120, interval: 0.058, after: 0.85,
                Enemy("basic_mk1", 30),
                Enemy("scout_mk1", 30),
                Enemy("racer_mk1", 18),
                Enemy("reinforced_drone_mk1", 17),
                Enemy("medium_mk1", 5)),
            Batch(count: 5, interval: 0.110, after: 0.45,
                Enemy("gunner_drone_mk1")),
            Batch(count: 115, interval: 0.058, after: 0.90,
                Enemy("basic_mk1", 28),
                Enemy("scout_mk1", 27),
                Enemy("racer_mk1", 12),
                Enemy("reinforced_drone_mk1", 18),
                Enemy("medium_mk1", 10),
                Enemy("shield_frigate_mk1", 3, shield: true),
                Enemy("heavy_bulwark_mk1", 2)),
            Batch(count: 5, interval: 0.110, after: 0.45,
                Enemy("gunner_drone_mk1")),
            Batch(count: 130, interval: 0.054, after: 1.10,
                Enemy("basic_mk1", 28),
                Enemy("scout_mk1", 25),
                Enemy("racer_mk1", 15),
                Enemy("reinforced_drone_mk1", 17),
                Enemy("medium_mk1", 9),
                Enemy("shield_frigate_mk1", 3, shield: true),
                Enemy("elite_mk1", 2),
                Enemy("cruiser_mk1", 1, shield: true)),
            Batch(count: 5, interval: 0.110, after: 0.45,
                Enemy("gunner_drone_mk1")),
            Batch(count: 115, interval: 0.060, after: 0.00,
                Enemy("basic_mk1", 27),
                Enemy("scout_mk1", 25),
                Enemy("reinforced_drone_mk1", 20),
                Enemy("racer_mk1", 12),
                Enemy("medium_mk1", 9),
                Enemy("shield_frigate_mk1", 4, shield: true),
                Enemy("heavy_bulwark_mk1", 2),
                Enemy("big_mk1", 1)),
            Batch(count: 5, interval: 0.110, after: 0.00,
                Enemy("gunner_drone_mk1"))
        )
    ];
    #endregion
}
