# E3A DeathSpawn / Transporter verification

Build C# with a matching Godot .NET SDK, then run:

```sh
dotnet build
godot --headless --path . res://TESTS/Enemies/DeathSpawnChecks.tscn
```

The standalone runner keeps the real mission controller/waves and uses private open-terrain/settings fixtures. Checks are split into small lifecycle/results, safety, checkpoint and scheduling groups. Explicit queue frame input makes 30/60/120Hz checks deterministic; a separate real-physics test enables the automatic scheduler. No authored level wave/map is saved or changed.

## Coverage

- Real catalog Transporter:112×72 artwork,500 hull,45px/s,24px routing radius,15-Credit reward; six default Standard Drones near the captured death point, one per0.12s (within physics-tick quantization), never synchronously or in the parent's physics frame. No live per-carrier timer.
- Kill-only/idempotent trigger; shield-only hits, arrival, external free and unload do not release. Normal parent feedback/reward remains separate. Actual attributed parent/child damage grants ordinary tower XP; creating children grants none.
- Independent child hull/shield capacity/starting policy, valid local radius clearance, dispersed positions, own movement profile/seed/permissions, no grandchildren when the chosen child also has DeathSpawn.
- Multiple simultaneous/zero-interval jobs share the global budget, cap failures retire slots, a newly free active slot can admit a later child, all blocked attempts retire without holding the wave forever. Unknown IDs/invalid counts warn and do not substitute another enemy. Narrow lanes reduce scatter safely; newly occupied origins never teleport children back to a portal.
- Before/mid/after branched checkpoints: inherited current next target, completed count, captured network and completion flag. Post-death link edits do not rewrite that snapshot. Opt-out means direct Citadel routing. Removed required destinations cause bounded safe skips.
- Final wave carrier on both launchers: PendingDeathSpawns reserved before parent removal; no premature R1 victory, PlannedRemaining remains authored-only, each child attaches before slot retirement, all seven official kills/rewards included at actual victory. Authored carrier killed on entry Lava is owned/reserved before Initialize can remove it.
- F2/unowned children do not join or prolong an unrelated active wave, consume its RNG or count as official wave enemies. Spawn scatter also leaves the portal RNG unchanged. Reconfiguration (including inside a reservation callback) and unload cancel jobs without negative counts, stale callbacks or next-level leakage.
- Pause freezes the unpaused release clock; real automatic release works;30/60/120Hz intervals remain correct. Stress with500 existing enemies plus25 simultaneous carrier deaths releases150 children in75 ticks at no more than2 attempts/tick. New child A* requests obey the existing separate path budget.

## Actual execution — 2026-10-16

Godot4.7.2 Mono / .NET8.0.31 headless:

- C# compilation passed with Roslyn4.11 plus the matching Godot source generator/reference assemblies, no diagnostics. `dotnet build --no-restore` is blocked before compilation by this environment's missing SDK dependency `NuGet.Protocol, Version=6.11.2.1`. Editor asset import completed successfully.
- DeathSpawnChecks passed with process exit0, including the final frame-gating and reentrant-cancellation checks. ResultsChecks and PauseControlsChecks passed with exit0. MenuTowerFlowChecks, FootprintChecks, AmmunitionLoopChecks, CheckpointChecks, RoutePreviewChecks, CorridorNavigationChecks and PlacementPerformanceChecks passed with exit0.
- Queue-only peak creation CPU in the500-active/150-child stress fixture ranged from **1.636ms** in an earlier run to **7.953ms** in the final concurrent test batch. Each run stayed at2 attempts/tick and75 ticks. These are host-specific observed peaks, not rendered FPS or a universal frame-time promise; synchronous scene creation/native work can vary. The count budget is strict.
- The unchanged full EnemyIntegrationChecks still fails the preexisting Level002 painted-terrain assertion (“The western open ground and eastern maze are fully painted”). A separate diagnostic build temporarily omitted only `Level002Checks.Run()`, restored the tracked source immediately, and exited0 through the remaining31 enemy/navigation/combat checks: existing waves, ShieldEmitter, rewards/XP, damage/shields, beams/projectiles/AoE/wavefronts, spatial queries, levels/ranks, wallet and repathing. This is not an unmodified full-suite pass.
- An early PauseControls run printed PASS then exited139 during Mono/native shutdown. It was not counted as a pass; the runner now uses the existing test-only freed-fixture finalization barrier before quitting, and reruns exited0. No forced garbage collection was added to gameplay.
- Desktop visual quality/playfeel was not manually inspected. Headless checks verify artwork size, valid positions, timings, routing and lifecycle; a desktop playtest can review the deliberately large hull artwork relative to its24px collision disc.

E3B enemy weapons/tower durability, multiple spawn generations, arbitrary patterns, automatic changes to authored waves, saving and research remain out of scope. Inspector authoring is documented in `ENEMIES/Abilities/DeathSpawn/README.md` and `ENEMIES/Types/Transporter/README.md`.
