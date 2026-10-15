# Shared ammunition logistics (FUTURE / NOT IMPLEMENTED)

This category owns **what inventory must be transferred and when**, not the design or flight physics of each drone.

- [Ammunition](Logistics/README.md): Stage A automatic **factory small buffer → depot large storage** batch transfers (no drones required). Stage B depot stock → tower local reserves by drone once that feature is introduced.
- [DRONES](../../../../DRONES/README.md): separate reusable mobile-unit umbrella. Its [Transport](../../../../DRONES/Transport/README.md) behavior handles flight and returns; future [Combat](../../../../DRONES/Combat/README.md) drones may be launched by player abilities.
- ECONOMY continues to own resource IDs/credits/payments; AmmunitionFactory/Depot each own their local stocks; tower ammunition state stays inside TOWERS/Shared/Ammunition.

Keep a single compact dispatch/transfer authority where needed, with scheduled/event-driven work rather than every building scanning all others per frame. No generalized logistics engine and no change to current gameplay until requested.

**Documentation only; not implemented.**
