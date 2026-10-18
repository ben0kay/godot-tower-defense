# Terrain tests

Terrain categories and interactive Foundation/Lava correctness, with migration reports. Helpers run from the integration suite.

See [the test index](../README.md) for build/run commands and known suite limitations. This folder cleanup does not change test logic.

## Terrain Edges foundation

`TerrainEdgeFoundationChecks.Run()` is invoked by `TESTS/Integration/EnemyIntegrationChecks.cs`; it asserts the default-off Resource, Mars material-to-atlas bindings and eight-direction neighbourhood logic. It does **not** test rendered corners (not implemented) or existing Mars texture bleed (art fix pending). Later Codex passes should add a visual-mask fixture and dedicated performance checks described in `WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md`.
