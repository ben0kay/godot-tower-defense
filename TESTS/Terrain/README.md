## E1/E2 rendering checks — 2026-10-21

Run TerrainEdgeRenderingChecks.tscn for shared soft mask topology, generic profile/source opt-in, layer transforms/fallback, deterministic images, no navigation work from visual rebuilds, tower-occupancy isolation, exact material restoration and bounded4800-cell generation. It uses temporary Level002 terrain only. Diagnostic images save under user://terrain-edge-mask.png and user://mars-after.png. Headless tests do not assert desktop GPU shader output; real gameplay zoom/filtering review remains pending. Existing route/overlay regression checks are run separately.

# Terrain tests

Terrain categories and interactive Foundation/Lava correctness, with migration reports. Helpers run from the integration suite.

See [the test index](../README.md) for build/run commands and known suite limitations. This folder cleanup does not change test logic.

## Terrain Edges foundation

`TerrainEdgeFoundationChecks.Run()` is invoked by `TESTS/Integration/EnemyIntegrationChecks.cs`; it asserts the default-off Resource, Mars material-to-atlas bindings and eight-direction neighbourhood logic. It does **not** test rendered corners (not implemented) or existing Mars texture bleed (art fix pending). Later Codex passes should add a visual-mask fixture and dedicated performance checks described in `WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md`.
