# Shared tower helpers

Targeting/TowerTargeting.cs supports First (shortest remaining valid route to the Citadel) and Closest (nearest to the tower), filtering living enemies within weapon range. Projectiles/BasicProjectile.cs provides a lightweight visible guided test bullet calling Enemy.TakeDamage. Both can be reused by future offensive towers without forcing utility/economy towers to inherit weapon behavior.

Weapons/TurretAiming.cs provides shortest-angle, speed-capped aiming and tolerance checks for an up-facing visual pivot. Only offensive towers opt in; utility towers need no aiming component.
