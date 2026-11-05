# RT0–RT4 — Restoration Tower / Shared Friendly Targeting

> **Status: RT0–RT1 Source Implemented / Awaiting Owner Validation (2026-11-05); RT2–RT4 PLANNED.** Target repository: `ben0kay/godot-tower-defense`, branch `dev` only. Codex should implement **one pass at a time** when asked; do not merge or edit `main`. The intended class/file name is `RepairTower` for the existing planned folder, with the in-game display name **Restoration Tower**.
>
> Owner intent: a sleek white/silver 2D support tower with pale aqua accents, a fixed base and rotating laser head. It never damages enemies. It locks onto **damaged friendly towers** within range, turns toward them, projects a healing beam from its physical muzzle, restores structural hull and uses the existing shared heat and eventual per-tower capacitor-based power systems. Friendly/hostile selection must be reusable, but should not destabilize existing enemy targeting.

## Reference: original Vector TD 2026

Source: `ben0kay/VECTOR_TD_2026` (`main`):
- `scripts/scr_building_data/scr_building_data.gml` → `scr_building_data_utility_repairer` defines **Restoration Array**: 2×2, 350 Credits, 350 HP, 5s construction, 448px radius, 8 HP every 0.5s (16 HP/s), energy idle 1/activity 2/input 8/buffer 20; these are *historical*, not mandated balance.
- `scripts/scr_utility/scr_utility.gml` → `scr_utility_repairer_update` searches buildings and CPU, excludes self/destroyed/constructing/undamaged, chooses **lowest health fraction**, consumes energy before repairing, caps HP at max, and triggers a short beam and particles.

New design **differs intentionally**: 1×1 aimed turret, continuous repair laser (HP/s), only registered friendly `TowerBase` targets in first pass. Citadel support is deferred because it is not a `TowerBase` in `TowerRegistry`. Do not treat the historical support array as an enemy-targeting weapon.

## Read before each implementation pass

1. `NOTES/CodePreferences.md`, root `README.md`, `TOWERS/Support/RepairTower/README.md` and this plan.
2. `TOWERS/Shared/Targeting/TowerTargeting.cs`, `ITowerTargetingPolicy.cs`, `TOWERS/Shared/Registry/TowerRegistry.cs`, `TowerBase.Spatial.cs`.
3. `TOWERS/Offensive/LaserTower/LaserTower.cs`, `LaserTowerConfig.cs`, `LaserTowerLevels.cs`, `LaserTower.tscn`; `TOWERS/Shared/Weapons/TurretAiming.cs`, `TOWERS/Shared/Weapons/Beams/BeamRenderer.cs` and `BeamStyle.cs`.
4. `TOWERS/Shared/Durability/TowerBase.Durability.cs`, `TOWERS/Shared/Stats/{TowerStatBlock,TowerFinalStats,RepairStats}.cs` (RepairStats not yet created), `TOWERS/Shared/Stats/Pipeline/`, `TOWERS/Shared/Conditions/TowerBase.Conditions.cs`.
5. `TOWERS/Shared/Power/README.md`, `TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md` and actual PW4 code **at implementation time**. PW4-A now provides the Battle Tower projectile-funded capacitor; future continuous repair needs an extension of that shared funding seam. Distance/priority/Inspector phases remain separate.
6. `TOWERS/Shared/Construction/TowerConstructionVisuals.cs`, `TowerConstructionArt.cs`, `TOWERS/DefaultTowerCatalog.tres`, `TOWERS/Shared/TowerDefinition.cs`, `UI/GameplayHUD/GameplayHUD.cs` and `UI/GameplayHUD/Inspector/`.

## Architecture: friend/foe target domains

**Do not change the existing `TowerTargeting.Priority` enum numeric IDs**, `TowerTargeting.Find(EnemyRegistry, ...)` behavior, stealth/shield preference, or existing enemy-targeting Inspector choices. `First`/`Last` are based on an enemy's route toward the Citadel and have no meaningful ally equivalent.

Add a small explicit `TargetSide` or `TargetDomain` vocabulary under `TOWERS/Shared/Targeting/` (HostileEnemy, FriendlyTower) that future weapons/support features can opt into. Preserve **typed search backends** instead of making `Enemy` and `TowerBase` implement an artificial common damage/heal interface:
- **HostileEnemy:** the existing `TowerTargeting.Find` and `EnemyRegistry` spatial visitor, unchanged.
- **FriendlyTower:** new `FriendlyTowerTargeting.Find` (or `TowerAllyTargeting`) using the existing `TowerRegistry.GetNearby(center, radius, reusableList)`. Keep an owned scratch `List<TowerBase>` on the requesting repair turret, reused for every query. No `GetTree().GetNodesInGroup()`, `World/Towers.GetChildren()`, full-map scans or per-query list allocations.
- Reuse shared `TurretAiming.Track`, range/stat contracts, lock lifetime and presentation, but keep damage and repair application typed and separate.
- Implement friendly-only policy `LowestIntegrity` as default: lowest finite `Health / MaxHealth`; stable equal-score tie-break with `TowerRegistry.RegistrationOrder`. Other optional policy values for the future: `MostMissingHP`, `Closest`, `Farthest`. **Do not map enemy First/Last onto allies.** Do not rebuild the existing targeting UI merely to introduce one tower.
- Eligible target: `TowerRegistry.Contains(tower)`, `tower != healer`, `Health > 0`, `Health < MaxHealth`, within configured range from the healer to target root (consistent with `TurretAiming`), not queued for deletion, and not currently under initial construction or timed upgrade. An EMP-/heat-/NoPower-disabled **target may still receive repairs**; only the disabled *healer* is barred from operating. This mirrors the old health-only repair while respecting modern work lifecycle.
- Cache a target between staggered searches; revalidate cheaply before any repair tick. On destroyed/removed target, shut beam off promptly. On fully repaired/out-of-range target, stop and reacquire on next scheduled query. Optionally add a small policy hysteresis to avoid flickering between almost-equal damaged targets; stable scoring first.
- Do not implement Citadel, friendly mobile units or shields in this pass. Those need their own explicit provider/health contract later.

## Proposed folder ownership

```text
TOWERS/
  Shared/
    Targeting/
      TargetDomain.cs                 # Small optional typed selection domain
      FriendlyTowerTargeting.cs       # New; TowerRegistry-backed friendly acquisition
      FriendlyTargetPriority.cs       # If selectable friend-specific policies are enabled
    Stats/
      RepairStats.cs                  # Optional RepairPerSecond capability
      TowerStatBlock.cs               # Append optional capability, do not reorder old fields
      TowerFinalStats.cs              # Append optional capability
      Pipeline/07_SafetyClamps.cs     # Validate nonnegative finite repair rate as needed
      Pipeline/08_FinalStats.cs       # Forward capability to final snapshot
    Weapons/
      TurretAiming.cs                 # Reuse; do NOT duplicate
      Beams/                          # Reuse BeamRenderer, BeamStyle, scene
    Durability/
      TowerBase.Durability.cs         # Reuse RestoreHealth; no second health system
  Support/
    RepairTower/                      # Already exists as a PLANNED README
      RepairTower.cs                  # Own target, alignment, funded repair, beam lifecycle
      RepairTowerConfig.cs            # Type settings and private config copy behavior
      RepairTowerLevels.cs            # Absolute purchased levels (not cumulative)
      RepairTowerRanks.cs             # If ranks needed; same separated earned progression
      RepairTower.tscn                # Fixed BaseSprite, TurretPivot/Sprite/Muzzle, BeamRenderer
      RepairTowerDefinition.tres
      DefaultRepairTowerConfig.tres
      Artwork/                        # Finished base/emitter SVG, optional staged construction
      Beam/DefaultRepairBeamStyle.tres
      README.md
      IMPLEMENTATION_PLAN.md          # This file
TESTS/
  Towers/RepairTowerChecks.cs         # Focused tests only (when relevant)
```

If the existing codebase already provides a suitable target-domain interface or stat capability when Codex executes, **reuse it** instead of creating parallel code. The proposed file list is a responsibility map, not permission for a broad refactor.

## Repair behavior and visuals

- Repair uses a **continuous beam**, not per-hit negative damage. Author `RepairStats(float HullPerSecond)` or equivalent, an optional stat independent from `BeamStats.DamagePerSecond` and `DamageType`. Append the new optional stat property at the **end** of `TowerStatBlock` and `TowerFinalStats` positional records to preserve old construction call sites. Forward through the existing eight-stage pipeline; do not add a ninth pipeline or rewrite every weapon type.
- Only when the target is valid, the barrel is aligned and the healer can operate: compute the maximum allowable repair interval (current delta, actual missing HP, optional heat headroom and **future** funded power), then `target.RestoreHealth(repairRate * allowedSeconds)`. `RestoreHealth` already clamps to max and emits `HealthChanged`; never write `Health` directly, resurrect a destroyed tower or restore shields. Avoid over-healing and charging for time beyond the actual HP restored.
- For heat, use `FinalStats.Heat`, `RecordAttackHeat(0, actualBeamSeconds)` and the shared `TowerConditions` disable/recovery behavior. Passive idle/turning/target searching should not create active beam heat. `OnDisableChanged` must hide the beam/clear lock promptly, as the current Laser Tower does.
- Reuse `BeamRenderer.SetBeam(muzzle.GlobalPosition, target.GlobalPosition, true)`, with a unique `BeamStyle` asset: narrow **icy cyan/white core**, translucent aqua halo, very subtle width pulse, contact glimmer; no weapon damage or enemy status effects. No new standalone full-screen heal shader, new particle system, per-frame array allocations or duplicate beam renderer.
- Fixed clean white/silver base, slender rotating emitter, soft aqua center lens/edge lights; avoid copying Laser Tower's gold/yellow palette. Muzzle is an authored `Marker2D` at the actual emitter tip. Use simple stylized SVG geometry that fits a normal 1×1 tower. Keep optional construction artwork under this tower; reuse shared `TowerConstructionVisuals` and construction drones, not new build clocks.
- Use `InspectionRange` to preserve the standard selection-radius display. No beam at full health/no eligible target; no constantly running beam renderer while idle.
- Power: when the shared **PW4 capacitor consumer** capability is actually implemented, opt in through its existing settings (capacity, initial fill, recharge, idle draw, **active draw while genuinely repairing**, outage and NoPower). Electricity is the baseline repair operating cost; **do not disguise ammo as repair power** or invent a second power network. Never apply unpaid repairs. Until that capability ships, keep the integration as an explicit follow-on pass rather than claiming consumer power is enforced. If the owner requires power-gated healing at launch, complete the shared PW4 passes first.
- Future separate consumable repair nanites/supplies could use an optional explicit resource, **not** `AmmunitionLogistics` automatically. No Credit cost per repaired HP without owner approval.

## Provisional playtest numbers (NOT old-game requirements)

| Setting | Suggested Level 1 | Rationale |
| --- | ---: | --- |
| Category / footprint | Support, 1×1 | Aimed turret; easier placement than old 2×2 utility |
| Build cost / work | 350 Credits / 5 s | Mirrors old cost/time provisionally |
| Max hull | 800–1,000 HP | Same broad scale as combat tower, subject to balance |
| Repair | **16 hull HP/s** | Matches old 8 HP per 0.5s, now continuous |
| Range | **320 px** | Close to current Laser range, less than old 448 |
| Rotation | 200–220 degrees/sec | Responsive, not instantaneous |
| Aim tolerance | ~12 degrees | Reuse TurretAiming |
| Search cadence | 0.25–0.4 s staggered | Smooth target finding without per-frame registry queries |
| Heat | Gentle sustained-beam build-up | Follow current Laser HeatTiming/shared constraints |
| Power | PW4 consumer when available | Do not invent buffer/draw values before shared implementation |

Level 2/3 (separate approved phase) should increase absolute repair HP/s, perhaps 20 and 26, and modestly extend range; all numbers are subject to in-editor balancing. Earned rank XP and paid upgrades remain separate; do not interpret healing as enemy kills/award combat kill XP without a separate approved support-XP rule.

## Phased implementation contracts

### RT0 — Shared friendly targeting groundwork — Source Implemented / Awaiting Owner Validation
- Implement the small target-domain separation plus `FriendlyTowerTargeting` using `TowerRegistry.GetNearby` and a reused scratch buffer.
- Add deterministic lowest-integrity selection, self/full/destroyed/constructing/upgrading filters, proper radius, stable tie-breaks and fast lock validation. **Do not change enemy targeting or the HUD.**
- Narrow optional `TESTS/Towers/FriendlyTowerTargetingChecks.tscn` fixture covers 1×1/multi-cell roots, fraction scoring/ties, work, target/healer disables, full/queued/detached/destroyed targets, malformed radius and world isolation. Authored, not executed here: .NET unavailable. Source/diff review only.
- Implementation: `TargetDomain.cs` and `FriendlyTowerTargeting.cs` under shared Targeting. Only LowestIntegrity is enabled; no friend-specific policy enum/UI yet. Caller owns scratch, staggered queries and cached target lifetime; `Valid` rechecks in O(1). No registry or enemy targeting source changed.

### RT1 — Restoration Tower playable baseline — Source Implemented / Awaiting Owner Validation
- Add `RepairStats` optional shared capability, forward through existing pipeline and clamp safely; do not treat it as offensive `BeamStats`.
- Add `RepairTower` scene/config/definition/Level 1 absolute row and register it in `TOWERS/DefaultTowerCatalog.tres`, in **Support** category.
- Add white/silver/aqua base/emitter SVGs, muzzle, existing `TurretAiming`, `BeamRenderer` with unique style. Use `RestoreHealth`, pause/disable/work gates and the existing construction-drone/selection/health-bar/Inspector plumbing. Repair only hull and only eligible friendly towers.
- Keep status transitions and beam teardown safe. Do not touch existing Laser Tower behavior.
- No fake power gating until PW4 exists; clearly document current boundary.

### RT2 — Heat and actual power consumer wiring
- Opt in to shared beam heat with Inspector-configurable timing, using actual funded repair seconds only.
- **Once PW4 consumer code exists**, wire this turret to exactly that authoritative capacitor/network and NoPower disable. Use real active draw only during actual repair; idle otherwise. Respect partial charge and simultaneous heat/EMP/upgrade disables. Do not fork PW4 or pre-implement PW5.
- If RT1 must be power-gated from day one, defer RT1 release until this dependency is available rather than creating free or pretend energy accounting.

### RT3 — Purchased tiers, rank, inspector policy and authored artwork
- Add Levels 2/3 with absolute values/prices/work times and optional 35/70% assembly art using existing tower upgrade/ConstructionVisuals flow. Do not change footprint between levels.
- Report `REPAIR 16 HP/s`, range, health, heat and (once real) power in the normal Inspector; show paid-tier NEXT/Gain values. Do not label repair rate as **damage**. Use existing selected-only HUD events, not a new polling panel.
- Optional repair-priority selector `Lowest Integrity / Most Missing HP / Closest / Farthest` only after a clean shared **typed UI** API is designed; preserve all six existing enemy-priority enum values, names and buttons. New per-instance priority may need a save field if explicitly required; don't silently change save schema.
- Keep rank/XP mechanics separate; decide deliberately whether healing earns support XP before enabling it.

### RT4 — Optional extension, only on request
- Support Citadel repair via explicit provider/health interface and agreed range semantics, not by stuffing the Citadel into `TowerRegistry`.
- Optional healing beams for friendly drones, shield repair, multiple simultaneous targets and dedicated repair material logistics. These are **NOT** part of RT0–RT3.

## Focused verification / acceptance

Codex: perform source/diff review and at most one basic C# build if readily available; owner performs in-game visual and performance acceptance. Do not run large test suites. Check:
- Registry candidate selection is scoped (no `GetChildren`/all-enemy full scan), avoids itself, prioritizes lowest **HP fraction**, and always clamps HP to MaxHealth.
- Damaged disabled **target** can be repaired; disabled/upgrading **healer** cannot heal. Unfinished/upgrading and destroyed targets excluded. Unselected and unloaded towers have no beam/target leaks.
- Multiple healers and switching targets never give negative health or exceed maximum, and UI health bars react through existing `HealthChanged` signals.
- Beam always comes from the physical muzzle, tracks a rotating head, stops instantly when invalid/disabled/full or when changing scenes; no beam-to-enemy or enemy damage.
- Heat increments only while actually repairing, cools between beams and respects existing overheat logic. When PW4 is present, energy is actually charged for applied repairs and a depleted capacitor blocks them.
- Works with normal selection-radius, placement, construction drones, Save/Load, 1×1/multi-cell neighbor queries and the current default tower catalog.

**RT0–RT1 source implemented; RT2–RT4 remain PLANNED.** Level 1 Restoration Tower, repair stats, shared beam and basic correctly labelled Inspector rows are present. Heat/power, tiers and policy UI are not implemented. Owner build/runtime acceptance is pending. Do not implement the remaining roadmap without an explicit pass request.

## RT1 implementation record — 2026-11-05

Delivered the `repair` Support definition, private config/absolute single level, scene, simple native white/silver/aqua SVG layers and composite icon, initial foundation via shared construction, physical muzzle and unique shared BeamStyle. Provisional defaults: 350 Credits, 5s, 1000 HP, 1×1, 320px, 16 HP/s, 210°/s, 12° tolerance, 0.3s staggered acquisition. RT0 selector and existing hull RestoreHealth remain authoritative; HealthChanged/work/spatial/death/exit callbacks invalidate cached locks without new acquisitions. No new registry, health, save or damage architecture.

RepairStats is appended (not reordered), sanitized in Stage07 and forwarded in Stage08; existing old-capability values and enemy policy enum IDs are unchanged. A minimal capability branch keeps the ordinary Inspector's current rows/header labelled Hull Repair rather than Damage/Kinetic; RT3's tiers and policy UI are still future work. Existing Support-category smoke expectation is updated for the populated category. Optional RepairTowerChecks covers real catalog/scene/beam/hull/disable/pause/work and checkpoint-input seams; it is authored, not run.

**RT1 resource boundary:** consistent with the approved baseline description, repair currently has no operating resource cost and no heat. RT2 must extend the real shared capacitor seam for continuous applied repair seconds and add heat; no fake Power profile, ammo substitution or partial RT2 is shipped here. This clarifies the broader future “funded repair” language above. No other tower stats, authored waves, enemy targeting or save schema changed. Existing strict content fingerprints may reject pre-change checkpoints.

Validation is source/resource review only because the execution environment is unavailable. No build, runtime/visual acceptance, large suite or benchmark. Owner: rebuild/import, place Support → Restoration Tower and inspect actual hull gain/muzzle/full/out-of-range/EMP/pause/construction, and a new Save/Load before moving on to RT2.
