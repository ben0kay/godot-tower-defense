# Shared enemies

`EnemyDefinition` is the Inspector-editable per-type resource. `Enemy` owns health, alive/dead/arrived state and removal. `EnemyRegistry` is a per-world list of living enemies shared by targeting and placement; no scene-tree rescans. Ships use lightweight Node2D roots with one movement helper and Sprite2D.

Arrival counts in the debug HUD and removes the ship. CitadelDamage and Armor are reserved data, not active mechanics. Death removes registry membership immediately before QueueFree, so additional bullets cannot damage a dead enemy.

## Kill credit rewards

Every `EnemyDefinition` exposes **Rewards / Kill Reward Credits** (defaults to 0). Enemy001 sets 3 in its `.tres`. Death runs `Enemy.Finish(false)` once, and `EnemyRegistry.RecordOutcome(false, enemy)` emits the centralized `EnemyKilled` event. `ECONOMY/Wallet/EconomyManager` listens and grants Credits once, also updating the HUD. Arrivals and non-kill removals never emit the event. This is not tied to a particular turret or wave. Do not pay credits on nonlethal hits.
