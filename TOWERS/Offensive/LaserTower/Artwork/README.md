# Laser Tower artwork — silver precision emitter

The 64×64 SVG family was refined on 2026-11-09 toward the owner's silver/yellow top-down reference: a segmented circular steel mounting ring, deep gunmetal recesses, warm amber indicator slots, a **long narrow axial laser barrel**, and a separate golden reactor/focusing gimbal. These remain native vector textures, not baked reference-image imports.

- `laser_tower_base.svg` — Level 1 **stationary ring** on `BaseSprite`.
- `laser_tower_gun.svg` — Level 1 **independently rotating long barrel** on `TurretPivot/Sprite`.
- `laser_tower.svg` — matching Level 1 combined catalog/fallback illustration.
- `Upgrades/laser_base_l2.svg` and `laser_base_l3.svg` add reinforced ring armour; `laser_gun_l2.svg` and `laser_gun_l3.svg` upgrade the focusing/stabilizer components without making the barrel chunky. Matching combined catalog illustrations and 35/70% construction previews share the same new geometry.
- `Construction/stage0_foundation.svg`, `stage1_frame.svg`, `stage2_assembly.svg` show the same ring gaining its axial rails and partially energized emitter.

**Alignment contract:** all textures remain on exactly `64×64` with viewBox `0 0 64 64`, facing up; the tip is `(32,3)`, matching existing `TurretPivot/Muzzle` local `(0,-29)`. No scene/config, beam style, barrel rotation, physics stats or marker changes. Godot imports the existing SVG paths normally. Verify Level 1–3 full builds, rotating emitter, the beam origin, and 0/35/70% construction in the editor.
