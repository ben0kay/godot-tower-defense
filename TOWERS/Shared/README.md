# Shared tower foundation

Phase 4A adds TowerConfig (canonical identity, construction recipe, optional tiers), TowerDefinition (stable ID, category, scene, config reference, icon and 1×1 footprint) and TowerCatalog (validated ID lookup). Name/description/prices are read from Config rather than duplicated in definition or HUD. The default catalog registers Battle and Laser towers. Unavailable types remain registered but cannot be built. Rebuild validates blank/duplicate IDs, costs, category, footprint, scene root, config compatibility and type-specific scene prerequisites; call Rebuild after editing registration data.

TowerBase owns type/definition, fixed grid anchor, footprint, placement/selection hooks, optional range capability, purchased UpgradeLevel, FinalStats and StatsChanged. Types supply config compatibility/application and resolution; it has no mandatory weapon nodes or processing. TowerConfig explicitly copies external tier/cost resources per instance. Passive types can have no combat snapshot, range or upgrades.

Targeting/, Weapons/TurretAiming and Projectiles/ are optional existing weapon helpers. Battle and Laser retain their distinct projectile/continuous-beam attack loops while reusing shared targeting and aiming. Stats/ preserves the eight-stage resolver; purchased upgrades are active and ranks/research/conditions/effects remain neutral. Upgrades/ uses the sole ECONOMY wallet.


Phase 4C adds COMBAT/Damage typed response authority and optional Beams/ presentation. Laser Tower is the second actual weapon family, reusing targeting/aiming and the shared progression pipeline. Battle delivery remains guided projectiles; Laser delivery remains sustained DPS. No mandatory combat was added to TowerBase.
