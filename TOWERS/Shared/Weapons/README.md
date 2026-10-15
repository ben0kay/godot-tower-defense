# Weapon aiming

TurretAiming.Track turns an up-facing visual pivot toward a world target and reports alignment. BattleTower calls it before cooldown-based firing. Rotation speed, tolerance and tracking are held in the tower config; the helper owns no target search, damage or economy.


Phase 4C-2: Laser reuses TurretAiming and TowerTargeting; Beams/ owns reusable endpoint-only visual presentation and BeamStyle. LaserTower owns sustained base DPS delivery, COMBAT/Damage owns defenses. There is no forced weapon node/tick on passive TowerBase instances.

**PLANNED delivery distinction:** The 4D Blaster Cannon uses a projectile-triggered **instant circular damage event**; its visual blast is independent. [`Shockwaves/README.md`](Shockwaves/README.md) reserves true **expanding edge-only damage over time** for Phase 4E, using swept annulus crossing and hit-once tracking. Both can reuse `COMBAT/AreaDamage/` distance falloff and `COMBAT/Damage/` shield/hull resolution, but must not share a repeated full-disc damage loop.
