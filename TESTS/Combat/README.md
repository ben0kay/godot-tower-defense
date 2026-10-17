# Combat tests

Damage, shields, instant area damage, travelling shockwaves, death feedback and spatial-query correctness.

See [the test index](../README.md) for build/run commands and known suite limitations. This folder cleanup does not change test logic.

## Stacked shield/hull feedback regression (2026-10-17)

`EnemyShieldChecks.cs` asserts the inherited `DefenseBar` displays both meters on a full-shield/full-hull spawn; unshielded enemies remain bar-free at full health. Shield-only hits shrink the upper meter without changing hull; complete shield break hides it, hull hits reveal only hull, and explicit support restoration reuses the same bar/scene and restores the upper meter. The existing movement callback counter-rotates both at once, with no added per-frame processing. Headless and in-game visual QA are pending.
