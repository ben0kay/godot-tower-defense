## CTX1 — shared periodic-damage family

**Source Implemented / Awaiting Owner Validation.** Appended `Corruption=2` after existing Slow/Freeze IDs. `DamageOverTimeStats` is an immutable DPS/duration capability; `StatusEffectPayload` carries strength (DPS for DoT), duration and an optional existing weak DamageSource. Both physical and instant delivery forward the launched damage token when applying surviving-target status. Runtime clocks remain on the enemy.

`StatusEffectRules.IsDamageOverTime` is the single family extension seam: another future DoT identity reuses the same storage, refresh and ticking code after adding its enum/mapping. Corruption uses one-second hull-only Energy DamageInfo with penetration1 and hull override1; effect identity is not a new DamageType. Reapplications keep stronger DPS, reset to the new duration, retain tick phase and transfer subsequent tick ownership to the latest successful source. Final fractions are paid at expiry; hitch ticks batch equivalently without catch-up loops. See ENEMIES/StatusEffects and the Corruptor README.

---

# Status payloads

Status identities (`StatusEffectId`) are separate from `DamageType`. Immutable `StatusEffectPayload` captures strength at launch. Only permanent Slow is implemented now; it requires a finite fraction strictly between zero and one. `BasicProjectile` carries an optional value alongside immutable DamageInfo, resolves damage once, then applies the effect only to a surviving valid enemy. Existing Kinetic shots have no payload and retain their appearance and damage behavior.

Runtime storage, immunity/stacking rules, costs and future Freeze/DoT extension contracts are documented in `ENEMIES/StatusEffects/README.md`. Damage coefficients and XP remain in the existing damage/attribution systems. A payload retains no config or tower reference, so removing/upgrading the tower does not invalidate an in-flight effect.


## Stasis ST1 — 2026-10-18

Freeze=1 follows the existing Slow=0 identity, preserving authored immunity numbers. StatusEffectPayload now includes explicit DurationSeconds (finite, positive for Freeze), while Slow keeps its original two-argument fraction constructor. Default/invalid payloads are rejected. Projectile impacts still apply damage once before status to surviving eligible targets. Freeze does not change Energy damage or shield coefficients; shields do not block status. EnemyStatusEffects and its world-owned active-only scheduler own refresh/expiry; see ENEMIES/StatusEffects/README.md.
