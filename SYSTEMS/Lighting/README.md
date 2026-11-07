# Native battlefield lighting

`Runtime/` owns the world controller, pooled lights, drone attachment and lava grouping. `Config/` holds typed settings and the default world resource. `Presets/` contains source profiles and `Textures/` holds the shared falloff.

Tune ambient, startup state, budgets, viewport culling and construction multipliers in `Config/DefaultLightingConfig.tres`. The default is `#515e6b` dusk with up to 16 active lights, including up to 2 drone lights and 3 lava lights. Visible Citadels take priority, then one visible tower when available, then the capped drone and lava shares and any remaining towers. F6 in a running debug battlefield compares lighting with original colors. The override resets on level entry. Reopen the level after editing resources.

Placed towers inherit a 192px cyan radius from `Presets/DefaultTowerLight.tres`. In a tower definition's **Lighting** group, zero radius inherits the default. Each definition can also adjust the offset or opt out. Construction uses half the resolved radius and 55% energy until completion. Citadel reactors keep their 320px radius and (64,64) local offset. Flying drone bodies get a small following light. Lava uses cached connected patches inside 4×4-cell sections, centred on a real lava cell. Shadows are reserved for LGT0-B.

## Removal

Set the master config's `Enabled` to false and reopen the level for a configuration rollback. Revert the A.1 commit to return to the original pilot. For selective removal, detach `Lighting` and its resources from `WORLD/World.tscn`, remove `FlightLight` and its script reference from `DRONES/Shared/DroneBody.tscn`, and remove the labeled lighting integrations in `DroneBody.cs`, `TowerDefinition.cs`, `InteractiveTileManager.cs` and `RoutePreviewOverlay.cs`. The latter keeps the F3 route colors independent. Lighting scripts and profiles live in this folder.
