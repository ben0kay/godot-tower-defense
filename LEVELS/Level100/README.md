# Level100 — Five-Gate Swarm Stress Test

**New standalone debug/stress mission, registered as Level100 in the Campaign list.** This level is intentionally unbalanced and is not part of natural campaign progression: 2 manually started waves of **400 and 500 scheduled ships**, total **900**. Each wave contains **exactly 20 armed Gunner Drones** (four separate five-ship groups interspersed among the lighter swarms), for **40 total minigun enemies** across two waves. The final wave is the hardest and introduces a small handful of large/shielded ships.

The original waves 1–3 were removed at the user’s request. New wave1 is former wave4; new wave2 is former wave5. Their batches, enemy weights, shield flags, intervals and delays are unchanged. Historical performance captures referring to waves4/5 refer to these recipes before renumbering.

## Map authoring — originally blank

`Level100.tscn` is cloned from `LEVELS/LevelTemplate.tscn`. The original template had four unpainted TileMapLayers — Passable, Impassable, Interactive and Decorations — for you to design. Existing authored terrain is preserved by wave edits. The normal background, checkpoints, objective, debug/F2/F3 hooks and correct tile-set resources are retained. Five placeable/movable EnemyPortals are spread vertically near the **left edge** (x=736, y=672/1632/2592/3552/4512). The Citadel is on the **right** (5536,2592), encouraging long defense lines. Both are regular scene objects and can be repositioned or removed with config changes. The grid uses the existing world size/config (default 100×80 cells at 64 px each); no separate debug world machinery.

**You MUST paint walkable terrain before this map can spawn waves successfully.** Empty/unpainted cells are not navigable and not buildable. The level is intentionally available in the Campaign menu immediately, but pressing Start Wave on an unpainted map will leave spawning blocked until valid paths exist. Paint Passable ground and any optional Impassable/Interactive obstacles, making sure **each active portal has a valid path to a Citadel approach point** and enough spawn clearance for 22px-radius Gunner Drones and occasional bigger ships. The five portal NodePaths in `Level100Config.tres` must match the actual scene if you rename/remove them. Once painting is complete, launch Level100 from Campaign; no bespoke World/Main launcher required.

## Mission and starting resources

- `Level100Mission.tres`: number=100, enabled, data-driven campaign entry (does **not** unlock via Level020; selection is direct).
- `Level100Config.tres`: **100,000 Starting Credits**, **10,000 Citadel HP**, five active portals, portal-only spawning, `WaveSetId=level100` and Inspector-assigned `Level100Waves` resource. Those generous test values are level-only and do not modify shared economy/enemy/wave settings or other maps. No global enemy-cap/spawn-budget override is introduced; defaults are 1,000 maximum active enemies and 2 spawn attempts/frame (current `ENEMIES/EnemyMasterConfig.cs`).
- `Level100Waves.cs`: source-owned `[GlobalClass]` `WaveSetSource` so scene config serialization works. Every enemy ID is from the registered catalog; gunners use actual `gunner_drone_mk1` weapons and their small red minigun bullets. No new enemy asset is invented.

## Wave breakdown

| Wave | Mixed small/occasional larger ships | Armed gunners | Total scheduled | Design |
| --- | ---: | ---: | ---: | --- |
| 1 | 380 | 20 | **400** | Very dense attack, shielded Frigates, a few Heavy Bulwarks and Elites |
| 2 | 480 | 20 | **500** | Fastest/lightest flood with tougher escorts, rare shielded Cruiser and Big hull |
| **TOTAL** | **860** | **40** | **900** | Each wave uses four spaced 5-gunner groups |

Four ordinary-light groups alternating with four five-gunner groups create clear salvos rather than a single homogenous 500-ship queue. Gunners remain exactly 20 because they get their own dedicated `Batch(count: 5, ... Enemy("gunner_drone_mk1"))` each time. Batch `Count` means total ships, **not** ships per listed enemy type or per portal. Within mixed groups, enemy type proportions use the existing seeded exact-quota `WaveCatalog.Compose` machinery. Rare big types **do not include Transporters**, to avoid DeathSpawn children making the displayed on-screen total exceed scheduled counts. Normal kill/arrival mechanics still apply; wave completion waits for all owned live ships.

## Performance testing / cautions

Use F4 performance overlay and the built-in F2 spawner. Test wave counts one by one: record active ships, alive bullets, frame time and path budgets with increasing load. The scheduler deliberately does **not** spawn all 500 on a single frame; short `interval` values and small `after` gaps produce heavy overlapping groups. Wider/multiple corridors reduce congestion artifacts when measuring pure combat performance. The battlefield is intentionally punishing; a large Citadel HP pool and starting wallet make stress testing easier, not guaranteed victory.

Godot build/headless tests: `TESTS/Integration/Level100StressChecks.tscn` verifies mission resources, blank tile layers, five portals, wave quota/count composition and valid catalog IDs. Re-run existing `TESTS/Integration/MissionCatalogChecks.tscn` after registration. **Tests were authored but not executed by this GitHub-only work; no benchmark or desktop test is claimed.**

## Two-wave shortcut verification

C# build passed with zero warnings/errors; Godot headless import completed. The updated stress runner passed the two-wave count, numbering, catalog composition and20-gunners-per-wave checks (400/500 ships). Its later preexisting blank-terrain assertion still fails on the painted map; this wave edit does not change terrain or that assertion. A source comparison confirmed both retained recipes match the former waves4/5 byte-for-byte apart from numbering/comments.
