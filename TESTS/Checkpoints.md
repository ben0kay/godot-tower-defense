# Required portal checkpoints — verification

Implemented against GitHub base `2867a3b` on 2026-10-15 with Godot4.7.2 Mono/.NET8. Rebased onto `2e13f43` before push (new ammunition work-order documentation only; runtime source unchanged). See WORLD/ROUTING/RouteCheckpoint/README.md for place/link instructions and bounded-target limitations. No authored level art, waves, enemy definitions or master-config resources are changed.

## Focused coverage

`CheckpointChecks.tscn` runs against an isolated runtime map and production classes:

- A free Node2D scene has no footprint, children/physics areas or processing; moving it changes no navigation reservation/revision.
- All8 independent movement permission combinations × global masters off/on, mixing Enemy001/002/004 radii/stats. Planning targets the region without marking it complete. Physical entry is required before normal Citadel arrival; profile, seed and offset persist through promotion and both stages preserve clearance.
- Actual F2-style SpawnManager spawns get portal1's link; a required-portal wave spawn gets portal2's different link, independent of type/catalog order.
- Fast movement clips at analytic swept circle entry. Physical entry is consistent at30/60/120Hz; spawn already inside starts the Citadel stage. A small0.01px circle boundary tolerance handles native float interpolation (an initial stricter squared-distance comparison failed and was corrected).
- A missing/broken or removed required checkpoint safely waits, never bypasses. An inserted full tower wall prevents completion; removal recovers through existing retry/queue work. Unlinked portal behavior stays direct-to-Citadel.
- Preview uses both stages, reports physical circle entry, and handles missing required links. Actual F3 ALL mode resolves each portal's different link; moving a marker clears cached output. Shared/private navigation still uses unit-cost neutral profiles where appropriate.
- Target lists are shared/cached and bounded at33 probes, independent of circle area. Every actual A* call remains under production or separate preview count/time budgets.
-500 enemies share the same checkpoint target cache and shared grids; no per-enemy A* grid. Both1-cell and3-cell passages work with scout and medium/Bulwark radii.

## Results

- Direct Roslyn C# compile with Godot source generators: passed, no diagnostics.
- Focused checkpoint runner: passed.
- RoutePreviewChecks: passed both campaign levels, preserving private diversity controls, ALL/50/16, RNG/assignment/queue isolation and500-enemy coverage.
- CorridorNavigationChecks: exit0 including MovementPermissionChecks and all twelve straight/winding,100/500,0/50/100% cases.
- MenuTowerFlowChecks: exit0, actual Boot/Main/Campaign flow, construction and paid upgrade.
- PlacementPerformanceChecks: exit0,100/500.
- Unmodified EnemyIntegrationChecks: still stops on the pre-existing Level002 painted-tile-count assertion, reproduced independently before this pass. No map or assertion is changed here. A diagnostic run skips only Level002Checks.Run to execute the remaining checks; the original call is restored before final compilation. The diagnostic completed with exit0 and all remaining31 integration checks passed, including SwarmNavigationChecks, RepathMovementChecks and SmoothRepathChecks. This is not an unmodified full-suite pass.

Standard SDK/MSBuild remains blocked by the container's missing NuGet.Protocol dependency verified in the preceding pass. Actual .NET Godot headless scenes run successfully with the directly compiled assembly. No manual graphical editor playtest is claimed. Expected existing missing-catalog fallback warnings remain.

## Performance sample

The focused open-map500-ship fixture recorded50.19ms for spawning and241.48ms **total** initial path-work CPU across5,000 budgeted goal queries. This map/radius produced10 cached candidates per ship. These are headless cumulative timings, not one physics frame, rendered FPS or a speedup comparison; concurrent suites influence timings. Count limits stay strict and the time limit remains soft by one indivisible query/refinement. More candidate targets add search work, deliberately scheduled over frames.

Marker geometry is a required region, not a global path-detour bound. Bounded deterministic samples can miss a very narrow portion of a large circle; F3/live diagnostics report the unavailable target rather than claiming a direct bypass. Move/enlarge/reposition the marker in that case. First targeting retains physical current-leg distance; the downstream Citadel route is selected only upon actual entry. A brief wait at the stage boundary is possible while the next bounded request is serviced.
