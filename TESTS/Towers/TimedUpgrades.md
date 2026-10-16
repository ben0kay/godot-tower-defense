# Timed paid upgrades and Inspector — C3

Run the compiled project with `Godot --headless --path . res://TESTS/Towers/ConstructionChecks.tscn`. That runner now includes TimedUpgradeChecks plus existing construction, placement, performance and TowerUpgradeChecks groups.

## Verified behavior

- Actual HUD starts a150-Credit Battle1→2 upgrade lasting4 seconds. Old level/cached stats/stock remain unchanged at purchase, the work is operational, its pending tier is2, and duplicate purchase is disabled. Level2→3 costs250 and takes its independent5 seconds.
- WorkStarted sees an already-paid wallet. No charge happens at completion. Invalid/negative/nonfinite durations, unavailable/paused/queued/freed towers, busy/stale requests and a missing manager cannot partially schedule or charge. Existing TowerUpgradeChecks retains insufficient secondary-resource, atomic multi-resource recipes, expected-level, maximum and row independence coverage.
- Old-tier firing continues while work advances. A mid-work Veteran promotion updates Level1 to31 damage; completion resolves Level2 with that current rank to34, rather than using the purchase-time snapshot. XP remains1000. Enlarging ammo capacity to1200 triggers one capacity change and preserves the spent stock; no refill or duplicate application.
- During upgrade, normal depot drones still deliver to the operational tower. Separate initial Factory construction and paid upgrade clocks overlap and finish independently. Real Mission Results defeat freezes the paid upgrade clock.
- Both real launchers complete a positive-duration upgrade through automatic Godot physics, without explicit clock advancement. Zero-duration rows commit immediately, with stats notification after payment and no scheduled job.
- Selected Inspector shows CONSTRUCTING / UPGRADING to Level N / OPERATIONAL, a small progress bar, elapsed/total and remaining seconds. Existing current stats show the active old tier; the pending preview is free, marked paid, and cannot buy another tier. Switching selection, closing and reselecting do not retain stale progress; closing/unloading detach work listeners. Progress events touch only work controls, not cached stats or full previews. Initial Factory progress uses the same section.
- Existing unrelated combat/rank/ammunition/menu tests now wait through real paid work with ConstructionTestClock rather than asserting an immediate upgrade or changing authored durations to zero.

## Actual execution — 2026-10-16

Godot4.7.2 Mono, .NET8.0.31 / Roslyn4.11:

- All production/test C# compiled with matching Godot source generation and reference assemblies, no diagnostics.
- ConstructionChecks (C1–C3 and existing TowerUpgradeChecks), AmmunitionLoopChecks, GenericEnemyChecks, FootprintChecks, HudCategoryChecks, MenuTowerFlowChecks, PauseControlsChecks and ResultsChecks exited0.
- GenericEnemyChecks exercises catalog/shields, ShieldEmitter, waves/portals/Citadel, instant area damage and Laser/Blaster/Shockwave damage/delivery suites, including30/60/120Hz cases. FootprintChecks keeps terrain/navigation/payment/rollback and500-enemy checks.
- Standard `dotnet build --no-restore` aborted before compilation with host `Win32Exception` in `Process.GetStat` / `StartTime`. This is not an SDK-build pass.
- Full unmodified EnemyIntegrationChecks still stops at the preexisting Level002 painted-terrain assertion (“The western open ground and eastern maze are fully painted”), followed by native shutdown exit139. No map data or assertions were changed to bypass it. The full TowerRankChecks group is behind that blocker; focused C3 promotion/XP and ammunition-rank checks passed separately.
- Active-only benchmark rerun:500 idle +100 active jobs,600 ticks, **0.0271ms mean /0.2591ms peak**,100 active visits/tick, zero idle visits and no physics callback after completion. These are headless host CPU observations, not rendered FPS or universal performance guarantees.
- Desktop visual QA was not performed. Existing392×360 Inspector dimensions, scroll behavior and subtle panel opacity passed HUD regressions. Staged SVGs, sparks and other construction VFX remain C4; no research, refunds or saving was implemented.
