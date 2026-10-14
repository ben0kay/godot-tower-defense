# Enemy001 — basic test ship

`Enemy001.tscn` uses Shared/Enemy.cs, a Sprite2D and Movement/EnemyMovement.cs. `Artwork/enemy001.svg` is a replaceable small top-down placeholder facing right.

Select `Enemy001.tres` in Godot's FileSystem to edit DisplayName, EnemyId, MaxHealth (60), MoveSpeed (100 px/s), RadiusPixels (12) and TurnSpeed. CitadelDamage is active on arrival; Armor remains inactive. The current cell-center navigation supports small ships; keep radius below half a cell (32px with the default grid). Larger ship clearance/navigation will need a separate pass.

F5 -> tap F2 repeatedly. Each press spawns one ship at a permitted reachable Portal exit. Ships choose the shortest calculated route to a reachable Citadel approach cell and are removed on arrival or death. Waves start manually through the existing HUD.

## Health display

`Enemy001.tscn` now includes a reusable `FEEDBACK/DefenseBars/DefenseBar.tscn` child. It appears **only after Enemy001 takes damage**, remains above the rotating ship, and hides at full/zero health. `Enemy.TakeDamage()` drives it directly; the bar does not poll health every frame. Edit `FEEDBACK/DefenseBars/DefaultDefenseBarConfig.tres` to adjust its small height, colors, vertical offset, minimum width and capped logarithmic growth for high-HP enemies. No enemy-specific drawing code is needed.

## Kill rewards

`Enemy001.tres` -> **Rewards / Kill Reward Credits** is **3** by default. Every killed Enemy001, including F2 and wave spawns, grants 3 Credits to the current level's wallet. Other enemy types can set their own reward in their `EnemyDefinition.tres`; the shared default is 0. Ships reaching the Citadel or unloaded without a kill give no reward. An enemy can pay out only once. The wallet's `BalanceChanged` event updates the top-left HUD. Credit caps still apply. Wave-completion bonuses are a separate, future feature.

## Death explosion

Killed Enemy001 ships now produce an automatically sized **orange flash followed by a grey/orange smoke puff**; arrival at the Citadel produces none. Size is measured from Enemy001's actual `Sprite2D` (32×24 px by default), including all scale transforms. `Enemy001.tres` inherits **Death Feedback / Death Explosion Multiplier = 1.0** from the shared enemy definition, so **nothing needs to be configured on new enemy types**. Override just that multiplier if a particular enemy should have a bigger or more subtle puff. Shared look, particle count and size limits are in `FEEDBACK/DeathExplosion/`.

## Shield capacity (opt-in wave behavior)

`Enemy001.tres` sets **Balance / Max Shield = 100** for testing. This is *capacity only*: standard Level001 wave entries and F2/debug enemies **still start at 0 shield**. To give this same enemy shields in a wave, author `Enemy("enemy001", shield: true)` in `LEVELS/Level001/Level001Waves.cs` (or mix weighted shielded/unshielded entries). A shielded ship absorbs damage before hull HP, with overflow passed to health; the current health bar still displays hull only. No passive recharge/blue shield bar yet.

## E2 preservation
Enemy001 scene/art/base balance/F2 defaults are unchanged. DefaultEnemyCatalog registers its existing EnemyDefinition.EnemyId. It has no ability node, but its 100 shield capacity can receive restoration from a registered ShieldGenerator. Ordinary starting shield remains 0 unless wave shield:true is authored. Typed shield/hull damage,3-Credit kills,1 Citadel arrival damage and shared navigation remain active.
