## PF2 debug update (2026-10-20)

Optional subsystem timing hooks feed the existing F4 panel (Shift+F4 toggles profiling; F5 saves a report). Old GridDebugOverlay/Info labels are retired; F2/F3 remain. See `DEBUG/README.md` for inclusive timing semantics, fixed sample storage and validation details. Historical notes below remain as reference.

# Shared navigation — weighted, incremental and budgeted

WorldGrid is the sole authority for terrain, footprints and tower occupancy. NavigationGrid caches a small pool of shared AStarGrid2D profiles, not one grid per enemy. Weighted preferences, optional congestion, corner-safe diagonals and Citadel approach goals remain active. Jumping is disabled because it bypasses point weights; weights stay at least 1 for the octile heuristic.

## Updates and route invalidation

WorldGrid.NavigationChanged reports one changed cell on tower placement/removal, or a full invalidation after RebuildTerrain. A 1×1 tower updates that one solid flag in every shared profile immediately. Rectangular occupancy publishes one NavigationBatchChanged after the complete claim is staged; each newly blocked/unblocked PASSABLE cell updates incrementally in every profile, with one revision and one goal refresh. Buildings on entirely IMPASSABLE terrain publish no changes. Combined rectangle proposals use one scratch-buffer flood at click-time; mixed terrain is rejected by WorldGrid before navigation. Radius clearance tests nearby circle tiles against the same proposal, not a flood/check per footprint cell. Previews do not perform route work. Full map scans are reserved for startup, terrain reloads and profile configuration changes. FullRebuildCount and IncrementalCellUpdates expose which work actually ran.

EnemyRoute caches swept segment cell dependencies when assigning a path. PathRequestManager indexes ships by those cells. A new block marks only potentially affected remaining routes; cells behind a ship are ignored. Marking is cheap, and exact checks run in a separate bounded queue. Unaffected ships continue moving. A confirmed affected ship queues a replacement while following its safe old prefix; immediate per-tick swept clearance also stops a ship safely if validation has not reached it yet. Removing a block leaves valid routes alone. Failed routes retry on the existing timer.

Full terrain changes queue audits, rather than stopping/repathing all ships immediately. Existing routes do not compare the global revision each physics tick. Pending A* candidates still restart on navigation/cost revisions to prevent accepting stale results.

## Scheduling and Inspector settings

Select ENEMIES/DefaultEnemyConfig.tres:

| Setting | Default | Meaning |
| --- | --- | --- |
| MaxPathCalculationsPerFrame | 8 | Maximum actual A* goal queries per physics frame |
| MaxPathfindingMillisecondsPerFrame | 2 ms | Optional soft budget for the entire path-work stage; 0 disables |
| MaxRouteValidationsPerFrame | 16 | Maximum cached-route validations per frame |
| MaxRouteValidationMillisecondsPerFrame | 0.5 ms | Optional soft validity-work budget; 0 disables |
| FairRequestEveryCalculations | 4 | Regular oldest-request service alongside distance-based urgency |
| RouteAuditIntervalSeconds | 3 s | Staggered lightweight audits; 0 disables periodic audits |
| EnablePathOffsets | true | Safe movement refinement without extra A* |
| PathOffsetMaxPixels | 8 px | Maximum stable per-spawn lateral bias |

Duplicate requests coalesce. Ships close to the blocking segment get higher path priority; regular oldest-request service prevents distant/spawn requests from starving. A native A* call and its path assignment cannot be interrupted, so time budgets may overrun by one work item. The count limit remains strict. Candidate approaches are compared by weighted cost; First targeting uses physical remaining refined-path distance.

## Route preferences and congestion

EnablePathDiversity, PathDiversityStrength (0.6), RouteProfileCount (4, capped at 16), PreferencePatchCells (8), and RoutePreferenceSeed configure smooth deterministic cost patterns. Ships receive a profile on spawn. Disable diversity for ordinary shortest paths. No algorithm can create another corridor where only one exists.

CongestionInfluence defaults to 0/off. Throttled snapshots of living ship density affect new/repath queries; existing routes are not periodically recalculated for crowds. Per-profile sparse weight caches restore old snapshot costs before replacing them. A* candidates are rescored consistently against the current snapshot. Separation settings remain reserved and inactive.

## Connectivity and diagnostics

Build clicks still reject sealed usable entries and trapped active ships. Connectivity uses reusable flat flood-fill buffers and cardinal links: these have the same connected components as diagonals allowed only when both side cells are walkable. It remains a synchronous correctness check, but does not rebuild A* profiles or allocate a queue/result array per click. ReachableWithout returns borrowed scratch data: copy it if it must survive another flood call. SpawnManager keeps a separate reusable cache.

Level001/Debug/HUD displays placement-validation, grid-update, native A*, total path-work and route-validation timings independently, plus work counts/queues. Tests and before/after measurements are in TESTS/README.md and TESTS/Performance/NavigationPerformance.md. These are headless CPU diagnostics, not a rendered 500-ship/60-FPS guarantee. Long individual searches/path assignments and synchronous mass spawning can still exceed a requested time budget.

## Moving replacement-path handoffs

Confirmed blockage now queues a replacement without stopping safe movement immediately. Ships retain old waypoints while waiting, with strict immediate swept/corner checks; they stop only when the next movement would be unsafe. Queue urgency follows the shrinking distance to the obstruction. Failed replacement searches retain any safe old prefix and use the existing retry interval.

Each multi-goal request uses one position snapshot, so its candidate costs are comparable while the ship moves. When the result arrives, EnemyMovement joins it near the ship's current progress, preferring a forward clearance-tested waypoint. A safe two-leg elbow can turn around a nearby wall without returning to the cell center. Both legs obey swept clearance and corner-safe cell transitions. If reconnection fails, a fresh request uses the same centralized budgets. Backtracking remains possible when the actual map requires it.

Safe offset refinement cannot turn the first new segment backward merely due to lateral bias. Existing speed/turning/offset settings, weighted profiles, congestion, auditing and count/time budgets are preserved. First targeting excludes routes known to need replacement until a valid handoff completes. No new pathfinding algorithm or per-frame A* is added.

## N4 corridor refinement

Optional corridor variation is downstream of the existing shared weighted AStar result. No new AStarGrid2D instances, profile changes, congestion changes or weight writes are involved. Selected ships refine waypoints through bounded radius-aware lateral probes; off/unselected ships take the original movement route builder. Configuration is centralized in EnemyMasterConfig's Corridor Path Variation group (off by default).

Actual varied geometry supplies cached route dependencies, physical remaining distance and diagonal side-cell checks. Incremental grid updates, NavigationRevision, selective route validation, fair prioritized requests and count/time budgets remain unchanged. A swept-disc supplement validates selected route connections against solid terrain/occupied cells, avoiding missed near-tangent corners. All expensive refinement occurs inside accepted budgeted path requests; it does not recalculate every frame. See ENEMIES/Movement/README.md and TESTS/Performance/CorridorPerformance.md.

## Per-type diversity permission and neutral routing

EnemyDefinition.AllowPathDiversity=false uses reserved NavigationGrid.NeutralProfile (-1). One extra shared A* grid has unit point weights and never receives congestion, including RouteCost multi-goal comparisons. Existing weighted IDs and assignment stream are unchanged; profile zero is not neutral when diversity is enabled. SharedProfileCount retains its weighted-pool diagnostic meaning; TotalSharedProfileCount includes the extra neutral grid. Every grid shares blocked cells, corner policy, revisions and incremental tower updates. No additional requests, budget changes or per-enemy grids are introduced. Permissions are cached at initialization. Corridor/offset gates remain independent geometric refinements. See TESTS/Navigation/MovementPermissions.md.

## Isolated debug route queries

F3 owns a detached NavigationGrid preview pool created from a private EnemyMasterConfig copy. It reuses production A* construction, unit-cost neutral routing and the same authoritative WorldGrid solidity, but no weighted/crowd caches are shared with live navigation. One preview pool is reused across all portals in a batch. Congestion is copied once from the live sparse snapshot, never rescanned by the debugger. The pool and copied resource are explicitly released on reset/close/level exit as appropriate.

Live and preview RouteProfileCount accept 1–16, with the existing default 4 unchanged. F3 controls expose preview-only diversity enable/strength/count; no file writes or live-config assignments are made. Jobs respect type permissions and compare every current Citadel goal using selected costs, including neutral -1. All portals together use one bounded debug request budget, never gameplay request queues or assignment counters. Native timing diagnostics of live navigation remain unchanged. Finished plans are cached; source/settings/navigation changes cancel output. No checkpoint routing is implemented.

## Required circular checkpoint stages

PathRequestManager can search the active enemy's cached checkpoint targets before ordinary Citadel goals. A request retains one origin snapshot and compares costs normally; stage/marker, navigation or cost revisions reset partial candidates. Hard A* counts, soft time limits, urgency/fair service, moving safe-prefix handoffs and clearance rules are unchanged. The shared CheckpointRoute adapter makes an unsnapped terminal connection with clearance checks and consistent weighted/neutral rescoring. Each stage target query still counts as exactly one native A*.

Only physical swept entry changes an enemy to the Citadel stage. Checkpoint geometry has at most33 candidate probes, cached per world and enemy radius. No per-enemy grid, Area2D, enclosed-tile A* loop or per-tick registry scan exists. Unreachable required regions wait/retry; ordinary placement connectivity is unchanged. First distance remains the physical current-stage chain. Unlinked portals use the old request path verbatim. F3's isolated pools reuse the same targets/adapter/builders and plan both legs at actual circle entry. See WORLD/ROUTING/RouteCheckpoint/README.md and TESTS/Navigation/Checkpoints.md.

Checkpoint networks now support acyclic chains and uniform branching. EnemyMovement switches its active checkpoint only on physical entry, increments a destination serial so queued multi-goal candidates reset even for overlapping/same-geometry destinations, and keeps chosen branches fixed across repaths. CheckpointNetwork validates and snapshots links at spawn; it never changes native A* weights/grids or consumes wave RNG. The existing per-stage target cache, centralized budgets and safe-prefix mechanisms remain. Editor graph drawing is separate from routing. F3 uses isolated profile pools and the same branch helper, labels complete sample sequences and invalidates downstream graph edits.

## IT4 targeting journey cache

JourneyEstimateCache shares downstream checkpoint/approach estimates by edge, effective profile and radius. It uses only spare PathRequestManager query/time budget after real movement work; combined query count remains capped. Comparisons read cached scalar totals, never run A*. Navigation/cost revisions and checkpoint geometry/removal invalidate estimates without rerolling branches or changing movement. Current leg length remains separate. See TOWERS/Shared/Targeting/README.md for estimate semantics, canonical circle representatives and unavailable-score fallback.


## MC1 + MC2 — implemented 2026-10-18; MC3 remains pending

Multiple independently damageable Citadels now have stable session destination objects. LevelConfig exposes ordered CitadelPaths; an empty array preserves CitadelPath. Actual additional Health/ApproachPoints Citadels are discovered in authored child order so a second objective is not silently ignored. WorldGrid reserves all physical footprints/approaches. All objectives currently share the level's authored CitadelMaxHealth, but own separate health state; the existing deployment research percentage initializes each one. Changing/adding the Citadel roster requires restarting the level. Geometry/terrain rebuilds can refresh goals and assignments for future spawns; already spawned enemies and released children keep their original identities. Removing an assigned objective retires it: enemies stop, stale distance scores become unavailable, and there is no redirect to another objective.

**Single-Citadel fast path:** assign the sole objective directly, with zero destination-comparison A*. Ordinary non-checkpoint placement retains its original single scratch flood. No per-enemy objective searches, per-frame goal scans, per-Citadel AStar grids or extra movement nodes. Available/retired state is cached through objective lifecycle events, not native-object queries in every scoring/movement call.

**Multi-Citadel policy:** PortalCitadelAssignments compares neutral navigable physical route lengths once at setup/explicit terrain rebuild, with authored-order ties. Selection uses cell-center clearance (radius zero); each actual source/radius is additionally cached and validated against clearance-aware connectivity on navigation revisions. Unsupported or blocked ships fail/retry, never select another objective. MapEdges is disabled on multi-Citadel levels; Both uses authored portals only and reports that edge sources are disabled. Single-Citadel edge/Both behavior is retained.

**Required checkpoints:** assignment minimizes the worst branch's neutral journey using deterministic, reachable canonical checkpoint representatives (first valid cached target in the source component that reaches an assigned approach). This is the documented discrete selection policy, not a claim of globally optimizing continuous circle-entry geometry. Every captured branch must remain connected. Runtime branch choices/random streams and varied routes are unchanged. Journey tail caches include destination identity; normal path requests/repaths use the assigned approach goals under the existing combined query/time budgets. DeathSpawn always inherits Citadel identity, including when InheritCheckpointProgress is false.

**Placement/outcomes:** click-time validation shares component maps per radius, protects each usable portal's assigned goal, all remaining mandatory checkpoint branches and live enemy clearance/remaining obligations. It cannot accept a different reachable Citadel as a substitute. Losing any objective emits a single session event into the existing LevelSessionController defeat/Results/mission-XP lifecycle. Results uses the failed objective's HP, never an aggregate pool that hides failure. Existing HUD still shows the primary Citadel; independent multi-objective HUD/F3 destination labels and desktop visual verification are MC3 work. Existing Level001/Level020 maps, waves and Inspector layouts were not changed.

See TESTS/Navigation/MultiCitadel.md for executed checks, measured setup/batch behavior and limitations. Original MC1–MC3 instructions below are retained; this status supersedes their earlier NOT IMPLEMENTED wording.


## MC3 preview destinations

RoutePreviewJob accepts an optional final CitadelDestination. F3 always supplies the cached live identity, captures only its goal cells, and uses a private preview navigation pool for calculations. Direct legacy single-Citadel callers keep their existing default API. Multi-Citadel jobs without an explicit destination have no goals and report failure, rather than silently previewing Citadel 1. All live destination goals/availability participate in the overlay input stamp. No live AStar/profiles/assignment work is performed by F3.
