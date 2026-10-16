# Machine Gun Tower — two sequential barrels

A 1×1 offensive tower with one aim target and **two independently positioned muzzles**. `TurretPivot/Muzzles/MuzzleA` and `MuzzleB` are Marker2D nodes at -9/+9 horizontal and -25 vertical local coordinates. The single shared `ProjectileTurretBase` launches guided Kinetic bullets from **A → B → A → B**, with `BarrelFireController` advancing only after a successful funded shot.

| Purchased level | Credits (build/upgrade) | Damage/bullet | Total bullets/s | Range | Projectile speed |
|---|---:|---:|---:|---:|---:|
| 1 | 300 | 7 | 6.0 | 208 px | 850 px/s |
| 2 | 200 | 8 | 6.2 | 216 px | 850 px/s |
| 3 | 300 | 9 | 6.4 | 224 px | 850 px/s |

Rate applies to **both barrels combined**: 6 total shots/s means ~3 shots/s per barrel; it is not 6 per barrel. Ammo reserve 1,200, consumption 1 per bullet (from physical shared logistics). No shield penetration; ordinary Kinetic damage coefficients apply. Standard → Veteran → Enhanced → Superior → Legendary use the normal 0/1,000/3,000/7,000/15,000 total XP table. Paid level and earned rank remain independent.

The config exposes optional `Single`, `Sequential`, and `Simultaneous` modes for future prototypes. Simultaneous uses all barrels per volley, with total-bullets/s and ammo costs preserved; do not silently double DPS. No rotary barrel animations, barrel-specific overheating, accuracy, independent targets, recoil or special machine-gun ammo yet. All visual art is a local 64×64 SVG; no HUD C# was touched.
