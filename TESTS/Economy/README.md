# Economy tests

Wallet transactions, kill rewards and the existing ammunition foundation helpers run in the integration suite. `AmmunitionLoopChecks.tscn` is a separate production-world end-to-end runner, with focused `WeaponAmmunitionChecks`, `DeliverySafetyChecks` and shared `AmmoLoopFixture` files rather than one large test script. See [Ammunition.md](Ammunition.md) for current results and coverage; [the test index](../README.md) has other suite commands.
