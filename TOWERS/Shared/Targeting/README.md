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
