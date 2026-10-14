# Shared enemies

`EnemyDefinition` is the Inspector-editable per-type resource. `Enemy` owns health, alive/dead/arrived state and removal. `EnemyRegistry` is a per-world list of living enemies shared by targeting and placement; no scene-tree rescans. Ships use lightweight Node2D roots with one movement helper and Sprite2D.

Arrival counts in the debug HUD and removes the ship. CitadelDamage and Armor are reserved data, not active mechanics. Death removes registry membership immediately before QueueFree, so additional bullets cannot damage a dead enemy.
