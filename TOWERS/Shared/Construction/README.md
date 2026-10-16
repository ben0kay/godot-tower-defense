# Construction and upgrades — FUTURE / NOT IMPLEMENTED

## Gameplay design

Building and paid upgrading eventually take time rather than completing instantly. Each tower's existing absolute *Levels.cs row would define its own `WorkSeconds`: Level 1 for initial construction and later rows for that individual upgrade. Never compound purchased-level stats.

The placed tower owns its current work phase and progress; authored row values remain immutable. The current Inspector should show a progress indicator and status. Decide, with tests, when the grid cell is occupied, whether unfinished/upgrading structures can fire or take damage, when materials/credits are charged, and how cancel/refund, pause, interruption, and checkpoint saving work. Preserve the existing `WorldGrid` placement/path-validation and `ECONOMY/Wallet` atomic purchase/rollback guarantees.

## Visual construction stages

Timed construction is intended to support **visible build stages**, not just an invisible timer.

Possible first-pass approach:
- each constructible building/tower can provide **multiple authored art stages**
- e.g. **Stage 0 = foundation / scaffold**, **Stage 1 = partially built**, **Stage 2 = nearly complete**, **Stage 3 = finished**
- the game swaps stage visuals based on normalized construction progress thresholds

For now, the simplest content pipeline is likely:
- **multiple SVG files** (or exported PNGs) per tower/building, one for each construction stage
- later, a building could instead use layered visuals or a more procedural approach if desired

Examples:
- `BattleTower_stage0.svg`
- `BattleTower_stage1.svg`
- `BattleTower_stage2.svg`
- `BattleTower_stage3.svg`

The system should not hardcode exactly four stages forever. Prefer a simple authored list/array of stage visuals and thresholds so different structures can use different counts later if needed.

## Construction feedback / effects

Construction should also support optional visual feedback while work is in progress, for example:
- spark particles
- welding flashes
- smoke / dust puffs
- subtle glow pulses
- light machinery/build effects
- future drone/build-beam effects if desired

These effects are **supporting feedback only** and should not be required for the construction system to function. The construction logic must work even if a structure has no special particles assigned.

A good first implementation would allow:
- an optional looping "under construction" effect while work is active
- an optional one-shot "construction complete" effect when finished
- clean stop/restart behaviour on cancel, interruption, removal or upgrade completion

## Ownership and implementation

This directory is reserved for an optional shared construction state/controller. Do not make every `TowerBase` instance tick a construction node; track active work only. Per-type upgrade values belong beside their existing level rows, not in a separate modifier tree. No speculative scene/node classes yet.

The construction system should be able to answer:
- current construction state
- total work seconds
- elapsed work seconds
- normalized progress
- current visual stage index
- whether gameplay functionality is locked until completion

A tower/building should be able to expose:
- optional authored construction-stage visuals
- optional construction VFX
- optional completion VFX

## Suggested first gameplay rules

These are not final, but are a sensible first implementation target:
- the building footprint becomes occupied immediately once placement succeeds
- cost is paid immediately on valid placement / upgrade start
- a structure under initial construction is not yet operational
- an upgrading structure keeps clear defined behaviour (to be decided: either remains active until completion, or is temporarily disabled)
- final authored stats/behaviour only apply when the timed work completes

## Future verification

Construction and upgrade timers, occupancy, blocking, costs/cancellations, multiple levels, interruption, pause, disposal, Inspector and save restoration.

Additional visual verification should cover:
- correct stage visual at different construction progress values
- correct fallback if a structure has missing stage assets
- construction particles/effects starting and stopping correctly
- completed structure switching cleanly to its final operational visual
- upgrade-stage visuals behaving consistently if upgrade timing is later enabled

Related: [TOWERS roadmap](../../README.md), [Placement](../../Placement/README.md).

**Documentation-only reservation. No implementation has been made.**