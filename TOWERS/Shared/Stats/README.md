# Tower stats — Phase 3A

BattleTowerConfig is the editable authored input. TowerStatPipeline copies its resolvable combat values into a read-only TowerStatContext/TowerStatBlock and resolves all eight stages into an immutable TowerFinalStats snapshot. Identity, construction prices, targeting priority and RotateWhileTracking remain policy/metadata on Config.

Open Pipeline/TowerStatPipeline.cs to see the actual execution order. The numbered files each contain one stage. BaseStats, purchased TowerUpgrades, SafetyClamps and FinalStats are active; ranks, permanent research, level rules and active effects are neutral. Paid tiers reuse the existing wallet; no future managers or fake saved progression are required.

BattleTower builds its cache on Ready and Config.Changed. Combat reads FinalStats for range, rate, damage, projectile speed, aiming and search interval. BattleTowerConfig numeric setters emit Changed only when a value differs, so runtime C# edits and Remote Inspector tuning rebuild immediately. Replacing the Config unsubscribes the old resource; leaving the scene unsubscribes too. RebuildStats() is the explicit entry point for future input invalidation.

Selected tower details and range rings read the same final cache; StatsChanged refreshes displayed values. Build previews use the same resolver without spending/mutating anything. Resolution is not performed each physics tick.

TowerStatModifier carries typed operation/source data in immutable resolution snapshots; Stage 02 consumes purchased tier modifiers. It owns no timers or manager. Future context inputs/stages should be activated only when their owning features exist. Permanent research belongs to persistent player progression, level rules to LEVELS and timed effects to their instance sources.

TowerStatTrace is opt-in and records immutable snapshots for debugging. Calling Resolve(config, trace) clears/reuses that trace. Ordinary runtime resolution supplies no trace; normal combat allocates no pipeline snapshots.

The current safety rules preserve all valid baseline values. NaN/infinity resolve to minima: range/rate/rotation/damage >= 0, projectile speed >= 1 (matching BasicProjectile), search interval >= 0.05 seconds, aim tolerance within 0–180 degrees. Zero fire rate disables shooting. No arbitrary balance upper limits are introduced. Inputs are never clamped in place.

TESTS/TowerStatChecks.cs covers baseline parity, numbered order, neutral stages, deterministic results, trace reuse, clamps, immutable snapshots, independent towers, numeric Resource changes/replacement, unsubscribe on exit, no tick-based rebuilding, live zero-rate gating and updated Inspector values. Existing combat, economy, wave, terrain and navigation tests remain active.

Phase 4A: shared TowerConfig.ResolveStats/DescribeStats/DescribeChanges provide type-aware input/presentation adapters. BattleTowerConfig retains its numeric exports and unchanged Battle input adapter into TowerStatContext; none of the eight stages were rewritten. Passive types may have no combat snapshot or weapon fields. TowerDefinition references canonical Config inputs; catalog/HUD do not duplicate math or prices.
