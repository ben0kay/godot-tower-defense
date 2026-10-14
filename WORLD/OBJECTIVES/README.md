# OBJECTIVES — Physical Defended World Objects

World objects that enemies seek, rather than enemy AI/logic or terrain tiles. The first primary objective is **The Citadel**.

- `Citadel/Citadel.tscn` — editable destination with placeholder visuals, a reserved solid 3×3 footprint and four approach markers.
- `Citadel/README.md` — design intent and future integration.

The Citadel now has a logical solid footprint. Health damage on arrivals is implemented; armor, destruction VFX and level-loss consequences remain future work, so **enemy paths should target reachable approach points around it**, not force ships into its blocked center.

The spawning/movement systems live under `ENEMIES/`; place the Citadel under a level's `Objectives` container. No gameplay logic or ObjectiveMasterConfig exists yet.

## Active objective HP

`Citadel/CitadelHealth.cs` now owns live HP and the one-time `Destroyed` event. `Citadel/CitadelSession.cs` subscribes to enemy-arrival events and connects the active level config to the center health HUD. Level-specific maximum HP is `LEVELS/LevelConfig.CitadelMaxHealth`; do not duplicate that value on the Citadel scene. Defeat-screen/state handling and Citadel shields/armor are later work.
