# Scrap Refinery — visual building implemented (SALV1 logistics pending)

**Current source:** `ScrapRefinery.tscn`, `ScrapRefinery.cs`, `ScrapRefineryConfig.cs`, `DefaultScrapRefineryConfig.tres` and `ScrapRefineryDefinition.tres`. Registered in the Economy category with the stable ID `scrap_refinery`.

The finished `Artwork/refinery.svg` is a **128×128 top-down 2×2 design**: a slightly cylindrical rust-brown smelting drum, curved elliptical hatch, dark industrial footing, pipework, riveted bands and warm molten inspection window. This is a real scene and icon, not just concept art. Footprint, placement, ordinary paid Credits construction, shared construction drone/hologram, durability, pause and normal tower saving follow existing TowerBase mechanisms.

**Provisional values:** 2×2 footprint, 350 Credits, 5s construction, 1,000 hull HP. Adjust Credits/time/HP in `DefaultScrapRefineryConfig.tres`; the 2×2 footprint is authored on its `TowerDefinition`.

**Not implemented in this artwork/building-shell pass:** Scrap intake, physical input/output buffers, refining clock, metal couriers, metal economy, SALV1 saving or dual-drone material construction. The selected-building Inspector explicitly identifies these as **pending**. No credits/resources appear from this building.

**Future integration:** follow the [SALV1 full implementation plan](SALV1_IMPLEMENTATION_PLAN.md), starting from this existing scene/config/artwork instead of recreating the building. Its future output must remain local physical Refined Metal, transferred to [Resource Storage](../ResourceStorage/README.md), never an automatic wallet grant.
