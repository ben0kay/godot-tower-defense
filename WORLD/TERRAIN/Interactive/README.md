# Interactive Terrain — Foundation + Lava implemented

One `World/InteractiveTiles` manager handles gameplay interactions from `Level/Terrain/Interactive`. Foundation is passive terrain metadata; Lava uses swept entry and timed exposure. There are no per-tile nodes, physics shapes or timers, and no per-frame tile or full-enemy-registry scans. The existing shared enemy spatial index and combat query consumers remain intact.

## Authoring and precedence

Both Level001 and LevelTemplate expose `Terrain/Interactive` with `TileSets/InteractiveTerrain.tres`:

| Palette choice | Source / atlas | can_build | blocks_enemies |
| --- | --- | --- | --- |
| Raised Foundation | 0 / (1,0) | true | true |
| Lava | 1 / (0,0) | false | false |

Paint Lava using this layer; no Lava was arbitrarily added to the playable map. Its64×64 SVG is predominantly vivid molten orange with subtle yellow highlights and tiny sparse flecks, suitable for adjoining pools. Foundation reuses its original Metal atlas art. Behavior comes from explicit `interaction` metadata, never color or artwork.

WorldGrid caches Passable, then Interactive overrides (including false build permission), then Impassable structural rules. Portal/Citadel reservations and tower occupancy retain final authority. Thus Lava over a buildable floor denies towers but remains walkable; it cannot erase a wall or reserved placement rule. Structurally blocked cells do not become active Lava hazards. Foundation stays buildable **and solid**, retaining its existing navigation/placement semantics.

All74 saved Level001 Foundation cells were explicitly moved without changing coordinates, tile IDs, artwork or effective rules. Template receives the new layer/palette. Foundation was removed from the current Impassable editor palette after migration; the legacy mixed WorldTerrain palette is retained. See `TESTS/Terrain/InteractiveTerrainMigration.md` for the exact176-cell equality check and compatibility note for other old maps.

Call `WorldGrid.RebuildTerrain()` after runtime paint/erase edits. It rebuilds caches/reconciles occupants, but only changes NavigationRevision when effective static blocking or Citadel approach cells change. Spawn-source edits invalidate their own cache without forcing A* work. Unchanged Lava repaint preserves exposure/timer; erased or blocked Lava ends it. Newly affected occupants are discovered using nearby circle queries from the existing EnemyRegistry index, then snapshotted before entry damage callbacks.

## Inspector configuration

`World/InteractiveTiles.LavaConfig` references `Interactive/Lava/DefaultLavaTileConfig.tres`:

| Setting | Default | Behavior |
| --- | --- | --- |
| DamagePerTick | 5 | Base amount for entry and periodic hits |
| DamageIntervalSeconds | 0.5 | Finite, strictly positive interval |
| DamageOnEntry | true | Immediate first/re-entry hit when enabled |
| DamageType | Energy | Existing provisional type; inherited defense coefficients apply |

Damage must be finite/nonnegative and the type valid. Malformed settings reject before replacing cached hit rules. Live valid resource edits change future hits and reschedule active next ticks from the last accounted instant, without applying a new entry hit. Assign the config resource before startup; edit its fields through the Inspector. No Thermal type, research or burning status was introduced.

## Exposure and movement

- Entering Lava applies one immediate hit when enabled, then schedules periodic hits per enemy. Standing still continues taking damage. With defaults, entry plus0.5/1.0/1.5/2.0sec hits totals25 base points by2sec, before defenses.
- Adjacent Lava cells maintain the same exposure/timer, including several crossed in one physics tick. No connected-region flood-fill or cell-boundary damage multiplier is needed.
- Leaving removes the timer. Re-entry applies a fresh entry hit and starts a new schedule. Crossing a normal gap and another Lava region within one movement segment produces another entry.
- EnemyMovement reports each actual clearance-approved segment, its travel duration and offset within the tick. A grid DDA accounts only for the time spent inside each cell. Fast through-passes cannot tunnel; diagonal corner-only touches have zero exposure. Exact endpoints use normal floored cell ownership.
- A late manager physics priority100 completes only the unaccounted part of each active occupant's tick, so moving time is not counted twice and stopped/waiting enemies still burn. Double clocks plus an interval-scaled maximum1-microsecond boundary tolerance keep float movement samples consistent at30/60/120Hz. Every owed nonzero periodic hit is delivered; zero-amount hits can fast-forward their no-op schedule.
- Initialization and direct position assignments are arrivals/teleports: one destination entry, no damage from intervening cells. Rotation cannot cause re-entry. Normal movement uses a suppression helper that retains existing spatial notifications while bypassing teleport semantics.

Hits go through `Enemy.ApplyDamage(new DamageInfo(amount, type))` with **no tower DamageSource**. Shields, hull, death and Credits use the existing resolver/lifecycle; no tower XP is awarded. Entry/periodic encounters position death feedback at the encounter, and lethal hits stop further movement immediately. Dead/arrived/freed enemies leave the exposure dictionary immediately. Successful spawns already killed by entry hazards still count as scheduled wave spawns, but never become pending living wave ownership. Unload releases occupants, caches and resource subscriptions.

## Verification and scope

`TESTS/Terrain/InteractiveTerrainChecks.cs` runs in EnemyIntegrationChecks. It covers both palettes/art, Foundation/overlay/reservation/occupancy rules, hazard-only rebuilds, entry/periodic and moving exposure30/60/120Hz, stationary/disabled entry, contiguous/gapped/fast/corner/exact-edge/tiny-segment crossings, partial-frame timing, teleport/spawn arrival, live edits/config changes, large deltas, shields/spillover/hull, environmental Credits/noXP, death/free/arrival/unload, lethal movement and spawn-time wave completion. A500-occupant burst verifies2000 canonical hits without per-cell nodes. See TESTS/README.md for actual compilation/headless results.

This pass adds only Foundation/Lava authoring, basic SVG art and environmental exposure. It does not change routing weights or make enemies avoid Lava; no Electrical/Cryogenic/Regeneration tiles, lingering DoT, shaders, research, saving, tower HP or unrelated mechanics were implemented.
