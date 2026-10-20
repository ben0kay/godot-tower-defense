// Level020 / The Five Gates: seven escalating encounters across five eligible entrances.
// Waves 1-3 are small swarms, 4-5 mix transporters with escorts, and 6-7 introduce heavy ships.
using Godot;
using static WaveBuilder;

[GlobalClass]
public partial class Level020Waves : WaveSetSource
{
    #region Wave Source
    // =========================================================
    // Keep this recipe assignable to LevelConfig.WaveSource as well as existing Create() callers.
    public override WaveDefinition[] Build() => Create();
    #endregion

    #region Five Gates Encounters
    // =========================================================
    // Every entry uses the level's existing unpinned portal distribution; numbers are total ships per batch.
    // All shields are deliberately opt-in, and Heavy Transporter child ships are separate death-spawn effects.
    public static WaveDefinition[] Create() =>
    [
        // 1 — First contact: a dense stream of Standard and Swift, with only a couple of fast Racers.
        Wave(1,
            Batch(count: 32, interval: 0.18, after: 3,
                Enemy("basic_mk1", 75), Enemy("scout_mk1", 25)),
            Batch(count: 20, interval: 0.23, after: 0,
                Enemy("basic_mk1", 50), Enemy("scout_mk1", 40), Enemy("racer_mk1", 10))),

        // 2 — Fast swarms from all portals; Reinforced Drones join without heavy hulls.
        Wave(2,
            Batch(count: 42, interval: 0.15, after: 3,
                Enemy("basic_mk1", 55), Enemy("scout_mk1", 30), Enemy("racer_mk1", 15)),
            Batch(count: 26, interval: 0.22, after: 0,
                Enemy("basic_mk1", 30), Enemy("scout_mk1", 25),
                Enemy("racer_mk1", 20), Enemy("reinforced_drone_mk1", 25))),

        // 3 — Largest light swarm, followed by early Bulwarks and a few support generators.
        Wave(3,
            Batch(count: 50, interval: 0.16, after: 3,
                Enemy("basic_mk1", 35), Enemy("scout_mk1", 20), Enemy("racer_mk1", 15),
                Enemy("reinforced_drone_mk1", 20), Enemy("medium_mk1", 10)),
            Batch(count: 30, interval: 0.31, after: 0,
                Enemy("reinforced_drone_mk1", 30), Enemy("medium_mk1", 40),
                Enemy("basic_mk1", 20), Enemy("shield_emitter_mk1", 10))),

        // 4 — Transporter debut; mixed escort ships and the first opt-in shielded frigates.
        Wave(4,
            Batch(count: 40, interval: 0.19, after: 3,
                Enemy("basic_mk1", 30), Enemy("scout_mk1", 25),
                Enemy("reinforced_drone_mk1", 25), Enemy("medium_mk1", 20)),
            Batch(count: 20, interval: 0.36, after: 3,
                Enemy("medium_mk1", 35), Enemy("reinforced_drone_mk1", 30),
                Enemy("shield_emitter_mk1", 20), Enemy("shield_frigate_mk1", 15, shield: true)),
            Batch(count: 12, interval: 0.90, after: 0,
                Enemy("transporter_mk1", 33), Enemy("medium_mk1", 33),
                Enemy("basic_mk1", 34))),

        // 5 — Carrier convoy: lots of smaller escorts, regular Transporters and a few Heavy Transporters.
        Wave(5,
            Batch(count: 36, interval: 0.17, after: 2,
                Enemy("basic_mk1", 25), Enemy("scout_mk1", 25),
                Enemy("reinforced_drone_mk1", 25), Enemy("racer_mk1", 25)),
            Batch(count: 24, interval: 0.38, after: 3,
                Enemy("transporter_mk1", 33), Enemy("medium_mk1", 25),
                Enemy("reinforced_drone_mk1", 25), Enemy("shield_frigate_mk1", 17, shield: true)),
            Batch(count: 12, interval: 0.95, after: 0,
                Enemy("heavy_transporter_mk1", 25), Enemy("transporter_mk1", 25),
                Enemy("heavy_bulwark_mk1", 25), Enemy("shield_frigate_mk1", 25, shield: true))),

        // 6 — Armoured push: Bulwarks, Big/Elite hulls, frigates and heavy carrier escorts.
        Wave(6,
            Batch(count: 30, interval: 0.34, after: 3,
                Enemy("medium_mk1", 20), Enemy("reinforced_drone_mk1", 20),
                Enemy("heavy_bulwark_mk1", 30), Enemy("elite_mk1", 20),
                Enemy("shield_frigate_mk1", 10, shield: true)),
            Batch(count: 22, interval: 0.75, after: 4,
                Enemy("big_mk1", 30), Enemy("heavy_bulwark_mk1", 25),
                Enemy("elite_mk1", 25, shield: true), Enemy("shield_frigate_mk1", 20, shield: true)),
            Batch(count: 8, interval: 1.05, after: 0,
                Enemy("heavy_transporter_mk1", 25), Enemy("big_mk1", 25),
                Enemy("elite_mk1", 25, shield: true), Enemy("shield_frigate_mk1", 25, shield: true))),

        // 7 — Final assault: large hulls with shield escorts, finishing on two shielded Cruisers.
        Wave(7,
            Batch(count: 28, interval: 0.29, after: 3,
                Enemy("reinforced_drone_mk1", 20), Enemy("medium_mk1", 15),
                Enemy("heavy_bulwark_mk1", 30), Enemy("elite_mk1", 20, shield: true),
                Enemy("shield_frigate_mk1", 15, shield: true)),
            Batch(count: 20, interval: 0.70, after: 4,
                Enemy("big_mk1", 30), Enemy("heavy_bulwark_mk1", 25),
                Enemy("elite_mk1", 25, shield: true), Enemy("shield_frigate_mk1", 20, shield: true)),
            Batch(count: 10, interval: 1.20, after: 0,
                Enemy("cruiser_mk1", 20, shield: true),
                Enemy("heavy_transporter_mk1", 30),
                Enemy("big_mk1", 30, shield: true),
                Enemy("shield_frigate_mk1", 20, shield: true)))
    ];
    #endregion
}
