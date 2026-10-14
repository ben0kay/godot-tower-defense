# Shared enemies

`EnemyDefinition` is the Inspector-editable per-type resource. `Enemy` owns health, alive/dead/arrived state and removal. `EnemyRegistry` is a per-world list of living enemies shared by targeting and placement; no scene-tree rescans. Ships use lightweight Node2D roots with one movement helper and Sprite2D.

Arrival counts in the debug HUD and removes the ship. CitadelDamage is applied by CitadelSession; Armor is reserved for later mitigation. Death removes registry membership immediately before QueueFree, so additional bullets cannot damage a dead enemy.

## Kill credit rewards

Every `EnemyDefinition` exposes **Rewards / Kill Reward Credits** (defaults to 0). Enemy001 sets 3 in its `.tres`. Death runs `Enemy.Finish(false)` once, and `EnemyRegistry.RecordOutcome(false, enemy)` emits the centralized `EnemyKilled` event. `ECONOMY/Wallet/EconomyManager` listens and grants Credits once, also updating the HUD. Arrivals and non-kill removals never emit the event. This is not tied to a particular turret or wave. Do not pay credits on nonlethal hits.

## Automatic death puff

A killed enemy (only `Finish(false)`) calls the shared `FEEDBACK/DeathExplosion/DeathExplosion.Spawn` using its actual `Sprite2D` world size, or `RadiusPixels * 2` if no sprite is available. All enemy types inherit `EnemyDefinition.DeathExplosionMultiplier = 1.0`; optional Inspector overrides such as 1.5 for a boss scale the automatically calculated effect without changing the enemy's size. The effect is placed under independent `World/Effects`, so `Enemy.QueueFree` does not remove the puff. Arrivals, duplicate damage and world unloads trigger none. See `FEEDBACK/DeathExplosion/README.md`.

## Optional per-enemy shield capacity (implemented)

`EnemyDefinition.MaxShield` is an Inspector-editable **capacity** (default `0`); it does **not** mean every spawn has shields. `Enemy.CurrentShield` is private-set, per-instance runtime state initialized by `Enemy.Initialize(..., spawnWithShield: false)`. A wave entry with `shield: true` starts that specific ship at full `MaxShield`; all default spawns, including F2/debug, start at zero. If a definition has zero capacity, the opt-in remains zero.

`Enemy.TakeDamage` absorbs finite positive damage into the shield first and applies overflow to hull `Health` in the same hit. The existing `DefenseBar` continues showing **hull HP only**; shield-only hits leave it hidden at full hull health. Shield rendering, regeneration, resistance and armor are separate future features. Dying/arriving/rewards work as before. No wave or spawn changes are stored on the shared authored `.tres` file.


Typed ApplyDamage(DamageInfo) now delegates base hits to COMBAT/Damage. TakeDamage(float) keeps 1:1 shield-first behavior. One internal helper commits state; only hull changes update DefenseBar, only hull death pays a kill. Capacity/opt-in spawning remain unchanged.
