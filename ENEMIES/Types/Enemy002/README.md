# Enemy002 asset — Swift Interceptor (`scout_mk1`)

Shared Enemy/EnemyMovement scene with its own right-facing artwork and Inspector EnemyDefinition. Provisional balance:30 hull,40 optional shield capacity,160px/s,8px radius,1 Citadel damage and2 kill Credits. Enemy001 remains 60 hull/100px/s/12px radius/3 Credits. Standard spawns start unshielded; shield:true requests40 shield for Enemy002. No ability nodes or unique AI script are required.

DefaultEnemyCatalog registers the stable `scout_mk1` ID, regardless of the numbered scene filename. Author Enemy("scout_mk1") in a wave or change the level Debug EnemyScene to Enemy002.tscn for manual tests. F2 cycles every registered scene in catalog order, while current Level001 waves still use only `basic_mk1`. Hull bar, typed/legacy hits, route budgets, kill feedback/reward and Citadel arrival use existing shared mechanics.
