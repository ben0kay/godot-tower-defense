# Status payloads

Status identities (`StatusEffectId`) are separate from `DamageType`. Immutable `StatusEffectPayload` captures strength at launch. Only permanent Slow is implemented now; it requires a finite fraction strictly between zero and one. `BasicProjectile` carries an optional value alongside immutable DamageInfo, resolves damage once, then applies the effect only to a surviving valid enemy. Existing Kinetic shots have no payload and retain their appearance and damage behavior.

Runtime storage, immunity/stacking rules, costs and future Freeze/DoT extension contracts are documented in `ENEMIES/StatusEffects/README.md`. Damage coefficients and XP remain in the existing damage/attribution systems. A payload retains no config or tower reference, so removing/upgrading the tower does not invalidate an in-flight effect.


## Stasis ST1 — 2026-10-18

Freeze=1 follows the existing Slow=0 identity, preserving authored immunity numbers. StatusEffectPayload now includes explicit DurationSeconds (finite, positive for Freeze), while Slow keeps its original two-argument fraction constructor. Default/invalid payloads are rejected. Projectile impacts still apply damage once before status to surviving eligible targets. Freeze does not change Energy damage or shield coefficients; shields do not block status. EnemyStatusEffects and its world-owned active-only scheduler own refresh/expiry; see ENEMIES/StatusEffects/README.md.
