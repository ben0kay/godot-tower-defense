# ECONOMY — Shared Credits, Resources, Wallet and Transactions

This **top-level umbrella** owns the shared economy for the tower-defense game. It is **NOT** the same as `TOWERS/Economy/`: that folder holds placeable producer towers (Solar/Resource/Refinery), while this folder holds **player balances, resource identities and payment/reward mechanics** that any gameplay system may use.

## Status: Basic foundation only (October 2026)

**Created in this foundation:** `EconomyMasterConfig.cs` + `DefaultEconomyConfig.tres` (Inspector-editable proposed balance rules), `Resources/ResourceDefinition.cs` (typed resource identity), `Resources/Credits.tres` (initial credit definition), and the Wallet / Resources / Transactions README scaffolds.

**NOT implemented:** there is no live EconomyManager or player wallet; no level starting balance, affordability checks, construction/upgrade payments, income, kill rewards, selling or saving. The gameplay HUD has a `SetCredits` entry point but displays `--` until supplied actual state. Building towers is currently free even though Battle Tower shows a proposed price.

## Folder ownership

```text
ECONOMY/
├── EconomyMasterConfig.cs                # Exists: proposed global balance rules
├── DefaultEconomyConfig.tres             # Exists: Inspector-editable resource
├── README.md
├── Resources/
│   ├── ResourceDefinition.cs             # Exists: stable ResourceId and display metadata
│   ├── Credits.tres                      # Exists: resource definition, ID 'credits'
│   └── README.md
├── Wallet/
│   ├── README.md
│   └── EconomyManager.cs                 # Codex TODO (does not exist yet)
└── Transactions/
    ├── README.md
    └── ResourceCost.cs                   # Codex TODO (does not exist yet)
```

## How the economy integrates with the rest of the game

| System | Responsibility |
| --- | --- |
| `WORLD/LEVELS/LevelConfig.cs` | **Per-level starting credits**, Inspector-editable; each map can have a different starting amount. Codex to add. |
| `ECONOMY/Resources/` | Definition of Credits now, and possible Alloy, Energy Cells, etc. later. Identity, not balances. |
| `ECONOMY/Wallet/` | **One runtime wallet per playable session/level**, initialized from level settings; owns current amounts and emits change notifications. Codex to implement. |
| `ECONOMY/Transactions/` | Reusable typed cost entries plus **all-or-nothing** affordability, spending and granting. Codex to implement. |
| `TOWERS/Shared/Costs/` (planned) | Uses the shared `ResourceCost` entries to describe tower/upgrade prices; **no second wallet or cost-entry type**. |
| `TOWERS/Placement/` | Only after validating tile rules and spawn-to-Citadel connectivity, check/consume build costs without partial builds. |
| `TOWERS/Shared/Upgrades/` (planned) | Uses the same `ResourceCost` entries for upgrade tiers. |
| `TOWERS/Economy/` | A resource-producing tower can ask the wallet to grant credits/materials, but never owns the shared balance. |
| `ENEMIES/` | Future rewards can grant credits on a verified enemy death once; none are active yet. |
| `UI/GameplayHUD/` | Top-left Credits label is a **view**, updated by events or pushes via `GameplayHUD.SetCredits(int)`; it never stores or spends balances. |

## Codex — requested first economy implementation

Read this file, `TOWERS/README.md`, `UI/GameplayHUD/README.md`, `WORLD/LEVELS/LevelConfig.cs` and `NOTES/CodePreferences.md`, then inspect existing tower placement and Inspector API.

1. Implement `Wallet/EconomyManager.cs` as a single level/session-scoped owner of balances. Seed Credits from an **Inspector-editable `StartingCredits` in each LevelConfig .tres**, NOT a global constant. On level restart/new level, initialize from the relevant level settings; do not accidentally carry another level's balance unless an explicit later campaign system says so.
2. Implement **one** reusable Inspector-editable `Transactions/ResourceCost.cs` Resource containing `ResourceDefinition` asset reference + nonnegative integer amount. Use collections of these entries for both tower build costs and future upgrade costs. Do not define parallel `TowerCostEntry` and `ResourceCost` models unless a concrete need emerges.
3. Implement `CanAfford(costs)`, `TrySpend(costs)`, and `Grant(resource, amount)` (or equivalent compact API). Validate **all costs together before any deduction**, including duplicate resource entries, null definitions, zero/negative amounts and overflow. A failed operation changes no balances; never allow negative currency by default.
4. Convert Battle Tower's display-only **`BuildCost=100`** to a **100 Credits** cost entry. Retire or migrate the legacy field without double charges. Keep tower config pricing editable in the Inspector.
5. **Integration:** validate build terrain/occupancy and spawn-to-Citadel route first; then make cost deduction and successful tower placement one consistent transaction. If scene creation/placement fails, never lose credits. Prevent a simultaneous or double-click purchase from spending twice. Preserve the current debug build workflow only if it is an explicit, clearly labeled toggle.
6. Initialize `GameplayHUD.SetCredits` with the actual live balance and update it **only when credit balance changes**, not per physics frame. Render numbers with grouping; do not invent starting funds while no economy exists.
7. Add tests: separate levels with different starting credits; affordable/unaffordable and multi-resource construction; no partial deduction on failure; refund/rollback on placement failure; HUD update events; preservation of navigation/connectivity build restrictions; no duplicate rewards.
8. Upgrade pricing and selling can come in a **later focused pass** after construction spending works. Keep credit rewards, resource generators and wave income for subsequent systems; the basic wallet should already be reusable for them.

**Key separation:** `Resources/ResourceDefinition.cs` is just metadata; `Credits.tres` represents a resource **type**, not a stored amount. `EconomyMasterConfig.cs` holds future global economy constraints, **never a map's starting credits**. Do not write economy logic in `GameplayHUD.cs`, `Enemy.cs` or individual `BattleTower.cs` instances.

## Design and code conventions

Use Godot 4 C#/.NET, clear `[ExportGroup]` settings where needed, short script descriptions, `// =========================================================` above methods, small reusable methods and per-feature READMEs. Avoid polling, per-tower wallet instances, extra autoloads unless justified, multiple competing price definitions and premature crafting/inventory complexity. Verify Godot's .NET build and test in `Main.tscn` after Codex adds the live systems.
