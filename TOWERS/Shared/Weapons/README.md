# Weapon aiming

TurretAiming.Track turns an up-facing visual pivot toward a world target and reports alignment. BattleTower calls it before cooldown-based firing. Rotation speed, tolerance and tracking are held in the tower config; the helper owns no target search, damage or economy.


Phase 4C-2: Laser reuses TurretAiming and TowerTargeting; Beams/ owns reusable endpoint-only visual presentation and BeamStyle. LaserTower owns sustained base DPS delivery, COMBAT/Damage owns defenses. There is no forced weapon node/tick on passive TowerBase instances.
