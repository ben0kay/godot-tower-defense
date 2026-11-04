# Ammunition Factory — initial construction artwork

This building keeps its **single stationary root `Sprite`**, its **`Vector2(3, 2)` sprite scale**, and its existing **3×2 gameplay footprint**. The shared event-driven `TowerConstructionVisuals` presenter targets `../Sprite` directly; no turret, separate base/gun, production animation or new update loop is added.

| Build work milestone | Texture | Look |
| --- | --- | --- |
| 0% | `stage0_foundation.svg` | Industrial ground slab, empty machinery mount |
| 35% | `stage1_frame.svg` | Open conveyor and factory silhouette framework |
| 70% | `stage2_assembly.svg` | Partially finished factory and incomplete power fixtures |
| 100% | `../factory.svg` | **Unchanged** original orange/navy completed Level 1 factory |

The shared `TowerConstructionArt` resource also includes **future-only** cosmetic Level 2/3 entries, but **the Ammunition Factory currently has no paid production upgrades**. These entries do not create upgrade buttons, prices, tiers, or gameplay rules.

The original `DefaultAmmunitionFactoryConfig.tres` declares its new `[ext_resource]` before `[resource]`, and sets `load_steps=3`. This follows the Resource Tower parser fix in commit `3ae0e25`: in files containing `[sub_resource]`, *all* external resources must appear before the first subresource.

The existing 8-second build, 200-Credit construction price, production/buffer/drones and saved state remain intact. Owner in-game appearance verification pending.
