# Resource Storage — visual building implemented (SALV1 logistics pending)

**Current source:** `ResourceStorage.tscn`, `ResourceStorage.cs`, `ResourceStorageConfig.cs`, `DefaultResourceStorageConfig.tres` and `ResourceStorageDefinition.tres`. Registered in the Economy build category with the stable ID `resource_storage`.

The finished `Artwork/resource_storage.svg` is a distinct **128×128 top-down 2×2 design**: an armoured material vault containing two visibly stacked banks of silver Refined Metal ingots, heavy locking rails, a cargo hatch, small cyan indicators and steel/bronze details. Its art is intentionally different from the ammunition depot.

**Provisional values:** 2×2 footprint, 200 Credits, 4s construction, 1,000 hull HP. Adjust Credits/time/HP in `DefaultResourceStorageConfig.tres`; the 2×2 footprint is on its `TowerDefinition`. Placement, shared construction drones/holograms, health, selection and ordinary Save/Continue reuse existing TowerBase contracts.

**Not implemented in this artwork/building-shell pass:** physical Metal stock, production delivery, availability/reservations, material collection drones or two-drone material construction. The Inspector says SALV1 storage is pending; it does not fake stock or convert Metal into ammunition. The planned capacity of 100 Metal is a **future configuration**, not a live inventory.

**Future integration:** [SALV1 full implementation plan](../ScrapRefinery/SALV1_IMPLEMENTATION_PLAN.md). Implement the separate metal inventory and real shipment accounting on this existing building; do not attach it to Ammunition Depot or ammunition logistics.
