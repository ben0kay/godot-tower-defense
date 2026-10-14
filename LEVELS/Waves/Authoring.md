# C# wave authoring

`LEVELS/Level001/Level001Waves.cs` contains **three currently authored Enemy001-only test waves**. This is a content choice, not a restriction of the wave system: the validated catalog already includes **Enemy001, Enemy002 and ShieldGenerator**. Edit counts, enemy IDs, intervals and delays there. `WaveCatalog` maps `LevelConfig.WaveSetId = "level001"` to this file; empty IDs disable waves and unknown IDs raise setup errors.

Use static WaveBuilder helpers:

    Wave(1,
        Batch(count: 10, interval: 0.1, after: 2, Enemy("enemy001")),
        Batch(count: 5, interval: 0.3, after: 5, Enemy("enemy001")),
        Batch(count: 20, interval: 0.2, after: 0, Enemy("enemy001")))

Count is the total ships in a batch. The first spawn is immediate. Interval follows a successful spawn, and After begins after the batch's final successful spawn. Final-batch After is ignored. Earlier batches keep moving while later ones spawn. Completing a wave requires both zero planned spawns and zero owned live ships; Next Wave remains manual.

Entries accept relative double weights (80/20, 8/2 or .8/.2). Composition uses exact largest-remainder quotas with stable entry-order ties, then seeded shuffle. Types are selected once per batch; failed attempts retain the selected entry. **Register future enemy scenes in `ENEMIES/DefaultEnemyCatalog.tres`, not in `WaveCatalog.cs`**; registered Enemy002 and ShieldGenerator can already appear in weighted batches.

Optional Enemy(..., portal: "Portals/EnemyPortal2") requires that exact registered permitted portal. Missing/blocked entrances pause and retry without fallback. Opt-in full spawn shields are supported through `shield: true`; boss, stealth and other modifiers remain future work.

Global spawn attempt limit, retry interval and deterministic RNG seed live in ENEMIES/DefaultEnemyConfig.tres / Wave Scheduling. Wave data is immutable; runtime state and owned ships live only in WaveManager.

## Opt-in wave shields (implemented)

`WaveBuilder.Enemy` has a trailing optional `shield: false` parameter. An omitted flag keeps every ship unshielded, even if its type has nonzero MaxShield. Turn it on for a whole weighted entry with `shield: true`:

```csharp
// Four ordinary and one shielded Enemy001 in a five-ship batch:
Batch(count: 5, interval: 0.2, after: 0,
    Enemy("enemy001", weight: 80),
    Enemy("enemy001", weight: 20, shield: true))
```

`MaxShield` is configured once on `ENEMIES/Types/Enemy001/Enemy001.tres` (**100 SP** for Enemy001). Shielded spawns begin full; unshielded spawns begin at zero, including F2. Exact weighted quotas/shuffle remain unchanged; two entries can share the same EnemyId with different shield flags. Existing Level001 authored waves have **not** been changed, so nothing spawns shielded until you explicitly edit an entry. **ShieldEmitter restoration is already implemented** on ShieldGenerator; generic passive shield regeneration, shield visuals, stealth and boss wave modifiers remain future work.


## Mixed types and ShieldGenerator (implemented)

```csharp
// Example only: change Level001Waves.cs if you want this batch in the game.
Batch(count: 20, interval: 0.2, after: 0,
    Enemy("enemy001", weight: 75),
    Enemy("enemy002", weight: 25, portal: "Portals/EnemyPortal2", shield: true))
```

The example uses **15 Enemy001 and 5 Enemy002** in exact shuffled proportions; the Enemy002 entries require the named registered portal and begin with their 40 shield points. `Enemy("shieldgenerator")` also works and spawns a support ship with a separate `ShieldEmitter` component, which restores nearby eligible ships' shields even when those ships started unshielded. No general `flags:` API exists. Runtime catalog edits require a catalog `Rebuild()` and wave reconfiguration. Enemy death-spawning and attacks on towers are future E3 mechanics.
