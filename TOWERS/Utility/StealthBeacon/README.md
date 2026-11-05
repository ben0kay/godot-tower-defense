## Stealth Beacon paid scanner tiers and artwork (2026-11-05)

**Source Implemented / Awaiting Owner Validation.** Stealth Beacon now has three consecutive purchased levels. Level 1 slows its clockwise sweep from **3s to 4.5s per full revolution**. All three levels retain the same 4.5s sweep speed; coverage increases by tier instead:

| Level | Credits (build / upgrade) | Work | Full cone | Range | Max HP |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 250 | 2s | 70° | 256px | 1,800 |
| 2 | 175 | 3s | 80° | 280px | 1,890 |
| 3 | 250 | 4s | 90° | 304px | 1,984.5 |

**Core semantics:** The authored cone angle is now an optional value on the existing immutable `TowerStatBlock` and `TowerFinalStats` (0 when no scanner). `StealthBeacon` draws its sector from `FinalStats.ScanConeDegrees`; the shared `StealthRevealSystem` uses the same effective value to compute the real indexed detection angle, so changing the purchased level changes actual reveal coverage. Config exports for Level 1/2/3 cone and range are editable separately. Existing indexed, bounded sweep, overlap, EMP, construction, pause and stealth-handling logic are unchanged. The normal Inspector's tier description and `DescribeChanges` preview show cone/range changes.

**Finished art:** Distinct native 64×64 SVG fixed bases and independently rotating forward-facing scanner heads for Levels 2 and 3 in [Artwork/Upgrades](Artwork/Upgrades/README.md). The existing `TowerConstructionVisuals` switches finished base/head art at paid completion, with separate 35% and 70% scaffold composites while upgrading. The original Level 1 SVGs and build stages are untouched.

**Source checks only:** Godot build/editor play and visual checks remain with the owner. Verify detection against stationary stealthed enemies near the new angle/range boundaries, pause/disable, upgrade preview, payment/2→3 work, the 35/70% stages, saved/restored tiers and layered rotation. Older SE1/SE2 notes below describe the historic one-tier state, now superseded for tier count and scan speed.

---

## SE2 — directional scanner implemented (2026-11-05)

**Source Implemented / Awaiting Owner Validation.** The prepared head now rotates clockwise while operational and its `TurretPivot/ScanCone` placeholder owns one faint 24-segment Polygon2D sector. Defaults: **70° full cone, 3s revolution, 0.1s detection refresh, 0.08 opacity**, still **256px / 250 Credits / 1800 HP / 2s / 1×1**. No artwork, scene paths, waves, prices, power requirement or tower upgrades changed.

Only in-cone, in-range stealthed enemy centres receive this source's reveal. Rotation resamples stationary enemies through the existing enemy spatial index; movement/registration use the same exact cone predicate. Leaving one source restores stealth only when no independent reveal source remains. Heading updates mutate the existing indexed Area in place, without unregister/register or retained-member hide/reveal flicker. Detection follows the configured discrete refresh (default 10Hz); head rotation is continuous. There is no passive detection bubble.

The beacon alone owns an active physics rotation/accumulator; disabled/unfinished/upgrading/destroyed/removed beacons stop processing and hide the sector. Pause freezes both heading and sampling. Activation finds stationary enemies immediately, retaining facing. The world reveal system remains event/callback-driven with no frame loop. Reused query/diff buffers, retained membership sets and pooled per-enemy source lists avoid per-sweep collection creation; no full enemy scan, per-enemy node, particle system or redraw per frame. [SE2 implementation details](DIRECTIONAL_SCAN_IMPLEMENTATION_PLAN.md).

Source/diff/resource checks only; .NET is unavailable. Updated focused `TESTS/Stealth/StealthChecks.tscn` is authored but not executed here. Owner: rebuild, inspect head/sector at normal zoom, enemy entry/exit and overlap, pause/EMP/construction, stationary sweeps and scene reload. Earlier dated notes below are historical.

---

## Stealth Beacon split scanner artwork — 2026-11-05

The original single 64×64 artwork is now split into a **fixed octagonal base (`Sprite`)** and an independent **`TurretPivot/BeaconHeadSprite`** with a pronounced forward-facing teal scan lens. The composite `Artwork/stealth_beacon.svg` remains the catalog icon and construction overlay fallback. Dedicated initial **0/35/70%** construction SVGs are wired to the existing shared split-layer construction presenter. The existing 1×1 footprint, Level 1 cost 250 Credits, build 2 seconds, 1800 HP and 256px radius are unchanged. There are no authored purchased upgrades.

**Historical artwork-only boundary (superseded by SE2 above).** This art pass originally left mechanics radial and the head stationary. For the requested genuine directional Option B, see the complete [SE2 Codex implementation plan](DIRECTIONAL_SCAN_IMPLEMENTATION_PLAN.md), including proposed 70° cone, 3-second sweep, 0.1-second spatial update, exact dot-product predicate, shared membership and lifecycle requirements. No gameplay code changed and no Godot runtime validation was run.

---

## Tower durability rebalance — 2026-10-20 (source updated; Godot runtime tests pending)

**Current authoritative defaults supersede older HP references below:** Base/max hull health for every available player-built tower/structure is **10× the previous level-1 value**. All **nine offensive towers** (Battle, Laser, Blaster Cannon, Shockwave, Machine Gun, Sniper, Rocket, Cryo, Stasis) have explicit, independent paid level snapshots: **1,000 → 1,050 → 1,102.5 HP** (two-level towers stop at 1,050). **Every purchased upgrade adds 5% of the previous tier's Max HP**, compounded, not 5 percentage points of the original at every tier; the values are authored as complete absolute tier stats, not runtime additive bonuses.

**Other buildings:** Ammunition Factory and Depot **1,000 HP** each (one build tier/no paid upgrades yet); Resource Tower and Adaptable Resource Generator **1,500 → 1,575 → 1,653.75 HP** (new tier progression replaces their former 150/200/250 HP schedule); Stealth Beacon **1,800 HP** (one tier). Generic passive config fallback is 1,000 HP. These are **hull**, not shields. Enemy damage, friendly firing stats, costs, unlocks, construction timing and Citadel HP are unchanged.

**Pipeline and HP semantics:** Stage02 still selects one authored tier, then Stage03 applies earned **rank HP bonuses independently**. The existing `TowerBase.Durability` synchronizes max capacity while **preserving missing absolute HP**; upgrades do not fully heal already-damaged towers. The current Inspector's `Max HP` comparison automatically shows the extra `(+50)` at level2 and `(+52.5)` at level3 for a standard-rank 1,000-HP offensive tower. No duplicate HP system, per-frame calculations, or tower-specific damage handlers were added.

**Verification still needed:** Compile Godot .NET, run `TESTS/Towers/TowerHealthChecks.tscn` and the existing integration scene that executes `TowerStatChecks`/`TowerUpgradeChecks`, plus `TESTS/Economy/CreditTowerChecks.tscn`. Inspect upgraded/damaged towers at runtime, including the resource producer `.tres` serialized tiers. Written test assertions were updated, but **not executed here**.

---

# Stealth Beacon — SE1

Registered as `stealth_beacon` under Ctrl → Utility. One1×1 paid tier:250 Credits,2 seconds construction,1800 HP and256px reveal range. Artwork is an original muted teal/green antenna beacon. Uses existing paid placement, footprint, construction, hull/destruction, selection and range-ring flows. No attack, ammo, earned combat ranks, artificial firing loop or paid upgrades are authored.

DefaultStealthBeaconConfig.tres exposes RevealRange (1–1024px) and MaxHealth; default cost/work are in StealthBeaconConfig's single level row. Private placed configs prevent changes leaking across towers. Historical SE1 beacons revealed enemies inside their inclusive center-to-center radius for all ordinary towers. Leaving the radius restores stealth unless another source still reveals the enemy. The beacon does not grant detection to nearby towers.

Construction, upgrade work, generic disable and destruction/removal all stop revealing. Reactivation/range edits/source relocation refresh stationary enemies without waiting for a scan interval. The shared world index consumes movement events and uses spatial buckets; this tower has no Process/PhysicsProcess callback. World/Registry unload and tower removal release source references.

Existing offensive towers retain CanDetectStealth=false. Future specialists may enable the shared config flag independently; future tower-grant beacons can be a separate mechanic. This pass does not implement Radar Tower, research unlocks or beacon upgrades. Existing waves remain unchanged; author stealth:true explicitly when ready. See ENEMIES/Shared/Stealth/README.md and TESTS/Stealth/README.md.
