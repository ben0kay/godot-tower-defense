# ShieldGenerator — first optional support enemy

DefaultEnemyCatalog registers the stable `shield_emitter_mk1` ID; `ShieldGenerator` remains the development asset name. The scene reuses Enemy and EnemyMovement with teal 40×32 placeholder art, Movement, hull DefenseBar and Abilities/ShieldEmitter. Provisional EnemyDefinition balance: 100 hull,80 optional shield capacity,70px/s,16px radius,2 Citadel damage,5 kill Credits. Default spawns have no starting shield; shield:true starts full. AffectSelf=false means this generator initially protects allies rather than replenishing itself.

ShieldEmitter's saved config lives in ENEMIES/Abilities/ShieldEmitter/DefaultShieldEmitterConfig.tres. Radius/pulse/eligibility/budgets are Inspector-editable; live emitter configs/timers are private. Current allies keep supplied shield points when the source dies/arrives/unloads. Enemy001/Enemy002 remain plain scenes without ability nodes.

Author Enemy("shield_emitter_mk1") in a wave, or explicitly select ShieldGenerator.tscn as the level Debug EnemyScene to test it with F2. F2 cycles catalog scenes, including ShieldGenerator, while Level001's authored waves do not include this type. No attack, death-spawn, shield visual, capacity-granting aura or linked shield mechanic is implemented.

## Shared scene inheritance (GE1–GE3)

This type's `.tscn` inherits `ENEMIES/Shared/GenericEnemy.tscn`. Its own Definition and SVG remain independently editable; Sprite, Movement and DefenseBar defaults come from the parent. Edit common settings there, or deliberately override an inherited property here and use Inspector Revert to resume inheritance. Only ShieldGenerator and Transporter add their respective optional ability children. Catalog registration, stable IDs and gameplay values are unchanged.
