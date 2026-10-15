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


Phase 4C-2: optional nullable BeamStats on TowerStatBlock/FinalStats carries genuine float DamagePerSecond. Existing seven-argument constructors remain valid with absent Beam. A shared base-block/tiers adapter snapshots purchased modifiers for both configs; Stage 02 applies BeamDps flat/percent/multiplier operations without integer rounding, Stage 07 clamps invalid/negative DPS, Stage 08 publishes it. Neutral stages propagate the optional capability unchanged. Laser attack/live Inspector/upgrade preview consume the same cached result; BeamStyle never enters this pipeline.

## Phase 4C-3 penetration

TowerStatBlock/FinalStats add an optional trailing ShieldPenetration float (default0), preserving old constructors. Battle's adapter supplies its 0.05 base; all eight existing stages propagate it. Stage 07 clamps negative/nonfinite values to 0 and values above 1 to1; Stage 08 publishes it. Paid Battle tiers do not modify it, and Stage 04 permanent research remains neutral. Config setters invalidate once and the selected Inspector reads the effective snapshot. Laser/passive types are not given fictitious projectile capability. Future permanent ballistic research must use the existing pipeline with cache invalidation, not purchased tier assets.

## Phase 4D optional area capability

AreaBlastStats(RadiusPixels, EdgeDamageFraction) appends as an optional nullable Area value on TowerStatBlock/FinalStats, preserving old constructor defaults and leaving Battle/Laser/passive capabilities unchanged. Blaster supplies it through the single coordinator. AreaRadius and AreaEdgeFraction append to TowerStatId without renumbering existing IDs. Stage 02 applies existing flat/percent/multiplier arithmetic; Stage 07 clamps radius to finite nonnegative pixels and edge retention to finite 0..1; Stage 08 publishes both. All eight stages remain in the same order.

Blaster's authored paid tier adds damage/radius only. Edge retention is a weapon input ready for future permanent research, with Stage 04 still neutral. Config changes invalidate cached stats and selected Inspector details; launched projectiles retain their own immutable snapshot. BlasterCannonChecks covers stage propagation/operations/safety, source isolation, no tick-based cache rebuilding and paid preview parity.
