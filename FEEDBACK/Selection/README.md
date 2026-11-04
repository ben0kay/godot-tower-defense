# Shared selected-tower overlay

**Status: Source Implemented — Awaiting Owner Validation.** One reusable presentation for the placed tower currently selected through `TowerPlacementController`. It does not apply to build-preview tiles or unselected structures.

## Presentation
- A restrained full-screen aqua wash and faint tactical grid appear *behind* the existing gameplay HUD (the overlay's Control is inserted as the HUD's first child, with `MouseFilter.Ignore`). Existing wave/top bars, build tray, Inspector, buttons, targeting, range circle and click handling remain unchanged.
- A lightweight caption centred just above the lower HUD identifies the selected tower by its actual `AuthoredConfig.DisplayName`. It says **TOWER SELECTED** or **TOWER DISABLED**.
- A slowly pulsing square/corner-bracket hologram highlights the actual WorldGrid footprint, correctly sized for 1×1, 2×2, 3×2 or other rectangular structures; the faint fill and fake glow are cosmetic only.
- **Normal = aqua**, **disabled = muted red**. The disabled category is the existing `TowerBase.IsDisabled` gate (heat, EMP, no power, and `Upgrading` while timed work is active). No duplicate status engine.
- Selection changes, Inspector close, Ctrl build-tray opening, build preview, deselection, tower removal and scene unload deactivate the overlay. Existing selected tower's `DisableStateChanged` / `HeatChanged` and `WorkStarted` / `WorkCompleted` hooks refresh the state immediately, without scanning towers.

## Ownership and performance
- `TowerSelectionOverlay.cs`: one world-space Node2D for the selected tower's pulsing footprint, instantiated by `TowerPlacementController`.
- `TowerSelectionScreen.cs`: one non-interactive Control behind the gameplay HUD, drawing a very faint colour wash/grid and the caption only on selection/state/resize changes.
- `TowerSelectionStyle.cs` + `DefaultTowerSelectionStyle.tres`: Godot Inspector tuning. Aqua/red colours, wash/grid enabled and opacity, grid spacing, caption font/height, hologram enabled/inset/corner size/fill/border, pulse period and redraw rate.
- The world outline redraws at up to 20 Hz *only while a tower is selected*; it never processes while deselected. The HUD wash has no per-frame redraw/process. No per-tower effect nodes, timers, materials, combat calculations or save-format changes.

## Acceptance checks (owner)
In Godot, inspect aqua Battle Tower selection, disabled/overheated or EMP tower turning red and returning aqua, upgrading with drone, 2×2 Depot and 3×2 Factory bracket extents, switching/deselecting/closing Inspector/entering build mode, and different resolutions/camera zooms. Verify top and bottom HUD buttons remain clickable and that the new effects do not obscure combat. No Godot/.NET or runtime acceptance was executed in this connector session.
