# Wallet — Planned Runtime EconomyManager

**No EconomyManager script exists yet.** Codex will implement `EconomyManager.cs` here.

Responsibilities: maintain one current balance per resource definition **for the loaded level/session**, initialize from each level's Inspector-editable `StartingCredits`, validate credits and other resources, award/grant, and publish balance-change signals to the HUD. Do not create a wallet for each tower; no per-frame polling.

Expose a compact API such as `GetBalance(resource)`, `CanAfford(costs)`, `TrySpend(costs)`, `Grant(resource, amount)`, and a `BalanceChanged` signal/event. Route every construction/upgrade payment through this service, never directly edit an HUD label.

Default policy: no negative balances. Full `ECONOMY/README.md` lists the tower integration and failure/rollback tests.
