// Level020 / The Five Gates: seven escalating encounters with more light enemies and fewer heavy escorts.
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
            Presentation(speaker: "TACTICAL COMMAND",
                text: "Commander, multiple contacts are approaching through the five gates. Prepare your defenses.",
                voiceCueId: "level020_wave01"),
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

        // 3 — Light swarm remains dominant; early Bulwarks and shield support appear only sparingly.
        Wave(3,
            Batch(count: 50, interval: 0.16, after: 3,
                Enemy("basic_mk1", 40), Enemy("scout_mk1", 20), Enemy("racer_mk1", 15),
                Enemy("reinforced_drone_mk1", 20), Enemy("medium_mk1", 5)),
            Batch(count: 30, interval: 0.31, after: 0,
                Enemy("reinforced_drone_mk1", 30), Enemy("medium_mk1", 20),
                Enemy("basic_mk1", 35), Enemy("scout_mk1", 10),
                Enemy("shield_emitter_mk1", 5))),

        // 4 — Transporters still debut, but fewer carriers and shielded escorts increase early recovery time.
        Wave(4,
            Presentation(alert: WaveAlertKind.Heavy, bannerTitle: "TRANSPORTERS DETECTED",
                speaker: "TACTICAL COMMAND",
                text: "Enemy transporters have joined the assault. Expect additional drones when they are destroyed.",
                voiceCueId: "level020_wave04"),
            Batch(count: 40, interval: 0.19, after: 3,
                Enemy("basic_mk1", 40), Enemy("scout_mk1", 30),
                Enemy("reinforced_drone_mk1", 20), Enemy("medium_mk1", 10)),
            Batch(count: 20, interval: 0.36, after: 3,
                Enemy("medium_mk1", 20), Enemy("reinforced_drone_mk1", 25),
                Enemy("shield_emitter_mk1", 15), Enemy("shield_frigate_mk1", 10, shield: true),
                Enemy("basic_mk1", 20), Enemy("scout_mk1", 10)),
            Batch(count: 12, interval: 0.90, after: 0,
                Enemy("transporter_mk1", 17), Enemy("medium_mk1", 25),
                Enemy("basic_mk1", 42), Enemy("scout_mk1", 16))),

        // 5 — Keep the convoy theme but halve the heaviest escorts and replace carriers with drones.
        Wave(5,
            Batch(count: 36, interval: 0.17, after: 2,
                Enemy("basic_mk1", 25), Enemy("scout_mk1", 25),
                Enemy("reinforced_drone_mk1", 25), Enemy("racer_mk1", 25)),
            Batch(count: 24, interval: 0.38, after: 3,
                Enemy("transporter_mk1", 17), Enemy("medium_mk1", 20),
                Enemy("reinforced_drone_mk1", 25), Enemy("shield_frigate_mk1", 8, shield: true),
                Enemy("basic_mk1", 15), Enemy("scout_mk1", 15)),
            Batch(count: 12, interval: 0.95, after: 0,
                Enemy("heavy_transporter_mk1", 8), Enemy("transporter_mk1", 17),
                Enemy("heavy_bulwark_mk1", 17), Enemy("shield_frigate_mk1", 17, shield: true),
                Enemy("basic_mk1", 41))),

        // 6 — Heavy units still headline the wave; light drones now fill out much of the assault.
        Wave(6,
            Presentation(alert: WaveAlertKind.Heavy, bannerTitle: "HEAVY HOSTILES INCOMING",
                speaker: "TACTICAL COMMAND",
                text: "Commander, heavy armor is approaching. Focus your strongest towers on the largest ships.",
                voiceCueId: "level020_wave06"),
            Batch(count: 30, interval: 0.34, after: 3,
                Enemy("medium_mk1", 20), Enemy("reinforced_drone_mk1", 20),
                Enemy("heavy_bulwark_mk1", 15), Enemy("elite_mk1", 10),
                Enemy("shield_frigate_mk1", 10, shield: true), Enemy("scout_mk1", 25)),
            Batch(count: 22, interval: 0.75, after: 4,
                Enemy("big_mk1", 15), Enemy("heavy_bulwark_mk1", 15),
                Enemy("elite_mk1", 15, shield: true), Enemy("shield_frigate_mk1", 15, shield: true),
                Enemy("basic_mk1", 20), Enemy("reinforced_drone_mk1", 20)),
            Batch(count: 8, interval: 1.05, after: 0,
                Enemy("heavy_transporter_mk1", 12), Enemy("big_mk1", 12),
                Enemy("elite_mk1", 12, shield: true), Enemy("shield_frigate_mk1", 12, shield: true),
                Enemy("basic_mk1", 26), Enemy("reinforced_drone_mk1", 26))),

        // 7 — Retain exactly two shielded Cruiser bosses; replace many escort heavies with lighter ships.
        Wave(7,
            Presentation(alert: WaveAlertKind.Final, bannerTitle: "FINAL WAVE INCOMING",
                speaker: "TACTICAL COMMAND",
                text: "Commander, massive hostile signatures detected. Protect the Citadel. This is the final assault.",
                voiceCueId: "level020_wave07"),
            Batch(count: 28, interval: 0.29, after: 3,
                Enemy("reinforced_drone_mk1", 20), Enemy("medium_mk1", 15),
                Enemy("heavy_bulwark_mk1", 15), Enemy("elite_mk1", 12, shield: true),
                Enemy("shield_frigate_mk1", 10, shield: true), Enemy("basic_mk1", 18),
                Enemy("scout_mk1", 10)),
            Batch(count: 20, interval: 0.70, after: 4,
                Enemy("big_mk1", 15), Enemy("heavy_bulwark_mk1", 15),
                Enemy("elite_mk1", 15, shield: true), Enemy("shield_frigate_mk1", 15, shield: true),
                Enemy("basic_mk1", 20), Enemy("scout_mk1", 20)),
            Batch(count: 10, interval: 1.20, after: 0,
                Enemy("cruiser_mk1", 20, shield: true),
                Enemy("heavy_transporter_mk1", 10),
                Enemy("big_mk1", 20, shield: true),
                Enemy("shield_frigate_mk1", 10, shield: true),
                Enemy("basic_mk1", 20), Enemy("scout_mk1", 20)))

    ];
    #endregion
}
