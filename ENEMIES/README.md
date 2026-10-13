# ENEMIES — Ships, Waves, Spawning and Navigation

This top-level umbrella owns **enemy game mechanics**, separate from the physical battlefield and placed map objects.

| Folder | Responsibility |
| --- | --- |
| `Types/` | Enemy ship scenes, reusable definitions, variants and combat attributes. |
| `Movement/` | Enemy movement/steering and navigation toward a reachable approach to the Citadel. |
| `Spawning/` | SpawnManager mechanics: selects allowed spawn sources, checks spawn validity and instantiates ships. |
| `Waves/` | WaveManager mechanics: decides which ships appear, when, and in what quantities. |

**Do not put the Portal world scene here.** The visible reusable Portal object belongs in `WORLD/PORTALS/`; the defended Citadel belongs in `WORLD/OBJECTIVES/Citadel/`. Level-specific settings belong to `WORLD/LEVELS/`.

Expected future interaction: **WaveManager -> SpawnManager -> level-defined Portal(s) and/or map edges -> instantiated enemy -> navigation -> Citadel**.

The next planned development pass is the **TileMap/level-authoring system**, not enemy mechanics. This is a documentation/folder foundation only: no enemies, spawner scripts, wave logic or config Resources have been implemented yet. Add an `EnemyMasterConfig.cs` / `SpawnMasterConfig.cs` with purposeful exported settings when implementing the systems, following `NOTES/CodePreferences.md`.
