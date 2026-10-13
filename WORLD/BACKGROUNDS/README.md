# BACKGROUNDS — Cosmetic Space Scenery

Purely visual **non-tilemap** art intended to appear behind a level's physical terrain. Keep the reusable assets/scene components here; place instances in each level's `SpaceBackground` section.

- `Nebulae/`: large, positionable world-anchored `Sprite2D` images, optionally animated by shaders later.
- `Stars/`: starfield layers, optionally `Parallax2D` for depth.
- `Planets/`: distant planets, moons, silhouettes, other large decorative objects.

A nebula is **not a wall tile, an occupancy cell or a gameplay obstacle**. Leaving terrain cells empty allows it to show through. No collision, buildability, pathfinding or save-state requirements.

The current `WorldGrid._Draw()` paints an opaque `MapColor` rectangle. When Codex integrates backgrounds, change the draw/ordering appropriately so the nebula is visible. Preserve camera/grid overlay and do not blend collision properties into cosmetics.

This pass provides folder structure and documentation only, no background artwork/shaders or live scene integration.
