# Resources — Types and Presentation, Not Stored Balances

- `ResourceDefinition.cs` is a simple Godot `[GlobalClass] Resource` with stable `ResourceId`, `DisplayName`, optional description and icon.
- `Credits.tres` is the **first resource type** (ID `credits`). Select it in Godot's FileSystem to edit its display information.
- Later materials such as Alloy or Energy Cells can each have a separate `.tres` using the same definition.

A resource definition has **no current amount**. Per-level starting Credits live in `LEVELS/LevelConfig.cs` and the *mutable balance* belongs in `ECONOMY/Wallet/EconomyManager.cs` for the active session.

Codex should reference these definition assets from reusable `ECONOMY/Transactions/ResourceCost.cs` entries and the wallet, rather than comparing display strings. Resource IDs must be unique and stable across save files and upgrade recipes. Currency logic lives in the wallet, not these definitions.
