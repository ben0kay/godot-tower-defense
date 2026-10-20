## SE1 — stealth enemies and reveal beacon — implemented 2026-10-20 UTC

Waves may now author `Enemy("basic_mk1", stealth: true)` independently of `shield: true`. Default spawns remain ordinary; no existing wave/map was changed. Stealth ships render at30% alpha, revealed ships at100%, with configurable/dimmed defense feedback that composes with Cryo/Stasis. All aimed towers share detection filtering and cached-lock validation; private TowerConfig.CanDetectStealth opts a specialist in without globally revealing enemies. Existing attacks already emitted, splash and untargeted Shockwave remain able to damage hidden ships (stealth is targeting, not immunity).

New Ctrl / Utility Stealth Beacon:250 Credits,1×1,2s build,180 HP,256px range, one paid tier. It reveals enemies inside range for all towers; overlap, exit/re-entry, disable/reactivation, construction, destruction, relocation, enemy removal/reuse and world teardown are source-owned. One lazy spatial reveal index listens to enemy motion only while beacons are active; no per-frame enemy scans or per-beacon processing. See ENEMIES/Shared/Stealth, FEEDBACK/Stealth, TOWERS/Shared/Detection, TOWERS/Utility/StealthBeacon and TESTS/Stealth READMEs. Existing Inspector layout/resize is retained. Earlier blanket “stealth/detection future” notes below are historical; stealth preference scoring, Radar, tower-grant detection beacons and further content remain separate future work.

---

## Laser shield preference — implemented 2026-10-18

Laser configs now expose **Targeting → TargetPreference**, default **Shielded** (None opts out). This preference is separate from First/Last/Closest/Farthest/Strongest/Weakest: choose eligible in-range enemies with **CurrentShield > 0** first, apply the chosen mode within that group, and fall back to ordinary eligible targets when no preferred candidate qualifies. First/Last retain full remaining-journey distance across portals; Strongest/Weakest retain current hull + shield scoring and stable registration-order ties.

TowerTargeting.Find accepts an optional preference after its existing required-effect argument. One existing spatial traversal ranks preference tiers before ordinary scores; no second scan/list allocation, new per-frame loop or tower-local pathfinding. Other towers default to None. Eligibility/range/finite-route checks still run before preferences; Cryo's required-effect filter is unchanged. Laser reacquires on its existing staggered interval, including after shield depletion. Damage/beam/ammo/heat/rank values are unchanged.

Future stealth preference can add a real matching rule here once stealth/detection exists; it must not bypass detection/eligibility. No speculative stealth flag, detector or UI button is implemented now. The latest HP/ammo Inspector bars and compact-rank visual commit is preserved.

Validation: .NET SDK build (zero warnings/errors); ShieldTargetingChecks passed, including real Laser acquisition/opt-out/config isolation, six-mode preference/fallback, actual two-portal First/Last, shared route-query budget/RNG invariants and 500-enemy route sharing. PlacementHudChecks and CompactInspectorChecks also passed against the latest HP/ammo-bar Inspector revision. See TESTS/Towers/ShieldTargeting.md.

---

# Shared target selection — IT3–IT4

`TowerTargeting.Find` visits the world's existing spatial index within firing range. One compact visitor scores eligible living enemies; no result lists, scene searches, tower-local A* or per-frame enemy-registry scans are added. Equal scores use the existing stable registration order.

| Priority | Score policy |
|---|---|
| First (serialized 0) | Smallest finite estimated remaining Citadel journey |
| Closest (serialized 1) | Smallest squared physical tower distance |
| Last (2) | Largest finite estimated remaining Citadel journey |
| Farthest (3) | Largest squared physical tower distance |
| Strongest (4) | Largest **current Health + CurrentShield** |
| Weakest (5) | Smallest **current Health + CurrentShield** |

First/Closest retain original enum values and First remains the default. First/Last ignore unavailable, stale, failed or infinite estimates; infinity never wins Last. Scores are distance, not arrival time, so Slow does not arbitrarily change priority. Removed/out-of-range enemies never qualify. Cryo additionally filters already-slowed/immune enemies before scoring every mode, without fallback damage.

`ITowerTargetingPolicy` is optional on aimed weapon configs. `TowerBase.SetTargetPriority` validates placed state, enum and pause, changes only its private config, and notifies selected presentation. Aimed weapons set the existing search deadline to zero for prompt next-tick acquisition; cooldown, ammo and cached FinalStats stay intact. No choices are exposed for Shockwave or passive buildings. Runtime choice is not persisted yet; future saves should store the enum per tower using stable numeric values.

## Full-journey estimates

`Enemy.RemainingPathDistance` still means the current refined route leg. `RemainingJourneyDistance` adds a shared cached checkpoint tail for aimed targeting. No-checkpoint enemies retain their previous score. Movement, branch RNG, route budgets, clearance, handoffs and checkpoint obligations are unchanged.

`WORLD/NAVIGATION/JourneyEstimateCache` follows the existing captured graph with the same deterministic branch formula. It does not consume wave RNG, reroll links or force an enemy onto a preview path. Edges are shared by source/destination checkpoint, normalized effective A* profile and enemy radius. The current leg is actual refined distance; downstream legs use existing weighted navigation and clearance-tested geometry from a canonical valid source-circle representative to candidate destination regions / Citadel approaches. Physical lengths, not weighted costs, form the score. Circle entry points, later offsets, congestion and future obstructions make this an **estimate**, not an exact prediction of eventual travel.

Tail A* runs only in PathRequestManager's spare query/time budget, after movement requests. Combined LastFrameCalculations stays within MaxPathCalculationsPerFrame. LastFrameJourneyCalculations / TotalJourneyCalculations identify optional work. No extra per-enemy query loop runs during target comparison. Pending estimates exclude that enemy from First/Last deterministically; the four non-route modes remain available. Under sustained movement pressure tail estimates can wait.

Navigation/cost revisions and observed checkpoint geometry/removal invalidate shared tails. Active checkpoint movement additionally invalidates the old current-leg score until its normal replacement route is accepted. Captured live links remain stable through authoring link edits, matching existing movement. Buffers/caches are reused across revisions; estimate storage is capped at4096 shared edges. An unavailable/capped/blocked estimate safely excludes First/Last instead of choosing an infinite longest route. Removed enemies detach plan references; world exit releases checkpoint subscriptions. There is no tail allocation for ordinary no-checkpoint enemies.

Verification: `TESTS/UI/InspectorTargetingChecks.tscn`, `TESTS/Navigation/JourneyTargetingChecks.cs`, existing spatial oracle/benchmarks and navigation suites; actual results in `TESTS/UI/InspectorTargeting.md`.


## Stasis eligibility — ST1

Stasis passes RequiredEffect=Freeze to the existing eligibility hook. Already-frozen and Freeze-immune enemies are removed before mode scoring, including First/Last full journey estimates. Cryo Slow is independent and does not exclude Stasis targets. No eligible target means wait; after expiry the enemy qualifies again. This is a strict eligibility rule (like Cryo), distinct from Laser shield preference with fallback.


## MC2 cross-Citadel journeys

The selector itself is unchanged. Enemy.RemainingJourneyDistance now reflects its fixed assigned Citadel, including destination-keyed checkpoint tails. First/Last remain global physical remaining-distance comparisons, not preferred-objective selection; shield preference and Slow/Freeze eligibility still precede ordinary mode scoring. Retired destination estimates cannot win First/Last.
