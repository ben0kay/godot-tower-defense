# Shared enemies

`EnemyDefinition` is the Inspector-editable per-type resource. `Enemy` owns health, alive/dead/arrived state and removal. `EnemyRegistry` owns authoritative per-world living membership plus the shared `EnemySpatialIndex` used by targeting, instant areas and timed wavefronts; no scene-tree rescans. Ships use lightweight Node2D roots with one movement helper and Sprite2D.

Arrival counts in the debug HUD and removes the ship. CitadelDamage is applied by CitadelSession; Armor is reserved for later mitigation. Death removes registry membership immediately before QueueFree, so additional bullets cannot damage a dead enemy.

## Kill credit rewards

Every `EnemyDefinition` exposes **Rewards / Kill Reward Credits** (defaults to 0). Enemy001 sets 3 in its `.tres`. Death runs `Enemy.Finish(false)` once, and `EnemyRegistry.RecordOutcome(false, enemy)` emits the centralized `EnemyKilled` event. `ECONOMY/Wallet/EconomyManager` listens and grants Credits once, also updating the HUD. Arrivals and non-kill removals never emit the event. This is not tied to a particular turret or wave. Do not pay credits on nonlethal hits.

## Automatic death puff

A killed enemy (only `Finish(false)`) calls the shared `FEEDBACK/DeathExplosion/DeathExplosion.Spawn` using its actual `Sprite2D` world size, or `RadiusPixels * 2` if no sprite is available. All enemy types inherit `EnemyDefinition.DeathExplosionMultiplier = 1.0`; optional Inspector overrides such as 1.5 for a boss scale the automatically calculated effect without changing the enemy's size. The effect is placed under independent `World/Effects`, so `Enemy.QueueFree` does not remove the puff. Arrivals, duplicate damage and world unloads trigger none. See `FEEDBACK/DeathExplosion/README.md`.

## Optional per-enemy shield capacity (implemented)

`EnemyDefinition.MaxShield` is an Inspector-editable **capacity** (default `0`); it does **not** mean every spawn has shields. `Enemy.CurrentShield` is private-set, per-instance runtime state initialized by `Enemy.Initialize(..., spawnWithShield: false)`. A wave entry with `shield: true` starts that specific ship at full `MaxShield`; all default spawns, including F2/debug, start at zero. If a definition has zero capacity, the opt-in remains zero.

`Enemy.TakeDamage` absorbs finite positive damage into the shield first and applies overflow to hull `Health` in the same hit. The existing `DefenseBar` continues showing **hull HP only**; shield-only hits leave it hidden at full hull health. Shield rendering, regeneration, resistance and armor are separate future features. Dying/arriving/rewards work as before. No wave or spawn changes are stored on the shared authored `.tres` file.


Typed ApplyDamage(DamageInfo) now delegates base hits to COMBAT/Damage. TakeDamage(float) keeps 1:1 shield-first behavior. One internal helper commits state; only hull changes update DefenseBar, only hull death pays a kill. Capacity/opt-in spawning remain unchanged.


E2A: EnemyCatalog validates an Inspector scene list once, derives canonical IDs from each scene Definition and provides stable lookup. DefaultEnemyCatalog registers Enemy001/Enemy002; WaveCatalog.LoadScenes is now an adapter. Unknown/duplicate IDs, invalid balance and missing root/Movement fail setup.


E2B adds only optional Initialized notification after registry/movement setup, a read-only Registry accessor and RestoreShield(amount). The latter rejects invalid/nonpositive amounts/dead recipients/zero capacity and clamps finite additions at MaxShield without touching hull feedback or definitions. ShieldEmitter owns pulse behavior; ordinary Enemy001/Enemy002 have no ability node/manager. Kill/arrival/unload behavior and typed COMBAT damage remain unchanged.

Phase 4D adds constant-time registry membership/uniqueness with a HashSet mirrored alongside the existing ordered living list. Contains lets snapshotted impacts skip enemies removed by earlier kill callbacks without another list scan. Enemy lifecycle/rewards and existing list consumers are unchanged.

## Spatial queries (implemented)

`GetEnemiesInRadius(center, radius, List<Enemy>)` clears/fills caller storage with exact inclusive center-distance matches, in unspecified order and without a cap. Zero means exact center. Negative/nonfinite radius or center throws before modifying results. Nonfinite enemy positions are skipped. Living/Contains remain compatible; registration is unique and death/arrival/free removes both point and swept memberships immediately. World exit clears membership, history and buffer pools.

EnemyRegistry exposes Inspector `SpatialCellSize` (128px default, set before first registration/query). The hash uses floored world coordinates and recycled buckets; point memberships change only across cell boundaries. Enemy transform notifications cover normal/direct repositioning, and the existing movement path explicitly synchronizes position. No global movement scan or periodic rebuild is needed. Native transform reads are cached into position/order snapshots for combat callers.

Active Shockwave emitters hold revision cursors in this same service. Movement records conservatively index their retained swept envelopes; queries reject outside/behind-front cells and return each enemy once with the correct previous/current samples. Only the existing wave solver decides a hit. New spawns use their current sampled position. Histories are retired amortized as cursors advance and released at completion/unload. Exceptional envelopes larger than64 cells use conservative overflow candidates; no speed assumption or silent truncation is imposed. Registration order preserves targeting ties and mutation-sensitive damage order. See TOWERS/Offensive/Shared/README.md and TESTS/Performance/SpatialQueryBenchmarks.md for measured overhead and limitations.

## Enemy identity and waves

`EnemyDefinition.EnemyId` is the unique, stable internal catalog key (e.g. `basic_mk1`) and `DisplayName` is the player-visible label (e.g. `Standard Drone`). Numbered scene filenames (`Enemy001.tscn`, etc.) are merely development assets. The catalog validates nonempty, unique IDs and wave entries resolve scenes by those IDs. New definition instances deliberately default to a blank `EnemyId`, so register types only after assigning one. Enemy ordering in waves is independent of filesystem and catalog order.

## Interactive terrain integration

Enemy initialization reports destination arrival to the world InteractiveTileManager. Direct position changes use teleport semantics; normal movement uses SetMovementPosition to retain spatial notifications while bypassing teleport re-entry. The per-world registry now emits EnemyUnregistered after authoritative list/index removal, allowing the single terrain manager to release exposure synchronously for death, arrival, external free and manual unregister. No spatial query algorithm, level stats, target priority or damage source semantics were replaced. Environmental hits use ApplyDamage with no tower source; existing shields/hull, kill Credits and finite damage-XP budget remain authoritative.

## Movement variation permissions

EnemyDefinition has Inspector **Movement Variation** permissions AllowCorridorVariation, AllowPathOffsets and AllowPathDiversity, default true. Set these independently per authored type; no ID/size whitelist is used. Permissions are captured at spawn, and global master controls remain authoritative. Corridor selection/seed persists across repaths, offsets may be disabled independently, and diversity-disallowed ships use the single shared unweighted profile. Existing type resources retain their defaults. See ENEMIES/Movement/README.md.

## Planned inherited shared enemy scene (GE1–GE3; NOT IMPLEMENTED)

See [GENERIC_ENEMY_SCENE_PASS.md](GENERIC_ENEMY_SCENE_PASS.md). Keep per-type `.tscn` scenes for readability but derive their shared `Sprite`, `Movement` and `DefenseBar` children from one `Shared/GenericEnemy.tscn`. Type-specific SVG, Definition and optional ability children remain in inherited type scenes. Preserve the existing catalog/spawn behavior and regressions; no scene migration has happened yet.

## GenericEnemy scene (GE1–GE3)

`GenericEnemy.tscn` is the authoring parent for all five enemy scenes. It owns Enemy.cs on a Node2D root and exactly `Sprite` (Sprite2D), `Movement` (EnemyMovement.cs) and `DefenseBar` (shared feedback scene). The parent has no Definition, artwork, ability nodes or extra processing. Instantiate a registered type scene through the existing catalog, not this incomplete authoring template.

Open each type's `.tscn` normally in Godot: its inherited common nodes remain Inspector-editable. Change common transforms/settings in GenericEnemy so they propagate to all types; a deliberate child property override stops inheritance for that property (use Inspector Revert to restore it). All current types inherit the bar transform unchanged. Bar drawing/style remains owned by `FEEDBACK/DefenseBars/`. Each type overrides only root Definition and Sprite texture, and adds its actual optional Abilities children. New types should use **New Inherited Scene** from GenericEnemy, assign their Definition/SVG, then register that individual scene in the existing EnemyCatalog.

No runtime scripts, node counts, balance, scene paths, wave IDs or spawn lifecycle changed. See `TESTS/Enemies/GenericEnemy.md` for propagation, gameplay and 100/500-instance measurements.
