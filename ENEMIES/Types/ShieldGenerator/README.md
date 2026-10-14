# ShieldGenerator — first optional support enemy

DefaultEnemyCatalog registers shieldgenerator. The scene reuses Enemy and EnemyMovement with teal 40×32 placeholder art, Movement, hull DefenseBar and Abilities/ShieldEmitter. Provisional EnemyDefinition balance: 100 hull,80 optional shield capacity,70px/s,16px radius,2 Citadel damage,5 kill Credits. Default spawns have no starting shield; shield:true starts full. AffectSelf=false means this generator initially protects allies rather than replenishing itself.

ShieldEmitter's saved config lives in ENEMIES/Abilities/ShieldEmitter/DefaultShieldEmitterConfig.tres. Radius/pulse/eligibility/budgets are Inspector-editable; live emitter configs/timers are private. Current allies keep supplied shield points when the source dies/arrives/unloads. Enemy001/Enemy002 remain plain scenes without ability nodes.

Author Enemy("shieldgenerator") in a wave, or explicitly select ShieldGenerator.tscn as the level Debug EnemyScene to test it with F2. The default F2 scene and Level001 authored encounters remain unchanged. No attack, death-spawn, shield visual, capacity-granting aura or linked shield mechanic is implemented.
