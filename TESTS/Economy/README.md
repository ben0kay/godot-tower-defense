## Tower durability rebalance — 2026-10-20 (source updated; Godot runtime tests pending)

**Current authoritative defaults supersede older HP references below:** Base/max hull health for every available player-built tower/structure is **10× the previous level-1 value**. All **nine offensive towers** (Battle, Laser, Blaster Cannon, Shockwave, Machine Gun, Sniper, Rocket, Cryo, Stasis) have explicit, independent paid level snapshots: **1,000 → 1,050 → 1,102.5 HP** (two-level towers stop at 1,050). **Every purchased upgrade adds 5% of the previous tier's Max HP**, compounded, not 5 percentage points of the original at every tier; the values are authored as complete absolute tier stats, not runtime additive bonuses.

**Other buildings:** Ammunition Factory and Depot **1,000 HP** each (one build tier/no paid upgrades yet); Resource Tower and Adaptable Resource Generator **1,500 → 1,575 → 1,653.75 HP** (new tier progression replaces their former 150/200/250 HP schedule); Stealth Beacon **1,800 HP** (one tier). Generic passive config fallback is 1,000 HP. These are **hull**, not shields. Enemy damage, friendly firing stats, costs, unlocks, construction timing and Citadel HP are unchanged.

**Pipeline and HP semantics:** Stage02 still selects one authored tier, then Stage03 applies earned **rank HP bonuses independently**. The existing `TowerBase.Durability` synchronizes max capacity while **preserving missing absolute HP**; upgrades do not fully heal already-damaged towers. The current Inspector's `Max HP` comparison automatically shows the extra `(+50)` at level2 and `(+52.5)` at level3 for a standard-rank 1,000-HP offensive tower. No duplicate HP system, per-frame calculations, or tower-specific damage handlers were added.

**Verification still needed:** Compile Godot .NET, run `TESTS/Towers/TowerHealthChecks.tscn` and the existing integration scene that executes `TowerStatChecks`/`TowerUpgradeChecks`, plus `TESTS/Economy/CreditTowerChecks.tscn`. Inspect upgraded/damaged towers at runtime, including the resource producer `.tres` serialized tiers. Written test assertions were updated, but **not executed here**.

---

# Economy tests

Wallet transactions, kill rewards and the existing ammunition foundation helpers run in the integration suite. `AmmunitionLoopChecks.tscn` is a separate production-world end-to-end runner, with focused `WeaponAmmunitionChecks`, `DeliverySafetyChecks` and shared `AmmoLoopFixture` files rather than one large test script. See [Ammunition.md](Ammunition.md) for current results and coverage; [the test index](../README.md) has other suite commands.


## CP1A Citadel backend — code landed 2026-10-18

`TESTS/Economy/CitadelCreditChecks.tscn` exercises `CreditBuffer`/`CreditProductionTimer`, real wave-state-only Citadel production, pausing, capped storage, real wallet collection/overflow/idempotency and independent two-Citadel buffers. No scene/UI clicks are wired until CP1B.

**Execution is pending:** build the .NET solution, then run `godot --headless --path . res://TESTS/Economy/CitadelCreditChecks.tscn`. This change has not been Godot-compiled or headless-tested by the authoring environment. See `ECONOMY/Production/README.md` for the detailed contract and remaining CP1B–CP3 plan.

## CP2 / CP3 economy towers

`CreditTowerChecks.tscn` passes paid construction/upgrade, private tier data, active-wave timers, attributed-kill range/overlap/lifecycle, exact XP/normal rewards and Inspector collection/overflow. Existing CP1 buffer/collection suites also pass. See CreditTowers.md for full coverage and unrelated baseline fixture failures.
