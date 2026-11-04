## Classification migration — 2026-11-04

Existing fleet, catalog, Gunner and F2 source checks were updated for Class/Rank/Role, compact names and preserved scene-preview indices. These checks were **not executed** in this environment (Godot/.NET unavailable); focused resource/diff checks only. See the [roster](../../ENEMIES/Types/ROSTER.md) for editor acceptance and checkpoint compatibility caveats.

# Fleet expansion verification — 2026-10-18

Run `TESTS/Enemies/FleetExpansionChecks.tscn` in Godot .NET headless after importing assets and building C#.

Historical validation of the original fleet expansion with Godot 4.7.2 / .NET 8 (before the 2026-11-04 classification changes):

- C# build: zero errors/warnings; all new SVG textures imported successfully and their dimensions verified through actual spawned Sprite2D textures. Enlarged vector renders visually reviewed.
- At that time: 13 registered definitions, valid family values and invalid-enum rejection; each new type spawned with its authored family, health, speed and art size. Current source instead checks all three independent classifications, Tiny Boss/Common Capital flexibility, and 14 registered types; those changed assertions await owner execution.
- Normal spawns start unshielded; Shield Frigate starts at 350 SP when wave-opted, uses the shared aura and absorbs damage before hull.
- Hull hits, once-only kill rewards, once-only Citadel damage and no carrier payload on arrival.
- Heavy Transporter reserves both payloads before removal, releases six Drones plus four Interceptors under existing queue budgets, transfers wave ownership and destination, and blocks victory until every child is defeated. Verified on both Level001 and Level020 with actual results snapshots (11 kills).
- Existing `DeathSpawnChecks.tscn` passes, including original Transporter, routing, pause, limits, unload cancellation and 500-active scheduling checks.

The broader `EnemyIntegrationChecks.tscn` stops at `TowerCatalogChecks`: “Actual passive selection has safe no-upgrades Inspector”. The same assertion fails on unchanged base commit `3b076aa`; this is an existing unrelated failure and that suite is not reported as passing. Existing default-tower-catalog recovery warnings also occur in the baseline fixtures.

Desktop play/appearance review remains: F2 cycles new types in the appended catalog order (8–13); F3 offers them through the same catalog. Existing mission waves are unchanged. Shield Frigate shields are opt-in, including during debug spawning. Large hulls exceed their 24px routing radius; this pass does not implement hull-shaped pathfinding or guarantee wing clearance in narrow corridors.
