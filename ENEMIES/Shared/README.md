# Shared enemies

`EnemyDefinition` is the Inspector-editable per-type resource. `Enemy` owns health, alive/dead/arrived state and removal. `EnemyRegistry` is a per-world list of living enemies shared by targeting and placement; no scene-tree rescans. Ships use lightweight Node2D roots with one movement helper and Sprite2D.

Arrival counts in the debug HUD and removes the ship. CitadelDamage and Armor are reserved data, not active mechanics. Death removes registry membership immediately before QueueFree, so additional bullets cannot damage a dead enemy.

## Kill credit rewards

Every `EnemyDefinition` exposes **Rewards / Kill Reward Credits** (defaults to 0). Enemy001 sets 3 in its `.tres`. Death runs `Enemy.Finish(false)` once, and `EnemyRegistry.RecordOutcome(false, enemy)` emits the centralized `EnemyKilled` event. `ECONOMY/Wallet/EconomyManager` listens and grants Credits once, also updating the HUD. Arrivals and non-kill removals never emit the event. This is not tied to a particular turret or wave. Do not pay credits on nonlethal hits.

## Automatic death puff

A killed enemy (only `Finish(false)`) calls the shared `FEEDBACK/DeathExplosion/DeathExplosion.Spawn` using its actual `Sprite2D` world size, or `RadiusPixels * 2` if no sprite is available. All enemy types inherit `EnemyDefinition.DeathExplosionMultiplier = 1.0`; optional Inspector overrides such as 1.5 for a boss scale the automatically calculated effect without changing the enemy's size. The effect is placed under independent `World/Effects`, so `Enemy.QueueFree` does not remove the puff. Arrivals, duplicate damage and world unloads trigger none. See `FEEDBACK/DeathExplosion/README.md`.
