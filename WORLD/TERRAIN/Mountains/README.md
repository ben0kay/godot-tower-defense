# Procedural mountains and tower sight

**Source Implemented / Awaiting Owner Validation — 2026-11-05.**

Paint `Terrain/Impassable`, Mars source **2**, atlas **(2,0)**. A 3-wide × 2-high rectangle is a useful first preview. Launch the battlefield through its Main launcher or Campaign (the raw map scene does not contain world services). No existing map has been repainted. Prototype atlas and previous coordinates remain unchanged.

`mountain_visual` Boolean TileData opts an impassable tile into the renderer; no Mars source IDs are hardcoded in C#. Marker artwork remains as the editor/fallback depiction. World/Mountains reads marked cells on load and explicit `WorldGrid.TerrainRebuilt`, generating two cached triangle surfaces: shadow and rock. Half-cell vertices are shared across adjacent cells, producing connected ridges rather than six repeated sprites. Stable coordinate hashing preserves appearance across reloads without saved random state. No per-cell scene nodes, processing callbacks, timers, navigation requests or occupancy-triggered rebuilds.

Tune `DefaultMountainVisual.tres`: Enabled, Seed, HeightCells, EdgeVariationCells, RockDetailStrength, RockColor and ShadowColor. Restart the level or explicitly call Rebuild after config edits. Disabled rendering leaves marker artwork and gameplay rules intact. The first pass uses the existing square grid and standard aligned impassable layer; arbitrary rotated/scaled tile layers are not supported.

Mountains are nonbuildable and block enemy navigation using existing tile rules; they conduct power like existing rock. Raised facets project upward and render at Z=6, partially covering battlefield objects behind their silhouette. This initial fixed-Z presentation can also cover foreground objects where silhouettes overlap; depth-aware sorting is a later refinement. Shadows are cosmetic, not additional blocked cells. Small irregular edge offsets may extend beyond the painted footprint; gameplay remains tile-based.

## Sight and visual refinement — 2026-11-05

Peaks are about 11% taller (HeightCells 1.1 → 1.22). Static world-anchored mineral grain and subtle weathering bands modulate the existing facet colors; tune RockDetailStrength (default 0.14, 0 disables detail). UVs and surfaces build only with terrain, and the shader has no TIME/animation. No generated bitmap texture, additional node, processing loop or extra mesh surface.

Mountain TileData now explicitly sets **blocks_tower_sight=true**, independently of mountain_visual, blocks_enemies and conducts_power. WorldGrid caches this optional Boolean through the existing layer precedence; palettes without it do not block sight. Rendering can be disabled without removing gameplay obstruction. Old rock, walls, foundations and towers retain previous sight behavior unless deliberately opted in later.

`WorldGridSight.cs` owns allocation-free supercover segment traversal, checking touched cells including exact corners/axis-aligned boundary rays. `EnemyRegistry` caches its world Grid once. Shared TowerTargeting applies sight to potential winning candidates and checks cached locks every normal weapon tick. Battle, Laser, Blaster, Rocket, Sniper, Machine Gun, Cryo and Stasis therefore cannot acquire/fire at enemies across a mountain; moving behind one stops sustained Laser damage/beam on the next tick. Visible candidates remain eligible under existing priorities and shield/stealth policies. Sight runs from tower root to enemy center over the painted footprint, not cosmetic peak/shadow geometry. Maps with zero blockers take a constant-time fast path before coordinate transforms.

This is **target visibility**, not projectile/physics collision, fog of war or a height simulation. Already-emitted shots, splash damage, untargeted Shockwave, repair/support targeting, beacon reveal and enemy guns retain their existing rules. No automatic artillery exception or new upgrade is introduced. Terrain changes rebuild the mask; occupancy does not. Existing acquisition cadence resumes targeting after a sight route opens. Saves rebuild derived sight from the map rather than saving masks.

Validation: source/resource review only; build/import/gameplay/appearance awaiting the owner. Inspect a single tile, a 3×2 block, an L-shaped area and separated groups; verify reload consistency, height/shading, blocked placement/routes and nearby object overlap. No benchmarks or broad test suite required.


Optional focused fixture: `TESTS/Terrain/MountainSightChecks.tscn` (authored, not run here). Inspect a Battle/Laser on one side, enemies behind the mountain and an exposed edge; ensure hidden enemies are skipped and moving into cover stops new shots/beam. Godot shader appearance remains for owner review.
