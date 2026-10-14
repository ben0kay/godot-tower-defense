# C# wave authoring

LEVELS/Level001/Level001Waves.cs contains three finite Enemy001-only test waves. Edit counts, intervals and post-batch delays directly here. WaveCatalog maps LevelConfig.WaveSetId = "level001" to this file; empty disables waves, and unknown IDs report setup errors instead of using another level's encounters.

Use static WaveBuilder helpers:

    Wave(1,
        Batch(count: 10, interval: 0.1, after: 2, Enemy("enemy001")),
        Batch(count: 5, interval: 0.3, after: 5, Enemy("enemy001")),
        Batch(count: 20, interval: 0.2, after: 0, Enemy("enemy001")))

Count is the total ships in a batch. The first spawn is immediate. Interval follows a successful spawn, and After begins after the batch's final successful spawn. Final-batch After is ignored. Earlier batches keep moving while later ones spawn. Completing a wave requires both zero planned spawns and zero owned live ships; Next Wave remains manual.

Entries accept relative double weights (80/20, 8/2 or .8/.2). Composition uses exact largest-remainder quotas with stable entry-order ties, then seeded shuffle. Types are selected once per batch; failed attempts retain the selected entry. Enemy001 is the only scene in the runnable catalog; register real future scenes in WaveCatalog before using new IDs.

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

`MaxShield` is configured once on `ENEMIES/Types/Enemy001/Enemy001.tres` (**100 SP** for Enemy001). Shielded spawns begin full; unshielded spawns begin at zero, including F2. Exact weighted quotas/shuffle remain unchanged; two entries can share the same EnemyId with different shield flags. Existing Level001 authored waves have **not** been changed, so nothing spawns shielded until you explicitly edit an entry. Shield regeneration/visuals/stealth/boss abilities are future work.


E2A: register new Enemy scenes in ENEMIES/DefaultEnemyCatalog.tres, then use their canonical EnemyDefinition.EnemyId. Enemy002 is now available with30 hull/160px/s; the authored Level001 encounters remain unchanged. Wave timing/weights/portal/shield ownership are unchanged.


E2B: Enemy("shieldgenerator") spawns the registered support scene with a ShieldEmitter child. Its default pulses replenish nearby positive-capacity allies, including unshielded starts, while shield:true separately controls full starting capacity. E3 death-spawn/attack behavior is not implemented. Runtime catalog changes require Rebuild and wave reconfiguration.
