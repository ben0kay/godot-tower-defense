# Economy tests

Wallet transactions, kill rewards and the existing ammunition foundation helpers run in the integration suite. `AmmunitionLoopChecks.tscn` is a separate production-world end-to-end runner, with focused `WeaponAmmunitionChecks`, `DeliverySafetyChecks` and shared `AmmoLoopFixture` files rather than one large test script. See [Ammunition.md](Ammunition.md) for current results and coverage; [the test index](../README.md) has other suite commands.


## CP1A Citadel backend — code landed 2026-10-18

`TESTS/Economy/CitadelCreditChecks.tscn` exercises `CreditBuffer`/`CreditProductionTimer`, real wave-state-only Citadel production, pausing, capped storage, real wallet collection/overflow/idempotency and independent two-Citadel buffers. No scene/UI clicks are wired until CP1B.

**Execution is pending:** build the .NET solution, then run `godot --headless --path . res://TESTS/Economy/CitadelCreditChecks.tscn`. This change has not been Godot-compiled or headless-tested by the authoring environment. See `ECONOMY/Production/README.md` for the detailed contract and remaining CP1B–CP3 plan.

## CP2 / CP3 economy towers

`CreditTowerChecks.tscn` passes paid construction/upgrade, private tier data, active-wave timers, attributed-kill range/overlap/lifecycle, exact XP/normal rewards and Inspector collection/overflow. Existing CP1 buffer/collection suites also pass. See CreditTowers.md for full coverage and unrelated baseline fixture failures.
