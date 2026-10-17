# C# wave authoring

`LEVELS/Level001/Level001Waves.cs` contains **three escalating mixed test waves (38 / 40 / 55 enemies)**. The first introduces small shielded Standard Drones; the second adds Swift Interceptors, and the third adds a few Bulwarks. This is a content choice, not a restriction of the wave system: the validated catalog already includes **Enemy001, Enemy002 and ShieldGenerator**. Edit counts, internal enemy IDs, intervals and delays there. The new medium ship `medium_mk1` (`Enemy004.tscn`, Bulwark Drone) can be authored as `Enemy("medium_mk1")`, independently of its numbered filename; current Level001 encounters have not been changed. Filenames remain `Enemy001.tscn`/`Enemy002.tscn` for development history; stable IDs are `basic_mk1`, `scout_mk1`, `shield_emitter_mk1`. `WaveCatalog` maps `LevelConfig.WaveSetId = "level001"` to this file; empty IDs disable waves and unknown IDs raise setup errors.

Use static WaveBuilder helpers:

    Wave(1,
        Batch(count: 10, interval: 0.1, after: 2, Enemy("basic_mk1")),
        Batch(count: 5, interval: 0.3, after: 5, Enemy("basic_mk1")),
        Batch(count: 20, interval: 0.2, after: 0, Enemy("basic_mk1")))

Count is the total ships in a batch. The first spawn is immediate. Interval follows a successful spawn, and After begins after the batch's final successful spawn. Final-batch After is ignored. Earlier batches keep moving while later ones spawn. Completing a wave requires both zero planned spawns and zero owned live ships; Next Wave remains manual.

Entries accept relative double weights (80/20, 8/2 or .8/.2). Composition uses exact largest-remainder quotas with stable entry-order ties, then seeded shuffle. Types are selected once per batch; failed attempts retain the selected entry. **Register future enemy scenes in `ENEMIES/DefaultEnemyCatalog.tres`, not in `WaveCatalog.cs`**; registered Enemy002 and ShieldGenerator can already appear in weighted batches.

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


## Mixed types and ShieldGenerator (implemented)

```csharp
// Example only: change Level001Waves.cs if you want this batch in the game.
Batch(count: 20, interval: 0.2, after: 0,
    Enemy("basic_mk1", weight: 75),
    Enemy("scout_mk1", weight: 25, portal: "Portals/EnemyPortal2", shield: true))
```

The example uses **15 Enemy001 and 5 Enemy002** in exact shuffled proportions; the Enemy002 entries require the named registered portal and begin with their 40 shield points. **Big mk.1 now registered:** `Enemy("big_mk1")` adds a slow, heavy 1,000-hull/80px/s/10-Credit enemy without shields; `Enemy("big_mk1", shield: true)` adds its optional 200 shield points and shared aura/shield meter. The large 112×120 SVG uses the existing 24px navigation circle, so tight-path visual clearance needs playtesting. This enemy is available in F2/F3 but **not automatically included in Level001 or Level002's authored waves**.

`Enemy("shield_emitter_mk1")` also works and spawns a support ship with a separate `ShieldEmitter` component, which restores nearby eligible ships' shields even when those ships started unshielded. No general `flags:` API exists. Runtime catalog edits require a catalog `Rebuild()` and wave reconfiguration. Enemy death-spawning and attacks on towers are future E3 mechanics.
