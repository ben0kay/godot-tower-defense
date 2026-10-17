# Shared tower foundation

## Permanent Battle damage research — 2026-10-17

Stage04 is now active for **captured TowerResearchBonuses**. Battle Tower Weapons I adds5% damage after Stage03's selected rank and before Stage07's safety clamps. DamagePerShot uses the existing rank convention (nearest/ties-to-even); at standard rank the three Battle tiers become32/35/38. Typed beam/wave damage retains fractional arithmetic if a future node explicitly targets such a tower; no such additional node is authored in this pass.

CitadelSession captures ResearchDeployment once on deployment. Placement passes the per-type immutable input into TowerBase.InitializeDefinition; TowerBase keeps it across paid upgrades, rank promotions and config edits. The same pipeline resolves actual shots, selected-tower Inspector, next-level comparison and build preview. No stat stage reads profile files or current menu state. Direct config/tooling calls remain neutral unless explicit research inputs are supplied, and profileless F6 remains neutral. New missions refresh bonuses; active missions never subscribe to purchase changes.

Research does not alter authored *Levels.cs values, attack rate, heat timing, ammo, wallet costs, or unrelated tower types. See TESTS/Research/BATTLE_RESEARCH.md. Older statements below about neutral Stage04 or deferred tower damage research are historical and superseded here.

---

## Research-first extension — 2026-10-17

TowerBase now publishes UpgradePurchased after the real wallet transaction commits. LevelRunStats subscribes only to placed towers for paid-upgrade totals and initial construction completion. Rank XP and stats are unaffected. The first permanent Research node affects the Citadel only; 04_PermanentResearch remains neutral for future tower-stat upgrades.

---

Phase 4A adds TowerConfig (Inspector identity, private complete level rows/recipes), TowerDefinition (stable ID, category, scene, config reference, icon and rectangular footprint (default 1×1)) and TowerCatalog (validated ID lookup). Name/description are read from Config; prices come from its C# level rows, never duplicated in definition/HUD. The default catalog registers Battle, Laser, Blaster Cannon and Shockwave towers. Unavailable types remain registered but cannot be built. Rebuild validates blank/duplicate IDs, costs, category, footprint, scene root, config compatibility and type-specific scene prerequisites; call Rebuild after editing registration data.

TowerBase owns type/definition, fixed grid anchor, footprint, placement/selection hooks, optional range capability, purchased UpgradeLevel, FinalStats and StatsChanged. Types supply config compatibility/application and resolution; it has no mandatory weapon nodes or processing. TowerConfig explicitly copies runtime rows/cost recipes per instance. Passive types can have no combat snapshot, range or upgrades.

Targeting/, Weapons/TurretAiming and Projectiles/ are optional existing weapon helpers. Battle and Laser retain their distinct projectile/continuous-beam attack loops while reusing shared targeting and aiming. Stats/ preserves the eight-stage resolver; purchased levels and earned ranks are active; research/conditions/effects remain neutral. Upgrades/ uses the sole ECONOMY wallet.


Phase 4C adds COMBAT/Damage typed response authority and optional Beams/ presentation. Laser Tower is the second actual weapon family, reusing targeting/aiming and the shared progression pipeline. Battle delivery remains guided projectiles; Laser delivery remains sustained DPS. No mandatory combat was added to TowerBase.

Phase4E: optional ShockwaveStats and ShockwaveEmitter provide timed outward contact, separate from instant AreaDamage. Purple Shockwave Tower emits independent fronts with per-emission snapshots/hit-once IDs. FEEDBACK/Shockwave handles shared visual-only rings for both weapons, with exact radius synchronization only when gameplay drives them. No weapon loop is required by passive types.

Ranks/ provides five named, XP-earned ranks independent of Level purchases. Every offensive type owns a *Ranks.cs table beside its *Levels.cs file; TowerBase owns private XP and event-driven promotion state. COMBAT/Attribution carries one weak source token per tower through attacks. Stage03 applies the current rank's total bonuses to the selected Level; the existing Inspector displays progress without polling. See Ranks/README.md for fixed enemy pools, shared Inspector XP multipliers, rounding and scope.

## Reserved future capability folders (README-only)

- [Construction](Construction/README.md): time-based build/upgrade state, with existing placement and wallet authority preserved.
- [Ammunition](Ammunition/README.md): optional per-tower reserve, magazine and reload state.
- [Power](Power/README.md): optional per-building draw, operational status and Inspector priority; the shared network belongs to [POWER](../Economy/Power/README.md).

Do not introduce mandatory processing or requirements to TowerBase before implementation.
