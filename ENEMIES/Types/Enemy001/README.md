# Enemy001 asset — Standard Drone (`basic_mk1`)

The numbered asset filename remains for development history. **EnemyId** is `basic_mk1` and **DisplayName** is `Standard Drone`, independently of wave order. `Enemy001.tscn` uses Shared/Enemy.cs, a Sprite2D and Movement/EnemyMovement.cs. `Artwork/enemy001.svg` is a replaceable small top-down placeholder facing right.

Select `Enemy001.tres` in Godot's FileSystem to edit DisplayName, EnemyId, MaxHealth (60), MoveSpeed (100 px/s), RadiusPixels (12) and TurnSpeed. CitadelDamage is active on arrival; Armor remains inactive. The current cell-center navigation supports small ships; keep radius below half a cell (32px with the default grid). Larger ship clearance/navigation will need a separate pass.

F5 -> tap F2 repeatedly. Each press spawns one ship at a permitted reachable Portal exit. Ships choose the shortest calculated route to a reachable Citadel approach cell and are removed on arrival or death. Waves start manually through the existing HUD.

## Health display

`Enemy001.tscn` now includes a reusable `FEEDBACK/DefenseBars/DefenseBar.tscn` child. For **unshielded** Enemy001 it appears only after hull damage, as before. For wave-spawned **shielded** Enemy001 it shows two bars immediately: warm shield above green hull, both sharing the same capped width. It remains above the rotating ship and hides at full hull health after the shield is depleted, or at zero hull health. `Enemy.TakeDamage()` drives it directly; the bar does not poll health every frame. Edit `FEEDBACK/DefenseBars/DefaultDefenseBarConfig.tres` to adjust its small height, colors, vertical offset, minimum width and capped logarithmic growth for high-HP enemies. No enemy-specific drawing code is needed.

## Kill rewards

`Enemy001.tres` -> **Rewards / Kill Reward Credits** is **3** by default. Every killed Enemy001, including F2 and wave spawns, grants 3 Credits to the current level's wallet. Other enemy types can set their own reward in their `EnemyDefinition.tres`; the shared default is 0. Ships reaching the Citadel or unloaded without a kill give no reward. An enemy can pay out only once. The wallet's `BalanceChanged` event updates the top-left HUD. Credit caps still apply. Wave-completion bonuses are a separate, future feature.

## Death explosion

Killed Enemy001 ships now produce an automatically sized **orange flash followed by a grey/orange smoke puff**; arrival at the Citadel produces none. Size is measured from Enemy001's actual `Sprite2D` (32×24 px by default), including all scale transforms. `Enemy001.tres` inherits **Death Feedback / Death Explosion Multiplier = 1.0** from the shared enemy definition, so **nothing needs to be configured on new enemy types**. Override just that multiplier if a particular enemy should have a bigger or more subtle puff. Shared look, particle count and size limits are in `FEEDBACK/DeathExplosion/`.

## Shield capacity (opt-in wave behavior)

The saved `Enemy001.tres` now has **MaxShield = 20** (small opt-in shield capacity). This does **not** automatically shield all Standard Drones: F2/debug and wave entries without `shield: true` still start at zero. `LEVELS/Level001/Level001Waves.cs` uses a few explicitly shielded `Enemy("basic_mk1", shield: true)` entries in each wave, with increasing proportions. A shielded ship shows the shared reddish `FEEDBACK/Shields/` aura, absorbs damage before hull HP, and flashes/fades when its shield breaks. The shared hull meter now displays a slim orange-red shield row above it while any shield points remain. Each row reflects its own points and both are controlled by the same event-driven `DefenseBar` node. **No passive shield regeneration.** The integration shield-math fixtures intentionally override this authored 20-point capacity to 100 temporarily to verify exact 100-point damage arithmetic.

## E2 preservation
Enemy001's hull, speed, scene/art and F2 defaults remain unchanged; only its optional capacity was raised from 0 to 20. DefaultEnemyCatalog still registers its stable EnemyId. It has no ability node and receives no natural shield regeneration. The existing ShieldEmitter ability on a separate support enemy can restore eligible nearby shields, but **Level001 does not currently spawn Shield Generators**. Ordinary spawns stay unshielded unless `shield: true` is authored. Typed shield/hull damage, 3-Credit kills, 1 Citadel arrival damage and navigation remain active.

## Shared scene inheritance (GE1–GE3)

This type's `.tscn` inherits `ENEMIES/Shared/GenericEnemy.tscn`. Its own Definition and SVG remain independently editable; Sprite, Movement and DefenseBar defaults come from the parent. Edit common settings there, or deliberately override an inherited property here and use Inspector Revert to resume inheritance. Only ShieldGenerator and Transporter add their respective optional ability children. Catalog registration, stable IDs and gameplay values are unchanged.
