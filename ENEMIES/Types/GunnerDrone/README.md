## Compact starter Gunner Drone — 2026-11-04

This armed starter drone has a new **52×38** compact, narrower gunship silhouette (previously 64×48). Its separate minigun is now **20×40** (previously 24×40): the artwork is 20% slimmer horizontally with the original full barrel length and the same local `Muzzle=(0,-18)`. The mounting socket remains at `TurretPivot=(-2,0)` to keep muzzle alignment and aiming unchanged. Native import dimensions and the shared rear thruster naturally follow the new hull size.

The authoritative `GunnerDrone.tres` now sets **135 hull HP** instead of 180 (**−25%**), with collision/corridor clearance radius **18 px** instead of 22 to match the reduced footprint. The gun's red projectiles, tracer/muzzle flash, 6/s cadence, damage, search/aim logic, 75px/s ship speed, rewards, class/rank/role and Citadel damage are unchanged. No new process loops or sprite scaling hacks. F2 preview should now report the actual smaller size. See [art details](Artwork/README.md). Godot live zoom/aim/clearance review pending.

---

## Current handling — 2026-10-20

Continuous6/s firing is now default (UseBursts=false), with1s closest-target reconsideration while locked and0.2s idle searches. Invalid targets still drop immediately. Dedicated red tracers now have a rounded fuller front and thin fading tail:9px length,1.5px nominal width,1.25 head multiplier. Hull/gun art, normal routing, damage type and authored wave membership are unchanged. Build and GunnerDroneChecks/F2 passed headless; normal-zoom visible playtesting remains pending. Old burst/rectangular tracer settings below are historical.
---

# Gunner Drone — EG3

Stable ID gunner_drone_mk1; Class Standard, Rank Common, Role Fighter (metadata only). GenericEnemy inherited scene, optional enemy minigun, no unique enemy gameplay script. Appended to DefaultEnemyCatalog as entry14, so F2/F3 pick it up automatically. Current Level001/002/020 waves omit it.

## Size and artwork

The original EG3 medium reference was Enemy004 at 64×48 and radius22. The current starter gunner was later reduced to **52×38 with radius18**; its minigun was narrowed to **20×40**, centre(10,20), tip(10,2), so Marker2D remains (0,-18). TurretPivot is(-2,0) on the hull's circular socket and initially points right. No hull rotation is performed by the weapon.

Scene path: Sprite/Weapons/Minigun/TurretPivot/GunSprite/Muzzle. Weapons and Minigun are Node2D so transforms follow the hull. Being under Sprite also inherits stealth alpha without polling. DefenseBar, hull/status/shields, routes, checkpoint/Citadel assignment and arrivals retain GenericEnemy ownership. Existing gun Freeze gating remains in the shared mount.

## Editable playtest settings

GunnerDrone.tres now owns135HP,0shield,75px/s,18px radius,5.5 hull turn speed,2 Citadel damage,8 Credits,1.35 death explosion scale. Weapon Config references Weapons/Minigun/DefaultEnemyMinigun.tres:260px range,0.2s staggered searches,240deg/s turn,6deg tolerance,6 shots/s,1.5s burst/1s rest,2 Kinetic damage,1200px/s straight bullet speed,2.5s lifetime. No spread or homing is added.

Minigun/Visuals owns dedicated muted coral-red tracers and5×3px65ms orange-red flashes. BrightTip is false and glow is faint. Only accepted world-system launches flash; cooldown/invalid/overflow opportunities do not. Source removal clears its flash renderer but never its emitted bullets. Budgets/collision/damage remain EG2 services, with no per-bullet scene nodes.

Open a playable Campaign/Main launcher and F2 through the catalog to entry14(after Heavy Transporter), then place towers near the route. Authoring waves with this ID is possible but no wave was changed by EG3. All balance is provisional and Inspector editable.

Verification: TESTS/EnemyWeapons/GunnerDroneChecks.tscn and README.md. No import/build/runtime/manual visuals were executed in this environment. Normal-zoom and1–10-gunner firefight acceptance remains pending; EG4 profiling follows separately.
