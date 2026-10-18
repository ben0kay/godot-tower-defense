# Cryo CT1–CT3 verification

Run `TESTS/Towers/CryoTowerChecks.tscn` with Godot .NET headless. The feature group is also linked into `TESTS/Integration/EnemyIntegrationChecks.cs` using an isolated production battlefield fixture.

## Automated coverage

- Full level 1–3 damage/rate/range/speed/Slow/ammo/cost data; all eight stages; safety clamps; independent level rows and private configs.
- Real catalog-driven HUD purchase, 250 CR construction and operational gate; 175/250 paid timed upgrades; actual final/preview parity, maximum level, XP retained and current 1k/3k/7k/15k rank curve.
- Shared definitions remain unchanged; 100→67 speed; equal/weaker applications ignored, stronger wins; arrival/death cleanup; independent sprite tint; Inspector immunity rejects status but not Energy damage.
- Actual guided Energy projectile resolves once, including shielded impact, actual damage XP, lethal kill XP/Credits once, launch snapshots of slow/overrides/penetration and source removal safety; immunity introduced during flight rejects the status without rejecting damage.
- Spatial filtering of slowed/immune enemies; unchanged ordinary targeting; no damage fallback or ammo consumption when all candidates are affected. First uses remaining physical route distance among eligible enemies.
- Actual slowed route movement preserves progress/navigation revision and pending A* count. Stationary slowed enemies continue receiving Lava interval damage.
- 500 real enemies; warmed cached speed-read loop allocates zero bytes with/without Slow. This is an allocation smoke/microbenchmark, not an FPS or whole-movement benchmark.
- Existing Sniper/Machine Gun shared-projectile, muzzle, cadence and ammunition tests run alongside Cryo.

## Actual host results — 2026-10-17 UTC

C# compilation passed with Roslyn 4.11, actual .NET8/GodotSharp references and Godot source generators. Standard SDK `dotnet build --no-restore` could not start compilation: installed host SDK throws missing `NuGet.Protocol, Version=6.11.2.1`. Godot editor imported the SVG/assets and discovered Cryo config successfully, while its SDK/MSBuild discovery also reports the host dependency issue.

Passed headless (exit 0): CryoTowerChecks, GenericEnemyChecks (including Laser, Blaster instant AoE and Shockwave), ConstructionChecks (C1–C4 pilot and existing paid upgrades), CorridorNavigationChecks (permissions, routing and 100/500-enemy benchmarks), AmmunitionLoopChecks, FootprintChecks, HudCategoryChecks, CheckpointChecks, RoutePreviewChecks, ResultsChecks, PauseControlsChecks and MenuTowerFlowChecks.

Unmodified full EnemyIntegrationChecks fails the pre-existing Level020 assertion **“The western open ground and eastern maze are fully painted”**, then encounters native shutdown exit139. It does not reach the Cryo group. Saved level art and that unrelated assertion were not changed. Production scene fallback-catalog warnings are existing and nonfatal.

SVG artwork rendered and inspected separately. Interactive desktop playfeel, colour visibility against all terrain, and manual Cryo combat viewing remain unverified on this headless host. Freeze/Stasis, timed scheduling, DoT, research and saves are deferred; their documented future contracts are not executable feature claims.
