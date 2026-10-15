# Terrain artwork

Metal/prototype_atlas.svg remains the original shared64px atlas for floors, walls, rock, decals and raised Foundation. Interactive/Foundation deliberately references that same(1,0) art through the new Interactive palette, preserving visuals and one source.

Interactive/Lava/lava.svg is a separate64×64 vivid molten-orange pool tile with sparse tiny flecks and subtle highlights. Matching orange edges support adjoining pools; no dominant dark rock/crust or per-cell effects were added. Gameplay uses authored metadata/config rather than texture colors. See ../Interactive/README.md and TileSets/README.md for authoring/rules.

Dirt and Decorative folders retain their existing organization/future art notes. This pass does not redesign other terrain textures.
