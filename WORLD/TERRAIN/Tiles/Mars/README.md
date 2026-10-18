# Mars ground tiles

`mars_ground_atlas.svg` is an independent, editable **128×64 SVG atlas** with two 64×64 top-down terrain cells, designed to complement the dark-blue metal terrain and stay readable beneath dark enemy ships.

| Atlas coordinate | Look | Godot palette | can_build | blocks_enemies |
| --- | --- | --- | --- | --- |
| (0,0) | Bright orange/tan Martian dust, shallow wind-shaped sediment, sparse flecks | PassableTerrain source **2** | false | false |
| (1,0) | Dark burnt-orange rocky mountain ground, overlapping angular slabs and crevices | ImpassableTerrain source **2** | false | true |

Paint **dust on Terrain/Passable**, not on the cosmetic-only Decorations layer, because it must contribute unbuildable/walkable terrain metadata. Paint **rock on Terrain/Impassable** so it blocks A* navigation and building placement.

## Adding variations later

Expand the vector atlas in whole 64px cells (e.g. width192 for a third variant) and add a new `N:0/0 = 0` entry under the corresponding `TileSetAtlasSource` in the category palette, with gameplay metadata on that tile. If you need many variants, add another row of 64px cells (adjust SVG height) or a dedicated source for each Mars material family. Retain old source IDs and coordinates; **do not renumber 0/1/2** or change previously painted tiles. Every variation can remain independently playable/buildable based on its TileData, without any WorldGrid or pathfinding changes.

These are two initial hand-authored tiles, not automatic terrain transitions or procedural tile picking. Future auto-tiling, transitions, noise variants, and biomes may be added separately; editing the SVG alone does not add new palette cells.
