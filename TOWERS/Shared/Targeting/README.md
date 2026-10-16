# Target selection

`TowerTargeting.Find` queries the world's shared spatial enemy index by weapon range, then compares cached remaining route length for First or squared distance to the tower for Closest. A failed/stale/pending route has infinite distance and cannot win First. Different movement speeds do not affect First; estimated time to arrival would be a separate future priority.

BattleTower staggers acquisition by instance ID and checks periodically (default 0.15s), including valid targets so overtaking can change First. Between searches, range/alive checks are cheap. The spatial visitor scores cached nearby positions directly, without per-search collections. Stable registration order preserves the former living-list tie-break; First still rejects nonfinite/stale remaining paths. Query range is conservatively expanded for float boundary rounding, then the unchanged float range comparison is applied.


Laser Tower also staggers periodic First/Closest searches and validates cached range/alive state between them. Periodic search may replace an existing valid beam lock with the current best candidate; invalid lock hides immediately, and removal/unload subscriptions clear visuals before the next tick.

See `TESTS/Combat/SpatialQueryChecks.cs` for brute-force parity and `TESTS/Performance/SpatialQueryBenchmarks.md` for paired sparse/dense timing, maintenance, candidate and allocation results.
