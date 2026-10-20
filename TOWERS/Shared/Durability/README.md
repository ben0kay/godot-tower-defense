## Tower durability rebalance — 2026-10-20 (source updated; Godot runtime tests pending)

**Current authoritative defaults supersede older HP references below:** Base/max hull health for every available player-built tower/structure is **10× the previous level-1 value**. All **nine offensive towers** (Battle, Laser, Blaster Cannon, Shockwave, Machine Gun, Sniper, Rocket, Cryo, Stasis) have explicit, independent paid level snapshots: **1,000 → 1,050 → 1,102.5 HP** (two-level towers stop at 1,050). **Every purchased upgrade adds 5% of the previous tier's Max HP**, compounded, not 5 percentage points of the original at every tier; the values are authored as complete absolute tier stats, not runtime additive bonuses.

**Other buildings:** Ammunition Factory and Depot **1,000 HP** each (one build tier/no paid upgrades yet); Resource Tower and Adaptable Resource Generator **1,500 → 1,575 → 1,653.75 HP** (new tier progression replaces their former 150/200/250 HP schedule); Stealth Beacon **1,800 HP** (one tier). Generic passive config fallback is 1,000 HP. These are **hull**, not shields. Enemy damage, friendly firing stats, costs, unlocks, construction timing and Citadel HP are unchanged.

**Pipeline and HP semantics:** Stage02 still selects one authored tier, then Stage03 applies earned **rank HP bonuses independently**. The existing `TowerBase.Durability` synchronizes max capacity while **preserving missing absolute HP**; upgrades do not fully heal already-damaged towers. The current Inspector's `Max HP` comparison automatically shows the extra `(+50)` at level2 and `(+52.5)` at level3 for a standard-rank 1,000-HP offensive tower. No duplicate HP system, per-frame calculations, or tower-specific damage handlers were added.

**Verification still needed:** Compile Godot .NET, run `TESTS/Towers/TowerHealthChecks.tscn` and the existing integration scene that executes `TowerStatChecks`/`TowerUpgradeChecks`, plus `TESTS/Economy/CreditTowerChecks.tscn`. Inspect upgraded/damaged towers at runtime, including the resource producer `.tres` serialized tiers. Written test assertions were updated, but **not executed here**.

---

## EG2 hostile bullets — 2026-10-20

Optional enemy miniguns now deliver world-owned straight bullets whose first swept tower-footprint intersection calls the existing ApplyDamage(DamageInfo) once with Kinetic damage and null DamageSource. Existing HP, DefenseBar, Inspector, destruction and occupancy/work/logistics cleanup remain authoritative. No extra health model or direct fake hit is introduced; no current authored enemy/wave is armed. The F2 Gunner Drone comes in EG3. Build/runtime verification is pending. Earlier EG1-only/deferred-delivery wording below is historical.

## EG1 target lookup — 2026-10-20

../Registry now indexes placed/living tower roots and complete footprints for optional enemy weapon targeting. Destroyed immediately unregisters the hull; tree exit releases membership idempotently. Disabled and constructing towers remain valid living targets. This adds no new damage path: actual hostile bullets remain EG2 and must call the existing ApplyDamage(DamageInfo) resolver. Runtime/build verification is pending.

# Tower hull / HP foundation

`TowerBase.Durability.cs` owns per-instance `Health`, `MaxHealth`, `IsDestroyed`, `HealthChanged` and `Destroyed`. All nine registered towers/structures start full. The provisional baseline is **100 HP**; balance is intentionally unfinished.

- Offensive `*Levels.cs` tables accept an optional named `MaxHealth: 100` column per absolute purchased row. `TowerStatBlock.MaxHealth` follows Stage02 selection, Stage03 total rank `HpBonus`, Stage07 finite/minimum-1 safety, and Stage08 cached snapshots. Upgrade previews use the same source. No new stat pipeline.
- Every config exposes `MaxHealth` (the Level 1 row for weapons, standalone for Factory/Depot) in the Durability Inspector group. Passive types need no artificial weapon rows or rank system.
- Rank promotions, purchased upgrades and stat rebuilds **preserve missing HP**. Example: 75/100 becomes 175/200; damage is not erased. Reducing capacity below missing HP can destroy a structure. Damage/repair do not rebuild weapon stats.
- `ApplyDamage(DamageInfo)` uses `COMBAT/Damage/DamageResolver` with zero shields, including damage-type hull coefficients and overrides. `RestoreHealth(amount)` is an explicit repair API capped at maximum; no repair producer or regeneration is enabled. Both reject paused/deleted/destroyed owners. There is no tower shield mechanic yet.
- Zero HP latches destruction once, stops operation/work, queues deletion and publishes events. Existing `TreeExiting` cleanup clears selected Inspector, whole-footprint occupancy/navigation, logistics jobs/stock ownership and construction. No refund, kill credit, reward or salvage is generated. Already-emitted attacks keep their existing ownership/lifetime policy.

The existing `FEEDBACK/DefenseBars/DefenseBar.tscn` is attached to the **fixed root**, not the rotating weapon. It uses the same style, colors, logarithmic width, feedback switch and damaged-only visibility as enemies. Its offset clears the footprint. Health events update it without any processing tick. No tower-specific copy of the bar renderer exists.

Selected HP is combined into the existing runtime label. Rank bonuses include HP; changed next-tier maximum HP appears in the upgrade comparison. The 560×330 Inspector scene/config dimensions remain unchanged.

Enemy tower attacks (E3B), shields, armor, autonomous repairs/Repair Tower, destruction effects and campaign/battlefield persistence remain separate passes. This foundation does not make enemies damage towers automatically. Run `res://TESTS/Towers/TowerHealthChecks.tscn`; see `TESTS/Towers/TowerHealth.md`.
