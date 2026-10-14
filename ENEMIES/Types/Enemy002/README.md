# Enemy002 — weaker, faster plain ship

Shared Enemy/EnemyMovement scene with its own right-facing artwork and Inspector EnemyDefinition. Provisional balance:30 hull,40 optional shield capacity,160px/s,8px radius,1 Citadel damage and2 kill Credits. Enemy001 remains 60 hull/100px/s/12px radius/3 Credits. Standard spawns start unshielded; shield:true requests40 shield for Enemy002. No ability nodes or unique AI script are required.

DefaultEnemyCatalog registers enemy002. Author Enemy("enemy002") in a wave or change the level Debug EnemyScene to Enemy002.tscn for manual tests. F2 defaults and Level001 encounters are unchanged. Hull bar, typed/legacy hits, route budgets, kill feedback/reward and Citadel arrival use existing shared mechanics.
