# Procedural mountain visuals — first pass

**Source Implemented / Awaiting Owner Validation — 2026-11-05.**

Paint `Terrain/Impassable`, Mars source **2**, atlas **(2,0)**. A 3-wide × 2-high rectangle is a useful first preview. Launch the battlefield through its Main launcher or Campaign (the raw map scene does not contain world services). No existing map has been repainted. Prototype atlas and previous coordinates remain unchanged.

`mountain_visual` Boolean TileData opts an impassable tile into the renderer; no Mars source IDs are hardcoded in C#. Marker artwork remains as the editor/fallback depiction. World/Mountains reads marked cells on load and explicit `WorldGrid.TerrainRebuilt`, generating two cached triangle surfaces: shadow and rock. Half-cell vertices are shared across adjacent cells, producing connected ridges rather than six repeated sprites. Stable coordinate hashing preserves appearance across reloads without saved random state. No per-cell scene nodes, processing callbacks, timers, navigation requests or occupancy-triggered rebuilds.

Tune `DefaultMountainVisual.tres`: Enabled, Seed, HeightCells, EdgeVariationCells, RockColor and ShadowColor. Restart the level or explicitly call Rebuild after config edits. Disabled rendering leaves marker artwork and gameplay rules intact. The first pass uses the existing square grid and standard aligned impassable layer; arbitrary rotated/scaled tile layers are not supported.

Mountains are nonbuildable and block enemy navigation using existing tile rules; they conduct power like existing rock. Raised facets project upward and render at Z=6, partially covering battlefield objects behind their silhouette. This initial fixed-Z presentation can also cover foreground objects where silhouettes overlap; depth-aware sorting is a later refinement. Shadows are cosmetic, not additional blocked cells. Small irregular edge offsets may extend beyond the painted footprint; gameplay remains tile-based.

**Tower line-of-sight obstruction is not implemented in this visual first pass.** No attacks, enemy targeting or projectile rules change. Future sight integration should use an explicit shared obstruction query, independent of cosmetic mesh triangles.

Validation: source/resource review only; build/import/gameplay/appearance awaiting the owner. Inspect a single tile, a 3×2 block, an L-shaped area and separated groups; verify reload consistency, height/shading, blocked placement/routes and nearby object overlap. No benchmarks or broad test suite required.
