# TileSets

Reserved for reusable Godot 4 `TileSet` resources and atlas definitions. Codex should create the first 64 × 64-pixel tile atlas and assign it to level `TileMapLayer` nodes.

Define TileSet custom-data Boolean layers **`can_build`** and **`blocks_enemies`**. Paint different metal/dirt/decor variants with intentional rules; avoid deriving logic from visual texture ID alone.

Use source artwork in `../Tiles/`. No TileSet assets are created in this documentation-only pass: the starter scenes cannot paint tiles until a resource is assigned.
