# Shared ammunition buildings and logistics — implemented

`AmmoBuilding.cs` supplies the small passive building contract (local inventory, home drone settings and runtime status); concrete Factory/Depot nodes reuse the existing `AmmoProduction` and `AmmoStorage` helpers.

[Logistics](Logistics/README.md) owns demand, reservations and physical inventory accounting. [Transport drones](../../../../DRONES/Transport/README.md) own direct flight, visual payload indication, arrival/return callbacks and cleanup. Tower-local reserves/consumption remain in [Shared/Ammunition](../../../Shared/Ammunition/README.md). The ordinary ECONOMY wallet remains the only construction payment authority.

There is no generalized resource-network/power framework or combat drone implementation in this pass.
