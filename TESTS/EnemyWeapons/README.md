## Gunner polish verification — 2026-10-20

Executed with the local Godot4.7.2 .NET runtime and .NET8 SDK, using its local NuGet feed:

- Godot headless editor import: passed.
- dotnet build: passed,0 warnings/0 errors.
- EnemyWeaponChecks.tscn: passed (including new TargetHold and continuous-fire windows).
- EnemyBulletChecks.tscn: passed (including retained tapered geometry and existing real swept damage).
- GunnerDroneChecks.tscn: passed (updated energy-style snapshot, actual F2 cycle, flashes and tower HP loss).
- ProjectileVisualChecks.tscn: weapon/visual and multi-barrel sections passed; runner failed at Could not purchase cryo. The identical failure was reproduced on untouched baseline582eb89; this is not a regression from these changes.

Existing startup warnings about the missing configured EnemyPortal2 SpawnExit and tower-catalog fallback were printed. No desktop visual approval or EG4 throughput profiling is claimed. Older records saying execution was unavailable describe prior passes, not these runs.
---

## EG3 Gunner Drone verification — 2026-10-20

Added GunnerDroneChecks.cs/.tscn for actual14-entry catalog/F2 cycling, ordinary types remaining unarmed, unchanged authored wave membership, medium Enemy00464×48 size, separate gun SVG/muzzle, real independent aiming, small red visual snapshots, accepted-shot flashes, actual tower HP loss, cooldown/overflow flash rejection, captured presets, source lifetime, stealth alpha inheritance and Freeze. FleetExpansionChecks now expects14 entries. No tests were executed: no execution environment was available. Static source/reference review only; build/headless/manual visuals and throughput remain pending.

After importing/building in Godot .NET, run:

```sh
dotnet build
godot --headless --path . res://TESTS/EnemyWeapons/GunnerDroneChecks.tscn
godot --headless --path . res://TESTS/EnemyWeapons/EnemyWeaponChecks.tscn
godot --headless --path . res://TESTS/EnemyWeapons/EnemyBulletChecks.tscn
godot --headless --path . res://TESTS/Towers/ProjectileVisualChecks.tscn
godot --headless --path . res://TESTS/Enemies/FleetExpansionChecks.tscn
```

Manual: F2 through14 entries on a playable map, spawn1–10 gunners, place towers near the route and watch moving hulls/independent guns, actual barrel-tip7×1.3px coral tracers, tiny5×3px warm flashes, misses, tower HP/death and shooter death with surviving shots. Check pause, Freeze, selection/cleanup and normal camera zoom readability. Current normal waves must remain unchanged. EG4 benchmark testing remains separate. Earlier EG1/EG2-only status is historical.

---

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
