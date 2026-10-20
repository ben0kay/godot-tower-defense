## Recoil presentation — 2026-10-20

Fixed BaseSprite and rotating TurretPivot/RecoilRoot/Sprite now split the finished artwork; Muzzles travel with that assembly. The original composite SVG remains the catalog/ghost icon. The scene's Recoil node assigns Visuals/RocketTowerRecoil.tres (Enabled, kick distance/duration, return duration, maximum accumulated offset). Each funded shot/volley snapshots its launch and flash before moving the gun. Generic disabling/destruction resets the pose; idle controllers sleep. Selection colors base and gun. Combat balance, upgrades, construction gates and targeting are unchanged. Runtime/build/visual verification is pending. See [shared recoil notes](../../Shared/Weapons/Recoil/README.md).

# Rocket Tower — alternating unguided splash rockets

Registered in `TOWERS/DefaultTowerCatalog.tres` under `rocket` (Offensive, 1×1). Two cyan-accented launch tubes use existing shared `BarrelFireController` in Sequential mode: one rocket per firing interval, alternating muzzle A/B **only after a funded shot**. The fixed root stays still; the turret pivot uses shared aiming/targeting and rank/upgrade/ammunition systems.

## Authored absolute levels

| Level | Credits | Work seconds | Damage | Rockets/s (total) | Range | Speed | Splash radius | Edge retention | Ammo capacity/per rocket |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
|1|350|5|12|2.0|400|500|42|25%|800 / 1|
|2|230|6|14|2.2|420|520|45|25%|800 / 1|
|3|320|7|16|2.4|440|540|48|25%|800 / 1|

All explosions use the existing `SplashProjectile → COMBAT/AreaDamage.ApplyInstant → Enemy.ApplyDamage` path and inherit normal Explosive shield/hull response. There is no extra direct hit. Unlike Blaster's guided shot, Rocket Tower initializes the same projectile with `trackTarget: false`, so it flies toward its captured launch-time target location; moving enemies may evade the centre. Its pre-existing area selection still catches all eligible enemies within the blast radius when it arrives. No target subscriptions or per-frame target sampling for unguided rockets.

`RocketTowerConfig` inherits Blaster's typed area weapon description/stats and independent presentation resources. `RocketTowerLevels` and `RocketTowerRanks` own numeric inputs. The 64px silvery/gunmetal SVG has two cyan emitters with muzzle positions ±10,-25. The rockets, flash and ring use light cyan accents; no new global damage VFX implementation. Other Blaster rounds retain their original orange color and guided behavior.

No homing, collision with terrain, trajectory prediction, rocket trails or tower-specific damage logic is introduced. Live Godot build/headless checks and at-scale appearance still require verification after checkout.
