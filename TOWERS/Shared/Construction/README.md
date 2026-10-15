# Construction and upgrades — FUTURE / NOT IMPLEMENTED

## Gameplay design
Building and paid upgrading eventually take time rather than completing instantly. Each tower's existing absolute *Levels.cs row would define its own WorkSeconds: Level 1 for initial construction and later rows for that individual upgrade. Never compound purchased-level stats.

The placed tower owns its current work phase and progress; authored row values remain immutable. The current Inspector should show a progress indicator and status. Decide, with tests, when the grid cell is occupied, whether unfinished/upgrading structures can fire or take damage, when materials/credits are charged, and how cancel/refund, pause, interruption, and checkpoint saving work. Preserve the existing WorldGrid placement/path-validation and ECONOMY/Wallet atomic purchase/rollback guarantees.

## Ownership and implementation
This directory is reserved for an optional shared construction state/controller. Do not make every TowerBase instance tick a construction node; track active work only. Per-type upgrade values belong beside their existing level rows, not in a separate modifier tree. No speculative scene/node classes yet.

## Future verification
Construction and upgrade timers, occupancy, blocking, costs/cancellations, multiple levels, interruption, pause, disposal, Inspector and save restoration.

Related: [TOWERS roadmap](../../README.md), [Placement](../../Placement/README.md).

**Documentation-only reservation. No implementation has been made.**
