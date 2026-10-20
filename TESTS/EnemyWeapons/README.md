## EG2 verification record — 2026-10-20

EnemyBulletChecks.cs/.tscn now covers pure swept rectangle geometry; fast first-tower hits; multi-cell edges; successive bullets after lethal destruction; actual shared Kinetic HP loss; misses after target relocation; inclusive lifetime endpoint; expiry beyond lifetime; pause/malformed launch and delta guards; exact transformed-grid clipping; real mounted launch; source death independence; null friendly XP; capacity overflow/slot reuse; immutable/sanitized visuals; counters and no per-bullet scene children.

EnemyWeaponChecks retains the production TowerRegistry instead of replacing the service underneath the new bullet system. Its cadence check now verifies accepted central slots, with zero launch-time damage. Startup discovery remains an implementation path requiring runtime acceptance. Both fixtures use private worlds and leave source maps/catalog/waves unchanged.

Run after importing/building with your Godot .NET executable:

```sh
dotnet build
godot --headless --path . res://TESTS/EnemyWeapons/EnemyBulletChecks.tscn
godot --headless --path . res://TESTS/EnemyWeapons/EnemyWeaponChecks.tscn
godot --headless --path . res://TESTS/Towers/TowerHealthChecks.tscn
godot --headless --path . res://TESTS/Integration/EnemyIntegrationChecks.tscn
```

**Not executed:** build, headless/runtime, desktop visuals and performance benchmarks. No usable execution environment was available. Static resource/scene references and source were reviewed; do not treat authored checks as passing test evidence. EG3 Gunner Drone/F2/flash presets and EG4 profiling remain planned. Earlier EG1-only status below is historical. Manual EG2 testing may use a temporary mounted enemy; no production enemy is automatically armed.

---

# EG1 verification record

## Status — 2026-10-20

Source and static resource/scene review only. Godot import, C# build, runtime/headless tests and visual/manual review **have not run** because no execution environment was available. The checks are authored coverage, not passing runtime evidence. EG2 bullets/damage, EG3 Gunner Drone/art/F2 and EG4 benchmarking remain unimplemented.

## Run after pulling

Open the project in its Godot .NET editor and import/build scripts, then use the appropriate local Godot .NET executable:

```sh
dotnet build
godot --headless --path . res://TESTS/EnemyWeapons/EnemyWeaponChecks.tscn
godot --headless --path . res://TESTS/Towers/TowerHealthChecks.tscn
godot --headless --path . res://TESTS/Integration/EnemyIntegrationChecks.tscn
```

EnemyWeaponChecks covers actual container membership/startup discovery, independent worlds, deterministic closest-center ties, inclusive multi-cell bounds, root relocation/negative coordinates, queued/destruction cleanup, independent hull/gun heading, staggered acquisition, cached-lock range loss, pause, malformed deltas/config, captured-settings isolation, owner initialization/reuse/arrival, burst/rest/no-catch-up requests and unarmed Enemy001. Fixtures do not edit source waves/catalog or profiles.

Manual EG1 inspection can attach an EnemyWeaponMount or EnemyMinigun to a temporary debug copy of an enemy, with Node2D TurretPivot and a real GunSprite/Muzzle descendant. Keep any Weapons grouping Node2D. Assign a config, spawn/initialize normally and inspect independent aiming while the ship moves past placed towers; pause/resume, remove towers and unload the world. Plain existing enemies must remain unarmed. No red bullets, damage or new F2 entry should be expected until EG2/EG3.

Performance measurements for many armed enemies and hostile bullets belong to EG4. No frame-time/allocation throughput claims are made from source review.
