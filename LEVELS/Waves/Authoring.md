## SE1 — stealth enemies and reveal beacon — implemented 2026-10-20 UTC

Waves may now author `Enemy("basic_mk1", stealth: true)` independently of `shield: true`. Default spawns remain ordinary; no existing wave/map was changed. Stealth ships render at30% alpha, revealed ships at100%, with configurable/dimmed defense feedback that composes with Cryo/Stasis. All aimed towers share detection filtering and cached-lock validation; private TowerConfig.CanDetectStealth opts a specialist in without globally revealing enemies. Existing attacks already emitted, splash and untargeted Shockwave remain able to damage hidden ships (stealth is targeting, not immunity).

New Ctrl / Utility Stealth Beacon:250 Credits,1×1,2s build,180 HP,256px range, one paid tier. It reveals enemies inside range for all towers; overlap, exit/re-entry, disable/reactivation, construction, destruction, relocation, enemy removal/reuse and world teardown are source-owned. One lazy spatial reveal index listens to enemy motion only while beacons are active; no per-frame enemy scans or per-beacon processing. See ENEMIES/Shared/Stealth, FEEDBACK/Stealth, TOWERS/Shared/Detection, TOWERS/Utility/StealthBeacon and TESTS/Stealth READMEs. Existing Inspector layout/resize is retained. Earlier blanket “stealth/detection future” notes below are historical; stealth preference scoring, Radar, tower-grant detection beacons and further content remain separate future work.

---

## Level020 setup-error repair — 2026-10-20 UTC

The seven-wave content update had changed Level020Waves back to a static class, breaking the serialized LevelConfig.WaveSource contract from the shared-level pass. Restored [GlobalClass] Resource inheritance (WaveSetSource) and Build() → Create(); all seven wave definitions, map and settings are retained. Recipes assigned in LevelConfig must remain resource classes; preserve the wrapper when editing batches. Added TESTS/Integration/Level020WaveSetupChecks.tscn to load the actual saved resource and launcher, assert seven Ready waves and successfully start wave1. C# build (zero warnings/errors) and this headless regression passed.

---

## Level020 seven-wave Five Gates assault (2026-10-19)

`LEVELS/Level020/Level020Waves.cs` now defines **seven** manual waves with scheduled counts **52 / 68 / 80 / 72 / 72 / 60 / 58** (462 total, not counting death-spawn children). Waves1–3 use many small Standard/Swift/Racer/Reinforced ships and a few Bulwarks/supports; waves4–5 introduce regular and Heavy Transporters with escort squads; waves6–7 feature Big, Elite and Heavy Bulwarks, ending with exactly two opt-in **shielded Cruisers**. All five portals remain eligible; `Level020Config.tres` and the map are unchanged. Existing per-entry `shield: true` and weighted quota mechanisms are reused. Verify with `TESTS/Integration/Level020Checks.cs` and `LevelReorganizationChecks.cs`. See [Level020 design](../Level020/README.md) for the encounter-by-encounter table and balancing notes.

---

## Level002 tutorial waves — 2026-10-18 (implemented)

The newly painted Level002 is now playable through **Campaign → Levels → PLAY LEVEL 002**, `MainLevel002.tscn` (F6), and Level001's Next Level result action. Its dedicated `WORLD/WorldLevel002.tscn` mounts the existing user-authored map unchanged. Level020 remains separately playable and unchanged. Level002 Next stays disabled because there is no playable Level003; it never skips to Level020.

`LEVELS/Level002/Level002Waves.cs` defines four modest, manually started waves. Enemy “1–2–3” means the three introductory plain roles: Standard Drone (`basic_mk1`), Swift Interceptor (`scout_mk1`) and Bulwark Drone (`medium_mk1`, asset Enemy004). There is no Enemy003 scene. No shields, support ships, carriers, bosses or extra mechanics are introduced.

| Wave | Standard | Swift | Bulwark | Total | Introductory purpose |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 12 | 0 | 0 | 12 | Baseline shooting/building |
| 2 | 10 | 6 | 0 | 16 | Fast, fragile targets |
| 3 | 12 | 4 | 4 | 20 | Slower, tougher targets |
| 4 | 16 | 4 | 4 | 24 | Light mixed finale |

Spawn intervals are 0.65–1.4 seconds, with 3-second gaps between batches. Wave4 uses exact weighted composition; all other batches use one type. Existing Start/Next Wave controls allow unlimited preparation time. Local settings: **1,000 starting Credits**, **1,000 Citadel HP**, existing provisional **500 victory XP / 0 defeat XP**. Both authored portals (`EnemyPortal1`, `EnemyPortal2`) are permitted; map-edge spawning is replaced by portal mode for this level. Counts/timings remain C#-authored for AI-assisted tuning as requested. No map tile, Portal/Citadel coordinate, shared scheduler or enemy/tower balance changes.

C# build and `Level002TutorialChecks.tscn` passed: actual map/scheduler spawned all 72 unshielded enemies and produced victory only after wave4. `ResultsChecks.tscn` passed actual Level001→Level002 navigation, retry, menu and missing-next behavior. The isolated tutorial test kills enemies to verify scheduling/ownership; desktop tower balance/playfeel remains provisional. Earlier unplayable-template notes below are historical and superseded by this section.

---

# C# wave authoring

`LEVELS/Level001/Level001Waves.cs` contains **three escalating mixed test waves (38 / 40 / 55 enemies)**. The first introduces small shielded Standard Drones; the second adds Swift Interceptors, and the third adds a few Bulwarks. This is a content choice, not a restriction of the wave system: the validated catalog already includes **Enemy001, Enemy002 and Enemy003**. Edit counts, internal enemy IDs, intervals and delays there. The new medium ship `medium_mk1` (`Enemy004.tscn`, Bulwark Drone) can be authored as `Enemy("medium_mk1")`, independently of its numbered filename; current Level001 encounters have not been changed. Filenames remain `Enemy001.tscn`/`Enemy002.tscn` for development history; stable IDs are `basic_mk1`, `scout_mk1`, `shield_emitter_mk1`. `WaveCatalog` maps `LevelConfig.WaveSetId = "level001"` to this file; empty IDs disable waves and unknown IDs raise setup errors.

Use static WaveBuilder helpers:

    Wave(1,
        Batch(count: 10, interval: 0.1, after: 2, Enemy("basic_mk1")),
        Batch(count: 5, interval: 0.3, after: 5, Enemy("basic_mk1")),
        Batch(count: 20, interval: 0.2, after: 0, Enemy("basic_mk1")))

Count is the total ships in a batch. The first spawn is immediate. Interval follows a successful spawn, and After begins after the batch's final successful spawn. Final-batch After is ignored. Earlier batches keep moving while later ones spawn. Completing a wave requires both zero planned spawns and zero owned live ships; Next Wave remains manual.

Entries accept relative double weights (80/20, 8/2 or .8/.2). Composition uses exact largest-remainder quotas with stable entry-order ties, then seeded shuffle. Types are selected once per batch; failed attempts retain the selected entry. **Register future enemy scenes in `ENEMIES/DefaultEnemyCatalog.tres`, not in `WaveCatalog.cs`**; registered Enemy002 and Enemy003 can already appear in weighted batches.

Optional Enemy(..., portal: "Portals/EnemyPortal2") requires that exact registered permitted portal. Missing/blocked entrances pause and retry without fallback. Opt-in full spawn shields are supported through `shield: true`; boss, stealth and other modifiers remain future work.

Global spawn attempt limit, retry interval and deterministic RNG seed live in ENEMIES/DefaultEnemyConfig.tres / Wave Scheduling. Wave data is immutable; runtime state and owned ships live only in WaveManager.

## Opt-in wave shields (implemented)

`WaveBuilder.Enemy` has a trailing optional `shield: false` parameter. An omitted flag keeps every ship unshielded, even if its type has nonzero MaxShield. Turn it on for a whole weighted entry with `shield: true`:

```csharp
// Four unshielded and one shielded Swift Interceptor in a five-ship batch:
Batch(count: 5, interval: 0.2, after: 0,
    Enemy("scout_mk1", weight: 80),
    Enemy("scout_mk1", weight: 20, shield: true))
```

`Enemy001.tres` now saves **20 shield points of capacity**; the integration suite temporarily uses 100 for its exact damage fixtures. A real shielded wave entry can use `basic_mk1` (20 SP) or `scout_mk1` (40 SP). Shielded spawns begin full and automatically show the shared reddish `FEEDBACK/Shields` aura; unshielded entries and F2 still start at zero. Exact weighted quotas/shuffle are unchanged, and entries with the same EnemyId can have different shield flags. **Level001 now opts in 7, 12 and 18 shielded enemies across waves 1–3.** ShieldEmitter support restoration still exists but is not authored in Level001 waves; natural/passive shield regeneration, stealth and boss wave modifiers remain future work.


## Mixed types and Enemy003 (implemented)

```csharp
// Example only: change Level001Waves.cs if you want this batch in the game.
Batch(count: 20, interval: 0.2, after: 0,
    Enemy("basic_mk1", weight: 75),
    Enemy("scout_mk1", weight: 25, portal: "Portals/EnemyPortal2", shield: true))
```

The example uses **15 Enemy001 and 5 Enemy002** in exact shuffled proportions; the Enemy002 entries require the named registered portal and begin with their 40 shield points. **Big mk.1 now registered:** `Enemy("big_mk1")` adds a slow, heavy 1,000-hull/80px/s/10-Credit enemy without shields; `Enemy("big_mk1", shield: true)` adds its optional 200 shield points and shared aura/shield meter. The large 112×120 SVG uses the existing 24px navigation circle, so tight-path visual clearance needs playtesting. This enemy is available in F2/F3 but **not automatically included in Level001 or Level020's authored waves**.

`Enemy("shield_emitter_mk1")` also works and spawns a support ship with a separate `ShieldEmitter` component, which restores nearby eligible ships' shields even when those ships started unshielded. No general `flags:` API exists. Runtime catalog edits require a catalog `Rebuild()` and wave reconfiguration. Enemy death-spawning and attacks on towers are future E3 mechanics.
