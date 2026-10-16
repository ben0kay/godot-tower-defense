# Reusable aimed projectile turrets and muzzle sequencing

`ProjectileTurretBase.cs` is a **single** targeting / aiming / cooldown / ammo / guided bullet loop used by Sniper and Machine Gun Towers. Other existing towers are unchanged. `ProjectileTurretConfig.cs` keeps targeting, rotation and `BarrelFireMode` in their ordinary per-tower config resource. Each weapon owns complete numeric purchased levels and earned rank thresholds under `TOWERS/Offensive/<Type>/`.

## Muzzle contract

Each tower's `TurretPivot` contains its rotating `Sprite` and `Muzzles` Node2D holding **Marker2D** children. Their **local positions** are where projectiles originate. A cached `BarrelFireController` selects muzzle indices; there is **no node or physics tick per barrel**. Reuse it for any positive number of muzzles. Scene validation rejects missing/invalid muzzles before payment.

- **Single**: always fire muzzle index 0 (intended for Sniper).
- **Sequential**: fire A → B → C → ... → A one bullet at a time; only **successful** shots advance the index (Machine Gun: two muzzles).
- **Simultaneous**: fire once from every muzzle in one paid volley. Requires the **full ammunition cost** up front; insufficient ammo does nothing and doesn't advance. This mode is implemented but not used by the starting towers.

**Fire-rate policy:** `ShotsPerSecond` means total projectiles per second, **not each barrel's rate**. E.g. two-barrel sequential at 12 fires 6/s from each barrel; simultaneous at 12 fires two bullets every ~0.167s (also 12 bullets/s). Each individual projectile consumes `AmmunitionStats.PerShot` rounds and earns normal damage/rank XP only on actual impact. All volley bullets carry an immutable launch damage snapshot; guided projectiles reuse `BasicProjectile`, `COMBAT/Damage`, and `TowerTargeting`.

Scope: **no** barrel temperature/recoil/spread/reloading, separate per-barrel targeting, weapon changes for Battle/Laser/Blaster/Shockwave, HUD layout changes, or new autoloads. Existing authored ranks and shared damage rules apply unchanged. Upgrade rows affect cached stats only; ammo inventory persists. Godot import of new SVGs and a .NET/headless run are required to verify runtime behavior.

**Cadence precision:** A shared turret carries forward fractional cooldown overshoot (bounded to the current physics frame, at most 0.05 seconds), instead of rounding every interval up to a full physics tick. This is important at 12.4 and 12.8 bullets/s on a 60-Hz game. Long pauses/hitches still never emit more than one volley in one physics tick. `MultiBarrelTowerChecks` tests ten simulated seconds at each of 12 / 12.4 / 12.8 bullets/s.
