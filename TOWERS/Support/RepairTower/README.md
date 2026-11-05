## Purchased Levels 2–3 — gameplay and SVG upgrade art (2026-11-05)

**Source Implemented / Awaiting Owner Validation.** Restoration Tower now supports three consecutive purchased levels using the existing `TowerUpgradeController`, absolute level rows, construction drones/work clock, immutable repair stats and owner Inspector. Baseline Level 1 gameplay and its 64×64 art are unchanged.

| Level | Price (build / upgrade) | Work | Range | Hull repair | Max HP |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 350 Credits | 5s | 320 px | 16 HP/s | 1,000 |
| 2 | 225 Credits | 6s | 344 px | 20 HP/s | 1,050 |
| 3 | 325 Credits | 7s | 368 px | 24 HP/s | 1,102.5 |

A purchased tier increases **repair throughput** and **healing reach**, not attack damage. Level 2/3 Inspector exports provide authorable range and repair HP/s without modifying Level 1. The tier preview includes **Repair** and **Range** deltas; the normal 5%-per-level max hull values are complete absolute snapshots. Rotation and target priority are unchanged; the ongoing target lock remains sticky and recovers its larger range on upgrade completion.

**Artwork:** [Upgrades/](Artwork/Upgrades/README.md) contains distinct top-down native 64×64 white/silver/aqua fixed base and independently rotating repair emitter for Level 2 and Level 3; matching composites are available for reference. [RepairConstructionArt.tres](Artwork/Construction/RepairConstructionArt.tres) uses the same shared `TowerLevelArtwork` and existing `TowerConstructionVisuals` presenter for **35% and 70% paid upgrade** states; at work completion it restores the level's own split finished art. No new scene node, timing script, beam renderer, targeting subsystem or animation loop was introduced.

**Scope boundary:** RT2 heat and shared funded power remain separate planned work; adding tiers does **not** change the current cost-free repair baseline or award support XP. Older RT1 documentation below describes the historical one-level state and is superseded for purchased tiers.

**Owner QA:** Pull `dev`, allow Godot to import the new SVGs, purchase L2/L3, confirm prices/preview/repair rate/radius, 35/70% construction images, gun head rotation/muzzle position, 1×1 footprint, and save/restore. Source + resource sanity checks only; actual Godot runtime/visual verification has not been run.

---

# Restoration Tower — RT0 / RT1

**Source Implemented / Awaiting Owner Validation · 2026-11-05 · dev.** Build **Restoration Tower** from **Support** (stable ID `repair`). One 1×1 paid tier: **350 Credits / 5s construction / 1000 hull HP / 320px range / 16 hull HP/s**, 210°/s turn, 12° aim tolerance and 0.3s staggered search. These are provisional baseline values, not changes to other towers.

## Owners and behavior

- `RepairTowerConfig.cs` / `RepairTowerLevels.cs`: normal private config copies and a complete absolute Level 1 row. Inspector exports tune repair rate/range/aim/search. Standard-only rank; no automatic support XP or purchased tiers.
- `RepairTower.cs`: RT0 `FriendlyTowerTargeting`, one retained scratch list and a cached friendly lock. Lowest finite hull fraction wins, with registration-order ties. Queries use TowerRegistry's spatial index at the staggered interval **only while no valid target is locked**; cached validation is O(1). A valid target is retained until its hull is full or it becomes ineligible, even when another tower's integrity becomes lower. No enemy/Citadel/shield/mobile-unit targeting, group lookup or scene-container scan.
- Repair occurs only when operational, unpaused, aligned and target-eligible. It applies `RestoreHealth` for min(delta, missing hull / rate); no negative damage, direct health writes, resurrection or overheal. Damaged EMP/heat/NoPower-disabled targets can receive repair; busy/destroyed/full/out-of-range targets cannot. The healer's own disable/work gates stop repair.
- Current target health/work/spatial/death/exit events clear invalid locks and hide the beam immediately. Source disable/work/removal and registry exit stop processing and presentation. Inactive/zero-rate towers sleep; operational idle towers keep the small staggered acquisition clock. Scratch capacity is retained, and no query/result/delegate array is allocated per frame.
- `Artwork/` owns new native 64×64 white/silver/aqua base, head and composite icon SVGs. The head rotates about the fixed root; `TurretPivot/Muzzle` is at the physical tip (0,-29). One initial foundation image uses the existing construction presenter/drone/work clock, then restores split layers. Extra 35/70% stages and paid-tier art remain RT3.
- `Beam/DefaultRepairBeamStyle.tres` reuses BeamRenderer: icy cyan/white narrow core, translucent aqua halo, subtle pulse and small contact glimmer. No new shader, particle system or beam renderer. It sleeps while there is no actual repair.

## Shared stats, Inspector and saves

Optional `RepairStats(HullPerSecond)` is appended to TowerStatBlock/TowerFinalStats, clamped finite/nonnegative in Stage07 and forwarded in Stage08. Existing positional arguments and damage/rank/research behavior are unchanged; damage bonuses do not multiply repair. The ordinary Inspector shows **Repair (HP/s)** and **HULL REPAIR**, avoiding fake Damage/DPS/projectile rows. This is a small capability presentation addition, not RT3's future tier/policy UI.

Existing placement, selection/range ring, health bar, construction and checkpoint reconstruction work through TowerBase. Save files contain usual identity/work/hull inputs; repair rate is authored config and target/beam is transient. No schema addition, ammo logistics or paid restoration transaction. The existing fingerprint can reject earlier checkpoints after this catalog/resource update.

## Explicit RT1 boundary

**RT1 repair has no heat or operating resource cost.** RT2 owns actual continuous-repair capacitor funding and shared heat. PW4-A currently funds Battle projectile intervals; it does not yet provide funded repair seconds. This tower deliberately has no Power profile or supported capacitor flag, no fake energy/ammo consumption, and no power bar. This is the approved RT1 baseline, not a claim that electricity is enforced. RT2–RT4 remain planned; do not add tiers, materials, Citadel or shield repair in this pass.

[Authoritative plan](IMPLEMENTATION_PLAN.md) · [targeting](../../Shared/Targeting/README.md) · [focused owner check](../../../TESTS/Towers/RepairTower.md).

Validation: lightweight source/resource review only. Execution environment unavailable; no .NET build, fixture or Godot visual/performance pass was run. Owner: pull dev, rebuild/import SVGs, build Support → Restoration Tower, damage a nearby tower, inspect muzzle/beam/repair, full/range/work/EMP/pause behavior and a fresh Save/Load. Optional F6 fixture is `TESTS/Towers/RepairTowerChecks.tscn`.


## Sticky repair lock — 2026-11-05

A passing search deadline no longer replaces an eligible lock. Health percentage crossings or new damage on other towers cannot cause beam ping-pong. Full health, target work/death/removal/out-of-range, or healer shutdown still clears the target through existing events; the next eligible search selects Lowest Integrity again. The search clock remains bounded at zero while repairing, so a completed long repair can acquire another target next normal tick. No extra query, timer, save field or operating cost. `RepairTowerChecks` includes two-target crossing/completion checks, authored but unrun here.
