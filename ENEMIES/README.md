# ENEMIES — Enemy Ships, Navigation, Spawning, Waves and Targeting Integration

This top-level folder owns **enemy gameplay mechanics** in the Godot 4 C#/.NET space tower-defense project. Enemy001 is the first working **basic test ship**, intentionally named `Enemy001` internally rather than `ScoutShip`. Its scene uses shared `Enemy.cs` and `EnemyMovement.cs` rather than a unique gameplay script. This is the baseline future types should reuse.

**Implemented baseline (October 2026):** Enemy001, shared hull HP and opt-in shield capacity, `EnemyRegistry` with killed/arrived events, kill rewards, death-puff feedback, Citadel arrival damage, budgeted AStarGrid2D navigation/repathing, portal/edge spawning, F2 debug spawns, immutable authored waves, and tower targeting/projectiles. `ENEMIES/Types/Enemy001/Enemy001.tres` has **60 MaxHealth, 100 MaxShield capacity, 100 px/s MoveSpeed and 3 Credits on kill**; its currently authored waves and F2 still spawn unshielded by default. See `Shared/README.md`, `Types/Enemy001/README.md` and `LEVELS/Waves/README.md`.

**E2A/E2B implemented:** DefaultEnemyCatalog registers Enemy001, weaker/faster Enemy002 and ShieldGenerator by canonical EnemyDefinition.EnemyId. WaveCatalog supplies level wave sets; scene lookup comes from the validated enemy catalog. Enemy002 uses 30 hull/160px/s/8px radius/2-Credit reward and 40 optional shield capacity. ShieldGenerator uses a real optional ShieldEmitter scene child; ordinary types have no ability nodes. E2A was compiled/headless-verified independently before E2B.

**Current limitations:** no death-spawning (E3A), enemy weapons/electrical tower attacks or tower durability (E3B), armor mitigation, linked shields, shield UI, timed effects or universal ability manager. TOWERS Phase 4C typed combat and continuous Laser are already implemented and preserved. F2 still spawns Enemy001 unshielded; existing Level001 authored wave contents are unchanged. New registered IDs can be authored in waves or explicitly assigned to Debug/EnemyScene for playtesting.

## Corridor-aware enemy path variation (PLANNED — Codex navigation pass N4)

See **[enemypathing.md](enemypathing.md)** for the standalone Phase N4 implementation plan: Inspector master toggle, percentage of individual enemies assigned the new corridor-aware refinement, safe randomized routes within walkable boundaries, stable repathing, 0%/100% behavior and performance/regression requirements. This is **not implemented yet** and is independent of the completed E2A/E2B enemy catalog/ShieldEmitter and existing shared path diversity.

## Enemy architecture — multi-type roster and modular abilities (E2A/E2B implemented; E3 later)

**Design goal:** Support many enemy types—fast scouts, heavy tanks, generators, carriers, electrical ships, attackers, healers, stealth units, bosses and hybrids—without growing `Enemy.cs` into a giant switch on IDs or writing a full replacement AI/spawn/health script for each one. An enemy may have **zero, one or several independent optional abilities**. Keep ordinary enemies lightweight: speed, HP, shield *capacity*, Citadel damage, reward and navigation radius are already `EnemyDefinition` data.

**E2A/E2B are implemented; E3A/E3B remain separate future passes.** This section records the accepted contracts and future roadmap. Preserve all previously implemented wave, shield, navigation, tower, audio and UI features. Prefer compact C#, `[GlobalClass]` Inspector-editable Resources, exported scene references when appropriate, method separators and feature READMEs as described in `NOTES/CodePreferences.md`.

### Actual existing code versus future components

| Responsibility | Source | Status |
| --- | --- | --- |
| Shared per-ship state: `Health`, `CurrentShield`, death/arrival, `Removed` | `Shared/Enemy.cs` | **EXISTS**, retains lightweight lifecycle |
| Per-type ID, health, shield capacity, speed, Citadel damage, armor field, rewards | `Shared/EnemyDefinition.cs` | **EXISTS**; armor remains inactive |
| Navigation and route budgets, movement offsets, look-ahead clearance | `Movement/`, `WORLD/NAVIGATION/` | **EXISTS** |
| Living ship registry, killed/arrived events | `Shared/EnemyRegistry.cs` | **EXISTS** |
| Portal/edge spawning, F2, opt-in `SpawnWave(..., spawnWithShield)` | `Spawning/SpawnManager.cs` | **EXISTS** |
| Immutable weighted wave authoring, manual owned completion | `LEVELS/Waves/` | **EXISTS**; scene lookup delegated to EnemyCatalog |
| Enemy scene/catalog registration by stable `EnemyId` | `Shared/EnemyCatalog.cs` (suggested) | **EXISTS E2A** |
| Optional abilities + read-only authoring/instance-local state | `Abilities/` (suggested) | **EXISTS E2B** |
| Shield-emitting ally support | `Abilities/ShieldEmitter/` | **IMPLEMENTED reference ability in E2B** |
| Spawn several valid smaller enemies when killed | `Abilities/DeathSpawn/` | **LATER E3A** |
| Tower durability and tower-targeting enemy weapons | `TOWERS/` + `Abilities/Weapons/` | **LATER E3B** |
| Repeating electrical damage pulse against towers | `Abilities/ElectricalPulse/` | **LATER E3B**, after tower HP |
| Typed damage/shield/hull math | `COMBAT/Damage/` | **TOWERS Phase 4C-1 implemented**; reuse, don't duplicate |
| Timed EMP/slow/repair/stun/stealth/buff effects | Future common effects ownership | **LATER**; no universal effect manager yet |

**Current foundation plus future E3 folders; only implemented consumers have actual folders:**

```text
ENEMIES/
├── EnemyMasterConfig.cs                   # EXISTS: global budgets, scheduling/safety
├── DefaultEnemyConfig.tres                 # EXISTS
├── Shared/
│   ├── Enemy.cs                           # EXISTS: lifecycle/HP/shield/registry, remains small
│   ├── EnemyDefinition.cs                 # EXISTS: per-type authored balance/identity
│   ├── EnemyRegistry.cs                   # EXISTS
│   ├── EnemyCatalog.cs                    # NEW E2A: scene + definition registry
│   └── README.md
├── Movement/                               # EXISTS; keep common routes/pathfinding
├── Spawning/                               # EXISTS; extend safe spawn APIs, no per-type switches
├── Abilities/                              # NEW E2B, only for implemented abilities
│   ├── README.md
│   ├── ShieldEmitter/
│   │   ├── ShieldEmitter.cs                # Optional child/component
│   │   ├── ShieldEmitterConfig.cs          # Typed Inspector Resource
│   │   └── README.md
│   ├── DeathSpawn/                         # NEW LATER E3A
│   ├── Weapons/                            # NEW LATER E3B
│   └── ElectricalPulse/                    # NEW LATER E3B
└── Types/
    ├── Enemy001/                           # EXISTS: ordinary reference type, unchanged
    ├── Enemy002/                           # Example future fast/slow type
    ├── ShieldGeneratorShip/                # Example, can use normal Enemy + ability child
    ├── CarrierShip/                        # Example, can use normal Enemy + ability child
    └── ElectricalShip/                     # Example, can use normal Enemy + ability child

COMBAT/Damage/                               # EXISTS: shared typed combat; shared by combatants
LEVELS/Waves/                                # EXISTS: wave scheduling/ownership, no ability logic
TESTS/                                       # Add regression tests per implemented phase
```

**Example of composition, not a required fixed scene tree:**

```text
ShieldElectricalShip (Enemy.cs; EnemyDefinition.tres)
├── Sprite
├── Movement (EnemyMovement.cs)
├── DefenseBar
└── Abilities (optional organizer Node)
    ├── ShieldEmitter (ShieldEmitter.cs + typed config Resource)
    └── ElectricalPulse (ElectricalPulse.cs + typed config Resource)
```

An ordinary scout should not get empty ability controllers or per-frame AI nodes. A shield/electric ship should not need a new `Enemy007.cs` copied from `Enemy.cs`. Optional child components that explicitly reference the host Enemy are preferable to a deep inheritance tree or a mandatory always-running ability manager. A reusable, tiny `EnemyAbility` base/interface is acceptable **only when it removes repeated host wiring in two real abilities**; do not invent 15 no-op components first. Do not use runtime class discovery/reflection to decide ability order.

### Phase E2A — Generic enemy catalog and safe multiple-type spawning (implemented/verified first)

**Scope:** Replace the single hardcoded Enemy001 scene list with a validated, Inspector-editable `EnemyCatalog` of registered enemy types, while retaining immutable `WaveDefinition`/`WaveEnemyEntry` and their stable `EnemyId` strings.

1. `EnemyDefinition` already has `EnemyId`, `DisplayName`, balance and rewards; **do not duplicate these fields** in a second enemy type definition without a real reason. A small catalog entry can reference a `PackedScene` whose root is `Enemy` and `Enemy.Definition` (or an explicit definition reference when validation demands it). Register by the existing stable ID, not by filenames/display names.
2. Author one default `EnemyCatalog.tres` (location such as `ENEMIES/DefaultEnemyCatalog.tres`); validate missing/null scenes, blank/duplicate IDs, mismatched scene versus Resource ID, missing `EnemyMovement`, and invalid balance/clearance for new types. Validation happens at setup/registration, **not for every spawn or frame**. Keep unknown wave IDs as explicit setup errors; no fallback to Enemy001.
3. `WaveCatalog` remains the **wave set** resolver (which levels/waves to run). The enemy catalog supplies scene lookup, so `WaveCatalog.LoadScenes()` can become an adapter to the new registry without moving actual wave timing, composition, RNG or ownership logic into `ENEMIES/`. Respect `WaveEnemyEntry.Portal`, `SpawnWithShield`, weights, retry backoff and the existing explicit `SpawnManager` placement rules.
4. Validate the existing `Enemy001.tscn` unchanged. Prove a second plain test enemy with a different Inspector `EnemyDefinition` and artwork can be registered, spawned in a mixed weighted batch, move along valid routes, be targeted/killed, pay its own reward, damage the Citadel once on arrival and remain distinct from Enemy001 without a `switch (enemyId)`.
5. **Larger ships:** `RadiusPixels` is not just presentation scale; clearance/swept movement/path corridors must accommodate it. A larger sprite does not automatically enlarge safe path radius. If current authored portals/corridors cannot accommodate a selected large enemy, reject/hold the spawn with a clear reason rather than squeezing through a blocker or allowing invalid movement. Don't rewrite shared navigation purely for the catalog.
6. Leave `F2` as the existing Enemy001 debug path unless an explicit developer option is authored; normal debug spawns remain unshielded. Preserve credits, Citadel health, death puff, shield absorption/overflow, deterministic waves, the 500-enemy navigation fixtures, and all tower tests.

**E2A acceptance:** new non-special enemy types can be added by **authoring scene/art/EnemyDefinition and registering them**, then referring to their stable `EnemyId` in waves; no new manual scene dictionary entry or special-case spawn code for each type. Add catalog ID/scene checks, two-type wave/quota/portal tests, health/shield isolation, different sizes/speeds/rewards and unchanged single Enemy001 gameplay.

### Phase E2B — Optional ability plumbing and first ShieldEmitter (implemented after E2A verification)

**Scope:** Introduce only the lifecycle hooks actually necessary for the **first reusable ability**, then implement one real ShieldEmitter. A ShieldEmitter is a **source ability**; the shield points it provides remain state on the **recipient `Enemy`**. Do not transform `Enemy.cs` into a list of shield/EMP/carrier-specific booleans and timers.

1. An optional component can discover or be assigned its host `Enemy`, cache needed world services once and use a typed `Resource` with Inspector exports for **radius, shield points per pulse, pulse interval, target eligibility and whether the emitter can affect itself**. Each placed/spawned enemy owns its own *cooldowns/state*; shared `.tres` data is not modified. Null/disabled components do nothing.
2. Use **the existing `EnemyRegistry.Living`** as the authoritative set of candidate friendly enemies; perform bounded, staggered queries **on the configured pulse interval**, not every physics tick and not through `GetTree().GetNodesInGroup()` rescans. Keep this first aura simple; add a spatial lookup only if 500+ actual ability instances prove profiling requires it.
3. The recipient keeps the current `MaxShield` versus `CurrentShield` distinction. Emitter pulses may **replenish/raise** existing shield points but clamp each ally at its authored `MaxShield`; a type with `MaxShield = 0` remains ineligible for now. This preserves the semantics of `WaveBuilder.Enemy(..., shield: true)`: the wave controls **starting** shields, while an active generator may restore shields later. Giving capacity to previously zero-capacity units or granting a separate temporary shield pool is **future behavior**, not a hidden side effect.
4. On the generator's death/arrival/level unload, stop pulses and clean subscriptions; **do not retroactively erase existing allies' shield points** by default. This avoids secretly coupling their HP to a deleted source. If a future aura is meant to provide a temporary shield pool that vanishes when the source leaves, design that as a **separate timed/linked effect**.
5. Preserve shield-first damage, kill/arrival results, actual DefenseBar hull-only rendering and 4C-1 typed damage response once it lands. The shield emitter should restore shields through an explicit safe `Enemy` API (for example, `RestoreShield(amount)`), not by mutating `EnemyDefinition.MaxShield` or bypassing health invariants.
6. Don't build a universal aura/status/effect stack to implement just one shield pulse. Define clean optional host-lifecycle callbacks only when necessary; guard against use after the enemy is removed/queued for deletion and against duplicate pulses after a scene restart.

**E2B acceptance:** ordinary Enemy001 still runs unchanged with no ability node. A ShieldGenerator enemy with the component periodically replenishes only eligible allies within radius, obeys caps and intervals, can be combined with other optional children later, and stops affecting the world when removed. Add two-emitter/recipient-isolation, range, cap, no-ability-zero-cost and cleanup tests.

### Phase E3A — DeathSpawn ability and children owned by their parent wave (LATER)

**Scope:** A killed carrier can release several smaller enemies at its **death position**. This is not ordinary portal spawning and must not use `WaveManager` timing for each child.

1. `DeathSpawnConfig` defines registered **child EnemyId**, finite **child count**, spawn/scatter radius, optional inheritance of starting shields and safe bounds/depth limits. Resolve child scenes through `EnemyCatalog`, not a hardcoded new script or scene path per carrier.
2. Expose a **safe at-world-position spawn path** through `SpawnManager` (or a small subordinate spawner), reusing enemy-count caps, `Enemy.Initialize`, clearance, route connectivity, level ownership, bounded attempts and registry registration. Find positions within a validated region near the dead parent's location; never teleport children into blocked terrain, permanently trap them, silently overrun `MaxActiveEnemies`, or fall back to an unrelated Portal without an explicit rule.
3. Trigger **only once on a true kill**, not arrival, scene unload, duplicate `TakeDamage` or repeated `QueueFree`. Preserve the mother's one-time reward/death feedback and each child's normal individual state/reward unless a future rule explicitly overrides it. Configurable positive child counts must be finite and capped; avoid uncontrolled recursive children-of-children chains or a huge synchronous creation spike.
4. **Critical wave ownership contract:** If a wave-owned mother dies, successful spawned children become **owned by the same active wave before the parent is removed from that wave's `_owned` set**. Register children and their `Removed` handlers synchronously before `WaveManager.OnEnemyRemoved(parent)` can declare the wave complete. Children should contribute to `AliveCount`, but *not* mutate `PlannedRemaining` (which represents already-authored scheduled spawns). An F2/unowned parent spawns unowned children; they must not block unrelated wave completion. On wave cancellation/unload, subscriptions are safely released. This requires a deliberate, tested lifecycle hook or spawn notification—**not** a late deferred callback after wave completion.
5. Define what happens on a partially successful spawn (some valid positions but cap or clearance rejects others): record only actually created children; skip remaining safely or adopt an explicitly documented bounded retry rule. Do not leave half-initialized nodes. A killed mother must always be removed even if no children can spawn.

**E3A acceptance:** death produces registered, individually navigable children near the correct position; no duplicate generation; no path/limit violations; no premature wave completion; ordinary kill rewards and arrival/death events remain once-only. Add test cases for kill versus arrival, F2 versus wave ownership, empty spots, blocked terrain, spawn cap, cancellation, nested-spawn safeguards and simultaneous deaths.

### Phase E3B — Enemies attack player towers (LATER; requires tower durability)

**Prerequisites:** shared typed damage from `TOWERS/README.md` **Phase 4C-1**, plus a separately approved **optional tower health/durability** feature. `TowerBase` currently is not a destructible combatant; do not claim enemies can damage towers until that is implemented. A future `COMBAT/Damage/` should calculate defensive reductions for both kinds of target rather than duplicating damage-type math inside enemy abilities.

**First prerequisite: damageable towers.** Design optional `TowerHealth` (or a damageable capability) for placed towers, with per-instance max/current HP and optional shield, default behavior chosen intentionally. Tower destruction must deselect/close or update the Inspector, remove that instance from generic `TowerPlacementController` ownership, release its `WorldGrid` cell/footprint, trigger existing navigation revision/repathing, stop any weapon/projectile subscriptions, and pay **no construction refund unless a real refund rule exists**. Never damage the passive test tower accidentally just because it derives from `TowerBase`. This tower-health feature should be implemented and verified **before** E3B attack code.

**Then implement optional enemy attack components:**

| Example ability | Behavior | Authoring fields |
| --- | --- | --- |
| `RangedWeapon` | Shoots at a valid damageable player tower (may keep moving or stop if intentionally configured) | Range, cadence, damage, `DamageType`, target preference, delivery/visual scene |
| `ElectricalPulse` | Emits periodic **radial Energy damage** to eligible nearby towers; a distinct delivery style from bullets | Radius, pulse interval, base damage per pulse, target policy, VFX style |
| Future `EMPDisruptor` | Temporarily disables nearby towers **without necessarily dealing HP damage** | Radius, effect duration, stacking/refresh policy—**not implemented in E3B** |

- Reuse a focused tower-target lookup over the placement controller's placed-tower collection or a small maintained damageable-tower registry, **not** a scene-tree scan from every enemy on every physics tick. Validate range/alive/in-tree status; stagger attack searches/pulses; cache references; respect future target-eligibility rules.
- A ship may **continue following its Citadel route while shooting** in version one; do not turn all enemies into stop-to-fire combatants or copy BattleTower's weapon loop into `Enemy.cs`. Optional component C# owns attack timing, target acquisition and delivery; weapon config owns damage type; the shared `DamageResolver` owns shield/hull multiplication. Electrical damage can start as **Energy** type from 4C-1 (an electric pulse is a delivery/ability, not necessarily a new damage type).
- A hostile attack is not the same as reaching the Citadel: it must **not** call `Enemy.Arrive()` or award a tower kill payout. Tower destruction should not increment `EnemyRegistry.KilledCount` or change the owning wave's `PlannedRemaining`. Tower HP loss must not double-charge credits, alter already-purchased upgrades or cause a stale selection.
- Ability VFX (electric arcs/ground pulse/projectiles), audio and attack math remain independent. Visuals should be restylable without changing damage, compatible with the Laser Tower beam separation in 4C-2.
- A shield-emitting electric ship may carry **both** `ShieldEmitter` and `ElectricalPulse` as independent components; disabling one must not stop the other or change base movement/navigation.

**E3B acceptance:** a damageable Battle Tower can be targeted/damaged/destroyed by an enemy's optional ranged weapon or area electrical pulse; ordinary enemies still just travel to the Citadel. Destroyed-tower occupancy/navigation/selection and upgrades remain consistent, overlapping pulses never double-apply one activation to the same target, and distant/passive/indestructible structures are ignored per rules. Tests cover tower removal while aimed at, wave-end cleanup, high enemy count, damage-type multipliers, visual independence and construction-payment isolation.

### Later optional enemy mechanics (not a single giant next pass)

- **Repair/support ship:** a timed heal source raising ally hull HP to `MaxHealth` through a validated, event-driven API; distinct from an HP/shield modifier.
- **Commander/buff ship:** optional temporary movement, defense or attack buffs; requires clear recipients, source identities, lifetime and stacking; do not reuse `EnemyDefinition` as mutable per-instance buff state.
- **Stealth/detection ship:** change which enemies `TowerTargeting` considers eligible, not the enemy's identity or nav rules; add detection-capable towers only when implemented.
- **Boss/hybrid:** one larger scene with multiple real ability components; no separate boss base class or 30 feature flags unless a genuinely new lifecycle requires it.
- **Speed variants:** per-type speed/turn stats already support fast and slow ships; large ships may require radius-aware layout and validated spawn clearance.
- **Shield presentation:** separate optional bar/VFX from hull HealthBar and shared damage math; no polling every frame just to update visuals.
- **Durations, stacking and status:** build a reusable `COMBAT/Effects/` owner **when** a first actual EMP/slow/buff requires it; not as an empty future manager now.

### General rules and Codex checkpoints for enemy work

- **Data-driven types, composable behaviors:** `EnemyDefinition` = authored type stats/capacities; `EnemyCatalog` = scene lookup; `Enemy` = instance lifecycle; `EnemyMovement` = route execution; optional components = abilities; `LEVELS/Waves/` = authored scheduling and ownership. No `if (EnemyId == "carrier")`/per-type branches in `Enemy.cs`, `SpawnManager` or `WaveManager`.
- **Typed combat belongs outside both teams:** integrate with `COMBAT/Damage/` after tower Phase 4C-1, with weapon-specific source `DamageType`. Distinguish **delivery** (beam/bullet/radial pulse), **damage type** (Energy/Kinetic/Explosive), **effects** (shield refill/EMP/slow), and **enemy type** (a catalog ID). An enemy can combine several without becoming a special hardcoded subclass.
- **Performance and lifecycle:** no unconditional per-frame ability managers on ordinary ships; use small interval timers, staggered/cached target queries, registry sets and the existing path budgets. Cache scene/service references in `_Ready`, avoid new nodes on every effect tick, cap child spawns and guarantee cleanup on death, arrival, reconfigure and world unload. 30 **types** are inexpensive; hundreds of **active ability instances** need profiling.
- **Tests:** after **each** phase run the existing `TESTS/EnemyIntegrationChecks.tscn`, `WaveChecks`, `EnemyShieldChecks`, kill rewards, Citadel health, targeting, placement, navigational repath/500-ship, paid tower/upgrade, UI/menu and audio regressions. Add focused tests only for implemented features. Keep designer `.tres`/level art untouched; use in-memory fixtures and restore world/waves. Godot .NET compile + headless checks are required; don't claim passing tests unless actually run.
- **Development sequence:** finish or coordinate with the approved `TOWERS/README.md` 4C damage/Laser work; then **E2A (enemy catalog) → E2B (small ability system + ShieldEmitter)**. Implement E3A death-spawn after ownership is deliberately designed; E3B weapons/electrical attacks **only after tower durability exists**. No requirement to implement every illustrative enemy type during the foundation pass. Update `ENEMIES/Shared/`, `ENEMIES/Abilities/`, `ENEMIES/Types/`, `ENEMIES/Spawning/` and wave/test READMEs **as each feature actually lands**.

**Finished enemy foundation criteria:** register a different ordinary enemy or attach an implemented ShieldEmitter component to a ship **without editing the generic spawn/wave/health/movement source files for that particular type**. Advanced abilities should extend existing lifecycle/target/damage contracts rather than forcing a rewrite later.


## Folder and file structure (current, implemented baseline)

```text
ENEMIES/
├── EnemyMasterConfig.cs                # Global enemy-system tuning (not per-type balance)
├── DefaultEnemyConfig.tres
├── README.md
│
├── Shared/
│   ├── Enemy.cs                        # Reusable enemy instance: health, damage, death, state
│   ├── EnemyDefinition.cs              # [GlobalClass] Resource: per-type identity/stats/scene data
│   ├── EnemyRegistry.cs                # Active living enemies for tower target queries
│   └── README.md
│
├── Types/
│   ├── README.md
│   └── Enemy001/
│       ├── Enemy001.tscn               # First basic test ship scene
│       ├── Enemy001.tres               # Editable EnemyDefinition Resource
│       ├── Artwork/
│       │   └── enemy001.svg            # Simple top-down test artwork
│       └── README.md
│
├── Movement/
│   ├── EnemyMovement.cs                # Waypoint following / facing / speed
│   └── README.md
│
├── Spawning/
│   ├── SpawnManager.cs                 # Spawn *mechanics* (Portal or map-edge sources)
│   ├── README.md
│   └── MapEdges/
│       └── README.md
```

The folder names and filenames **`Enemy001/`, `Enemy001.tscn`, `Enemy001.tres` and `enemy001.svg` are intentional**. Additional variants will follow `Enemy002`, `Enemy003`, etc.; they should usually share `Enemy.cs` and `EnemyMovement.cs` rather than receiving a duplicate C# script for each type.

### Identity: filename vs editable game metadata

- **Internal file/folder identity:** `Enemy001` — stable even if the ship is renamed for players later.
- **Enemy key / spawn lookup ID:** a field in `EnemyDefinition`, provisionally `enemy001`, editable for future registration/spawn configuration. Keep IDs stable once referenced by saves or wave definitions.
- **Display name:** a separate editable string in `Enemy001.tres`, provisionally `Enemy 001` or `Basic Enemy`. This is not a requirement to rename the scene or files.
- **Type:** ordinary/basic hostile ship **for testing**. Do **not** bake a "Scout" archetype into filenames, source classes or permanent mechanics.

## First Enemy001 settings (starting values, not final balance)

`[GlobalClass] EnemyDefinition : Resource` is implemented and assigned through `Enemy001.tres`. The table below records the initial authoring defaults and current functionality:

| Setting | Initial test value | Notes |
| --- | --- | --- |
| DisplayName | Enemy 001 | Temporary; editable |
| Key / EnemyId | enemy001 | Separate from display name |
| MaxHealth | 60 | Implemented; Inspector editable |
| MaxShield | 100 for Enemy001 (shared default 0) | Capacity only; zero at spawn unless `shield: true` |
| MoveSpeed | 100 world pixels/second | Implemented route-following movement |
| CitadelDamage | 1 | **Implemented** through CitadelSession on arrival |
| Armor | 0 | **Reserved**, not yet applied by damage code |
| Visuals | 1 small SVG Sprite2D | Smaller than a 64px grid cell |

These are placeholders. Favor **Resource-driven data** over hardcoded stats inside `Enemy.cs`. Per-type settings belong in `Enemy001.tres`; global limits/debug/timing settings belong in `EnemyMasterConfig.cs` and `DefaultEnemyConfig.tres`. Add only meaningful global exports, not duplicate per-enemy properties.

## Responsibilities and integration boundaries

| Location | What it does | What it must NOT own |
| --- | --- | --- |
| `Shared/Enemy.cs` | Alive/dead state; current/max health; `TakeDamage`; death event/removal; exposes position/definition for targeting | Spawn-wave scheduling, level tile rules, portal art |
| `Shared/EnemyRegistry.cs` | Registers/unregisters living enemies for nearby tower target selection | Per-frame global rescan of the scene tree |
| `Movement/EnemyMovement.cs` | Follows a valid waypoint route, turns smoothly, stops safely if route unavailable/stale | Its own independent tile occupancy map |
| `Spawning/SpawnManager.cs` | Uses level settings to select valid Portal/map-edge spawn positions and instantiate enemies | Wave composition/timing; Portal scene internals |
| `LEVELS/Waves/` | Shared wave scheduling requests spawns from per-level authored definitions | Choosing physical map entry positions |
| `WORLD/PORTALS/` | Reusable physical Portal art and `SpawnExit` Marker2D | Enemy type selection or spawn schedule |
| `WORLD/OBJECTIVES/Citadel/` | Placed goal, solid footprint and reachable `ApproachPoints` | Enemy navigation algorithms |
| `TOWERS/` | Battle Tower targeting/projectile/damage logic | Enemy identity and path requests |

**Level spawn configuration already exists:** `LEVELS/LevelConfig.cs` exports `SpawnMode` = `Portals` / `MapEdges` / `Both`, `AllowedEdges`, root-relative `ActivePortals` NodePaths, and `CitadelPath`. Respect these settings rather than hardcoding Level001 or a single source type. Portal enemies emerge at a valid `SpawnExit`; edge-only levels do not require a Portal.

## Navigation is a prerequisite: follow the existing performance plan

Read `NOTES/NAVIGATION_PERFORMANCE_PLAN.md` and `WORLD/README.md`. The existing `WORLD/GRID/WorldGrid.cs` already caches terrain/tower blocking through `BlocksEnemies(cell)` and exposes a `NavigationRevision` updated after terrain/occupancy changes. **Reuse that authoritative grid**; do not create a competing interpretation of metal walls, open floors or tower blockers.

Suggested navigation implementation lives under **`WORLD/NAVIGATION/`** rather than per enemy:

1. Build a lightweight `NavigationGrid.cs` using the cached WorldGrid blocked cells; use Godot `AStarGrid2D` initially, preventing diagonal corner cuts.
2. Use a central `PathRequestManager.cs` with configurable **max path calculations per frame**. Enemies request paths on spawn or when invalidated, not every frame.
3. Find a reachable Citadel `ApproachPoint`, not the center of its blocked 3×3 footprint.
4. Follow path waypoints smoothly; turn the SVG toward travel direction.
5. If a path fails or becomes unsafe after a tower is placed, **stop and wait/retry safely**. Never move directly through walls as fallback.
6. Before a player builds on a currently enemy-passable tile, eventually reject placements that seal all required routes from active Portal exits and/or permitted map edges to the Citadel.

For this first pass favor correctness and simplicity. No RigidBody2D per ship, tile-by-tile scene nodes, per-enemy full A* each frame, always-on all-vs-all checks, or complex flow-field implementation.

## Tower targeting and basic damage (follow-up integration)

The active `TOWERS/Offensive/BattleTower/` baseline has editable stats: 256px range, 1 shot/second and 12 damage. These now work against `Enemy001`.

- Put **reusable targeting** in `TOWERS/Shared/Targeting/`, and **reusable basic projectiles** in `TOWERS/Shared/Projectiles/`; do not embed projectile behavior in Enemy001.
- Default to **First** (shortest remaining valid route) among living enemies within range; **Closest** remains selectable. Use `EnemyRegistry` and cache the chosen target; compare again on staggered intervals so overtaking can update First; validate cached range/alive state between queries.
- First projectile can fire from the **center** of the current one-piece Battle Tower sprite (no rotating multi-part tower/art overhaul yet).
- At the current pre-4C baseline, projectile hits call `Enemy.TakeDamage(amount)` and shields absorb damage before hull health. The planned 4C-1 typed damage pass will add a shared resolver. Zero hull health marks the enemy dead, deregisters it and removes it cleanly; already-dead enemies cannot be damaged again.
- More targeting priorities and armor mitigation remain future work. **Credits/kill rewards, tower upgrades and death explosions already exist**. Typed damage and Laser Tower are planned under `TOWERS/README.md` Phase 4C-1/4C-2.

## Historical initial Codex implementation passes (completed; reference only)

### Pass 1 — Basic Enemy001 + correct movement

Implement `EnemyDefinition`, `Enemy.cs`, `Enemy001.tscn`/`.tres`, small `enemy001.svg`, EnemyRegistry, navigation and basic spawning. **No complete WaveManager needed yet.** Provide a simple Inspector-editable or debug-only way to spawn one or a few Enemy001 ships using Level001's Portal and later test permitted map edges. A ship should move around solid painted obstacles/occupied tower cells, arrive at a *reachable* Citadel approach point and be removed/marked arrived. No Citadel health implementation required yet.

### Pass 2 — Battle Tower automatic targeting and firing

Implement reusable target selection, basic bullet/projectile and damage flow. Spawn test enemies; placing Battle Towers near their route should cause automatic firing and eventual kills. The current HUD, world click-to-place/select, and Inspector must still work.

### Pass 3 — Correctness and scale validation

Test multiple ships, multiple buildable placements, navigation revision/repath behavior and valid spawn-source-to-goal connectivity. Add pathfinding/targeting budgeting according to the performance plan; confirm map-edge or mixed spawn modes use level settings. **Do not start full waves/economy/campaign logic yet.** Scale toward large swarms after measuring performance, not by prematurely overengineering.

## Historical baseline acceptance tests (already covered by regression suite)

- `Enemy001` exists on disk by that name, uses the shared C# enemy code, and its display name/key/stats can be edited without renaming scene/art files.
- In F5 play, the ship can emerge at Level001's Portal exit, navigate around **solid** painted tiles and reach a valid Citadel approach.
- Ships never cross blocked terrain, the Citadel's blocked footprint or a tower; failed routes stop safely.
- After Pass 2, a placed Battle Tower detects Enemy001 in range, shoots at its baseline configured rate and damages/kills it; out-of-range enemies are not targeted.
- Buildable passable cells occupied by towers invalidate affected paths; placement does not silently create an unwinnable sealed map once route checks are connected.
- Empty/open space stays traversable but unbuildable; current terrain painting, camera controls, Ctrl tower tray, Inspector and Portal/Citadel placements remain intact.
- Godot C# compiles and the scene can be playtested without missing resources or node-path errors. Document any deliberate unimplemented features.

## Project conventions

**C#/.NET only, not GDScript.** Follow `NOTES/CodePreferences.md`: short header comments explaining scripts, `// =========================================================` above methods, reasonable `#region` blocks, Inspector `[Export]` / `[ExportGroup]`, compact reusable code, and a README in each feature folder. Keep per-enemy nodes minimal; one central registry and a bounded navigation-request manager support future hundreds of ships. Refer to the user's existing `isometric-new-2d` project when appropriate.

**Current status:** Enemy001, finite waves, kill rewards, Citadel HP/damage, shield-capacity opt-in, the economy wallet, 4A/4B generic tower catalog/UI and tested baseline navigation are implemented. `TESTS/EnemyIntegrationChecks.tscn` exercises the integrated systems. Larger-ship route edge cases, ship separation, flow fields, reusable enemy abilities, enemy attacks and advanced target priorities remain future work. The E2A/E2B/E3A/E3B roadmap above supersedes the historical initial implementation plan.

## Shared weighted route diversity

EnemyMasterConfig / ENEMIES/DefaultEnemyConfig.tres now exposes EnablePathDiversity (true), PathDiversityStrength (0.6), RouteProfileCount (4, capped at 8), PreferencePatchCells (8), and RoutePreferenceSeed. Ships receive a stable round-robin profile on spawn. The navigation service caches a small pool of AStarGrid2D instances and smooth seeded weight arrays, shared by all ships; it never allocates a navigation grid per ship. WorldGrid remains the sole authority for solids and NavigationRevision.

Weights are at least 1, keeping the octile heuristic admissible. Jumping is explicitly disabled because it bypasses weighted cells. Approach selection compares weighted costs; First tower targeting still compares the physical remaining route distance, not preference/congestion cost. Broad cost patches encourage alternate corridors without per-tile random jitter. Movement speed, smooth facing and swept obstacle checks remain unchanged. A single navigable corridor still produces shared routes; safe waypoint offsets are now implemented; local separation remains future work.

CongestionInfluence defaults to 0 (off). When enabled, living-ship density is sampled every CongestionSnapshotSeconds (0.5), with capped costs in occupied/neighbor cells. Only sparse affected weights are refreshed per profile; old snapshots are restored before replacement. Profiles never borrow another profile's temporary state. New/repath requests consider snapshots; existing routes are not recalculated merely because crowds move. Earlier goal candidates are rescored against the current snapshot before comparison, without additional A*. Map changes invalidate solids and clear stale congestion. All actual A* searches remain within the configured frame budget.

SeparationStrength and SeparationRadius are reserved Inspector settings with no movement effect in this pass. A later separation implementation must use spatial buckets and swept obstacle clearance. Change configuration in the saved Resource before F5; runtime weight edits affect new/pending routes without forcibly repathing existing ships. Use terrain/occupancy revision changes to test repathing.

Tests include deterministic alternate corridors, swept clearance, ordinary paths with diversity disabled/zero strength, congestion/no weight leakage, 500 queued ships, budget limits and tower revision repathing. The headless timing output is a diagnostic, not a rendered 60 FPS guarantee; a synchronous 500-node spawn batch can still exceed a 16.7ms frame and future waves should spread spawn creation across frames.

## Incremental navigation and selective repathing

Tower placement/removal now emits a cell change: NavigationGrid updates that one cell across the shared weighted profiles. Full grid updates remain for startup, terrain reloads and profile changes. Route dependencies index potentially affected ships; exact remaining-route validations are bounded separately. Unaffected ships keep moving. Affected ships request prioritized A* and keep moving along their safe old prefix; per-tick immediate clearance prevents crossing newly blocked terrain while checks/repaths wait.

EnemyMasterConfig adds soft path-work/validation millisecond budgets, a validation count limit, fair oldest-request service, optional staggered three-second audits, and EnablePathOffsets/PathOffsetMaxPixels (default true/8px). Audits do not calculate A* for valid routes. One stable lateral bias per ship is applied to interior waypoints with corner bisectors and clearance-tested tapering; no extra A* or per-frame jitter. Local separation remains inactive.

Placement connectivity is preserved using reusable flat flood-fill storage. Level Debug shows separate placement, grid, native A*, total path-work and validation timings. TESTS/RepathMovementChecks.cs covers 500 affected ships, unaffected movement, budgets, fairness, offsets, audits and safety. TESTS/PlacementPerformanceChecks.tscn measures identical 100/500-ship fixtures; TESTS/NavigationPerformance.md records before/after results. Time budgets are soft: a single native search/path assignment cannot be interrupted, and rendered 60 FPS still requires hardware testing.

## Moving replacement-path handoffs

Confirmed blockage now queues a replacement without stopping safe movement immediately. Ships retain old waypoints while waiting, with strict immediate swept/corner checks; they stop only when the next movement would be unsafe. Queue urgency follows the shrinking distance to the obstruction. Failed replacement searches retain any safe old prefix and use the existing retry interval.

Each multi-goal request uses one position snapshot, so its candidate costs are comparable while the ship moves. When the result arrives, EnemyMovement joins it near the ship's current progress, preferring a forward clearance-tested waypoint. A safe two-leg elbow can turn around a nearby wall without returning to the cell center. Both legs obey swept clearance and corner-safe cell transitions. If reconnection fails, a fresh request uses the same centralized budgets. Backtracking remains possible when the actual map requires it.

Safe offset refinement cannot turn the first new segment backward merely due to lateral bias. Existing speed/turning/offset settings, weighted profiles, congestion, auditing and count/time budgets are preserved. First targeting excludes routes known to need replacement until a valid handoff completes. No new pathfinding algorithm or per-frame A* is added.

## Manual waves (Phase 1)
LEVELS/Waves contains the shared immutable data, catalog and one Main/Waves scheduler; LEVELS/Level001/Level001Waves.cs contains this map's authored encounters. WaveSetId in LevelConfig chooses a finite set; the HUD Start/Next button controls progression. Wave spawns use random eligible sources, preserve F2 round-robin debugging, retain blocked pending ships and complete only after scheduled and wave-owned ships resolve. DefaultEnemyConfig exposes attempt cap/retry/seed. Kill rewards and opt-in shield-spawn flags already work; only Enemy001 is currently registered. See LEVELS/Waves/README.md.

## Optional shield capacity and wave starting state

Enemy types can define `MaxShield` in their own `EnemyDefinition.tres` (default 0; Enemy001 explicitly has 100). Runtime `CurrentShield` is **per ship** and starts at 0 unless an authored wave entry requests `Enemy("enemy001", shield: true)`. Shield hits absorb damage first, with spillover damaging hull HP; the current enemy health bar continues to represent hull health. F2 debug spawns and all currently authored Level001 waves remain unshielded. See `Shared/README.md` and `LEVELS/Waves/Authoring.md`; no regeneration or shield visuals are implemented.

## Implemented E2A/E2B — authoring and verification

Register a new type by authoring its own Enemy scene/art/EnemyDefinition and adding the PackedScene to DefaultEnemyCatalog.tres Scenes. Existing EnemyId is the only registration key; no separate copied identity/balance exists. Rebuild validates root/Movement/Definition, blank/duplicate IDs and finite valid health, shield capacity, speed, radius, turning, rewards and Citadel damage before spawning. SpawnManager exports optional Catalog, resolves string-ID Spawn/SpawnWave requests, and preserves legacy scene APIs/F2. WaveManager reads its actual world's catalog; WaveCatalog.LoadScenes remains a default adapter for authoring/tests. Runtime registration edits require Rebuild and wave reconfiguration, not per-frame polling. Unknown IDs fail explicitly without substituting Enemy001.

Current shared single-cell route clearance guarantees require radius smaller than half CellSize. Larger art is valid with a separately configured radius; oversized physical types are held with a clear diagnostic until wider-radius path support is designed, rather than forced through narrow terrain. Existing HasClearance/SegmentClear, source permissions, A* count/time budgets, revisions, offsets and route handoffs remain unchanged.

Enemy.Initialize publishes an optional Initialized callback after registry/movement setup. ShieldEmitter uses it plus Removed/ExitTree, with no mandatory ability hierarchy/list/controller. Enemy.RestoreShield safely changes only current shield points up to existing capacity; hull/DefenseBar/typed damage/rewards remain separate. Emitter configs/timers are private, pulses are staggered/bounded and no retroactive shield removal occurs. See Abilities/ShieldEmitter/README.md for exact eligibility/limits.

EnemyCatalogChecks covers real two-type weighted waves/portal rules, Enemy002 valid movement/targeting, independent shields, distinct reward/arrival and invalid registration/oversized rejection. ShieldEmitterChecks covers two emitters, caps/range/intervals/self/eligibility, bounded queries, invalid config and death/arrival/unload/queued cleanup. The full Godot 4.7.2 headless regression suite passed at E2A and again at E2B, preserving Battle/Laser stats/upgrades, waves, damage, shields and the 500-ship fixture. Compilation uses Roslyn plus actual Godot source generators because the environment's ordinary editor SDK/MSBuild setup is incomplete. Live visual quality is not claimed. E3A/E3B were not implemented.
