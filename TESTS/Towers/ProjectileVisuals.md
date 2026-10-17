# PV1/PV2 verification — 2026-10-17

Base: GitHub main `c24d82f867da`. No weapon-level, rank, target priority, UI layout, navigation, economy or enemy production data changed. Battle's deliberate origin change is center → actual barrel tip.

## Compilation and commands

All project C# compiled successfully with Roslyn 4.11, .NET 8.0 reference assemblies, Godot 4.7.2 API and Godot source generators. Godot headless loaded the generated assembly and imported the new resources/scenes.

Normal `dotnet build --no-restore` was attempted but the installed SDK fails before project compilation: missing `NuGet.Protocol, Version=6.11.2.1` during CLI initialization. Godot's editor import likewise reports SDK discovery limitations; this is why direct compiler plus actual engine execution was used. This is not a successful standard SDK build.

Run the focused suite with an installed Godot .NET environment:

```sh
dotnet build
godot --headless --path . TESTS/Towers/ProjectileVisualChecks.tscn
```

## Focused suite — PASS

Real Battle barrel-tip origin, one projectile/flash/ammo charge, cooldown/invalid-target/empty/nonoperational suppression; ten alternating Machine Gun launches, matching flash/bullet marker origins, simultaneous volleys and atomic insufficient-stock refusal. Existing MultiBarrel checks also validate 12/12.4/12.8 total bullets/s over ten seconds at 60 Hz, paid tiers, rank XP and weapon snapshots.

Guided tracer node rotation follows changed motion heading; zero-distance impact remains finite. Long artwork does not extend contact reach. Launched style values survive resource edits; flash values are copied too. Source-tower removal does not cancel a fired hit; disabled projectile art still hits once; legacy missing-style projectiles retain five-second expiry; removed targets clean up safely. Invalid visual numeric/color values are sanitized. Presets share read-only resources rather than per-shot Resource copies.

Sniper: subpixel red style, actual muzzle anchor, barrel-forward rotation, moving targets, hidden idle/empty/removed-target/disabled-style states, no aiming damage or independent processing. Existing Cryo checks pass with styled projectiles, covering permanent Slow, Energy damage, shields, actual XP, paid/rank stats, movement and 500-enemy status reads.

Renderer: fixed 32 slots; 100 immediate emissions overwrite 68 slots without growing nodes; expiry clears state and disables processing. Warmed 10,000 flash emit+expire operations measured **6.371 ms and 0 managed bytes** in the final focused run. This excludes drawing/GPU cost and is not a rendered FPS measurement. No materials/textures/nodes/timers are created per flash.

## Regression scene results

| Headless scene | Result |
|---|---|
| ProjectileVisualChecks | PASS |
| ConstructionChecks | PASS: C1–C3 and Battle staged artwork, weapon/production/logistics gating |
| AmmunitionLoopChecks | PASS: physical production/delivery, 900 actual shots, beam/Blaster/Shockwave consumption |
| FootprintChecks | PASS |
| CheckpointChecks | PASS |
| CorridorNavigationChecks | PASS |
| GenericEnemyChecks | PASS: inherited enemies, abilities, waves, Laser DPS, Blaster instant AoE and swept Shockwave checks |
| DeathSpawnChecks | PASS |
| RoutePreviewChecks | PASS: both launchers |
| HudCategoryChecks | PASS |
| MenuTowerFlowChecks | PASS |
| PauseControlsChecks | PASS |
| ResultsChecks | PASS |
| SpatialQueryBenchmarks | COMPLETED exit0: existing paired sparse/dense and extreme-teleport benchmark; not a new FPS claim |
| InspectorTargetingChecks | INCOMPLETE: spatial oracle and rank/XP groups pass, then native `gchandle.is_released()` / thread notification crash; isolated retry also aborts |
| EnemyIntegrationChecks | FAILS at existing Level002 terrain assertion: “The western open ground and eastern maze are fully painted”, then shutdown exit139; new linked visual group is not reached |

No unrelated terrain or native-resource cleanup changes were made to hide these failures. The focused production-world runner executes the new visual group independently of the full-suite blocker.

## Still requires desktop review

Normal-zoom readability, subtle Sniper aim width, glow/flash silhouettes, high-rate multi-tower appearance and actual rendered performance. Headless tests do not verify pixel appearance. Adjust tower-owned `.tres` presets after playtesting; all look changes are separate from damage and level stats. Optional texture/ring draw branches compile but need visual artwork review before claiming an approved art variant.

## Muzzle visibility follow-up — 2026-10-17

Base8c7ab2e includes the newer20% Battle/Machine Gun fire-rate balance; those level rows/prices are preserved. Added real Blaster barrel-origin/flash/no-launch-damage/cooldown assertions. Renderer tests cover first-process hitch survival, bright hold/fade and eventual bounded expiry; explicit renderer clocks now advance both the initial presentation opportunity and later expiry. The existing Blaster30/60/120Hz timing assertion now derives expected flight time from the real muzzle distance rather than assuming a center-origin one-second trip. Increased visual durations/sizes and draw order are presentation-only. Manual screenshot approval is not claimed.

Follow-up verification: all C# compiled using the documented Roslyn/Godot source-generator path. ProjectileVisualChecks, GenericEnemyChecks (including Laser/Blaster/Shockwave), AmmunitionLoopChecks, CompactInspectorChecks and the final ConstructionChecks run all exited0. Construction initially found a stale700px-width assertion left by the earlier UI narrowing; it now correctly expects620px. Flash emit+presentation-step+expiry microbenchmark:10,000 cycles,6.945ms,0 managed bytes. Standard SDK and manual rendered-appearance limitations remain as documented above.
