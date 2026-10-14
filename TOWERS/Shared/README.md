# Shared tower helpers

Targeting/TowerTargeting.cs supports First (shortest remaining valid route to the Citadel) and Closest (nearest to the tower), filtering living enemies within weapon range. Projectiles/BasicProjectile.cs provides a lightweight visible guided test bullet calling Enemy.TakeDamage. Both can be reused by future offensive towers without forcing utility/economy towers to inherit weapon behavior.

Weapons/TurretAiming.cs provides shortest-angle, speed-capped aiming and tolerance checks for an up-facing visual pivot. Only offensive towers opt in; utility towers need no aiming component.

Stats/ now provides typed read-only values/context, immutable final snapshots, opt-in traces and the single eight-stage numbered pipeline. Only base input, safety validation and final publication are active in Phase 3A; future progression/effect stages remain neutral. No upgrade purchases or managers are added.

TowerBase and Upgrades/ implement lightweight per-instance level progression and generic typed tier/modifier data, paid atomically through ECONOMY. Stat Stage 02 applies the purchased tiers; weapon loops stay optional. See Upgrades/README.md.
