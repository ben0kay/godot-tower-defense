# Gunner Drone artwork

**Starter gunner redesign — 2026-11-04.** The compact right-facing hull is now **52×38** (formerly 64×48), with shorter swept stabilizers and a slimmer needle-shaped central body. The independent up-facing minigun is now **20×40** (formerly 24×40), scaled **20% narrower only**; its length and muzzle tip registration stay unchanged. The gun still points toward the muzzle at local `(0,-18)`, with the hull socket at `(-2,0)`.

The native SVG width/height now describe the smaller real ship in F2 preview and drive the shared thruster's automatic size-responsive appearance. The two original artwork filenames, scene node hierarchy, `uid://` import IDs and gun/turret marker positions are retained. No scale transforms are added to the scene, and all minigun projectile textures, flash/tracer geometry, damage/cadence, targeting and bullet physics remain unchanged.

In `GunnerDrone.tres`, health drops **180 → 135 HP** (25%) and navigation radius **22 → 18 px** to suit the smaller footprint. No changes to projectile visuals or movement speed. Source-only checks; visual Godot review pending.
