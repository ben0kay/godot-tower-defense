# Shared selected-tower overlay

**Status: Source Implemented — Awaiting Owner Validation.** One reusable presentation for the placed tower currently selected through `TowerPlacementController`. It does not apply to build-preview tiles or unselected structures.

## Presentation
- A stronger but still transparent aqua/red screen tint (7.5% opacity by default) and faint tactical grid (10% default opacity) appear *behind* the existing gameplay HUD (the overlay's Control is inserted as the HUD's first child, with `MouseFilter.Ignore`). Existing wave/top bars, build tray, Inspector, buttons, targeting and click handling remain unchanged. The range display is extended below.
- The **TOWER SELECTED / TOWER DISABLED** status and the actual selected tower's name now render **in world space to the right of its footprint**, remaining attached to the tower during camera movement/zoom instead of hovering above the bottom UI.
- A slowly pulsing square/corner-bracket hologram extends **8 world pixels outside** the WorldGrid footprint by default, correctly sizing for 1×1, 2×2, 3×2 or other rectangular structures; the faint fill and brighter border/glow are cosmetic only.
- **Normal = aqua**, **disabled = muted red**. The disabled category is the existing `TowerBase.IsDisabled` gate (heat, EMP, no power, and `Upgrading` while timed work is active). No duplicate status engine.
- Selection changes, Inspector close, Ctrl build-tray opening, build preview, deselection, tower removal and scene unload deactivate the overlay. Existing selected tower's `DisableStateChanged` / `HeatChanged` and `WorkStarted` / `WorkCompleted` hooks refresh the state immediately, without scanning towers.

## Ownership and performance
- `TowerSelectionOverlay.cs`: one world-space Node2D for the selected tower's pulsing footprint, instantiated by `TowerPlacementController`.
- `TowerSelectionScreen.cs`: one non-interactive Control behind the gameplay HUD, drawing only the aqua/red colour wash and faint grid on selection/state/resize changes. World-space caption lives in the selected outline renderer.
- `TowerSelectionStyle.cs` + `DefaultTowerSelectionStyle.tres`: Godot Inspector tuning. Aqua/red colours, wash/grid enable/opacity/spacing, name font size and **NameRightOffsetPixels** (default 16), hologram enable and **FootprintPaddingPixels** (default 8), corner size/fill/border, pulse period and redraw rate.
- The world outline redraws at up to 20 Hz *only while a tower is selected*; it never processes while deselected. The HUD wash has no per-frame redraw/process. No per-tower effect nodes, timers, materials, combat calculations or save-format changes.

## Acceptance checks (owner)
In Godot, inspect aqua Battle Tower selection, disabled/overheated or EMP tower turning red and returning aqua, upgrading with drone, **right-side world caption following camera pan/zoom**, 8px outside-footprint hologram, 2×2 Depot and 3×2 Factory bracket extents, switching/deselecting/closing Inspector/entering build mode, and different resolutions/camera zooms. Verify top and bottom HUD buttons remain clickable and that the new effects do not obscure combat. No Godot/.NET or runtime acceptance was executed in this connector session.


## Sight-aware range shading — 2026-11-05

`TowerRangeOverlay.cs` is one additional selected-only, non-processing Node2D explicitly owned by placement. Aimed hostile tower ranges retain their cyan outline/visible area, while mountain-obstructed portions of the disk and outer arc are red. The first obstruction starts the red region; space in front remains cyan. Support/passive and untargeted tower circles retain their normal color because those systems do not use hostile terrain sight. This is a range/visibility guide, not a second gameplay rule or height simulation.

The renderer reuses `WorldGrid.HasTowerLineOfSight` and the painted mountain footprint. A fixed 384-ray visual approximation with ten binary refinement steps finds obstruction boundaries; only a potential selection/range/position/settings/terrain change builds it. Angular interpolation can soften narrow corners; gameplay keeps its authoritative exact cell traversal. Where the game rejects off-map sight endpoints, the preview also shows that area blocked. Worlds with zero blockers skip sight traversals. Camera pan/zoom, health, ammunition, heat, repair ticks and unchanged-range rank changes do not regenerate the mesh. One cached fill surface plus at most two recorded line batches; no image texture, shader, per-frame rays or per-tower mesh.

Existing `TowerMasterConfig` adds VisibleRangeColor (original cyan), BlockedRangeColor (red), VisibleRangeFillOpacity (0.035) and BlockedRangeFillOpacity (0.14). ShowSelectionRange hides the entire presentation. Selection close/deselect/build browsing/tower exit releases subscriptions and geometry. No gameplay/save changes.

Owner check: select Battle/Laser beside a mountain and inspect cyan in front, red behind and red arc segments; compare with actual targeting. Switch towers, upgrade range, pan/zoom, close the Inspector and re-open selection; no stale red regions should remain. A Restoration Tower or Shockwave keeps its ordinary circle. Source review only; .NET/Godot execution unavailable.
