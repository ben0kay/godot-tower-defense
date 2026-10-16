# DRONES — Reusable mobile units

[Transport](Transport/README.md) is implemented: visible direct flights carry actual ammunition **Factory→Depot and Depot→Tower**, then return to their home building. Shared flight/lifecycle and small vector visuals remain independent of ammo inventories, transactions and reservations, which belong to [ammunition logistics](../TOWERS/Economy/Ammunition/Shared/Logistics/README.md).

Transport drones are not placeable towers and do not inherit mandatory targeting/combat behavior. Home buildings provide editable speed, payload and finite drone slots. Only active vehicles have processing nodes. Pause/unload follows the world; no saving is implemented.

[Combat](Combat/README.md) remains a future player-ability concept: possible corridor-following attack drones must integrate COMBAT attribution/spatial queries later. This pass does not add weapons, health or an unused shared drone framework.
