# Enemy001 — basic test ship

`Enemy001.tscn` uses Shared/Enemy.cs, a Sprite2D and Movement/EnemyMovement.cs. `Artwork/enemy001.svg` is a replaceable small top-down placeholder facing right.

Select `Enemy001.tres` in Godot's FileSystem to edit DisplayName, EnemyId, MaxHealth (60), MoveSpeed (100 px/s), RadiusPixels (12) and TurnSpeed. CitadelDamage/Armor remain inactive. The current cell-center navigation supports small ships; keep radius below half a cell (32px with the default grid). Larger ship clearance/navigation will need a separate pass.

F5 -> tap F2 repeatedly. Each press spawns one ship at a permitted reachable Portal exit. Ships choose the shortest calculated route to a reachable Citadel approach cell and are removed on arrival or death. No wave schedule runs.

## Health display

`Enemy001.tscn` now includes a reusable `FEEDBACK/DefenseBars/DefenseBar.tscn` child. It appears **only after Enemy001 takes damage**, remains above the rotating ship, and hides at full/zero health. `Enemy.TakeDamage()` drives it directly; the bar does not poll health every frame. Edit `FEEDBACK/DefenseBars/DefaultDefenseBarConfig.tres` to adjust its small height, colors, vertical offset, minimum width and capped logarithmic growth for high-HP enemies. No enemy-specific drawing code is needed.

## Kill rewards

`Enemy001.tres` -> **Rewards / Kill Reward Credits** is **3** by default. Every killed Enemy001, including F2 and wave spawns, grants 3 Credits to the current level's wallet. Other enemy types can set their own reward in their `EnemyDefinition.tres`; the shared default is 0. Ships reaching the Citadel or unloaded without a kill give no reward. An enemy can pay out only once. The wallet's `BalanceChanged` event updates the top-left HUD. Credit caps still apply. Wave-completion bonuses are a separate, future feature.
