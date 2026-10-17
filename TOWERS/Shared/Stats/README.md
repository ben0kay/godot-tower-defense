# Tower stats — complete explicit levels

## Permanent Battle damage research — 2026-10-17

Stage04 is now active for **captured TowerResearchBonuses**. Battle Tower Weapons I adds5% damage after Stage03's selected rank and before Stage07's safety clamps. DamagePerShot uses the existing rank convention (nearest/ties-to-even); at standard rank the three Battle tiers become32/35/38. Typed beam/wave damage retains fractional arithmetic if a future node explicitly targets such a tower; no such additional node is authored in this pass.

CitadelSession captures ResearchDeployment once on deployment. Placement passes the per-type immutable input into TowerBase.InitializeDefinition; TowerBase keeps it across paid upgrades, rank promotions and config edits. The same pipeline resolves actual shots, selected-tower Inspector, next-level comparison and build preview. No stat stage reads profile files or current menu state. Direct config/tooling calls remain neutral unless explicit research inputs are supplied, and profileless F6 remains neutral. New missions refresh bonuses; active missions never subscribe to purchase changes.

Research does not alter authored *Levels.cs values, attack rate, heat timing, ammo, wallet costs, or unrelated tower types. See TESTS/Research/BATTLE_RESEARCH.md. Older statements below about neutral Stage04 or deferred tower damage research are historical and superseded here.

---

Author combat values and credit prices in each tower's `*Levels.cs` file. Its labeled, strongly typed rows contain actual values at that level, including aiming/search interval, penetration, optional beam DPS or area radius/edge retention, and applicable shield/hull response overrides. There are no purchased modifier resources or duplicate numeric Inspector defaults.

TowerConfig holds private runtime row/recipe copies. Its C# Level 1 convenience accessors forward directly to row 1; they are not separate sources and do not affect other levels. For any level use `config.SetLevelStats(level, config.Levels[level - 1].Stats with { ... })`. This replaces an absolute snapshot and emits Changed once. Direct low-level row edits require RebuildStats; use CreateInstanceCopy rather than Godot Duplicate for copying runtime rows/recipes.

The single [pipeline](Pipeline/README.md) retains all eight stages. Stage 01 establishes Level 1; Stage 02 substitutes the selected full row; Stage03 applies the current earned rank; Stages04–06 remain neutral. Stage 07 retains finite safety bounds: nonnegative range/rate/damage/rotation/DPS/radius, projectile speed >= 1, search interval >= .05s, aim 0–180°, penetration/edge retention 0–1. Damage responses retain DamageResolver's independent override validation and fallback policy. Stage 08 publishes immutable TowerFinalStats.

BeamStats and AreaBlastStats remain optional; WeaponDamageStats carries damage type/independent override flags/coefficients on weapons that use them. Laser beams and Blaster launch payloads read the selected cached response, not the Level 1 config. Battle keeps Kinetic guided delivery and 5% penetration at every authored level.

Ready, Config.Changed/replacement and successful purchases rebuild the cache; normal physics/beam ticks reuse FinalStats. StatsChanged refreshes the selected gameplay Inspector/range ring. Upgrade previews use the same resolver as the paid snapshot. Target priority, tracking policy, artwork and visual style stay outside numerical resolution. Passive fixtures still need no combat values or attack loop.

TowerStatChecks compares every field and price at Battle 1–3, Laser 1–2 and Blaster 1–2; checks Stage 02 substitution, level independence, append-only expansion, bounds, immutable caches and no per-tick rebuild. TowerUpgradeChecks/LaserTowerChecks/BlasterCannonChecks cover real purchases, Inspector parity and combat. See TESTS/README.md for execution results.

## Phase4E optional travelling-wave capability

ShockwaveStats(DamagePerWave, ExpansionSpeedPixelsPerSecond, IntervalSeconds, EdgeDamageFraction) appends optionally to TowerStatBlock/FinalStats; existing constructors/towers remain unchanged. Maximum radius uses shared RangePixels, avoiding a second radius authority. ShockwaveTowerLevels.cs supplies complete independent rows and prices. Stage02 selects the whole row; Stage03 applies the current earned rank; Stages04–06 remain neutral; Stage07 clamps finite damage >=0, speed >=1, interval >=.05s and edge0–1; Stage08 publishes the same cache used by attacks/Inspector/previews. Future research can modify these capability values through its existing stage and cache invalidation; it is not implemented. Style resources never enter combat math.

Ranks are explicit immutable *Ranks.cs rows beside each *Levels.cs table. TowerStatContext carries only the selected rank, and Stage03 applies that row's total Damage/Range percentages once. Purchase previews retain the instance's Rank/XP; ordinary XP progress never invalidates FinalStats, while automatic promotions do. HP columns now scale MaxHealth; runtime hull state lives in Shared/Durability and preserves missing HP on changes. UI ExperienceChanged and combat StatsChanged remain separate. See Shared/Ranks/README.md relative to TOWERS/ for exact attribution/budget/rounding policy.

## Optional ammunition capability

AmmunitionStats adds absolute capacity and per-launch/per-second cost to the same complete C# rows and cached pipeline. Safety clamps preserve nonnegative finite values; ranks do not silently change ammo cost. TowerAmmunition owns current stock and paid beam fractions outside FinalStats. Stats rebuilds, rank promotions and paid upgrades preserve stock (with explicit excess discard on capacity shrink). Current reserve/delivery status has a separate event-driven Inspector label; ammo changes do not run the pipeline. See ../Ammunition/README.md.

Cryo CT1–CT3 adds optional `SlowStats` to working/final snapshots. Complete level rows author the fraction; all eight stages preserve it, Stage07 clamps finite values to 0–0.99 and Stage08 publishes it. Current rank bonuses affect damage/range only. Cryo previews and combat read this same effective capability. No research is activated.

HD2 adds optional `HeatStats` to the existing working/final snapshots. Each weapon row can opt in independently; current authored defaults are null. Stage07 sanitizes capacity/rates/restart threshold. Runtime heat/latches are instance state, never stat values; ticks do not rebuild FinalStats. See ../Conditions/README.md. HD3 shows current heat and cached current/next thermal values in a tooltip without extra permanent stat rows.
