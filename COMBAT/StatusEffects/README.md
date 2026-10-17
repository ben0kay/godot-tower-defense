# Status payloads

Status identities (`StatusEffectId`) are separate from `DamageType`. Immutable `StatusEffectPayload` captures strength at launch. Only permanent Slow is implemented now; it requires a finite fraction strictly between zero and one. `BasicProjectile` carries an optional value alongside immutable DamageInfo, resolves damage once, then applies the effect only to a surviving valid enemy. Existing Kinetic shots have no payload and retain their appearance and damage behavior.

Runtime storage, immunity/stacking rules, costs and future Freeze/DoT extension contracts are documented in `ENEMIES/StatusEffects/README.md`. Damage coefficients and XP remain in the existing damage/attribution systems. A payload retains no config or tower reference, so removing/upgrading the tower does not invalidate an in-flight effect.
