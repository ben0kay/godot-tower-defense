# C# wave authoring

Level001Waves.cs contains three finite Enemy001-only test waves. Edit counts, intervals and post-batch delays directly here. WaveCatalog maps LevelConfig.WaveSetId = "level001" to this file; empty disables waves, and unknown IDs report setup errors instead of using another level's encounters.

Use static WaveBuilder helpers:

    Wave(1,
        Batch(count: 10, interval: 0.1, after: 2, Enemy("enemy001")),
        Batch(count: 5, interval: 0.3, after: 5, Enemy("enemy001")),
        Batch(count: 20, interval: 0.2, after: 0, Enemy("enemy001")))

Count is the total ships in a batch. The first spawn is immediate. Interval follows a successful spawn, and After begins after the batch's final successful spawn. Final-batch After is ignored. Earlier batches keep moving while later ones spawn. Completing a wave requires both zero planned spawns and zero owned live ships; Next Wave remains manual.

Entries accept relative double weights (80/20, 8/2 or .8/.2). Composition uses exact largest-remainder quotas with stable entry-order ties, then seeded shuffle. Types are selected once per batch; failed attempts retain the selected entry. Enemy001 is the only scene in the runnable catalog; register real future scenes in WaveCatalog before using new IDs.

Optional Enemy(..., portal: "Portals/EnemyPortal2") requires that exact registered permitted portal. Missing/blocked entrances pause and retry without fallback. No boss, shield, stealth or other modifier mechanics are added.

Global spawn attempt limit, retry interval and deterministic RNG seed live in ENEMIES/DefaultEnemyConfig.tres / Wave Scheduling. Wave data is immutable; runtime state and owned ships live only in WaveManager.
