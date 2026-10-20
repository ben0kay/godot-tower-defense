# PA1–PA3 projectile accuracy checks

Run `TESTS/Towers/ProjectileAccuracyChecks.tscn` headless after building/importing the Godot C# project. Covers authored levels, selected rank/research multiplication, preview parity, finite bounds, dormant homing capability, Rocket opt-out, moving/removed targets, pause/lifetime clipping, first swept contact including hidden blockers, one-time status, tangent/tie geometry, spread destinations and spatial-query budget. Samples are injected only for deterministic tests; production uses isolated launch RNG.

Desktop follow-up: inspect muzzle alignment, dense crossing enemies, splash dispersion and feel at all levels/ranks. Numerical spread is provisional tuning. Existing broad suites may also exercise paid placement and map setup independently of projectile handling.

## Validation recorded 2026-10-20

- `dotnet build --no-restore`: PASS, zero warnings/errors (Godot .NET 4.7.2, .NET 8 toolchain). Godot headless editor import completed.
- Headless `TESTS/Towers/ProjectileAccuracyChecks.tscn`: PASS; deterministic handling/rank/research/preview, launch spread, moving/removed target, clipped lifetime, swept first contact/status, explicit homing, splash and spatial budget.
- `TESTS/Towers/TowerConditionsChecks.tscn`: PASS (all seven weapons, heat/disable/pause and beam timing).
- `TESTS/Stealth/StealthChecks.tscn`: PASS (detection/locks/beacons/lifecycle and aimed weapons).
- Existing full `TowerRecoilChecks` fails at Rollout line181 (`Single`: no projectile). `BattleResearchChecks` fails paid battlefield setup (`Could not purchase battle`). `ProjectileVisualChecks` passes the multi-barrel/Sniper checks then fails Cryo paid placement (`Could not purchase cryo`). All three exact failures were reproduced in a separately rebuilt unchanged `98feb5e` worktree.
- `AmmunitionLoopChecks` passes foundations then fails the preexisting `Ammo Capacity` inspector-row assertion. The same assertion and stack location were reproduced on the separately rebuilt unchanged `98feb5e` baseline.
- Swept query benchmark: 1 nearby candidate among 201 enemies; 1,000 warmed calls measured 96,000 managed bytes in this runtime. No result buffers/collections or physics nodes are created per query; the regression budgets 128 bytes/call for runtime overhead. This is a bounded allocation budget, not a claim of zero allocation or a full-game FPS benchmark.
- Exact blast geometry fixtures explicitly set zero spread; guided lifecycle fixtures explicitly request Homing. Production spread remains authored. Desktop visual/balance review remains pending (PA4 manual QA).
