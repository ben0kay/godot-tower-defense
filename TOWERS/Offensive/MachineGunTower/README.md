# Machine Gun Tower — two sequential barrels

A 1×1 offensive tower with one aim target and **two independently positioned muzzles**. `TurretPivot/Muzzles/MuzzleA` and `MuzzleB` are Marker2D nodes at -9/+9 horizontal and -25 vertical local coordinates. The single shared `ProjectileTurretBase` launches guided Kinetic bullets from **A → B → A → B**, with `BarrelFireController` advancing only after a successful funded shot.

| Purchased level | Credits (build/upgrade) | Damage/bullet | Total bullets/s | Range | Projectile speed |
|---|---:|---:|---:|---:|---:|
| 1 | 300 | 7 | 14.4 | 208 px | 1062.5 px/s |
| 2 | 200 | 8 | 14.88 | 216 px | 1062.5 px/s |
| 3 | 300 | 9 | 15.36 | 224 px | 1062.5 px/s |

Rate applies to **both barrels combined**: 14.4 total shots/s means ~7.2 shots/s per barrel; it is not 6 per barrel. Ammo reserve 1,200, consumption 1 per bullet (from physical shared logistics). No shield penetration; ordinary Kinetic damage coefficients apply. Standard → Veteran → Enhanced → Superior → Legendary use the normal 0/1,000/3,000/7,000/15,000 total XP table. Paid level and earned rank remain independent.

The config exposes optional `Single`, `Sequential`, and `Simultaneous` modes for future prototypes. Simultaneous uses all barrels per volley, with total-bullets/s and ammo costs preserved; do not silently double DPS. No rotary barrel animations, barrel-specific overheating, accuracy, independent targets, recoil or special machine-gun ammo yet. All visual art is a local 64×64 SVG; no HUD C# was touched.

**Fire-rate rebalance (October 2026):** The original GameMaker `Machine Gun Tower` used `fire_rate: 5` frames at `option_game_speed: 60`, or 12 bullets/second. An earlier Godot rebalance matched that Level 1 cadence (12 shots/s), up from 6 bullets/s. Its level 2/3 rates were then 12.4/12.8; the shared projectile-turret cooldown preserves fractional subframe time so those upgrades matter at the default 60 Hz physics rate. The alternation sequence, damage per bullet, muzzle locations, ammunition/round, range, rank XP, and credits prices are unchanged. Overheat from the old game has **not** been ported.

## Initial construction (C1/C2)

Paid placement occupies its footprint immediately and takes **4 seconds**, authored in the Level1 WorkSeconds column of this tower's `*Levels.cs`. It cannot acquire targets/fire/consume ammunition or request deliveries until complete. Completion preserves stats, level, rank and stock. Direct scene fixtures remain operational by default. Current final artwork stays visible; stage SVGs/VFX (C4) remain deferred. C3 Inspector progress is implemented; purchased upgrades use their own WorkSeconds and retain old-tier operation until completion. Duplicate purchases are disabled while work is active. See `TOWERS/Shared/Construction/README.md`.

## Projectile presentation — PV1/PV2

Implemented 12px narrow amber tracer and brief orange/white cone at each actual A/B barrel. Tower-owned presets live in `Visuals/` and are assigned in `DefaultMachineGunTowerConfig.tres` (Weapon Visuals). Presentation never enters level/rank stat tables. Shared behavior and authoring: [Visuals](../../Shared/Weapons/Visuals/README.md). Regression evidence: [ProjectileVisuals](../../../TESTS/Towers/ProjectileVisuals.md). Manual appearance review at normal gameplay zoom remains pending.

**Current firing rate (additional +20%):** The two barrels now fire **14.4 / 14.88 / 15.36 total bullets/s** at Levels 1–3, up from 12 / 12.4 / 12.8. Alternate muzzle order stays A → B → A; a level-one barrel contributes an average of 7.2 shots/s. That rebalance changed only cadence (and therefore ammo used per second), not damage per shot, travel speed, cost, range, construction time, rank XP, or bullet/muzzle effects. Real 60Hz simulation checks now use all three updated rates.

**Projectile-speed rebalance (+25%, October 2026):** All three levels now launch Kinetic tracers at **1,062.5 px/s** instead of 850 px/s. The two barrels still alternate A → B → A, with unchanged 14.4 / 14.88 / 15.36 **total** bullets/s across both barrels. Damage, ammunition/round, visuals, ranks and prices remain unchanged.
