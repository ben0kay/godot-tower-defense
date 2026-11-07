## FD3 — Auto-patrol

Select a Drone Station and choose a fighter from its roster. **PATROL** starts a new order or resumes the fighter's existing route. Click A and B in the world to commit the figure-eight, or use **EDIT A/B** to replace it. Right-click or Escape cancels the draft. **AUTO-PILOT** switches back to Citadel defence while retaining the route. The station inspector displays the selected order and map cells.

Patrol follows a distance-sampled route and fires at nearby enemies through the existing fixed guns and LOS rules. Returning for service preserves the order during the mission. Checkpoint loads use Auto-pilot until patrol persistence is added in FD6.

## Fighter targeting performance — 2026-11-06
Cached weapons use cheap liveness/generation/detection checks between scheduled validation and firing; full muzzle range/arc/target+lead LOS is fresh for every shot, with same-step validation reused only for the unchanged lock. Existing staggered searches, bounded replacement, lead, cadence and ammo remain. Shared threat work is requested only by deployed, healthy fighters with ammunition, after launch completes; launch/return/service alone never requests it. Empty registries skip scans and spatial acquisition; shared bounded scans cache detectable weapon candidates separately from Citadel-ranked threats, suppressing per-fighter acquisition when none qualify. The existing shared cadence still probes nonempty populations for eligibility changes (including stealth reveals). Debug/unowned enemies remain legitimate combat targets between waves; wave ownership is deliberately not a combat gate. Movement/loiter, transit and save fields are unchanged. Source/diff review only; .NET unavailable, owner gameplay/performance validation pending.

---

## FD4.1 + FD5 — source implemented / awaiting owner validation
**Weapons:** movement still follows the existing Citadel-threat anchor; guns now independently select the nearest shootable enemy around the next physical muzzle. Shared spatial queries run at a staggered 0.2s cadence with 0.25s weapon hold, bounded invalid-target replacement and range/180° arc/detection/target+lead LOS checks before each shot. No hull turning toward weapon-only targets. Fixed twin guns, 9 rounds/s, 4 damage, 275px range and 200-round capacity remain.

**Station service:** Drone Stations start with **0/2000** rounds and request genuine Depot couriers through existing Factory→Depot→consumer logistics. A newly paid 250-Credit fighter still includes its one initial full reserve; launch, dock and load never create ammunition. Low-ammo return defaults to **25%** (50/200); depletion always returns, optional low-HP threshold is 35%. Automatic service resumes Auto-pilot when fully rearmed/repaired; manual or station-disable recalls stay docked. RECALL can cancel an automatic return's relaunch intent.

One FIFO service bay defaults to **6 seconds per full fighter reserve**, proportionally less for partial loads. Actual atomic station→fighter transfers advance the tiny right-side vertical cyan meter; empty stock shows a static amber cue and sleeps until delivery. Repair runs independently at **20 HP/s**, costing **1 Credit/10 HP**. Whole-Credit prepayment retains unused fractional credit per fighter, so repeated partial repairs never grant free HP. Empty wallets pause repair without blocking rearm; zero repair rate/cost disables repair. Configure these on DroneStationConfig; fighter return/search thresholds live on FighterDroneConfig.

Selected station roster shows stock/courier state, return reason, waiting/rearming/repairing/ready/deployed status. Manual launch accepts a nonzero partial reserve and cancels queued service; if deliberately launched below a return threshold, that sortie ignores fractional return thresholds until actual depletion.

Checkpoints preserve station stock via existing tower ammo, per-slot FIFO intent/progress/fractions/prepaid repair credit, true shot parity and physical flight/courier state. Restore initializes fighter reserves empty before applying saved quantities. Missing legacy fields mean empty station/no active service, under existing content-fingerprint rules. No schema-envelope bump, new dispatcher, FD3 patrol UI or anti-air. Lightweight source/resource review only; .NET unavailable, owner gameplay/save/editor validation pending.

---

## Calmer pursuit and wider firing — 2026-11-06
Default full firing arc is now **180°** (90° either side), configurable through `FiringArcDegrees`. Fighters may shoot during any deployed pursuit/return-loop stage when the target is in that arc, range and LOS; manual recall/launch still suppress firing. Twin-gun artwork and 9 shots/s stay. A 1.5s target hold reduces switching, but dead/lost targets release immediately. Filtered steering destinations, eased angular speed and smoothed acceleration provide gentler curves. Defaults: 300px/s cruise, 600/500px/s² limits, 90°/s hull turns, 210px return radius and 220px/s manoeuvre speed; response times are Inspector-editable. Corrected a mismatch between the return waypoint and its arrival check. Source review only; owner flight/aim acceptance pending.

# Fighter drones
**FD1-B, FD2 and FD4: source implemented / awaiting owner validation. See FD3 above for the current patrol implementation.**

Build a [Drone Station](../../TOWERS/Utility/DroneStation/README.md), buy a fighter, choose its slot and LAUNCH. Fighters prioritize the shortest remaining journey to the enemy's assigned Citadel using the existing shared budgeted targeting. RECALL returns to their own dock; no free HP/ammo refill.

**Fixed twin-gun fighter refactor:** one hull sprite includes both forward barrels. `Body/Sprite/LeftMuzzle` and `RightMuzzle` alternate successful shots; there is no rotating gun sprite. The hull stages a run, aligns and fires, flies past the enemy, extends away and makes alternating return lobes for a figure-eight pattern. Fire is permitted throughout pursuit and return manoeuvres within an **180° forward firing arc**, with slight lead correction, finite range and shared terrain LOS. Target loss returns to home loiter. Recall/load safely rebuilds the next pass; launch/docking remain shared finite flight.

Defaults: **9 shots/s total** (was 5), 4 Kinetic damage, 275px range, 1200px/s bullet speed, 200 rounds. Actual shared physical projectiles retain the cyan tapered tracer/flash and fighter damage attribution; ordinary kill Credits remain, no rank/fighter XP. Ammo parity preserves left/right alternation across checkpoint restore without another saved state field.

Tune `FighterDroneConfig`: flight speed, acceleration/braking, hull turn rate (90°/s), attack run-up distance (220px), pass exit distance (220px), return-loop radius (210px), manoeuvre speed (220px/s), full firing arc and fire rate. Shared `DroneBody` now has optional cosmetic engine-length/acceleration-flare settings; the fighter preset uses 3.2× exhaust length plus acceleration flare. Other drone presets keep the original defaults.

Existing snapshots retain ID/slot/HP/ammo/pose, finite flight, velocity/phase and weapon cooldown; targets and attack legs are reacquired rather than storing runtime enemy references. Content-hash checks still apply. Station sale/destruction cleans up its roster.

No FD3 manual patrol UI, FD5 resupply/repair or hostile anti-air added. Source/reference review and native SVG render only; .NET unavailable, owner gameplay/visual acceptance pending. [Roadmap](FIGHTER_DRONES_IMPLEMENTATION_PLAN.md).
