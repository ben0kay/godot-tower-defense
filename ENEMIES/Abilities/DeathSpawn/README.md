# DeathSpawn — E3A implemented

Optional passive child component, independent of DeathExplosion visuals and ShieldEmitter. Attach under an Enemy (e.g. Abilities/DeathSpawn), assign `DeathSpawnConfig`, optionally Host/Enabled. Ordinary enemies have no component or ability tick. The host is discovered/cached once; config is private per ability. Invalid config/catalog IDs warn explicitly; no fallback enemy is substituted.

## Inspector defaults

ChildEnemyId `basic_mk1`, ChildCount **6** (1–64), SpawnIntervalSeconds **0.12** (0–10), ScatterRadiusPixels **48** (0–256), SpawnPattern **RadialScatter**, MaxPositionAttemptsPerChild **8** (1–32), SpawnWithShield **false**, InheritCheckpointProgress **true**, MaxSpawnGenerations **1**. Only one generation/pattern is implemented; other generations/invalid values reject. Type stats and child shield capacity come from the child definition, never the parent's remaining defenses.

A true hull kill emits Enemy.KilledBeforeRemoval once, before registry outcome/Removed. DeathSpawn snapshots position, scene/type, tuning, independent scatter seed, generation and remaining checkpoint handoff into the world queue. Parent feedback, Credits and tower XP continue normally. Arrival, shield-only damage, external Free/QueueFree and unload do not enqueue; the dying node owns no delayed timers. Releasing children itself gives no Credits/XP.

`SpawnManager.DeathReleases` is one `DeathReleaseQueue` child created during service setup, asleep while empty. First release is eligible on the next permitted physics tick; each job then waits its interval of unpaused game time. No catch-up bursts/per-child timers: at most one attempt per job/tick, and the whole queue shares MaxDeathSpawnAttemptsPerFrame (default **2**). EnemyMasterConfig also exposes DeathReleaseSeed and MaxPendingDeathReleaseJobs (default **1024**). Round-robin jobs share the budget fairly; job cap exhaustion skips that release visibly without reservations.

## Positions, routes and ownership

At-position spawning tries a bounded deterministic radial scatter with shrinking offsets and a final center fallback. One connectivity result is reused across a child's candidate probes. Each candidate must have radius clearance, bounds and connectivity to its actual next required checkpoint or the Citadel. No portal selection, teleport fallback, competing nav grid or unbounded retries. Failure/cap exhaustion skips that child and retires its reservation. LastMessage and queue success/skip/attempt/peak-CPU counters expose outcomes.

`EnemyRouteHandoff` snapshots the current required checkpoint, complete flag/count, captured network and route error before parent removal. EnemyMovement consumes it before the child's first route request. Already-selected next branches/completed stages are preserved; future branch choices and physical paths use the child's own profile/corridor seed/offsets/definition permissions. Opting out means direct-to-Citadel. Broken required destinations skip local releases rather than silently bypassing obligations. Links remain the parent's captured network; geometry/normal route invalidation retains existing semantics.

WaveManager reserves PendingDeathSpawns while the parent is still owned. A child attaches to ownership and publishes wave-stat provenance before Initialize (including entry Lava deaths), then its pending slot retires. Completion requires scheduled=0, alive=0 AND pending=0. PlannedRemaining still counts authored entries only. F2/unowned jobs remain unowned. Configure cancels that wave's jobs/reservations immediately; unload retires all work. External enemy removal resolves ownership but is not a kill/reward/release.

See `Spawning/README.md`, `LEVELS/Waves/README.md`, `Types/Transporter/README.md` and `TESTS/Enemies/DeathSpawn.md`. E3B weapons, tower durability, further generations, research and saving are not included.


## MC1 destination inheritance

DeathReleaseJob always snapshots parent.AssignedCitadel separately from optional checkpoint handoff. InheritCheckpointProgress=false only opts out of checkpoint obligations, never Citadel identity. Local probes/clearance validate the inherited destination; removed/unreachable destinations fail safely. Captured handoffs also retain destination for direct users. Children never choose a new target based on release proximity.

## Heavy Transporter mixed payload — 2026-10-18

HeavyTransporter authors two independent instances of this existing component under Abilities, each with its own local config: six `basic_mk1` and four `scout_mk1`. Both enqueue/reserve before Removed and share normal queue budgets; no changes to the component, queue or original carrier defaults. FleetExpansionChecks exercises final-carrier wave completion on both maps.
