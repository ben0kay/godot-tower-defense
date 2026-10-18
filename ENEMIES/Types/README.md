# Enemy types

**Elite mk.1:** `Enemy006/` adds an 80×60 fast, heavily armored enemy (`elite_mk1`) with 1,000 HP, 250 optional shields, 100px/s and 10 kill Credits. It inherits `GenericEnemy.tscn` and is in the default catalog; existing waves are unchanged. See [Enemy006/README.md](Enemy006/README.md).

**Heavy Big mk.1:** `Enemy005/` is a 112×120 plated heavyweight ship with ID `big_mk1`, 1,000 HP, 200 optional shields, 80 px/s speed, 5 Citadel damage and 10 Credits. Inherits `Shared/GenericEnemy.tscn` with no unique scripts or abilities. See [Enemy005](Enemy005/README.md). Its shields remain wave-opt-in, not permanently enabled.


`Enemy001/` is the first reusable basic test ship. Enemy002 and later variants can reuse Shared/Enemy.cs and Movement/EnemyMovement.cs, changing scenes, artwork and EnemyDefinition resources instead of duplicating scripts.

**New medium hull:** `Enemy004/Enemy004.tscn` is the larger 64×48px `medium_mk1` / **Bulwark Drone** with 160 HP, 75 speed and a 22px clearance radius; see [Enemy004/README.md](Enemy004/README.md). `Enemy003` is intentionally not required for this asset and its absence does not dictate wave order.

Level-specific composition and future waves belong elsewhere. **Naming contract:** keep numbered development assets such as `Enemy001.tscn` and `Enemy002.tscn`. Set each `.tres`'s **unique stable `EnemyId`** for gameplay/wave lookup (`basic_mk1`, `scout_mk1`, `shield_emitter_mk1`), and its **`DisplayName`** for players (`Standard Drone`, `Swift Interceptor`, `Shield Generator`). Neither ID nor filename dictates wave appearance/order. New `EnemyDefinition` instances intentionally start with an empty ID; explicitly author a unique ID before registering a scene. Do not rename IDs lightly once saved levels refer to them.
