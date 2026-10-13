# The Citadel — Defended Destination

`Citadel.tscn` is a reusable visible placeholder, instanced in Level001 and the empty template. `WorldObjectFootprint` reserves and blocks a 3×3 grid footprint. Root position is the upper-left cell center; the visual core is offset (64,64) to the footprint center. Snap the root to 32 + 64*n on each axis.

Four editable `ApproachPoints` Marker2Ds sit one cell outside the footprint on each side. Keep them passable and reachable when painting terrain. The level's Settings/CitadelPath identifies the objective relative to the level root. Future AI should navigate toward reachable approach points, not the solid footprint.

Placeholder polygons/label are replaceable. Health, damage, defeat signals, enemy AI and combat remain unimplemented. Spawning belongs in ENEMIES.
