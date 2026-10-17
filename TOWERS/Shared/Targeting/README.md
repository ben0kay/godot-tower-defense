# Target selection

`TowerTargeting.Find` queries the world's shared spatial enemy index by weapon range, then compares cached remaining route length for First or squared distance to the tower for Closest. A failed/stale/pending route has infinite distance and cannot win First. Different movement speeds do not affect First; estimated time to arrival would be a separate future priority.

BattleTower staggers acquisition by instance ID and checks periodically (default 0.15s), including valid targets so overtaking can change First. Between searches, range/alive checks are cheap. The spatial visitor scores cached nearby positions directly, without per-search collections. Stable registration order preserves the former living-list tie-break; First still rejects nonfinite/stale remaining paths. Query range is conservatively expanded for float boundary rounding, then the unchanged float range comparison is applied.


Laser Tower also staggers periodic First/Closest searches and validates cached range/alive state between them. Periodic search may replace an existing valid beam lock with the current best candidate; invalid lock hides immediately, and removal/unload subscriptions clear visuals before the next tick.

See `TESTS/Combat/SpatialQueryChecks.cs` for brute-force parity and `TESTS/Performance/SpatialQueryBenchmarks.md` for paired sparse/dense timing, maintenance, candidate and allocation results.

## Planned targeting extension IT1–IT4 — NOT IMPLEMENTED

See [the Inspector/targeting implementation plan](../../../UI/GameplayHUD/Inspector/INSPECTOR_TARGETING_PASS.md). Six priorities are proposed: First, Last, Closest, Farthest, Strongest and Weakest, chosen **per placed aimed tower** from a wider selected Inspector. Current implementation remains **First/Closest only**, and First currently measures remaining physical distance along the active route leg, not the full downstream checkpoint chain. The plan requires scalable, bounded cached journey estimates; no tower-per-enemy A* on targeting.

**Inspector IT1 note:** the selected aimed tower now displays its authored `First` or `Closest` target priority as a **read-only value**, using `TowerConfig.InspectorTargetPriorityLabel` overrides. Six selectable buttons and full downstream checkpoint scoring still belong to IT3/IT4; neither targeting algorithm nor spatial-query cadence changed in IT1–IT2.
