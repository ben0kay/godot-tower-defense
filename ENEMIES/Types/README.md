# Enemy types

`Enemy001/` is the first reusable basic test ship. Enemy002 and later variants can reuse Shared/Enemy.cs and Movement/EnemyMovement.cs, changing scenes, artwork and EnemyDefinition resources instead of duplicating scripts.

**New medium hull:** `Enemy004/Enemy004.tscn` is the larger 64×48px `medium_mk1` / **Bulwark Drone** with 160 HP, 75 speed and a 22px clearance radius; see [Enemy004/README.md](Enemy004/README.md). `Enemy003` is intentionally not required for this asset and its absence does not dictate wave order.

Level-specific composition and future waves belong elsewhere. **Naming contract:** keep numbered development assets such as `Enemy001.tscn` and `Enemy002.tscn`. Set each `.tres`'s **unique stable `EnemyId`** for gameplay/wave lookup (`basic_mk1`, `scout_mk1`, `shield_emitter_mk1`), and its **`DisplayName`** for players (`Standard Drone`, `Swift Interceptor`, `Shield Generator`). Neither ID nor filename dictates wave appearance/order. New `EnemyDefinition` instances intentionally start with an empty ID; explicitly author a unique ID before registering a scene. Do not rename IDs lightly once saved levels refer to them.
