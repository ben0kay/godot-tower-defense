# OBJECTIVES — Physical Defended World Objects

World objects that enemies seek, rather than enemy AI/logic or terrain tiles. The first primary objective is **The Citadel**.

- `Citadel/Citadel.tscn` — minimal editable scene shell for the destination.
- `Citadel/README.md` — design intent and future integration.

The Citadel will eventually have a visual footprint, health/armor, possible damage/destruction VFX and level-loss consequences. It may be solid to enemies, so **enemy paths should target reachable approach points around it**, not force ships into its blocked center.

The spawning/movement systems live under `ENEMIES/`; place the Citadel under a level's `Objectives` container. No gameplay logic or ObjectiveMasterConfig exists yet.
