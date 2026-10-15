# Metal Tiles

`prototype_atlas.svg` is a replaceable 384×64 vector atlas with six 64px cells: unbuildable metal wall, buildable solid platform, open metal floor, rocky obstacle, open dirt and transparent decal. The temporary atlas mixes materials; final artwork can move into separate Metal/Dirt/Decorative sources.

Gameplay metadata belongs to the current category palettes under `WORLD/TERRAIN/TileSets/`, independent of artwork. The raised platform now appears as Foundation in InteractiveTerrain.tres while referencing the same atlas art; WorldTerrain.tres remains a legacy mixed palette. See `LEVELS/Level001/README.md` for the palette. Keep tile regions at 64px when replacing artwork.
