# Enemy types

`Enemy001/` is the first reusable basic test ship. Enemy002 and later variants can reuse Shared/Enemy.cs and Movement/EnemyMovement.cs, changing scenes, artwork and EnemyDefinition resources instead of duplicating scripts.

Level-specific composition and future waves belong elsewhere. File identity, definition EnemyId and player-facing DisplayName are independent.
