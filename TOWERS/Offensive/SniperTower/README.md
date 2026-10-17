# Sniper Tower — one-muzzle long-range Kinetic weapon

Implemented via the shared `TOWERS/Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs`; no bespoke targeting loop or HUD changes. The authored config enforces `Single` muzzle firing. Its one `Marker2D` sits at `TurretPivot/Muzzles/MuzzleA`; projectile launches from the barrel tip, follows the existing guided `BasicProjectile`, uses typed Kinetic shield/hull damage and standard tower XP. The scene is a 1×1 offensive tower available through `TOWERS/DefaultTowerCatalog.tres`.

| Purchased level | Credits (build/upgrade) | Damage/bullet | Total bullets/s | Range | Bullet speed | Penetration |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 325 | 80 | 0.35 | 448 px | 1,300 px/s | 15% |
| 2 | 225 | 88 | 0.37 | 456 px | 1,300 px/s | 15% |
| 3 | 325 | 96 | 0.40 | 464 px | 1,300 px/s | 15% |

Ranks: Standard 0, Veteran 1,000, Enhanced 3,000, Superior 7,000, Legendary 15,000 total XP with the existing rank damage/range percentages. Ammo: 600 local capacity, 1 round per bullet; normal physical logistics can resupply it.

Balance is provisional. Level 1 one-shots unshielded 60-HP Enemy001; its strength is long range/high per-hit damage, counterbalanced by slow cooldown and price. No headshot/crit mechanic, physics raycast, lead prediction, line-of-sight, overpenetration or special targeting behavior has been added.

## Initial construction (C1/C2)

Paid placement occupies its footprint immediately and takes **4 seconds**, authored in the Level1 WorkSeconds column of this tower's `*Levels.cs`. It cannot acquire targets/fire/consume ammunition or request deliveries until complete. Completion preserves stats, level, rank and stock. Direct scene fixtures remain operational by default. Current final artwork stays visible; stage SVGs/VFX (C4) remain deferred. C3 Inspector progress is implemented; purchased upgrades use their own WorkSeconds and retain old-tier operation until completion. Duplicate purchases are disabled while work is active. See `TOWERS/Shared/Construction/README.md`.

## Projectile presentation — PV1/PV2

Implemented 28px precision tracer and angular warm flash, plus an optional thin red aiming laser. Tower-owned presets live in `Visuals/` and are assigned in `DefaultSniperTowerConfig.tres` (Weapon Visuals). Presentation never enters level/rank stat tables. Shared behavior and authoring: [Visuals](../../Shared/Weapons/Visuals/README.md). Regression evidence: [ProjectileVisuals](../../../TESTS/Towers/ProjectileVisuals.md). Manual appearance review at normal gameplay zoom remains pending.
