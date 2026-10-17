# IT1–IT4 — executed Inspector / targeting verification

## Delivered

Compact non-scrollable two-column Inspector; six opt-in per-placed-tower policies; typed current/next/gain (including Cryo Slow), current-only maximum-tier stats; shared bounded checkpoint-tail estimates for First/Last. No combat balance, economy, research, saving, A* algorithm or checkpoint branch changes.

Primary command: Godot .NET `--headless --path . TESTS/UI/InspectorTargetingChecks.tscn`. The same entry is linked into EnemyIntegrationChecks. Additional journey fixture: `TESTS/Navigation/JourneyTargetingChecks.cs`.

## Actual results — 2026-10-17 UTC

- C# compilation **passed** with Roslyn4.11, .NET8 references, actual GodotSharp assemblies and Godot source generators. Standard SDK `dotnet build --no-restore` fails before source compilation because this host lacks `NuGet.Protocol, Version=6.11.2.1`. Godot editor asset/script import completed, with its existing SDK/MSBuild-discovery errors reported separately.
- Focused InspectorTargetingChecks **passed (exit0)**. All nine production tower types, construction/paid-upgrade headers, separate earned MAX RANK and current-only MAX LEVEL states were checked using actual Godot controls.
- Headless layout bounds: **700px width; maximum observed height331px**, at1920×1080,1366×768 and960×540. Every visible child control stayed inside the panel. These are geometry assertions, not rendered screenshot approval. No Inspector ScrollContainer or Additional Details foldout remains.
- All six priorities: stable ties, current hull/shield changes, restoration, empty/single/500-enemy candidates, out-of-range/removal, invalid-route exclusion, serialization values0/1 retained. Real HUD actions tested every aimed tower; private configs, other instances, authored defaults, cached FinalStats, XP, wallet and ammunition remain independent. Mode choice survives selection, rank and paid upgrades; next normal weapon tick reacquires without resetting cooldowns.
- Cryo's existing strict eligibility is preserved. Its focused suite passed alongside this change.
- First/Last: two real registered portals, a short checkpoint leg followed by a long remaining tail versus a shorter direct Citadel route; captured branch/actual transition parity; no target-query A* or wave RNG consumption; moving checkpoint geometry, blocked/open downstream routes, removed captured destinations and revision invalidation.
- Controlled500-enemy fixture (one effective profile): **3 shared cached tail edges;6 total optional tail queries across the tested setup/revisions**, not500 per-enemy tails. Combined movement+estimate work never exceeded the fixture's **2 queries/frame** cap. Actual gameplay counts depend on profile/radius/network diversity. This is a work-sharing check, not an FPS claim.

Passed standalone headless suites: InspectorTargetingChecks; GenericEnemyChecks (Laser, Blaster instant AoE, Shockwave); ConstructionChecks (including paid levels, preview parity and artwork pilot); CorridorNavigationChecks; CheckpointChecks; CryoTowerChecks; AmmunitionLoopChecks; FootprintChecks; DeathSpawnChecks; RoutePreviewChecks; HudCategoryChecks; MenuTowerFlowChecks; PauseControlsChecks; ResultsChecks (isolated rerun); SpatialQueryBenchmarks. Existing spatial circle/target/swept oracle and full rank/XP/Inspector/MAX RANK checks also passed inside the new runner.

The old rank test expected2000XP after1000+14000+500; corrected to15500, matching its next assertion and current rank table. Existing assertions follow migrated real scene paths and the new exact header/current-only table semantics; no production level art or balance was changed.

## Outstanding verification / inherited blockers

- Full EnemyIntegrationChecks stops at the unchanged Level002 assertion **“The western open ground and eastern maze are fully painted”**, then native shutdown exits139. The standalone groups above provide executed coverage; do not claim the full suite passed.
- One ResultsChecks run encountered native `gchandle.is_released()` / catalog-loading failure (exit134); an isolated rerun passed exit0. No speculative production fix was made for this intermittent native/runtime failure.
- No desktop display is available here. Manual visual/playfeel review, screenshots, hover/focus appearance and reading comfort remain pending. Headless geometry cannot approve aesthetics. Very small windows below the tested sizes may need a separate responsive layout.
- Estimates deliberately exclude First/Last while pending/invalid; heavy continuous movement work can delay optional tails. Downstream distance is approximate because circle entry, future offsets/congestion and dynamic buildings are not known exactly. No live route is rewritten to make an estimate exact. See `TOWERS/Shared/Targeting/README.md` for the canonical-point policy and4096-edge safety cap.
