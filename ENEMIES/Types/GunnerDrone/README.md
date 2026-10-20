## Current handling — 2026-10-20

Continuous6/s firing is now default (UseBursts=false), with1s closest-target reconsideration while locked and0.2s idle searches. Invalid targets still drop immediately. Dedicated red tracers now have a rounded fuller front and thin fading tail:9px length,1.5px nominal width,1.25 head multiplier. Hull/gun art, normal routing, damage type and authored wave membership are unchanged. Build and GunnerDroneChecks/F2 passed headless; normal-zoom visible playtesting remains pending. Old burst/rectangular tracer settings below are historical.
---

# Gunner Drone — EG3

Stable ID gunner_drone_mk1; family Drone. GenericEnemy inherited scene, optional enemy minigun, no unique enemy gameplay script. Appended to DefaultEnemyCatalog as entry14, so F2/F3 pick it up automatically. Current Level001/002/020 waves omit it.

## Size and artwork

The medium reference is Enemy004/Bulwark Drone:64×48 canvas and22px navigation radius. gunner_drone.svg matches that size with a distinct angular gunship hull. gunner_minigun.svg is a separate24×40 up-facing gun asset; center(12,20), tip(12,2), so Marker2D is(0,-18). TurretPivot is(-2,0) on the hull's circular socket and initially points right. No hull rotation is performed by the weapon.

Scene path: Sprite/Weapons/Minigun/TurretPivot/GunSprite/Muzzle. Weapons and Minigun are Node2D so transforms follow the hull. Being under Sprite also inherits stealth alpha without polling. DefenseBar, hull/status/shields, routes, checkpoint/Citadel assignment and arrivals retain GenericEnemy ownership. Existing gun Freeze gating remains in the shared mount.

## Editable playtest settings

GunnerDrone.tres owns180HP,0shield,75px/s,22px radius,5.5 hull turn speed,2 Citadel damage,8 Credits,1.35 death explosion scale. Weapon Config references Weapons/Minigun/DefaultEnemyMinigun.tres:260px range,0.2s staggered searches,240deg/s turn,6deg tolerance,6 shots/s,1.5s burst/1s rest,2 Kinetic damage,1200px/s straight bullet speed,2.5s lifetime. No spread or homing is added.

Minigun/Visuals owns dedicated7×1.3px muted coral-red tracers and5×3px65ms orange-red flashes. BrightTip is false and glow is faint. Only accepted world-system launches flash; cooldown/invalid/overflow opportunities do not. Source removal clears its flash renderer but never its emitted bullets. Budgets/collision/damage remain EG2 services, with no per-bullet scene nodes.

Open a playable Campaign/Main launcher and F2 through the catalog to entry14(after Heavy Transporter), then place towers near the route. Authoring waves with this ID is possible but no wave was changed by EG3. All balance is provisional and Inspector editable.

Verification: TESTS/EnemyWeapons/GunnerDroneChecks.tscn and README.md. No import/build/runtime/manual visuals were executed in this environment. Normal-zoom and1–10-gunner firefight acceptance remains pending; EG4 profiling follows separately.
