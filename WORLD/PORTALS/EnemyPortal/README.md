## SPAWN2 cinematic warp-in — revised Codex implementation handoff (2026-11-06)

**PLANNED, not implemented.** [The complete SPAWN2 art/logic specification](../../../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md#phase-spawn2--shared-cinematic-dimensional-warp-in-for-all-portals-codex-implementation-specification) now requires **every portal to animate in with the same impressive purple/lilac dimensional vortex, curved inward spiral particles, forming ring, bright ignition pulse and settling idle shader**. All `AppearAtWave=0` (START) portals **materialize together when a fresh level loads**, not simply appear fully formed; Wave N portals use the identical effect when their authored wave starts. `AppearAtWave=1` still means Wave 1 begins, so use 0 for loading-screen/start-of-level appearance. Restore from a between-wave checkpoint snaps already-open portals to Open without replaying the effect.

Codex must preserve SPAWN1's registered source identity/route protection, differentiate `Dormant / Forming / Open` and gate enemy spawns until a portal finishes forming, while keeping pause and effect progress synchronized. Reuse existing rotating PortalArt, EnergyField and **ordinary** per-spawn 24-particle bursts. The cinematic materialization is a separate **one-shot** (proposal ~2.2 s) with bounded, cullable GPU particle swirls and locally rendered shader/ring effects; it must be striking on-screen while preserving 60 FPS as an optimization target. No implementation or Godot runtime visual acceptance has occurred yet.

---

## START / Wave N activation and subtle spawn visuals

**Source Implemented / Awaiting Owner Validation.**

Select a placed portal → **Wave Activation (0 = START) → Appear At Wave**. `0` preserves deployment visibility; `6` reveals on accepted authored Wave 6. Keep its path registered in the level ActivePortals list and use Portals/Both spawn mode. Required batches must not name it earlier. Missing future waves warn without auto-authoring a wave. Editor previews always show the portal; runtime hides only Visuals, preserving footprint/exit and fixed route protection.

Under **Portal Feedback**, `RotationDegreesPerSecond` defaults to **2°/s** (180-second revolution); `0` disables it. A small vertex shader rotates the existing art only, keeping labels, marker, footprint and routes fixed. It shares the existing energy overlay's GPU TIME convention; cosmetic shader motion may continue during pause. No CPU animation callback is added.

`SpawnParticlesEnabled` controls a reusable **24-particle**, **0.7-second** purple GPU burst on successful spawns. Radial velocity and tangential acceleration produce outward curling sparks in all directions. Burst cadence is capped at one per **750ms per portal**, with a 30Hz GPU simulation, tiny soft texture and no lights, collision, screen reads or persistent trail. No per-enemy emitter is created. Hidden/offscreen portals skip new feedback; screen exit stops emission. The existing energy pulse remains. Emitter amount/lifetime/motion/ramp are editable in EnemyPortal.tscn. GPU cost is bounded, not measured or guaranteed zero.

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
