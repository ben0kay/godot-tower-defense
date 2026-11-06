## Organic pulse and lingering wave smoke — 2026-11-06
Source implemented; owner visual acceptance pending. Pulse strength/duration, spark count/angle and feedback spacing now vary using a private cosmetic RNG (`Irregularity`, default 40%), independent of wave RNG. Small pulses can occur closer together; a busy pulse finishes rather than continually restarting.

Open portals slowly emit large, spreading violet clouds throughout active waves, independent of per-ship bursts. Defaults: 18 live puffs maximum per portal, 110px initial size with variation, 5s life, 18px/s outward drift and gentle swirl. Clouds grow and billow with independent shader offsets. Wave completion stops new smoke and lets existing clouds dissolve; pause freezes it, screen exit hides/resets it, screen re-entry resumes only during a wave, and restore never restarts wave smoke. Tune the existing spawn-feedback preset. Opening timing/gameplay are unchanged. Source/lifecycle review only; .NET unavailable.

---

## SPAWN2.5 — source implemented / awaiting owner validation
Successful Open-portal spawns now trigger a soft 12% artwork swell over 0.8s and six large billowing violet GPU puffs, alongside existing sparks/energy feedback. Tune `Visuals/DefaultPortalSpawnFeedbackStyle.tres`: pulse enable/size/duration/brightness, smoke enable/count/size/spread/lifetime/outward speed/swirl/opacity/tint. Only the centred artwork scales; labels, exit, footprint and `SizeMultiplier` remain independent.

A single accepted-spawn cooldown (at least 750ms; default 1.2s to let smoke drain) coalesces rapid traffic. One retained eight-puff maximum emitter uses two noise octaves, no screen reads or CPU animation loop; particle age drives billows/dissolve and explicit pause speed freezes simulation. Offscreen/opening/restore/unload resets pulse and hides smoke. The 2.2s opening, 120°/s art rotation, readiness, waves, navigation and saves are unchanged.

Source/resource review and native Godot shader parse only; .NET unavailable. Owner to inspect single/rapid spawns, scale, pause, offscreen return and checkpoint restore.

---

## Restored slower portal opening — 2026-11-06

Restored the shared materialization animation to its original 2.2 seconds, including authored preset and code defaults/fallback. Level011 Wave 6 still leads with its existing Portal4 ship, so it launches on readiness without waiting for a later shuffled request. Drone motion and widget changes remain. Focused source review only; owner visual acceptance pending.

---

## Faster opening and immediate Level011 Wave 6 debut — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Default shared materialization duration is halved from 2.2s to 1.1s; opening particle lifetimes scale down to drain before completion. Enemy delivery still waits for the genuine Open/readiness event, then resumes on the scheduler's next tick without a separate post-opening timer. Checkpoint/no-replay and pause semantics remain.

Level011 Wave 6 formerly shuffled four Portal4 Reinforced Drones among the first 54 ships, so a formed portal could wait for its first selected entry. One existing Reinforced Drone is now a guaranteed opening batch from Portal4, followed 0.5s later by the remaining 53 ships in that original batch (three remaining Reinforced Drones). Total wave count stays 216; Portal4 stays 26 ships; all other enemy quotas, portal assignments, later batch gaps and other waves are unchanged. The first ship waits safely through opening and launches immediately when ready. Source/resource quota review only; owner in-engine acceptance pending.

---

## SPAWN2 — shared portal materialization — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Every registered START portal now forms on fresh deployment; Wave N portals use the same Dormant → Forming → Open presentation on their accepted authored wave. No Level011 map or wave data changed. A 2.2s pausable tween drives a localized violet spiral shader, growing segmented ring, art/label reveal, inward GPU curls and concentrated ignition/ripple. No fullscreen effects, audio assets or idle CPU animation loop were added. Existing 120°/s rotation and 48-particle outward per-spawn rim bursts remain the Open-state presentation.

`IsSpawnActive` means legally activated; `IsSpawnReady` additionally requires Open. Required requests retain their selected ship/count and sleep the wave scheduler during formation. A readiness event updates only that source's eligibility slot and wakes the scheduler; random requests ignore unfinished portals without consuming visual RNG. No terrain rebuild, route reassignment or new path queries. Between-wave restore uses explicit checkpoint context (including completed-wave count zero), snaps due portals Open and suppresses opening replay/warnings.

Tune `WORLD/PORTALS/EnemyPortal/Visuals/DefaultPortalMaterializationStyle.tres`: duration 2.2s, optional START stagger 0–0.2s, vortex/ring/flash, reduced motion, particles (72 inward + 24 ignition defaults), and at most 6 simultaneous visible particle pairs. Overflow/offscreen openings use the same timeline with shader/pose only; offscreen exit stops particle emission. Opening nodes hide and tween/budget slots release on completion, reset or unload. Disabled/reduced motion retains mechanical timing with simpler ring/fade and no opening particles. Later-wave activations can request one batched sector banner through the existing alert queue; START never alerts or interrupts mission intel. No suitable approved warp sound was present/wired; future sound may hook opening start/ignition without owning readiness.

**Minimal validation:** focused source/scene-reference and readiness fixture review plus one minimal Godot 4.6.3 headless shader parse (passed); .NET unavailable. Updated `PortalActivationChecks` covers forming exclusion/RNG retention, immediate START wait/resume, late activation, once-only eligibility, no navigation/assignment changes and wave-zero restore without replay. Fixture not executed; visual/FPS acceptance remains with owner. Check fresh multi-portal load, immediate START, pause/resume halfway, later-wave activation, window/camera culling, reduced effects, unload/retry and a fresh checkpoint.

---

## Faster spin and visible rim bursts — 2026-11-06

**Source Implemented / Awaiting Owner Visual Validation.** Portal artwork defaults to 120 degrees/sec (one revolution every 3 seconds), adjustable from 0–360 in Portal Feedback. Successful spawns reuse one 48-particle / 0.7-second lilac-white GPU emitter. All birth positions come from a shared immutable 64-point float texture at radius 46px, matching the SVG rim, with positive radial velocity (90–140px/sec) and acceleration pushing sparks outward. Larger 5.6–9.6px soft flecks and a brighter fade ramp improve visibility outside the portal. Visuals scaling also scales the rim geometry. Existing 750ms burst cap, 30Hz GPU simulation, offscreen skip, pause guards and culling remain; no enemy emitter nodes or CPU animation loop. SPAWN2 materialization remains planned.

Source/scene sanity review only; .NET unavailable. Owner: inspect fast rotation and clearly outward rim bursts at normal zoom, including larger portals and dense waves.

---

## SPAWN2 cinematic warp-in — revised Codex implementation handoff (2026-11-06)

**Historical design handoff: now source implemented as described above; owner acceptance pending.** [The complete SPAWN2 art/logic specification](../../../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md#phase-spawn2--shared-cinematic-dimensional-warp-in-for-all-portals-codex-implementation-specification) now requires **every portal to animate in with the same impressive purple/lilac dimensional vortex, curved inward spiral particles, forming ring, bright ignition pulse and settling idle shader**. All `AppearAtWave=0` (START) portals **materialize together when a fresh level loads**, not simply appear fully formed; Wave N portals use the identical effect when their authored wave starts. `AppearAtWave=1` still means Wave 1 begins, so use 0 for loading-screen/start-of-level appearance. Restore from a between-wave checkpoint snaps already-open portals to Open without replaying the effect.

Codex must preserve SPAWN1's registered source identity/route protection, differentiate `Dormant / Forming / Open` and gate enemy spawns until a portal finishes forming, while keeping pause and effect progress synchronized. Reuse existing rotating PortalArt, EnergyField and **ordinary** per-spawn 48-particle bursts. The cinematic materialization is a separate **one-shot** (proposal ~2.2 s) with bounded, cullable GPU particle swirls and locally rendered shader/ring effects; it must be striking on-screen while preserving 60 FPS as an optimization target. No implementation or Godot runtime visual acceptance has occurred yet.

---

## START / Wave N activation and subtle spawn visuals

**Source Implemented / Awaiting Owner Validation.**

Select a placed portal → **Wave Activation (0 = START) → Appear At Wave**. `0` preserves deployment visibility; `6` reveals on accepted authored Wave 6. Keep its path registered in the level ActivePortals list and use Portals/Both spawn mode. Required batches must not name it earlier. Missing future waves warn without auto-authoring a wave. Editor previews always show the portal; runtime hides only Visuals, preserving footprint/exit and fixed route protection.

Under **Portal Feedback**, `RotationDegreesPerSecond` defaults to **120°/s** (3-second revolution); `0` disables it. A small vertex shader rotates the existing art only, keeping labels, marker, footprint and routes fixed. It shares the existing energy overlay's GPU TIME convention; cosmetic shader motion may continue during pause. No CPU animation callback is added.

`SpawnParticlesEnabled` controls a reusable **48-particle**, **0.7-second** purple GPU burst on successful spawns. Radial velocity and tangential acceleration produce outward curling sparks in all directions. Burst cadence is capped at one per **750ms per portal**, with a 30Hz GPU simulation, tiny soft texture and no lights, collision, screen reads or persistent trail. No per-enemy emitter is created. Hidden/offscreen portals skip new feedback; screen exit stops emission. The existing energy pulse remains. Emitter amount/lifetime/motion/ramp are editable in EnemyPortal.tscn. GPU cost is bounded, not measured or guaranteed zero.

Activation is instant gameplay availability; there is **no SPAWN2 materialization delay or tactical warning** in this pass. Owner: check visibility at START/Wave N and after loading, slow rotation, purple curls during dense traffic, pause/culling and normal portal size overrides.

---

## VE2 visual energy — 2026-10-21

The existing violet SVG stays as the base. An EnergyField ColorRect under Visuals shares WORLD/VISUALS/Shaders/EnergyField.gdshader and PortalEnergy.tres. Very slow purple swirl/rim are GPU shader motion. A successful wave/F2 portal spawn triggers the optional PlaySpawnPulse on that portal; only the overlay brightens briefly. Visuals child retains existing Scale multiplier and SpawnExit is unchanged. The overlay ignores mouse events and never changes collision/path/spawn logic. Desktop appearance is pending verification.

---

## Violet portal artwork — 2026-10-20

`EnemyPortal.tscn` now uses a cached `Sprite2D` referencing `Visuals/PortalViolet.svg` instead of its old cyan Polygon2D placeholder. The new texture contains a violet spiral, metallic outer ring and softly shaded central glow; it is **static**, with no procedural per-frame drawing or gameplay-affecting animation. `Visuals` remains the existing size-multiplied child, so the current portal-size Inspector slider continues to scale the entire texture. `SpawnExit`, editor snapping, collision footprint, routes, wave spawning and portal size-dependent spawn spread remain unchanged. To replace artwork, edit the SVG under this portal's `Visuals/` folder.

---

## Automatic editor placement — 2026-10-18

This scene now inherits editor-only cell-center snapping from `WorldObjectFootprint`. Build C#, then drag or duplicate instances within a map normally. **Editor Placement → Snap To Grid In Editor** defaults on; disable it for intentional free placement. Uses the map's actual `Terrain/Passable` TileSet and transforms. Child-marker offsets and footprint settings are preserved; standalone asset origins and gameplay movement remain unchanged. Off-grid saved instances align when opened in the editor; save the level to persist that correction. See `WORLD/GRID/README.md` from the repository root for full authoring behavior.

---

# Enemy Portal

`EnemyPortal.tscn` is a reusable, visible placeholder with a `SpawnExit` Marker2D and `WorldObjectFootprint` script. The one-cell footprint reserves its location against towers but remains passable. The exit is at the portal root center. EnemyMasterConfig.PortalSpawnOffsetPixels adds bounded world-space X/Y variation (8 pixels per axis by default), with clearance/connectivity/segment checks and safe center fallback. Level001 now instances it.

Snap the root to a 64px cell center (32 + 64*n), keep the centered exit in reachable passable space, and list the root-relative Portal path in the level's Settings/ActivePortals. Placeholder polygons and label can be replaced with art/VFX.

This object creates no enemies and owns no wave timers. `ENEMIES/Spawning` will consume approved sources from LevelConfig; edge-only levels need no Portal.


Portal root now uses EnemyPortal.cs, a tool script derived from WorldObjectFootprint. In each level select Portals/EnemyPortal (or another instance) → Portal Size → Size Multiplier. Default1 preserves appearance;2 doubles Visuals and spawn spread. Values clamp0.1–16; nonfinite inputs reset1. The visual-only child scales immediately in editor/game, with no processing or navigation rebuild. SpawnExit stays centered; SizeCells remains separately authored rather than automatically expanding reservations.

SpawnManager uses base PortalSpawnOffsetPixels × that portal SizeMultiplier, transformed by the portal's authored rotation/scale. At the default8px, multipliers0.5/2/3 produce ±4/16/24px local-axis spread. Debug and waves use the same helper; bounded clearance/connectivity/segment checks and valid-center fallback remain. Setting base offset0 stays exactly centered. Runtime size edits affect subsequent spawns without rebuilding terrain; moving a portal still requires the existing source/navigation refresh.

## Optional required checkpoint area

Instance `WORLD/ROUTING/RouteCheckpoint/RouteCheckpoint.tscn` anywhere under the level (optionally grouped under RouteCheckpoints), move it freely and edit RadiusPixels. On this portal set **Route Checkpoint → Checkpoint Path** using the node picker. The path is relative to the portal, e.g. `../../RouteCheckpoints/TopCheckpoint`. There is no build cost, footprint or collision for the marker.

Both F2 and wave spawning pass the **actual chosen portal's** checkpoint before the enemy's initial path request. Empty path retains old direct routing. Nonempty invalid links produce an editor warning and unfinished enemies wait; no automatic bypass. Enemies must physically enter any safe part of the circle, then route to Citadel with the same lifetime movement preferences. Different portals may share one region. Map-edge spawns remain unassigned. Changing the portal link affects future enemies; moving/removing the assigned marker affects unfinished ones. F3 shows each portal's staged plan. See WORLD/ROUTING/RouteCheckpoint/README.md.

The portal's one CheckpointPath is the **entry** into a checkpoint network. Each checkpoint owns its NextCheckpoints array for chains/branching/merging. Empty outgoing lists end at Citadel. The editor's Debug/CheckpointOverlay draws the portal connection and the complete authored graph above tiles. Existing portal placement/spread and saved links are unchanged.
