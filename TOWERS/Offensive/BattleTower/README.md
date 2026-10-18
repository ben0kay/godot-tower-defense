# Battle Tower — explicit levels and guided Kinetic fire

## Permanent Battle damage research — 2026-10-17

Stage04 is now active for **captured TowerResearchBonuses**. Battle Tower Weapons I adds5% damage after Stage03's selected rank and before Stage07's safety clamps. DamagePerShot uses the existing rank convention (nearest/ties-to-even); at standard rank the three Battle tiers become32/35/38. Typed beam/wave damage retains fractional arithmetic if a future node explicitly targets such a tower; no such additional node is authored in this pass.

CitadelSession captures ResearchDeployment once on deployment. Placement passes the per-type immutable input into TowerBase.InitializeDefinition; TowerBase keeps it across paid upgrades, rank promotions and config edits. The same pipeline resolves actual shots, selected-tower Inspector, next-level comparison and build preview. No stat stage reads profile files or current menu state. Direct config/tooling calls remain neutral unless explicit research inputs are supplied, and profileless F6 remains neutral. New missions refresh bonuses; active missions never subscribe to purchase changes.

Research does not alter authored *Levels.cs values, attack rate, heat timing, ammo, wallet costs, or unrelated tower types. See TESTS/Research/BATTLE_RESEARCH.md. Older statements below about neutral Stage04 or deferred tower damage research are historical and superseded here.

---

Ctrl → Battle Tower → place on route-safe terrain. First targets the shortest remaining path; Closest targets nearest. Shared turret aiming rotates the child visual toward the target, leaving the grid root fixed. Fire requires a living in-range aligned lock and elapsed cooldown; zero rate disables firing. Guided shots originate at the barrel-tip marker and retain launch-time damage/penetration.

**Projectile-speed update (2026-10-18):** Battle Tower's authored projectile speed is now **2,300 px/s** at all three levels, exactly double its previous 1,150 px/s. Only projectile speed changed in the level code; the table below also reflects already-existing range/turn rebalances. No change to attack rate, damage, ammunition, heat, cost or turret mechanics.

## Level data

**Edit `BattleTowerLevels.cs`**, the sole authored source of all level-dependent numbers and credit prices. Each labeled typed row is complete; adding Level 4 means adding another consecutive row. Changing Level 2 never changes Level 3.

| Level | Build/upgrade price | Damage | Shots/s | Range | Projectile speed | Turn | Aim | Search | Penetration |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 200 | 30 | 1.68 | 323.4px | 2300px/s | 216°/s | 12° | .15s | 5% |
| 2 | 150 | 33 | 1.764 | 334.4px | 2300px/s | 216°/s | 12° | .15s | 5% |
| 3 | 250 | 36 | 1.848 | 344.3px | 2300px/s | 216°/s | 12° | .15s | 5% |

DefaultBattleTowerConfig.tres retains Inspector identity, target priority and RotateWhileTracking; scene/definition retain artwork and registration. Combat stats/prices are no longer numeric .tres exports. Each placed tower gets private runtime rows/recipes. C# Level 1 convenience accessors edit row 1 only; use SetLevelStats(level, snapshot) for another row.

## Purchases and cached stats

Select a placed tower to see its level, actual stats, next-row differences, price and Upgrade button. Level 3 displays Maximum level reached. Build/upgrade payments use the existing shared wallet and ResourceCost recipe; terrain, occupancy, route-safety and affordability failures never charge. Starting funds remain in level configs. The Inspector and range ring read the same cached FinalStats used by combat.

The eight-stage pipeline selects the complete purchased row at Stage 02. Stage03 applies the current earned rank; research/map/effect stages remain neutral. Config changes or purchases invalidate once; physics never resolves per tick. Upgrading preserves targeting, aim, cooldown and navigation.

Kinetic damage and 5% penetration retain the shared defense resolver contract: 30 base against 100 shield/60 hull leaves 92.875 shield/58.5 hull, or deals 30 to exposed hull. An unshielded 60-HP Enemy001 takes exactly two standard-rank level-one hits. Fired bullets preserve their launch snapshot. No research, saving, selling, armour, new artwork or weapon mechanics were introduced.

TowerStatChecks asserts exact stats/prices and level independence; TowerUpgradeChecks drives real HUD purchases/preview/max-level/atomic costs; DamageTypeChecks retains real projectile penetration coverage. See TESTS/README.md for actual compilation and headless results.

## Earned Rank and Experience

`BattleTowerRanks.cs` sits beside `BattleTowerLevels.cs` and defines all five named ranks in compact explicit rows: Standard0, Veteran1000, Enhanced3000, Superior7000 and Legendary15000 total XP. The selected rank's total bonuses are0/2/4/6/8% damage and0/1/2/3/4% range; future HP columns are inactive. Rank XP is independent of purchased level/prices and is preserved by upgrades. Stage03 modifies the selected complete Level snapshot, never accumulates earlier Rank bonuses. Integer projectile damage retains nearest/ties-to-even rounding; float beams/waves retain fractional damage.

Attacks carry the tower's weak DamageSource. Actual shield/hull loss earns a bounded share of XP derived from each enemy's KillRewardCredits; the lethal source receives kill XP. Shared TowerMasterConfig multipliers default to2× damage pool and10× kill reward. Debug/manual kills and arrivals grant no tower XP. Already-fired payloads retain combat values/source after promotion or upgrade; removed source towers do not receive XP. The bottom-right selected Inspector shows Rank, within-rank/total XP, progress bar, active bonuses and MAX RANK automatically through events. See TOWERS/Shared/Ranks/README.md for exact rules and regression results.

## Live ammunition supply

Current complete BattleTowerLevels.cs rows include 1000 reserve / 1 per projectile launch. Defaults are provisional except Battle1000/1. InitialAmmoFraction starts full by default; empty reserves stop unpaid attacks, and paid levels/ranks never refill them. The existing Inspector displays current ammo/delivery state and changed next-level ammo values. Factory/depot drones deliver physical cargo only on arrival. See `TOWERS/Shared/Ammunition/README.md` and `TESTS/Economy/Ammunition.md`.

**Earlier fire-rate rebalance (October 2026):** Levels 1–3 previously increased to 1.40 / 1.47 / 1.54 rounds/s (40% more than the preceding 1.00 / 1.05 / 1.10). This changes `ShotsPerSecond`, not the already-upgraded 800 px/s projectile travel speed; per-hit damage and ammunition per launch are unchanged. For comparison, the old GameMaker Basic Tower used a 30-frame cooldown at 60 FPS = 2 shots/s. This Godot value follows the requested **+40%** rather than silently copying the old game's rate.

## Initial construction (C1/C2)

Paid placement occupies its footprint immediately and takes **3 seconds**, authored in the Level1 WorkSeconds column of this tower's `*Levels.cs`. It cannot acquire targets/fire/consume ammunition or request deliveries until complete. Completion preserves stats, level, rank and stock. Direct scene fixtures remain operational by default. Initial construction now uses the Battle-only C4 stage SVGs described below; no sparks/VFX. C3 Inspector progress is implemented; purchased upgrades use their own WorkSeconds and retain old-tier operation until completion. Duplicate purchases are disabled while work is active. See `TOWERS/Shared/Construction/README.md`.

## Battle construction artwork (C4 pilot)

Three new64×64 SVGs in `Artwork/Construction/` show foundation (0%), frame (35%) and assembly (70%). Completion restores the unchanged `Artwork/battle_tower.svg`. Current build time remains3 seconds. The default config's ConstructionArt resource exposes all thresholds/textures; its LevelArtwork entries2/3 are optional empty slots for a future intermediate upgrade image and a finished tier image. Current upgrades retain their existing look. No sparks or particles.

The optional shared presenter uses `../TurretPivot/Sprite`, preserving aiming, selection tint and numeric gameplay. Other towers are unchanged. [Artwork preview](Artwork/Construction/construction_preview.svg) shows the actual SVG progression; it is not an in-game screenshot. See `TESTS/Towers/ConstructionArtwork.md` for tests and live playtest checklist.

## Projectile presentation — PV1/PV2

Implemented 18px amber tracer and stronger golden starburst; actual muzzle is `(0, -28.5)` under `TurretPivot/Muzzles/MuzzleA`. Tower-owned presets live in `Visuals/` and are assigned in `DefaultBattleTowerConfig.tres` (Weapon Visuals). Presentation never enters level/rank stat tables. Shared behavior and authoring: [Visuals](../../Shared/Weapons/Visuals/README.md). Regression evidence: [ProjectileVisuals](../../../TESTS/Towers/ProjectileVisuals.md). Manual appearance review at normal gameplay zoom remains pending.

**Current firing rate (additional +20%):** Levels 1–3 fire **1.68 / 1.764 / 1.848 bullets/s**, multiplying the immediately preceding 1.40 / 1.47 / 1.54 by 1.20. The projectile travel speed was 800 px/s at that time, with unchanged per-hit damage, ammunition cost, prices, aiming, construction times and rank bonuses.

**Projectile-speed rebalance (+25%, October 2026):** All three levels now launch guided Kinetic tracers at **1,000 px/s** instead of 800 px/s. Fire cadence remains 1.68 / 1.764 / 1.848 shots/s; muzzle flash, impact damage, range, ammunition and upgrades are unchanged.

## Enabled heat: minutes-based authoring

Battle now overheats after approximately **3 minutes of uninterrupted firing from cold**, at every purchased level, and recovers to its35% restart threshold in about9 seconds. Brief genuine combat breaks cool it; ordinary scheduled gaps between shots are already included in the timing calculation. Other towers have not been assigned heat balance in this pass.

In Godot's FileSystem, select `DefaultBattleTowerConfig.tres`, expand **Heat**, and edit **Overheat After Minutes**. Enter `3.5` for roughly210 seconds; this is minutes, not seconds. **Heat Enabled** switches the default timing profile off/on. **Overheat Recovery Seconds** defaults9. These are editor/Remote Inspector config fields, not a new editable field in the bottom-right gameplay HUD. The in-game Inspector now shows the live heat bar and disable reasons (HD3).

The shared HeatTiming value converts this authoring target to cached HeatStats after selected-level/rank/research/effect cadence inputs. It accounts for continuous passive cooling and the initial immediate shot. FinalStats are not rebuilt per attack. Explicit non-null per-level HeatStats override the timing profile; other level values/prices remain authoritative in BattleTowerLevels.cs. Config edits preserve accumulated heat, and runtime copies remain private. Invalid/nonpositive timing settings yield no timing capability; HeatEnabled is the intended off switch.

Battle cooldown now preserves fractional frame overshoot, as the shared multi-barrel weapons already do. This maintains authored rates instead of rounding the firing period upward each frame, and makes thermal timing consistent at30/60/120Hz without changing rate rows or firing catch-up bursts. Heavy frame hitches, ammo shortages or target loss reduce actual firing and can delay/prevent overheating. Verification: TESTS/Towers/BattleHeatTimingChecks.tscn.
