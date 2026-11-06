# Enemy Spawn Sources and Dynamic Portals — implementation plan

**Status: PLANNED (2026-11-06), not gameplay implemented.** Audited the current `dev` implementation. This is a future work plan for Codex/normal implementation passes, **not permission to change Level011 waves, source settings, combat, saves or map layout now**. Work on `dev`, read `NOTES/CodePreferences.md`, root `README.md`, and the owning files before each pass.

## 0. Baseline — what already works (do not duplicate)

- `LEVELS/Waves/WaveManager.cs`: one finite manual wave scheduler, **ordered batches**. A batch is fully scheduled before advancing to the next; `After` is the gap after the last successful spawn, not after the last enemy dies. Successful spawns are wave-owned; victory waits for planned, alive and reserved carrier children to reach zero.
- `LEVELS/Waves/WaveCatalog.cs`: exact largest-remainder weighted enemy quotas, seeded shuffle **within each batch**; ordering of batches is preserved. `WaveEnemyEntry.Portal` can select an exact level-root-relative portal. Blank selects an eligible random portal; failed required selection retries instead of silently falling back.
- `ENEMIES/Spawning/SpawnManager.cs`: source selection and actual enemy creation live here, not in physical portals. A wave without a required portal uniformly samples eligible authored portals; `SpawnMode.MapEdges` samples reachable permitted boundary-cell centres; `SpawnMode.Both` prefers portals and falls back to edges only when no portal is eligible. F2's current batch menu uses the same valid portal selection policy with an independent RNG; older round-robin descriptions refer to the retired F2 legacy cycler.
- `LEVELS/LevelConfig.cs`: `SpawnMode` (Portals / MapEdges / Both), `AllowedEdges` flags (Top/Bottom/Left/Right) and `ActivePortals` (currently the registered map paths, not a wave-activation schedule).
- `WORLD/PORTALS/EnemyPortal/EnemyPortal.cs`: `SpawnExit`, optional required checkpoint, visual size override, energy pulse. No spawn clocks, activation-wave Inspector property or materialization state.
- `ENEMIES/Spawning/MapEdges/README.md` is **historical** when it calls all map-edge spawning future: basic edge enumeration/selection already exists. Per-wave edge targeting and authorable edge sectors are future.
- `LEVELS/Level011/Level011Waves.tres` Wave 1 has three **sequential 18-ship batches**: Portal1 (interval 0.65, after 2.0), Portal2 (0.60, after 2.0), Portal3 (0.55, after 0). Each batch carries exactly 12 `basic_mk1` and 6 `scout_mk1`, deterministically shuffled. It is not random portal sequence. Level011 currently authors only Waves 1–5; a portal set to appear at Wave 6 will **never activate until Wave 6 exists**.
- Existing `WORLD/OBJECTIVES/Citadel/PortalCitadelAssignments`, checkpoint networks, navigation clearance, click-time placement protection, wave ownership, F3 routing preview, F2 debug and between-wave campaign checkpoints must continue to work.

**Immediate no-code use:** On a single-Citadel map, set `SpawnMode = MapEdges` and check all four `AllowedEdges` flags to sample all **valid, passable, connected** boundary cells. `Both` does **not** currently alternate between edges and portals while portals are available. A single current batch can already mix multiple portal paths in its entries for interleaved entry points, even though distinct batches remain sequential.

## Goals

1. Author any number of editor-placed portals, with per-instance **Appear At = START (default) or Wave N**. An invisible future portal materializes at a level-authored location on its activation wave, then becomes a valid source for normal waves/F2 (subject to ordinary source permission and routing checks). Support an optional short telegraph before the first enemy.
2. Preserve predictable ordered attacks and allow **controlled interleaved/parallel threats** from multiple lanes without forcing RNG or rewriting existing `.tres` files.
3. Support map-edge attacks from **all valid sides**, optionally weighted by edge/sector and mixed with portals in the same wave.
4. Keep gameplay deterministic across equivalent seeds, budgeted and compatible with valid between-wave saving, multiple Citadels, checkpoints, buildable terrain and 500+ enemy stress scenarios.
5. Keep surprise *where/when* a portal appears separate from surprise *whether a safe route exists*: only authored, validated entry points in the first pass; no uncontrolled procedural placements.

## Phase SPAWN0 — source model and acceptance contract (planning/QA, smallest)

**Before new code:** verify existing all-edges mode using a **single-Citadel** fixture with reachable exits on every boundary; confirm skip rules for impassable/blocked cells and that `Both` prefers portals. Inspect Level011's three currently authored paths and checkpoint chains. Preserve existing level defaults and current F2 behaviour.

Document an internal source identity model (stable portal path/ID plus edge side/region) that can participate in source validation and a targeted selection request. Avoid adding duplicate spawning services, repeating A* per spawn or a new Node per boundary cell.

## Phase SPAWN1 — authored portal activation by wave (highest-priority real feature)

**Owner:** `WORLD/PORTALS/EnemyPortal/` owns Inspector settings and presentation; `LEVELS/Waves/` owns wave start; `ENEMIES/Spawning/` owns active-source eligibility and cache refresh.

### Inspector / authoring

- On each `EnemyPortal`, add an Inspector-editable field such as `AppearAtWave = 0`, where **0 means START / visible from level deployment**, `6` means **activate at the beginning of accepted Wave 6**. Prefer a clear Inspector group and tooltip/label that explains the default. The number refers to `WaveDefinition.Number` / `WaveResource.Number`, **not** the array index or number of completed waves.
- The portal stays physically in the scene under `LevelXXX/Portals` even before activation and stays in the level `ActivePortals` *registration list*. `ActivePortals` must not be misused as a mutable visible/activated subset; the system needs separate **registered**, **currently eligible**, and **future reserved** concepts. Preserve paths/IDs.
- START portals remain visible and eligible under the previous default. A Wave N portal begins **hidden/ineligible** until that wave is successfully started, then materializes and becomes eligible. The transition is idempotent, and it stays active on later waves unless a future explicit lifetime rule is added.
- Even while invisible, a future portal's authored footprint/exit and required route must stay **reserved/protected** against tower placement. Do not allow a player to seal its route during Waves 1–5 and discover at Wave 6 that the portal is unusable. Precompute/validate assigned Citadel and required checkpoint paths for **all registered sources**, not only active ones. No reassignment to a nearer Citadel on activation.
- On `WaveStarted`, **activate newly due portals before the first immediate spawn** that can require them. Current `WaveManager.StartWave` fires `WaveStarted` before `Advance(0, 1)`; use a well-defined event listener/ordering contract. If a short materialization animation is later used, either defer eligible spawning until activation completes with an explicit scheduler-safe status, or treat appearance as instant for gameplay and play cosmetic VFX; never allow a hidden portal to produce invisible ships.
- Add an explicit **source-eligibility invalidation** that refreshes the cached active source set when activation changes, without fabricating a terrain edit, forcing a full navigation rebuild, recomputing A* for every spawn or invalidating unrelated route certificates. Existing connectivity/nav-revision cache must remain intact where geometry did not change.
- Required `WaveEnemyEntry.Portal` before its configured activation wave should produce a **clear authoring validation error** (ideally detected on wave-set load) instead of a permanent retry deadlock. A required portal after activation that is genuinely blocked keeps the current retry/diagnostic contract. Unknown paths and malformed activation waves must be reported.
- Never spawn through future portals from F2/debug, random wave selection, or silent fallback. F3 may optionally show future portals as **preview-only** with a distinct label; previews must not activate portals.
- Preserve energy field/portal pulse, checkpoint links, source spread/size, child enemy ownership and Catadel assignments.

### Lifecycle and saving

- Default `AppearAtWave=0` must preserve all existing levels and tests without migration. The current checkpoint system saves **between waves only**, not active combat. Reconstruct portal availability from the **restored completed-wave number** at load, with exact semantics: active if START or if its activation wave has already been started/completed. A portal that first appears in the *next* wave remains dormant at the restored preparation screen.
- If future combat/mid-wave saving is added, record activation and any pending telegraph explicitly; do not infer from only completed-wave count.
- The portal's materialization visual is **not** its route footprint: hiding Visuals must never remove the author's reserved cell or change navigation.

### Focused acceptance

- START default parity on Level001/002/011, hidden until due wave, activation once at Wave N, eligible for its first batch, still active later; never selected earlier by random selection/F2.
- Future source footprint cannot be occupied by a tower and its required Citadel/checkpoint path cannot be sealed before activation; a deliberate invalid route reports an actionable error.
- After-wave checkpoint restore before N keeps it hidden; after completed N keeps it active; no phantom first-wave spawns or double activation.
- Overlapping/invalid/missing activation setup fails safely; blocked required-source retry stays budgeted; no navigation rebuild on purely visual appearance.
- Multiple Citadels: future portal's fixed precomputed assignment is preserved; if not safely possible, mark source ineligible with a precise diagnostic rather than retargeting.
- **No edit to Level011 mission waves in this pass.** Use one small fixture containing a Wave N portal for proof.

## Phase SPAWN2 — portal materialization feedback (small, mostly visuals)

- Reuse the current `Visuals`/energy shader and spawn pulse; add a **one-shot, lightweight** appear animation (faint shimmer/charge, transparent-to-opaque, perhaps ~1–2s) and a tactical alert: **“NEW ENEMY PORTAL DETECTED — SECTOR …”**. Keep it optional for scripted story levels; offer an `AppearWarningSeconds` only if scheduling can wait safely without violating wave counts.
- Show the future portal's intended coordinates in the **editor**, but never its live visual during gameplay before activation. Explicit author-only gizmos/previews should avoid leaking surprise to the player.
- Avoid idle processing for hidden portals, heavy full-screen blur, per-enemy particle nodes, background timer callbacks while unused, and forced material edits.

## Phase SPAWN3 — map-edge and mixed source policy (medium)

**Baseline exists:** `LevelConfig.SpawnMode = MapEdges` + `AllowedEdges = Top|Bottom|Left|Right` allows spawning at all **reachable** boundary-cell candidates on a single-Citadel map. Current `Both` = **PortalPreferred** with edge fallback, not simultaneous.

- Add **opt-in** wave/batch source policy, preserving old behaviour for missing fields:
  - `LegacyPortalPreferred` (current Both behaviour, default for existing levels).
  - `AnyEligible` (genuinely sample portals and edges in one wave/batch).
  - `PortalsOnly` / `EdgesOnly` / `RequiredPortal` (explicit).
  - Optional **weighted sides** Top/Bottom/Left/Right or logical region IDs; per-side selection and per-position selection must be separate so a long boundary doesn't automatically outweigh a short one.
- Allow authored **edge sectors** (e.g. upper half of Left, first third of Top), preferably data/ranges, not hundreds of Marker2D nodes. Select only reachable + clearance-safe cells; optionally enforce no portal overlap and authored minimum spacing.
- Preserve existing per-entry exact `Portal` selectors and per-enemy shield/stealth flags. Decide format in `WaveEnemyResource` / `WaveEnemyEntry` with append-only defaults, and expose it in `addons/wave_editor` without breaking old Level011/003 resources.
- Keep **multi-Citadel edge spawning disabled** until a separate, validated deterministic edge→Citadel assignment design exists; do not bypass the existing safety policy.
- Explicit bad/missing required edge region should be clearly blocked/validated; never silently select an unrelated source.

## Phase SPAWN4 — parallel attack groups inside one wave (medium–high)

Current sequential `WaveManager` has **one** `_batchIndex`, `_entryIndex` and `_countdown`. You can already interleave portals *within one mixed batch*, but not run **multiple separately timed batches concurrently**.

- Preserve **Sequential (default)** scheduling for all current waves. Add opt-in `AttackGroup` or `BatchStartOffsetSeconds` to let e.g. Portal1 spawn 20 enemies at 0.5s **while** Portal3 spawns 10 at 0.9s, starting 3s later. Allow mixed compositions and a final simultaneous multi-side assault.
- Implement one central, attempt-budgeted scheduler (not one timer/physics node per batch or enemy), independent per-batch cooldowns, deterministic event ordering and fair attempt distribution. No unbounded catch-up bursts after lag. If one group is blocked, other runnable groups should be able to continue while ownership/PlannedRemaining still counts both.
- Preserve exact per-entry weighted quotas and seeded shuffle (at group initialization), correct `WaveEnemyCreated` ownership and death-release reservation accounting, wave-ending only after **every group is exhausted** and all owned enemies/offspring have resolved.
- Extend Wave Editor with readable group/timeline preview, expected portal/side distribution and validation. Resist feature creep into a full cutscene timeline tool.

## Phase SPAWN5 — bounded scripted surprise events (optional, design later)

- Add **authored** mid-wave trigger sources only after SPAWN1–4 are stable: at elapsed wave time, after a specific batch, when a boss arrives/dies, or Citadel HP crosses an authored threshold. Each trigger is **one-shot**, explicitly configured and mission-specific.
- Offer portal lifetime rules **StayActive / CloseAfterWave / LimitedShipCount** as separate authored policies. If a required batch names a portal after it closes, that is an authoring error rather than silent rerouting.
- Allow a randomized choice **among explicitly preapproved spawn sites** with seeded selection, not unconstrained teleport into buildable/impassable terrain. Validate all candidate sites ahead of time and reserve their required route footprints.
- Later flavour ideas: temporary flanking rifts, telegraphed siege incursions, decoy portal holograms (visual only), rare elite reinforcement groups, and rotating side pressure. None of these may bypass spawn cap, clearance, checkpoint obligations or Citadel assignment.

## Design guardrails and recommended order

1. **Implement SPAWN1 first**: user-facing `AppearAtWave` is clear, useful and mostly leverages existing portal registration/wave-start events.
2. SPAWN2 for readable materialization feedback.
3. SPAWN3 when missions need actual **portal + edge mixing / weighted sectors**, beyond today's basic all-edges checkbox.
4. SPAWN4 when Level011 or later maps should present independently paced three-lane attacks.
5. SPAWN5 only after playtesting the earlier patterns.

Performance: reuse the existing cached source grid and RNG; no per-ship A* start, polling hidden portals each frame, per-edge Marker2D, per-bullet nodes, or fully rebuilding navigation on reveal. Maintain explicit authoring errors and `MaxWaveSpawnAttemptsPerFrame`/`MaxActiveEnemies` guardrails. Avoid changing wave recipes, visuals, map points and mission outcomes without separate authorization.

**Related:** [Spawning README](README.md), [Map edge notes](MapEdges/README.md), [Portal design](../../WORLD/PORTALS/README.md), [Level011](../../LEVELS/Level011/README.md), [Wave authoring](../../LEVELS/Waves/Authoring.md), [Wave Editor plan](../../addons/wave_editor/PLAN.md), [Backlog](../../NOTES/IMPLEMENTATION_BACKLOG.md).
