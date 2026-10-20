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
