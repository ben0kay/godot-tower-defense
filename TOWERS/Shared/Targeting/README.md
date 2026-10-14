# Target selection

`TowerTargeting.Find` filters the world's living registry by weapon range, then compares cached remaining route length for First or squared distance to the tower for Closest. A failed/stale/pending route has infinite distance and cannot win First. Different movement speeds do not affect First; estimated time to arrival would be a separate future priority.

BattleTower staggers acquisition by instance ID and checks periodically (default 0.15s), including valid targets so overtaking can change First. Between searches, range/alive checks are cheap. Spatial buckets and further priorities can follow profiling.


Laser Tower also staggers periodic First/Closest searches and validates cached range/alive state between them. Periodic search may replace an existing valid beam lock with the current best candidate; invalid lock hides immediately, and removal/unload subscriptions clear visuals before the next tick.
