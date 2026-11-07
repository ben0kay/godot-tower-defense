# Native battlefield lighting

LGT0-A owns the world canvas tint and native point lights. Citadel reactors light a 320px radius from local (64,64), and completed Battle Towers light a 192px radius from their stationary base. The tower container and existing work/death events govern eligibility. Both presets use one soft radial texture. The HUD, menus and F3 route colors keep their independent presentation.

Tune dusk tint, startup state, debug access, the 16-light cap and optional viewport culling in `DefaultLightingConfig.tres`. The controller checks registered sources at most four times per second for camera culling and prioritizes visible Citadels, then towers in placement order. Tune color, brightness, radius and offset in the two source presets. Reopen the level after editing resources. Plain **F6** in a running debug battlefield toggles between lighting and original colors. The override resets on level entry. Shadow settings are reserved for LGT0-B.

## Removal

For a configuration rollback, set `DefaultLightingConfig.tres → Enabled` to false and reopen the level. To remove the implementation, revert the LGT0-A commit. For selective removal, delete the `Lighting` child and its resource references from `WORLD/World.tscn`, the `WorldLight` child and its references from the shared Citadel and Battle Tower scenes, and the labeled unshaded material assignment in `RoutePreviewOverlay.Initialize`. The lighting scripts and resources live entirely in this folder. Saves and gameplay scripts have no lighting dependencies.
