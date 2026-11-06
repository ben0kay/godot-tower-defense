## SPAWN2 — shared portal materialization — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Every registered START portal now forms on fresh deployment; Wave N portals use the same Dormant → Forming → Open presentation on their accepted authored wave. No Level011 map or wave data changed. A 2.2s pausable tween drives a localized violet spiral shader, growing segmented ring, art/label reveal, inward GPU curls and concentrated ignition/ripple. No fullscreen effects, audio assets or idle CPU animation loop were added. Existing 120°/s rotation and 48-particle outward per-spawn rim bursts remain the Open-state presentation.

`IsSpawnActive` means legally activated; `IsSpawnReady` additionally requires Open. Required requests retain their selected ship/count and sleep the wave scheduler during formation. A readiness event updates only that source's eligibility slot and wakes the scheduler; random requests ignore unfinished portals without consuming visual RNG. No terrain rebuild, route reassignment or new path queries. Between-wave restore uses explicit checkpoint context (including completed-wave count zero), snaps due portals Open and suppresses opening replay/warnings.

Tune `WORLD/PORTALS/EnemyPortal/Visuals/DefaultPortalMaterializationStyle.tres`: duration 2.2s, optional START stagger 0–0.2s, vortex/ring/flash, reduced motion, particles (72 inward + 24 ignition defaults), and at most 6 simultaneous visible particle pairs. Overflow/offscreen openings use the same timeline with shader/pose only; offscreen exit stops particle emission. Opening nodes hide and tween/budget slots release on completion, reset or unload. Disabled/reduced motion retains mechanical timing with simpler ring/fade and no opening particles. Later-wave activations can request one batched sector banner through the existing alert queue; START never alerts or interrupts mission intel. No suitable approved warp sound was present/wired; future sound may hook opening start/ignition without owning readiness.

**Minimal validation:** focused source/scene-reference and readiness fixture review plus one minimal Godot 4.6.3 headless shader parse (passed); .NET unavailable. Updated `PortalActivationChecks` covers forming exclusion/RNG retention, immediate START wait/resume, late activation, once-only eligibility, no navigation/assignment changes and wave-zero restore without replay. Fixture not executed; visual/FPS acceptance remains with owner. Check fresh multi-portal load, immediate START, pause/resume halfway, later-wave activation, window/camera culling, reduced effects, unload/retry and a fresh checkpoint.

---

# Enemy Spawn Sources and Dynamic Portals — implementation plan

**Status: SPAWN0 / SPAWN1 Source Implemented / Awaiting Owner Validation (2026-11-06). SPAWN2 is Source Implemented / Awaiting Owner Validation; SPAWN2.5 (per-spawn pulse + smoke) and SPAWN3–5 remain PLANNED. The original cinematic specification is retained below.**

SPAWN0 source identity/baseline contract is documented in README.md; an isolated owner-run fixture covers all edges and legacy Both preference. SPAWN1 now implements START/Wave N visibility/eligibility, route reservation, authoring errors and exact-number checkpoint restoration. Existing Level011 scenes/waves are unchanged. The separately requested slow rotation and bounded purple per-spawn particles are included; **they are not the SPAWN2 cinematic opening**. Existing START portals are visible instantly until SPAWN2 is implemented. C# fixtures are authored but unrun (.NET unavailable); gameplay/visual acceptance is pending.

**Historical design below:** Audited the current `dev` implementation. This is a future work plan for Codex/normal implementation passes, **not permission to change Level011 waves, source settings, combat, saves or map layout now**. Work on `dev`, read `NOTES/CodePreferences.md`, root `README.md`, and the owning files before each pass.

## 0. Baseline — what already works (do not duplicate)

- `LEVELS/Waves/WaveManager.cs`: one finite manual wave scheduler, **ordered batches**. A batch is fully scheduled before advancing to the next; `After` is the gap after the last successful spawn, not after the last enemy dies. Successful spawns are wave-owned; victory waits for planned, alive and reserved carrier children to reach zero.
- `LEVELS/Waves/WaveCatalog.cs`: exact largest-remainder weighted enemy quotas, seeded shuffle **within each batch**; ordering of batches is preserved. `WaveEnemyEntry.Portal` can select an exact level-root-relative portal. Blank selects an eligible random portal; failed required selection retries instead of silently falling back.
- `ENEMIES/Spawning/SpawnManager.cs`: source selection and actual enemy creation live here, not in physical portals. A wave without a required portal uniformly samples eligible authored portals; `SpawnMode.MapEdges` samples reachable permitted boundary-cell centres; `SpawnMode.Both` prefers portals and falls back to edges only when no portal is eligible. F2's current batch menu uses the same valid portal selection policy with an independent RNG; older round-robin descriptions refer to the retired F2 legacy cycler.
- `LEVELS/LevelConfig.cs`: `SpawnMode` (Portals / MapEdges / Both), `AllowedEdges` flags (Top/Bottom/Left/Right) and `ActivePortals` (currently the registered map paths, not a wave-activation schedule).
- `WORLD/PORTALS/EnemyPortal/EnemyPortal.cs`: `SpawnExit`, optional required checkpoint, visual size override, energy pulse. No spawn clocks, activation-wave Inspector property or materialization state.
- `ENEMIES/Spawning/MapEdges/README.md` is **historical** when it calls all map-edge spawning future: basic edge enumeration/selection already exists. Per-wave edge targeting and authorable edge sectors are future.
- `LEVELS/Level011/Level011Waves.tres` Wave 1 has three **sequential 18-ship batches**: Portal1 (interval 0.65, after 2.0), Portal2 (0.60, after 2.0), Portal3 (0.55, after 0). Each batch carries exactly 12 `basic_mk1` and 6 `scout_mk1`, deterministically shuffled. It is not random portal sequence. **Level011 now authors Waves 1–10** (updated 2026-11-06); Wave 6 exists, although its three existing portal activation settings have not been modified.
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

## Phase SPAWN2 — shared cinematic dimensional warp-in for **ALL** portals (Codex implementation specification)

**Status: Source Implemented / Awaiting Owner Validation (2026-11-06). SPAWN1 is already source implemented.** This supersedes the earlier faint fade/short future-only effect proposal. Author requests a **striking, polished violet dimensional warp opening with circular swirls, suction, glowing rings, curling particles and an ignition pulse**. The **same effect and state machine must run whenever any portal first appears**, including all START portals as the level loads, not only late-wave portals. No portal should simply pop into view as fully formed on fresh deployment.

**Read before coding:** `NOTES/CodePreferences.md`, root README, this plan, `WORLD/PORTALS/EnemyPortal/{EnemyPortal.cs,EnemyPortal.tscn,EnemyPortal.Presentation.cs,README.md}`, `ENEMIES/Spawning/{SpawnManager.cs,SpawnManager.Activation.cs,README.md}`, `LEVELS/Waves/WaveManager.cs`, `WORLD/VISUALS/{EnergyFieldVisual.cs,Shaders/EnergyField.gdshader,Presets/PortalEnergy.tres}`, current `PortalRotation.gdshader`, `TESTS/Integration/PortalActivationChecks.cs`, and level/world/mission setup + checkpoint restore paths. Inspect actual node paths and graphics capabilities before changes. Implement on `dev`, keep shared ownership, update docs, commit/push and perform **only lightweight source/compiler checks**; leave extended Godot visual/performance validation to the owner.

### User-facing appearance contract

- A portal has one cohesive materialization presentation whether it is placed at `AppearAtWave=0` (**START**, deployment) or `AppearAtWave=N` (specific accepted wave). This is **one reusable effect**, not separate startup/new-portal implementations. All currently registered START portals should **warp into existence together as the level loads**, with a small configurable deterministic stagger (default 0; optional 0.10–0.20 seconds per portal for readability if numerous). They must not be visible as finished portals for a frame before their effect starts.
- **Important preserved SPAWN1 semantics:** `AppearAtWave=0` means **level deployment**. `AppearAtWave=1` means **when Wave 1 is accepted**, not level load; it uses the identical materialization effect then. For a portal meant to appear immediately on loading, set **0 (START)**. Do not silently reinterpret 1 or shift activation earlier, alter authored waves, or rewrite level settings.
- Future Wave N portals remain fully hidden before their accepted activation. When Wave N starts, they play **the same warp-in from nothing**, not the old instant show. A portal becomes ready to release enemies **after** the opening/ignition (see spawn readiness below), and stays open thereafter. The current 120°/s idle portal-art rotation and **48-particle/0.7s outward rim spawn burst** stay intact once the portal is open; do not replace or accidentally multiply those per-shot effects.
- On returning from a between-wave checkpoint, **already-active** portals display instantly in their ordinary idle state (do **not** replay multiple old materializations, trigger alerts or spawn particles). Future dormant portals remain hidden. On a genuinely fresh mission/retry/redeployment, START portals materialize anew. If a future persistent mid-wave save is introduced, explicitly store any partial/finished transition or resume safely rather than inferring from completed waves.
- A portal is only **visually** materializing; it is not newly creating map terrain/collisions, altering `SpawnExit`, changing reserved footprint, teleporting routes or changing its fixed Citadel/checkpoint assignment. Editor preview stays fully visible and editable without runtime effects.

### Art direction and one-shot cinematic timeline (default ~2.0–2.4 seconds; Inspector adjustable)

Create a cohesive **top-down 2D** sci-fi dimensional tear, preserving the existing elegant purple/indigo identity and metallic `PortalViolet.svg`. Prefer a dark violet outer rim, bright lilac spirals, saturated purple core, brief nearly white-lavender highlights and soft magenta sparks. The effect should read clearly at actual gameplay camera zoom, including over pale terrain, without a giant screen-space glow wash.

1. **0–20% — Space distortion / seed:** portal starts visually absent. A tiny bright violet singularity and thin broken arc appear at the exact portal centre. Faint transparent concentric ripples and wisps hint at twisting space; **particles spiral inward**, not merely explode outward. The completed portal image must not be visible at full size yet.
2. **20–55% — Vortex winds up:** multiple curved, offset spirals orbit the forming core at different speeds; energy trails pull from a modest radius inward toward the centre. An irregular/segmented ring grows rapidly while its opacity ramps up. The core deepens from dark purple to luminous violet with subtle turbulence. Metallic artwork becomes visible through a controlled scale/alpha reveal instead of popping on.
3. **55–82% — Ring locks / dimensional breach:** the swirl concentrates into a defined rotating disk. The ring reaches its authored final scale, shimmering energy fractures close around it, and a few brighter sparks sweep the circumference. Use a **short concentrated violet-white ignition flash** at ~75–85%, not full-screen bloom. A small translucent circular ripple expands outward and fades.
4. **82–100% — Stabilize:** inward curls ease, core intensity returns to the existing idle shader appearance; metallic rim and label settle at their true scale. Opening VFX emitters finish and stop. Portal transitions to **Open/Ready** with the established slow rotation and normal per-spawn purple sparks. No persistent extra SPAWN2 processing or second duplicate energy field.

**Visual composition proposal (adapt to repository):** reuse the existing `Visuals/PortalArt` + `Visuals/EnergyField` and the portal's `SpawnParticles` for normal shots; add a dedicated **`Materialization`** child owned by `EnemyPortal` with (a) **one small shader-backed `Sprite2D` or `Polygon2D`** for the spiral/vortex/distortion, (b) one or two lightweight ring/ripple visuals, and (c) **one or two short-lived `GPUParticles2D`** for inward-curving wisps and a modest outward ignition spark shower. These VFX components are shared by every instance of the same portal scene and reused for its one opening; no instancing new particle nodes per enemy. Use scripted shaders / procedural radial curves if needed, not downloaded animations, stock footage, or enormous high-resolution textures. Art direction must make the effect visibly **swirly**, dimensional and dramatic, not just a scale/alpha tween.

**Particle specifics (provisional tunable Inspector/resource presets, not fixed mandated counts):** a ~64–96-particle inward curling cloud plus a ~16–32-particle short ignition burst **per visible opening** at high quality, with short lifetimes (~0.5–1.1s), lilac/magenta/purple colours, fading ends and zero gravity. Achieve curving trajectories with GPUParticles2D emission radius + negative radial acceleration, tangential acceleration or controlled shader motion; don't use individual CPU particle nodes. Use a tiny shared blurred gradient texture rather than allocating fresh textures per portal. Limit world-space radii (illustratively ~50–110 px at SizeMultiplier 1) so this is a localized portal event, not a map-wide explosion. The portal's `SizeMultiplier` scales visual presentation consistently but **not** logical footprint/exit/spawn spread rules beyond the existing contract.

### Timing, state, readiness and ownership (important)

**Single runtime state machine** per portal, e.g. `Dormant -> Forming -> Open`. SPAWN1's `IsSpawnActive` currently means wave-time activated; SPAWN2 must **separate logical activation from visual readiness**. A newly activated portal is *registered and legally due* but **not eligible to deliver enemies until the opening completes**. Do not abuse `AppearAtWave` or `LevelConfig.ActivePortals` to achieve the delay.

- **Fresh level load:** hook once into the actual post-configuration runtime setup after portal nodes are ready, but **before the first displayed gameplay frame** start the presentation for every due START portal. At initialization, force the finished `Visuals` state and idle spin to a zero/hidden opening pose; the vignette/spiral may then reveal it smoothly. Do not rely on an arbitrary `CallDeferred` that allows one fully formed frame. Start wave UI stays manually controlled and the initial portals animate during preparation.
- **Wave N:** SPAWN1 currently activates due portals at the accepted wave start before immediate `Advance(0,1)`. Trigger opening there, but prevent immediate spawn through an unfinished portal. Prefer an explicit `Portal.IsSpawnReady` gate in the existing spawn eligibility code. Required-portal requests should **wait safely** until this specific portal is Open, without consuming the selected enemy entry, decrementing the scheduled count or rerouting to an unrelated source. **Random** portal selection must ignore Forming portals and sample other ready eligible sources; if none are ready, wait instead of treating it as a permanently broken map. Keep existing blocked-source retries, bounded attempt budgets, `WaveEnemyCreated` once-only ownership and deterministic seeded selection. Do not consume wave RNG for purely visual timing, particles or portal readiness polls. Treat Forming as a recoverable wait/status rather than a hard SETUP ERROR or long-term `SpawnBlocked` failure. Avoid spin-waiting per frame for all hidden sources.
- **Readiness completion:** a single one-shot signal or tween completion updates just the affected source eligibility, exactly once. Reuse SPAWN1's eligibility cache / revision; no fabricated `TerrainRebuilt`, full flood, bulk A* recomputation, source reassignment or expensive per-frame recompute. If the wave's immediate first spawn requires that portal, it may be briefly queued until its opening completes; the next eligible attempt proceeds normally.
- **Cold start vs restore:** current `ConfigurePortalActivation` calls `RestorePortalActivation(waves, 0)`; checkpoint `RestorePortalActivation` reconstructs state. Introduce an explicit caller-provided context such as `FreshDeployment` vs `CheckpointRestore`, so there is no ambiguity between a fresh mission at completed-wave=0 and a resumed saved mission with completed-wave=0. Do not replay VFX from a plain `ResetActivation` call, editor preview or runtime source cache refresh.
- **Pause and cleanup:** the **visual formation clock and spawn readiness must pause together**. Current shader `TIME`-driven idle rotation may be independent of pause; don't use uncontrolled global `TIME` to drive the one-shot formation progress if gameplay is paused. Use one pause-respecting tween or event-driven elapsed/normalized parameter (per-instance material/uniform), with pausable particles and no heavyweight per-portal `_Process` loop. Cancel safely on unload/scene reentry; kill tweens and stop emissions. Final visuals remain correct if disabled/degraded due to rendering budget or reduced-motion mode.

### Inspector tuning, performance and presentation

- Prefer a shared `PortalMaterializationStyle` resource or equivalent preset owned under `WORLD/PORTALS/EnemyPortal/Visuals/`, with sane defaults: `Enabled=true`, `DurationSeconds=2.2`, `VortexIntensity`, `RingRadius`, `ParticleAmount`, `FlashStrength`, `StartStaggerSeconds=0`, and `WarningEnabled=true` **for later-wave activations only**. Make values safely clamped/finite. Each placed portal can override common style if the established art/Inspector pattern supports this cleanly; avoid a large global framework.
- Reuse the existing purple `PortalEnergy.tres`, GPU energy field, rotation shader and spawn-particle node as the Open-state look. Prevent per-portal material uniform changes from bleeding into other portals by cloning shader materials where needed **once per instance**, not per frame. Preserve the label and correct draw/z-order (under ships, ring behind/around art, particles above rim), so no source appears to be teleporting from an unrelated tile.
- **Visually impressive, but bounded:** on a map with many START portals, permit them all to animate at the same logical time, while using a capped **visible high-detail VFX budget** (e.g. up to 4–6 simultaneous full particle systems); overflow/offscreen portals still follow the same timeline with cheaper swirl/ring or minimal/cached animation and become Ready on schedule. This is *quality scaling*, not source activation throttling. Keep offscreen GPU work minimized, never make players wait for effects on an unseen portal or force continuous replays when it enters view.
- No full-screen bloom, screen-read distortion, lights, huge independent transparent quads, 4K per-portal textures, per-enemy particle nodes, per-portal physics scanners, extra world navigation queries or per-frame allocated arrays. Allow clear low/disabled effects profile and reduced-motion behaviour: **same mechanical readiness timing**, simple short ring/fade instead of a complex swirl. Cap particle/ripple resources and stop all materialization processing after Open.
- For START portals on load, **no stacked 'NEW PORTAL' alerts**, no forced camera pans and no interruption of mission-intel/narrator UI. A later-wave activation may use **one** batched sector-warning notification through the existing alert system if multiple portals open together, but must not block gameplay or spam one banner per portal. Optional sound (soft spatial rising warp hum + restrained opening bass transient) should be documented as an asset hook and only wired if an existing suitable clip exists; **do not invent or silently import unapproved audio assets**.

### Codex-focused delivery and acceptance

Files expected to be touched (after inspection): `WORLD/PORTALS/EnemyPortal/{EnemyPortal.cs,EnemyPortal.tscn,README.md}` plus one small companion presentation script/resource/shader/preset under `WORLD/PORTALS/EnemyPortal/Visuals/`; `ENEMIES/Spawning/SpawnManager.Activation.cs` for due/ready separation and event-driven refresh; `LEVELS/Waves/WaveManager.cs` only for the minimal ready-gate/wait integration if necessary. Ensure generic F2/random-wave eligibility is consistent. Prefer reuse of `WORLD/VISUALS/EnergyFieldVisual` and existing visual effects over duplicating them. Update `TESTS/Integration/PortalActivationChecks.cs`, owning READMEs and backlog. **Do not edit authored level maps, wave counts/types, or enemy/tower balance** for this phase.

Focused acceptance (minimal test code + owner editor visual review):
1. Fresh Level001/Level011 deployment with one/multiple START portals: **every START portal visibly forms** from absent to swirling/ringed/radiant open state; none pop in already completed. All show one consistent design and stop materialization emitters at the end. Editor scene view remains normally editable.
2. A Wave 1 portal stays dormant during pre-wave deployment and plays the **same** effect when Wave 1 is accepted; a Wave 6 portal remains hidden until accepted Wave 6, then plays once. Default `AppearAtWave=0` continues to mean START.
3. Pressing Start Wave quickly or starting a required Wave N batch immediately cannot spawn from an unfinished portal. The selected ship is **preserved**; it launches after the portal is Ready without consuming extra RNG, duplicating ship ownership or deadlocking. Ready portals can still spawn while a different portal is forming when allowed by the authored selector.
4. Pause halfway through opening: swirl/ring/particle progression and readiness freeze together; resume finishes once. Unload/cancel mid-effect leaves no leaked tween, particles or stale callbacks.
5. Fresh retry plays START effects again, but between-wave checkpoint restore displays previously active portals immediately Open, leaves not-yet-due portals Dormant, avoids stray alerts, and preserves save compatibility.
6. START with 3+ portals and a stress layout with many portals remains responsive; GPU bursts are capped/cullable without changing materialization completion or gameplay state. `SizeMultiplier`, ongoing slow rotation, regular per-spawn curl particles, required checkpoint routing, F3, F2, fixed Citadel assignment and future portal reservations remain unchanged.
7. No transient full-bright frame, duplicate one-shots, large offscreen effects, runaway allocation, unexpected initial spawn, level-load SETUP ERROR, mission-narrator overlap or modification to authored Level011 data.

**Delivery note:** This is a design/implementation handoff, not permission to mark SPAWN2 complete until its source and visuals actually exist and owner visual acceptance occurs.

## Phase SPAWN2.5 — per-spawn portal expansion + violet dimensional smoke (Codex handoff, APPROVED PLAN / NOT IMPLEMENTED)

**Order:** Implement **after SPAWN2** (already source implemented, opening duration restored to 2.2s) and **before SPAWN3**, the next planned spawning phase. SPAWN3 adds portal/map-edge mixing; SPAWN4 later adds concurrent attack groups. SPAWN2.5 is **cosmetic only**: a successful enemy emerging from an already-Open portal should make the portal swell gently and exhale several large smoky purple puffs, alongside its existing outward particle burst. This is **not** a replay of SPAWN2's one-time dimensional materialization and must not alter spawn readiness, success, timing, waves, AI, navigation, the map or saves.

### Starting point (read before editing)

- Read `NOTES/CodePreferences.md`, root README, `WORLD/PORTALS/EnemyPortal/README.md`, and this spawning plan on `dev`; check `AGENTS.md` if present.
- `SpawnManager.cs` already calls `chosenPortal?.PlaySpawnFeedback()` **after successful creation** for both wave spawns and F2 portal spawns. Failed spawn attempts/edge spawns must never trigger this.
- `EnemyPortal.Presentation.cs` already owns `PlaySpawnFeedback`, a one-per-750ms maximum reusable **48-particle violet rim burst**, on-screen check, offscreen shutdown, and the existing `EnergyFieldVisual.PlaySpawnPulse()` brightness pulse. `EnemyPortal.Materialization.cs` owns the separate Dormant/Forming/Open animation, readiness and its opening-only inward/ignition particles.
- `EnemyPortal.tscn` holds the rotating `Visuals/PortalArt`, `EnergyField`, `SpawnParticles`, `OnScreen`, and `Materialization`. `SizeMultiplier` scales `Visuals` while `SpawnExit` stays independent. Do **not** duplicate the callback, add a new spawn service, or route regular spawn visual requests through `BeginMaterialization()`.

### A. Expand and recover the open portal on a successful spawn

- Add a brief **soft, outward breathing pulse** to the already-open portal: roughly **1.0 → 1.12 → 1.0** of its resting presentation size over **0.7–0.9 seconds** by default, with an eased expansion followed by slower settling. Subtle glow/rim expansion can accompany it. Keep the existing idle rotation, energy brightness pulse and lilac rim spark burst.
- Pulse only **portal artwork/its appropriate visual child or a dedicated centred pulse layer**; never scale the portal root, `Visuals` container, `SpawnExit`, footprint, route checkpoint, labels, spawning offsets or enemy sprite. A scaled overlay must have a correct centred pivot. Respect existing `SizeMultiplier` and avoid accumulating scaling or interfering with SPAWN2's opening pose.
- Keep a **single bounded per-portal active pulse**, triggered by successful spawns, coalescing or gracefully extending repeated events rather than restarting every rapid spawn indefinitely. Always return exactly to the stored resting pose. Pause, offscreen transition, scene exit, fresh opening, and restore must cancel/reset cosmetic motion without changing availability.
- Provisional exported controls: enable toggle, expansion fraction (~0.12), pulse duration (~0.8s), brightness strength. Use the feature's existing resource/export pattern, not project-wide hardcoded magic values.

### B. Large billowing purple dimensional clouds (2D shader + bounded GPU puffs)

- Add one reusable `GPUParticles2D`-style emitter (or comparable single bounded, pooled 2D effect) under portal visuals, fired from the **same accepted-spawn event**. Default about **4–6 large puffs per eligible burst**, each roughly **24–60px diameter at native portal scale**, drifting outward from the rim/exit with small tangential swirl. Expand and disperse for approximately **0.9–1.4 seconds**, then dissolve fully; never linger as fog across the map.
- Use a **lightweight shared CanvasItem shader** or small noise-based puff texture/shader combination with soft feathered alpha, low-frequency irregular edges and subtle animated internal billows, so it reads as **puffy smoky vapour**, not identical plain circles. Colour: layered dark violet/plum with lavender/magenta illuminated edges, consistent with the current portal art; semi-transparent enough that emerging ships stay legible. Some puffs may bloom behind the ship, with lighter wisps around its sides.
- Distinguish visual layers: existing 48 particles remain small fast outward flecks; new clouds are **larger, slower, diffuse and rolling**. Keep the ordinary energy pulse. Do not use actual volumetric 3D fog or screen-space/postprocess blur. Prefer at most ~2–3 cheap procedural noise octaves or a preauthored noise texture; no expensive screen reads, dynamic lights, per-puff nodes, physics collision or per-frame CPU-driven cloud geometry.
- Expose smoke enable, puff count, size/spread, lifespan, outward speed, opacity and tint via a **portal-owned preset resource** (for example `Visuals/PortalSpawnFeedbackStyle.cs` + `DefaultPortalSpawnFeedbackStyle.tres`) or equivalent existing Inspector-authoring convention. New shader/preset belong in `WORLD/PORTALS/EnemyPortal/Visuals/`. Default look should be attractive at normal gameplay zoom without flooding the map.
- Native cloud emission must honour `SizeMultiplier`, respect camera/on-screen culling and stop on screen exit. Existing SPAWN2 materialization visuals are unaffected.

### C. Shared event lifecycle, GPU bounds and gameplay invariants

- Integrate under the **existing** `PlaySpawnFeedback` only. Use one visual cooldown/coalescing rule (retain at least the existing 750ms limiter for costly bursts) so dense Level011 traffic cannot cause unlimited puffs or independent tweens. If burst budget is unavailable/offscreen, skip cosmetic smoke but **never suppress an enemy spawn**. Energy brightness may retain its existing lighter per-spawn behaviour.
- If Godot pause does not freeze a GPU shader/particle effect automatically, explicitly manage time/particle speed where practical so new effects freeze and resume without restart. Ensure each live pulse uses a bound/paused tween (or equivalent) and resets on unload/restart; no animation should leak into a checkpoint restore, disappear/reappear corruptly, or continue while portal is Dormant/Forming.
- Aim for stable **60 FPS on the user's GTX 1080** as an optimization target, *not* an unmeasured guarantee. All effects are local, short-lived, retained per portal, culled when hidden and dormant when not triggered. No new CPU `_Process` loop, per-ship attachment, scene-wide enemy scan, multiplayer replication, source graph rebuild or gameplay timer.
- Keep SPAWN2's original **2.2s portal opening**, 120°/s ongoing artwork rotation, existing 48 flecks, SPAWN1 activation/checkpoint readiness, Level011's Portal4 wave-six appearance, and every existing mission and wave resource unchanged.

### D. Codex delivery and minimal owner acceptance

Implement this as a **single focused SPAWN2.5 commit on `dev`**, with code/art/preset changes in `WORLD/PORTALS/EnemyPortal/` and short updated README/backlog status. Do not advance SPAWN3–5, do not change gameplay rules or adjust opening duration. Lightweight source/resource checks only; if readily practical, one basic build or shader parse, no broad testing.

Owner checks in Godot: watch an Open portal when one ship emerges, then a quick stream of ships; confirm obvious gentle expansion and large smoky lavender clouds **in addition to** the existing 48 sparks, without hiding the spawned ship. Check all portals including late-appearing Level011 Portal4, portal scaling, F2, pause/resume, offscreen pan, rapid waves, fresh retry and between-wave restore. Check no growth accumulation, duplicate opening animation, stuck glow, runaway smoke or FPS spikes. Mark **Source Implemented / Awaiting Owner Validation** only after source delivery, not now.

---

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

1. **SPAWN0/1 already source implemented**: START/Wave N activation, source reservation and eligibility now exist; retain them.
2. **Validate implemented SPAWN2 in Godot**: spectacular *shared* startup and Wave N dimensional portal warp-in, a pausable one-shot transition and spawn-readiness gating; not just a future-only fade.
3. **SPAWN2.5 first:** per-spawn portal expansion and dimensional smoke, using the existing successful-spawn feedback callback; visual-only, after opening and before source-policy work.
4. SPAWN3 when missions need actual **portal + edge mixing / weighted sectors**, beyond today's basic all-edges checkbox.
5. SPAWN4 when Level011 or later maps should present independently paced three-lane attacks.
6. SPAWN5 only after playtesting the earlier patterns.

Performance: reuse the existing cached source grid and RNG; no per-ship A* start, polling hidden portals each frame, per-edge Marker2D, per-bullet nodes, or fully rebuilding navigation on reveal. Maintain explicit authoring errors and `MaxWaveSpawnAttemptsPerFrame`/`MaxActiveEnemies` guardrails. Avoid changing wave recipes, visuals, map points and mission outcomes without separate authorization.

**Related:** [Spawning README](README.md), [Map edge notes](MapEdges/README.md), [Portal design](../../WORLD/PORTALS/README.md), [Level011](../../LEVELS/Level011/README.md), [Wave authoring](../../LEVELS/Waves/Authoring.md), [Wave Editor plan](../../addons/wave_editor/PLAN.md), [Backlog](../../NOTES/IMPLEMENTATION_BACKLOG.md).
