# Ammunition Factory — prepared future upgrade artwork

**Important: these are future-ready cosmetics, not implemented paid upgrades.** As of this pass `AmmunitionFactoryConfig` has no authored upgrade-level rows or upgrade economics. Do not claim Levels 2/3 can currently be purchased.

If factory production upgrades are subsequently designed, the already-authored `AmmunitionFactoryConstructionArt.tres` can provide these artwork milestones through the shared single-sprite presenter:

| Tier | 35% temporary artwork | 70% temporary artwork | Completed artwork |
| --- | --- | --- | --- |
| Future Level 2 | `factory_upgrade_l2_35.svg` | `factory_upgrade_l2_70.svg` | `factory_l2.svg` |
| Future Level 3 | `factory_upgrade_l3_35.svg` | `factory_upgrade_l3_70.svg` | `factory_l3.svg` |

The designs build on the existing navy/orange 64×64 centred factory icon: a more fortified manufacturing body with additional hoppers, vents, illuminated assembly lines and larger chimneys at each tier. All use the same `viewBox="-32 -32 64 64"`, scene `Vector2(3,2)` scale and single nonrotating sprite.

Because the factory has **no paid upgrade levels**, these `TowerLevelArtwork` assets are dormant. Enabling upgrades later requires an intentional separate gameplay/config change establishing complete level rows, costs, production output, durability and lifecycle semantics. No values are invented here; the current production timer, logistics buffer, shipment thresholds, drones and save format remain unchanged. Godot visual acceptance pending.
