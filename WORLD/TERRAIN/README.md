## Mars terrain tiles (2026-10-18)

The new `Tiles/Mars/mars_ground_atlas.svg` adds **bright walkable/nonbuildable dust** and **dark impassable/nonbuildable rock** via source2 in the existing Passable/Impassable palettes. Artwork is a separate expandable 64px-cell vector atlas, not a replacement for any metal/old desert tiles. Nothing is painted into levels automatically; see `TileSets/README.md` and `Tiles/Mars/README.md`.

---

# Terrain — category-filtered authoring

Levels expose Terrain/Passable, Impassable, Interactive and Decorations TileMapLayers with matching TileSets under TileSets/. Passable contains metal/dirt floors; Impassable contains structural walls/rocks; Interactive contains raised Foundation and Lava; Decorations contains cosmetic decals. Boolean can_build/blocks_enemies rules are independent of appearance. Empty space is passable/unbuildable; decorations contribute no rules.

WorldGrid caches Passable → Interactive → Impassable, then applies Portal/Citadel reservations and separate runtime tower occupancy. Intentional Interactive-over-floor overlays are supported, including Lava's false construction permission. Structural blockers cannot become walkable through Lava overlays. Raised Foundation preserves its buildable/solid semantics and original artwork; all74 saved platforms moved explicitly to Interactive. See TESTS/Terrain/InteractiveTerrainMigration.md.

One world-scoped InteractiveTileManager handles immediate-entry and periodic Lava damage for only active occupants, with actual swept-segment exposure and no per-tile nodes or full-world enemy scans. Adjacent Lava retains one timer, exit removes it, teleports only enter their destination. Inspector LavaTileConfig owns damage/interval/entry toggle/type. Artwork is vivid orange liquid with tiny sparse highlights. Paint Lava in Terrain/Interactive source1; it has not been arbitrarily added to the playable map. See Interactive/README.md for complete behavior and tests.

Explicit WorldGrid.RebuildTerrain updates rules and reconciles hazards; hazard/build-permission-only changes do not unnecessarily rebuild A*. Blocking/goal changes retain navigation invalidation. Tower placement/removal retains incremental occupancy behavior. WorldTerrain.tres remains a legacy mixed palette; current scenes use the category palettes.
