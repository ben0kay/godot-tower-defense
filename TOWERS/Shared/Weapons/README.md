# Weapon aiming

TurretAiming.Track turns an up-facing visual pivot toward a world target and reports alignment. BattleTower calls it before cooldown-based firing. Rotation speed, tolerance and tracking are held in the tower config; the helper owns no target search, damage or economy.


Phase 4C-2: Laser reuses TurretAiming and TowerTargeting; Beams/ owns reusable endpoint-only visual presentation and BeamStyle. LaserTower owns sustained base DPS delivery, COMBAT/Damage owns defenses. There is no forced weapon node/tick on passive TowerBase instances.

**Implemented delivery distinction (Phase4E):** Blaster uses one projectile-triggered instant disc hit, followed by a cosmetic expanding ring. ShockwaveEmitter instead advances a real damage front, with swept relative crossings and per-emission hit-once state. It reuses AreaDamage.AtDistance and shared defenses, never repeated full-disc damage. Both use FEEDBACK/Shockwave with weapon-owned styles; only gameplay controls timed wave radius. See Shockwaves/README.md.

PV1/PV2 are implemented in [Visuals](Visuals/README.md): launch-snapshotted tracers, bounded muzzle flashes and optional Sniper red aiming. Per-tower presets own appearance; no combat-stat or HUD changes.
