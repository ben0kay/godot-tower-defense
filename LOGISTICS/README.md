# LOGISTICS — Shared transport and supply (FUTURE / NOT IMPLEMENTED)

Future top-level manager(s) for **moving physical supplies between buildings**, starting with [Ammunition/](Ammunition/README.md). This is not a replacement for the existing ECONOMY wallet or for a tower's own ammunition/magazine state.

Clear ownership:
- ECONOMY defines resource identities, balances and payments.
- TOWERS/Economy/AmmunitionFactory makes batches; AmmunitionDepot stores batches and owns drone capacity.
- TOWERS/Shared/Ammunition tracks reserve and magazines already delivered to an individual tower.
- LOGISTICS/Ammunition coordinates transfers, resupply requests, drone dispatch and flight lifecycle.

Aim for one shared active-job dispatcher and budgeted events, not every tower scanning every depot per physics frame. Keep space-flight visuals independent from inventory conservation. Future Idle/Empire use is possible, but do not prematurely generalize into a universal cross-genre logistics framework.

**Reserved README-only folder; no logistics code exists.**
