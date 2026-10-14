# Enemy001 — basic test ship

`Enemy001.tscn` uses Shared/Enemy.cs, a Sprite2D and Movement/EnemyMovement.cs. `Artwork/enemy001.svg` is a replaceable small top-down placeholder facing right.

Select `Enemy001.tres` in Godot's FileSystem to edit DisplayName, EnemyId, MaxHealth (60), MoveSpeed (100 px/s), RadiusPixels (12) and TurnSpeed. CitadelDamage/Armor remain inactive. The current cell-center navigation supports small ships; keep radius below half a cell (32px with the default grid). Larger ship clearance/navigation will need a separate pass.

F5 -> tap F2 repeatedly. Each press spawns one ship at a permitted reachable Portal exit. Ships choose the shortest calculated route to a reachable Citadel approach cell and are removed on arrival or death. No wave schedule runs.
