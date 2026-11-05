# Heavy Gunner Drone — implemented (HG1)

**New independent enemy** `heavy_gunner_drone_mk1`, display name **Heavy Gunner Drone**, Class **Heavy**, Rank **Veteran**, Role **Fighter**. A tankier, slower, more dangerous armed cousin to the lightweight Gunner Drone. The starter ship (`gunner_drone_mk1`) and its art, damage, firing cadence, bullet visuals and health are **unchanged**.

| Setting | Starter Gunner | Heavy Gunner |
| --- | ---: | ---: |
| Hull SVG | 48×30 | **62×42** |
| Rotating gun SVG | 12×44 | **18×50** |
| Hull HP | 135 | **260** |
| Move speed | 75 px/s | **52 px/s** |
| Navigation radius | 18 px | **24 px** |
| Turn speed | 5.5 | **4.4** |
| Shields | 0 | **0** |
| Citadel arrival damage | 2 | **4** |
| Kill reward | 8 Credits | **14 Credits** |
| Death explosion multiplier | 1.35 | **1.65** |
| Gun range | 260 px | **275 px** |
| Shots/sec | 6 | **3.5** |
| Shot damage | 2 Kinetic | **4 Kinetic** |
| Bullet speed | 1200 px/s | **850 px/s** |
| Bullet lifetime | 2.5s | **2.8s** |
| Red tracer dimensions | 9×1.5 | **11×2.2** |

## Implementation

`HeavyGunnerDrone.tscn` inherits `ENEMIES/Shared/GenericEnemy.tscn` and keeps the same weapon subnode layout as `GunnerDrone.tscn`, using the existing shared `EnemyMinigun` logic, shared snapshot-based world `EnemyBulletSystem` and shared muzzle flash renderer. **No bespoke enemy controller, new per-projectile nodes, duplicate AI, or extra per-ship update loop.** The gun is an independent centered rotating sprite `TurretPivot/GunSprite` mounted at hull-local `(-2,0)`. The physical barrel tip is local `(0,-21)` relative to the gun centre (SVG x=9,y=4). Gun sprite initially points right. The shared thruster automatically sizes from the larger hull.

`HeavyGunnerMinigun.tres` owns all changed weapon values. `Visuals/HeavyGunnerTracer.tres` defines a larger tapered **red/coral** projectile, preserving the starter's own assets untouched. `Visuals/HeavyGunnerMuzzleFlash.tres` supplies a subtly larger warm red muzzle flash. `HeavyGunnerDrone.tres` owns the independent hull, clearance, speed, rewards and classification.

Appended to the default enemy catalog after the starter Gunner: available in F2/debug and the Wave Editor by ID. **Existing authored waves are not modified** and won't automatically spawn the new type. For authoring, select ID `heavy_gunner_drone_mk1`.

## Verification

`TESTS/EnemyWeapons/GunnerDroneChecks.cs` now covers both armed drone identities, dimensions, proper muzzle alignment, weapon balance, source/shot visual independence, a live heavy shot in the existing world system and original starter checks. `TESTS/Enemies/FleetExpansionChecks.cs` includes Heavy Gunner in catalog/spawn/durability/reward coverage. Existing hard-coded catalog count expectations updated from 14 to 15. No Godot or `dotnet` runtime test was run in this source-only pass; owner in-game spawn/visual checks remain.
