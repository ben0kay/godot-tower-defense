# Enemy types

`Enemy001/` is the first reusable basic test ship. Enemy002 and later variants can reuse Shared/Enemy.cs and Movement/EnemyMovement.cs, changing scenes, artwork and EnemyDefinition resources instead of duplicating scripts.

Level-specific composition and future waves belong elsewhere. **Naming contract:** keep numbered development assets such as `Enemy001.tscn` and `Enemy002.tscn`. Set each `.tres`'s **unique stable `EnemyId`** for gameplay/wave lookup (`basic_mk1`, `scout_mk1`, `shield_emitter_mk1`), and its **`DisplayName`** for players (`Standard Drone`, `Swift Interceptor`, `Shield Generator`). Neither ID nor filename dictates wave appearance/order. New `EnemyDefinition` instances intentionally start with an empty ID; explicitly author a unique ID before registering a scene. Do not rename IDs lightly once saved levels refer to them.
