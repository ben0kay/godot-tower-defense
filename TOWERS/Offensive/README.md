# Offensive towers

BattleTower is the working guided Kinetic projectile family. LaserTower is the sustained Energy DPS/beam family, with presentation separated in Shared/Weapons/Beams. Both use shared targeting/aiming, catalog-driven generic placement/HUD, canonical ResourceCost recipes and the same cached eight-stage stats/paid upgrades. COMBAT/Damage owns shield/hull responses. Missile and additional families remain planned; no splash, armor, line-of-sight, effects or heat system is implemented.

**PLANNED — Phase 4D:** [`BlasterCannon/README.md`](BlasterCannon/README.md) specifies the next offensive weapon. Its projectile produces one *instant* area hit with per-weapon edge-damage falloff, using [`COMBAT/AreaDamage/`](../../COMBAT/AreaDamage/README.md); no AoE gameplay is implemented yet. [`MissileTower/`](MissileTower/README.md) remains a separate future tower. A timed expanding shockwave is a different delivery mechanic, reserved for future Phase 4E.
