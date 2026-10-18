# Metal Tiles

`prototype_atlas.svg` is a replaceable 384×64 vector atlas with six 64px cells: unbuildable metal wall, buildable solid platform, open metal floor, rocky obstacle, open dirt and transparent decal. The temporary atlas mixes materials; final artwork can move into separate Metal/Dirt/Decorative sources.

Gameplay metadata belongs to the current category palettes under `WORLD/TERRAIN/TileSets/`, independent of artwork. The raised platform now appears as Foundation in InteractiveTerrain.tres while referencing the same atlas art; WorldTerrain.tres remains a legacy mixed palette. See `LEVELS/Level001/README.md` for the palette. Keep tile regions at 64px when replacing artwork.

## Additive industrial texture atlas (2026-10-18)

`new_metal_terrain_atlas.webp` is a **256×64** atlas of four **64×64** gameplay tiles derived from user-supplied 256px artwork. Tiles are in this order: (0,0) bolted buildable wall, (1,0) cracked unbuildable wall, (2,0) decorative nonbuildable walkable metal, (3,0) cyan buildable walkable foundation. The original `prototype_atlas.svg` and all legacy atlas regions remain untouched. Their rules live in `TileSets/PassableTerrain.tres` and `TileSets/ImpassableTerrain.tres`; see [palette authoring](../../TileSets/README.md).
