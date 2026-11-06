## SPAWN2 expanded brief — shared ALL-PORTALS dimensional materialization (2026-11-06)

**Plan only.** All START portals should form from a tiny dimensional singularity into a bright swirling violet warp as a level **freshly loads**. Portals configured `AppearAtWave=N` use the exact same one-shot event when their wave is accepted; numeric Wave 1 is not deployment, START=0 is. [Codex-ready SPAWN2 specification](IMPLEMENTATION_PLAN.md#phase-spawn2--shared-cinematic-dimensional-warp-in-for-all-portals-codex-implementation-specification) defines 4 visual phases (~2.2 s), spiralling inward GPU particles, ring/ripple/flash, shared state `Dormant->Forming->Open`, pausable progress, source readiness (no enemies from forming portals), restore/no-replay, warnings and particle performance caps. Existing SPAWN1 mechanics, rotation and per-spawn particles remain unchanged until implemented.

---

## SPAWN1 startup registration follow-up — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Level001's config still named a removed second portal, and Level020 named an absent fifth portal. Removed those stale entries to match each actual map; missing required source validation remains strict. The new activation fixture now checks real Level001 setup separately and uses Level002's two actual portals for its future-portal scenario. Maps, wave recipes, active source selection, schedules and portal coordinates are unchanged.

---

## SPAWN0 / SPAWN1 — authored portal activation

**Source Implemented / Awaiting Owner Validation.**

`EnemyPortal.AppearAtWave` defaults to **0 = START**; a positive value reveals at the accepted wave's authored `Number`, before WaveStarted observers and the first immediate spawn. Activation is idempotent and persists for later waves. Keep all portals in `LevelConfig.ActivePortals`: that list is registration, not the currently visible subset. Future portals hide only Visuals; existing footprints, source entries, fixed Citadel assignments and checkpoint obligations remain protected by placement validation.

`SpawnManager.Activation.cs` validates registered identities/footprints, future exit/route safety and required-portal timing at wave setup. Unknown selectors and early requests become SETUP ERROR, instead of infinite retry. A genuinely blocked already-active required source still retries. Future portals need Portals/Both mode; edge-only configuration rejects them explicitly because its source cache cannot reserve their routes. An activation Number absent from the wave set warns and remains dormant. Defaults and Level011's authored maps/waves are unchanged.

A separate eligibility array/revision excludes dormant portals from all debug/random/required delivery. Revealing rebuilds only those booleans; it never edits terrain, floods navigation, recaptures assignments or consumes RNG. Existing geometry/connectivity caches continue to include future portals. F3 labels dormant sources **PREVIEW ONLY · WAVE N**; simulation never activates them.

### SPAWN0 source identity contract

| Source | Stable identity | Eligibility / routing |
| --- | --- | --- |
| Portal | Level-root-relative registered NodePath | Registered independently from active visibility; fixed exit, authored activation and assigned Citadel/checkpoint chain |
| Edge | Allowed side plus integer boundary cell | Existing reachable/clearance-safe cell centres; corners may belong to two sides; no sector/region IDs or weights authored yet |

Source-array indices are session cache locations, never durable IDs. Existing exact portal requests use the path; a blank request uses legacy permitted selection. Both still prefers eligible portals; edges remain single-Citadel only. Future edge regions should extend side + cell-range identity without per-cell Nodes, per-ship A* or new spawning services.

### Lightweight acceptance

`TESTS/Integration/PortalActivationChecks.tscn` is an isolated owner-run fixture for sparse numbered waves (2/6/9), dormant F2/required exclusion, reserved footprint/route, activation ordering/idempotence, checkpoint restore seams, bad timing, all four edges and Both preference. The existing WaveChecks now expects unknown selectors to fail setup. Fixtures are authored **but unrun**: .NET is unavailable here. Level011 checkpoint paths were source-reviewed; gameplay, rendering and frame-time acceptance remain for owner.

---

## Dynamic portals and source-policy roadmap — 2026-11-06 (PLAN ONLY)

A dedicated [SPAWN0–SPAWN5 implementation plan](IMPLEMENTATION_PLAN.md) now covers authored **START / Wave N portal activation**, hidden reserved future portals, materialization feedback, all-four-side map-edge use, opt-in true mixed portal/edge selection, independent concurrent wave batches and optional bounded scripted reinforcements. **No new spawning mechanics are implemented by this note.** Existing ordered wave batches, `SpawnMode.MapEdges`, `Both` portal preference, F2 and route validation are unchanged.

---

# Enemy spawning

`SpawnManager.cs` is a world service under World/EnemySystems/Spawning. Physical Portal scenes contain only visuals/SpawnExit; this service selects positions and creates requested scenes. Waves call SpawnWave(scene, random, optionalRequiredPortal), while level debug calls Spawn(scene, portalOnly: true).

LevelConfig supplies SpawnMode (Portals / MapEdges / Both), allowed edges, ActivePortals and CitadelPath. Entries and goal-connectivity are cached by navigation revision. Invalid, blocked, unreachable or insufficient-clearance sources are skipped. Map-edge entries are cell centers on allowed map borders. Portal-only debug respects permissions, so edge-only levels need PortalOnly unchecked on their Debug node to test edge spawning.

Placement checks run on a build click: reject a cell overlapping an active enemy's clearance, sealing any currently usable authored entry, or trapping a living enemy. In edge mode every currently reachable permitted boundary entry is preserved. The hover preview remains a cheap terrain/occupancy check; a route rejection is explained in the Inspector when clicked. RebuildTerrain after runtime changes to level source/goal settings.

Main/Waves schedules manual finite waves. SpawnWave selects uniform random eligible portals, prefers them in Both mode, and uses permitted map edges if no portal is eligible. A required registered portal never falls back. F2 Spawn(scene, portalOnly) retains round-robin behavior. EnemyMasterConfig controls the active ship safety limit and path budget. Sources already unreachable because of authored map errors do not become valid through this check; debug spawning reports the failure safely.

Connectivity storage now reuses buffers: NavigationGrid returns borrowed flood scratch space, copied into SpawnManager's reusable current-connectivity cache. Placement validation exposes LastPlacementValidationMilliseconds. The pre/post connectivity checks remain mandatory; route checking has not been removed to hide freezes.

Portal spawning now uses the centered SpawnExit plus a random offset up to PortalSpawnOffsetPixels on each world axis (default 8). Both F2 and waves use the same safety helper; source selection remains round-robin for F2 and random for waves. Up to eight clearance/connectivity/segment-checked samples are tried, falling back to the valid center. Edge positions remain exact. Set the offset to 0 for exact portal-center spawning.

`SpawnWave(scene, random, requiredPortal: "", spawnWithShield: false)` now accepts the wave's optional full-shield flag and passes it to `Enemy.Initialize` only after a valid spawn is found. This does not affect portal selection, retry logic, navigation or costs. `Spawn(scene, portalOnly)` (F2/debug) continues to initialize without shields by default; the per-type `EnemyDefinition.MaxShield` is capacity, not a spawning directive.


E2A: optional exported Catalog defaults to DefaultEnemyCatalog; string-ID Spawn/SpawnWave adapters resolve types while legacy scene APIs/F2 remain intact. WaveManager uses this actual world catalog. Current guaranteed single-cell route support requires radius smaller than half CellSize; oversized types are held/retried with a diagnostic, not squeezed through obstacles. No new A* work or radius grid is created.


Default catalog also registers `shield_emitter_mk1` (the `Enemy003.tscn` asset). Abilities initialize only after a safe physical source, registry and movement are ready. SpawnManager does not own pulse timers or shield-restoration logic; wave starting-shield policy remains unchanged.


Portal offsets now scale by each EnemyPortal.SizeMultiplier and its authored transform. Portal references are cached alongside source metadata; current size is read on each spawn, so changing it needs no A* or navigation rebuild. The base config offset remains the shared tuning control, and all8 bounded safety attempts/center fallback still apply. Map-edge sources are unaffected.

## Portal checkpoint assignment

Both debug and authored wave spawns inspect only the actual selected EnemyPortal's optional CheckpointPath. Its resolved node plus a separate required flag are supplied before initial movement setup, preserving missing-link requirements. No extra wave/spawn RNG draws or changes to reservoir selection, spread, registry/economy or terrain initialization are introduced. Empty links and map-edge sources preserve existing direct routing.

## Mission statistics provenance

`WaveEnemyCreated` identifies successful authored-wave spawns before Enemy.Initialize, so entry-hazard deaths are countable. It is not emitted by manual/F2 spawning. The level session subscribes once for basic official kill/breach tallies; SpawnManager does not own outcomes, rewards or result presentation.

## E3A at-position death releases

Spawning owns one DeathReleaseQueue child, inactive when empty. Optional DeathSpawn components enqueue finite jobs only on hull kills. SpawnDeathChild validates bounded local scatter against actual child radius and required checkpoint/Citadel connectivity before any scene enters the world. It never selects a portal/map edge, consumes wave RNG, bypasses blocked positions or spawns over MaxActiveEnemies. Partial failures retire slots; no indefinite retries. Shared frame budget defaults to2 attempts; queued jobs default-cap1024, with Inspector settings on EnemyMasterConfig. See `Abilities/DeathSpawn/README.md`.

WaveManager now claims authored enemies from WaveEnemyCreated before Initialize, enabling entry-hazard carriers to reserve releases. Successfully created wave offspring also emit that provenance before initialization so R1 official counters include them. F2 offspring do not. A successful child enters wave ownership before its pending slot retires. Captured remaining checkpoint obligations initialize before its first path request, with its own movement settings.


## Fixed Citadel ownership — MC1/MC2

SpawnManager owns PortalCitadelAssignments. It resolves source destinations at setup/explicit TerrainRebuilt only; ordinary occupancy revisions invalidate source/radius reachability without reassigning sources. Single objective bypasses comparisons. Debug/wave spawns pass AssignedCitadel into Enemy.Initialize; unknown/unreachable multi-objective sources refuse spawn. Multi-Citadel edge mode is disabled. See WORLD/OBJECTIVES/Citadel/README.md for neutral, checkpoint and clearance policies.


## MC3 authoring-rebuild timing

TerrainRebuilt now completes RefreshSources immediately when the spawning service is ready. This is the explicit authoring rebuild path already allowed to recompute assignments; it keeps F3's read of cached destinations current before another spawn. Initial setup and ordinary occupancy behavior are unchanged. F3 itself never calls RefreshSources or chooses a destination.
