# Per-building power use — FUTURE / NOT IMPLEMENTED

## Purpose
Optional **consumer-side** power capability for any placed tower/building. Top-level [POWER](../../../POWER/README.md) owns generation, connected terrain islands and priority allocation; an individual tower does not scan power sources or calculate its own grid.

## Parameters
- Fixed **IdlePowerDraw** and **ActivePowerDraw** baseline values in each type's non-level config (not its *Levels.cs rows). Buying Level 2/3 does not automatically increase electricity draw; future permanent research may change **effective** consumption without mutating base settings.
- Per placed instance: is-powered status, current operating mode and **PowerPriority** slider from 0 (lowest) to 100 (highest), starting at 50 in the existing tower Inspector. Priority is a player preference, not purchased level/rank/research.
- During shortages the shared network supplies higher-priority buildings first (initially full on/off operation, deterministic tie-breakers and anti-flicker logic). Define idle/firing behaviour and restoration with tests. Buildings with no power capability remain valid.

Keep power operational state separate from immutable cached combat stats and from the session Credits wallet; avoid required per-tower power nodes/ticks. Until a dedicated power pass, existing towers stay operational as they do now.

**Not implemented. Validate power interruption, priority, upgrades, Inspector/saves and many-tower performance later.**
