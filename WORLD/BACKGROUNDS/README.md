# BACKGROUNDS — Cosmetic Space Scenery

Purely visual **non-tilemap** art intended to appear behind a level's physical terrain. Keep the reusable assets/scene components here; place instances in each level's `SpaceBackground` section.

- `Nebulae/`: large, positionable world-anchored `Sprite2D` images, optionally animated by shaders later.
- `Stars/`: starfield layers, optionally `Parallax2D` for depth.
- `Planets/`: distant planets, moons, silhouettes, other large decorative objects.

A nebula is **not a wall tile, an occupancy cell or a gameplay obstacle**. Leaving terrain cells empty allows it to show through. No collision, buildability, pathfinding or save-state requirements.

The live Level001 includes a world-anchored SVG placeholder starfield/nebula under SpaceBackground/Stars. WorldGrid now draws transparent lines/diagnostics rather than an opaque fill. Add or replace sprites without changing gameplay rules. Keep SpaceBackground at z=-20, behind terrain.
