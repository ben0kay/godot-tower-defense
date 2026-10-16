# Machine Gun Tower — two sequential barrels

A 1×1 offensive tower with one aim target and **two independently positioned muzzles**. `TurretPivot/Muzzles/MuzzleA` and `MuzzleB` are Marker2D nodes at -9/+9 horizontal and -25 vertical local coordinates. The single shared `ProjectileTurretBase` launches guided Kinetic bullets from **A → B → A → B**, with `BarrelFireController` advancing only after a successful funded shot.

| Purchased level | Credits (build/upgrade) | Damage/bullet | Total bullets/s | Range | Projectile speed |
|---|---:|---:|---:|---:|---:|
| 1 | 300 | 7 | 12.0 | 208 px | 850 px/s |
| 2 | 200 | 8 | 12.4 | 216 px | 850 px/s |
| 3 | 300 | 9 | 12.8 | 224 px | 850 px/s |

Rate applies to **both barrels combined**: 12 total shots/s means ~6 shots/s per barrel; it is not 6 per barrel. Ammo reserve 1,200, consumption 1 per bullet (from physical shared logistics). No shield penetration; ordinary Kinetic damage coefficients apply. Standard → Veteran → Enhanced → Superior → Legendary use the normal 0/1,000/3,000/7,000/15,000 total XP table. Paid level and earned rank remain independent.

The config exposes optional `Single`, `Sequential`, and `Simultaneous` modes for future prototypes. Simultaneous uses all barrels per volley, with total-bullets/s and ammo costs preserved; do not silently double DPS. No rotary barrel animations, barrel-specific overheating, accuracy, independent targets, recoil or special machine-gun ammo yet. All visual art is a local 64×64 SVG; no HUD C# was touched.

**Fire-rate rebalance (October 2026):** The original GameMaker `Machine Gun Tower` used `fire_rate: 5` frames at `option_game_speed: 60`, or 12 bullets/second. This Godot version now matches that Level 1 cadence, compared with its earlier 6 bullets/s. The level 2/3 authored rates are 12.4/12.8; the shared projectile-turret cooldown preserves fractional subframe time so those upgrades matter at the default 60 Hz physics rate. The alternation sequence, damage per bullet, muzzle locations, ammunition/round, range, rank XP, and credits prices are unchanged. Overheat from the old game has **not** been ported.

## Initial construction (C1/C2)

Paid placement occupies its footprint immediately and takes **4 seconds**, authored in the Level1 WorkSeconds column of this tower's `*Levels.cs`. It cannot acquire targets/fire/consume ammunition or request deliveries until complete. Completion preserves stats, level, rank and stock. Direct scene fixtures remain operational by default. Current final artwork stays visible; stage SVGs/VFX (C4), timed upgrades and a full Inspector progress display (C3) remain deferred. Upgrades are blocked while constructing, then still apply instantly. See `TOWERS/Shared/Construction/README.md`.
