# WORK — First complete ammunition pipeline

**Status: IMPLEMENTED AND HEADLESS-VERIFIED (2026-10-16), with known standard-build/full-suite environment/map limitations below.** The original foundation helpers were reused, not replaced: AmmoInventory, AmmoProduction, AmmoStorage and AmmunitionMasterConfig.

## Completed gameplay

- [x] Optional AmmunitionStats in each offensive tower's complete independent C# level rows, through the existing cached eight-stage pipeline. Current state is separate.
- [x] Per-instance reserves; configurable InitialAmmoFraction (full by default); per-launch Battle/Blaster charges, per-pulse Shockwave charge and prepaid time-based Laser use.
- [x] Empty weapons stop unpaid attacks and resume after physical resupply. Damage types, shield penetration, AoE/wavefront delivery and source attribution are preserved.
- [x] Stock persists through rank promotions and purchased levels; capacity growth never refills, shrink explicitly discards excess and records it.
- [x] Buildable/selectable Factory and Depot, Economy catalog entries, vector placeholders, 1×1 navigation-safe placement and existing wallet/payment failure behavior.
- [x] Automatic factory whole-batch production, finite buffer, full/no-depot pause and departure-driven resume using AmmoProduction.Advance.
- [x] Separate depot AmmoStorage inventory; more than one producer/store, deterministic nearest eligible selection.
- [x] Visible factory-owned Factory→Depot drones **and** depot-owned Depot→Tower drones, carrying finite real cargo and returning home. No instant shipments.
- [x] Incoming space reservations, finite home slots, single outstanding consumer shipment, scarce-stock retries, partial/surplus delivery and interruption cleanup.
- [x] Selected-tower/building runtime Inspector status and authoritative next-level ammo previews, using coalesced events rather than HUD polling/pipeline work per shot.
- [x] Existing pure helper tests executed first; focused weapon, delivery safety, conservation, purchase and real-physics automatic-loop tests added under TESTS/Economy.
- [x] Relevant navigation/menu/placement regressions and remaining diagnostic integration checks executed; actual outcomes documented.
- [ ] Clean standard SDK/editor build and unmodified full integration pass: blocked by existing container SDK/MSBuild/NuGet dependencies and Level002 tile-count assertion respectively. These are not claimed passing.
- [ ] Manual desktop graphical playtest: headless scenes/motion/transactions are verified, actual desktop rendering has not been manually inspected.

## Editable provisional defaults

| Item | Values |
| --- | --- |
| Battle, all current levels | 1000 reserve; 1 per successful projectile launch |
| Laser, all current levels | 1000 reserve; 2 rounds per firing second, retaining prepaid fractions |
| Blaster, all current levels | 600 reserve; 3 per projectile launch |
| Shockwave, all current levels | 400 reserve; 5 per emitted pulse |
| Global threshold | Existing DefaultAmmunitionConfig.tres:10% |
| Factory | 200 Credits;100 ammo/2s;**1000 buffer; 1000 ShipmentAmount (full load to dispatch);**1 drone;1000 payload;220px/s |
| Depot | 150 Credits;**10000 storage**;2 drones;250 payload;220px/s |
| Dispatcher | 0.2s scheduling interval;8 alternating endpoint checks |

Battle1000/1 and the10% rule follow the requested example. Other defaults are provisional tuning, not permanent balance. Production/storage settings initialize when placed; transport settings affect subsequent dispatch. Offensive level values remain in each *Levels.cs file only. Building noncombat settings live in their Inspector config resources.

## Factory dispatch tuning (2026-10-16)

**New defaults implemented in code, regression tests updated; current adjustment has not been re-run against Godot yet.** The factory produces 100 rounds every two seconds but waits until its configurable `ShipmentAmount` reaches **1,000** before launching. Every flight is an exact 1,000-round load from its finite 1,000-round buffer. If no depot can reserve 1,000 free slots, the factory holds the stock and pauses, rather than sending an early/partial drone. Depot storage is now 10,000. Depot-to-tower resupply remains as before (depot drone payload 250; global 10% demand). Edit `AmmunitionFactoryConfig.cs` / Godot Factory config and `AmmunitionDepotConfig.cs` / Depot config; no hardcoded scheduler threshold is necessary. See [tests](../TESTS/Economy/Ammunition.md).

## Ownership and safety

TowerAmmunition wraps the existing stock helper; factories/depot nodes wrap the existing production/storage helpers. TOWERS/Economy/Ammunition/Shared/Logistics owns requests, stock and incoming-capacity reservations. DRONES/Transport owns reusable direct flight/rendering/callbacks, not damage or ammo minting. One logistics node lives in each World, not an Autoload. No enemy queries or per-frame building cross-product searches occur.

Load source→cargo at departure; unload cargo→destination on arrival. Home slots remain occupied until physical return. Missing destination/capacity reduction returns surplus. Missing source/drone loses remaining cargo once. A return to an already-full home returns what fits and explicitly counts remaining loss. Removed endpoint stock also counts as loss. World unload clears jobs and subscriptions. Conservation includes initial stock, factory production, all local stock/cargo, paid consumption and explicit loss/discard.

## Verification and testing

Run `res://TESTS/Economy/AmmunitionLoopChecks.tscn` after building. It first runs the original AmmoFoundationChecks, then small focused WeaponAmmunitionChecks and DeliverySafetyChecks using AmmoLoopFixture. It buys Factory/Depot/Battle through actual buttons/payment, fires900 Battle projectiles to the threshold, verifies both real drone legs and arrival-only stock changes, and verifies automatic real-physics callbacks/pause with no manual drone ticking. TESTS/Economy/Ammunition.md records exact results and outstanding environment/map limitations.

For a quick desktop test, place Factory and Depot near a Battle Tower. Default starting stock is1000; reduce that config's InitialAmmoFraction before placement if waiting for900 shots is inconvenient. Select each building/tower to inspect stock, slots and demand; amber cargo indicates loaded flight. No debug/manual refill is needed for production deliveries.

## Still deferred

No permanent research, saving, magazines/reload timers, power grids/consumption, raw material inputs, construction timers, per-tower threshold sliders, differentiated ammo types, selling, tower health or combat drones were implemented. Future work should build on this working loop rather than recreate inventory, production, storage, threshold or reservations.
